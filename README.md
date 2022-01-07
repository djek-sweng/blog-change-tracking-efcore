### Change tracking with Entity Framework Core - automatically time-stamp your database records
If data is stored in a database, then there is often the requirement to provide the respective data records with timestamps. The times at which a data record was created and changed are often of interest. It is convenient for you as a developer, if this work is done automatically.

This blog post shows you how [Entity Framework Core](https://docs.microsoft.com/en-us/ef/) automatically fulfills this requirement with the help of the change tracker and a few simple extensions. Microsoft's Entity Framework Core (hereinafter referred to as EF Core) is currently the most popular [ORM](https://en.wikipedia.org/wiki/Object%E2%80%93relational_mapping) framework for .NET.

#### **Advantages**
You have the following advantages:
* Your data records are monitored for changes and automatically time-stamped.
* Your business code is simplified, because it is free of any logic for setting timestamps.
* You use a uniform and universally applicable procedure for setting timestamps.
* Your testing effort is reduced, because only the responsible EF Core extension has to be tested once.

#### **Reference implementation**
The relational database system [MySQL](https://www.mysql.com/) offers the following solution for setting automatic timestamps, when creating and changing a data record. See the attributes `created_at` and `changed_at` using `current_timestamp`.

```sql
-- create and use database
drop database if exists db_efcore_reference;
create database db_efcore_reference
    character set utf8mb4
    collate utf8mb4_unicode_ci;

use db_efcore_reference;

-- create table
drop table if exists notes;
create table notes
(
    id            int unsigned not null auto_increment,
    message       varchar(256) not null,
    created_at    timestamp default current_timestamp,
    changed_at    timestamp null on update current_timestamp,

    constraint pk_notes primary key (id)
) engine = innodb;

-- insert table data
insert into notes (message) values ('Note A');
insert into notes (message) values ('Note B');

-- update table data
update notes set message = 'Note A - modified' where id = 1;
```

The MySQL script shown here is intended to serve as a reference implementation for the following extension of your EF Core application.

#### **Getting started**
In the following application example you implement a database for managing notes, see MySQL reference implementation. For this you create a table `Notes`, which saves notes via its property `Message`. Via the properties `CreatedAt` and `ChangedAt`, the table also saves the times when a note was created and changed. These should be set automatically by EF Core. To create the database, you use the [Code-First approach](https://www.entityframeworktutorial.net/code-first/what-is-code-first.aspx) below.

#### **Step one - create ICurrentTimestamps interface**
At the beginning you create the interface `ICurrentTimestamps` with the properties `CreatedAt` and `ChangedAt`. This is how you generate the standard timestamps that EF Core knows and processes after your extension.

```csharp
namespace Core.Database.Abstractions;

public interface ICurrentTimestamps
{
    DateTime CreatedAt { get; set; }
    DateTime? ChangedAt { get; set; }
}
```

The change time `ChangedAt` is `nullable`, because it is unknown (unprocessed) when a data record is created.

#### **Step two - create class Note**
Next you create the class `Note`, which EF Core will use as the entity for the table `Notes`. Implement the interface `ICurrentTimestamps` so that the standard timestamps are available.

```csharp
namespace Core.Models;

public class Note : ICurrentTimestamps
{
    public Guid Id { get; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ChangedAt { get; set; }

    private Note(Guid id, string message)
    {
        Id = id;
        Message = message;
    }

    public static Note Create(string message)
    {
        return new Note(Guid.NewGuid(), message);
    }
}
```

The property `Id` serves as the primary key and is automatically generated at instantiation via the factory method `Create()`, see `auto_increment` in the MySQL reference implementation. The property `Message` must be initialized via the factory method `Create()` and can be set directly later.

#### **Step three - create class DatabaseContext and override method OnModelCreating()**
Now create the class `DatabaseContext` and derive it from the EF Core base class `DbContext`. To be able to generate a database using the Code-First approach, override the method `OnModelCreating()`.

```csharp
namespace Core.Database;

public class DatabaseContext : DbContext
{
#nullable disable
    public DbSet<Note> Notes { get; set; }
#nullable enable

    public DatabaseContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Note>(entity =>
        {
            entity.ToTable("Notes");
            entity.HasKey(note => note.Id);
            entity.Property(note => note.Message).HasMaxLength(256);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

With `DbSet` the entity `Note` is made known to the associated `DbContext`. EF Core can now perform read and write access to the database via the property `Notes`.

With the method call `ToTable()` you create the table `Notes`. The `HasKey()` method defines `Id` as the primary key of the table.
The method `HasMaxLength()` limits the length of the `Message` note to a maximum of 256 characters. All other settings result implicitly from the implementation of the entity `Note`.

#### **Step four - create initial migration (Code-First approach)**
Before you generate the initial migration to create the database, the class `DatabaseContext` must be registered as a service and the MySQL server must be configured as your database system. In the file [`appsettings.json`](./src/CurrentTimestamps/WebApi/appsettings.json) you can store the `ConnectionString` for the connection to the database server.

```csharp
var connectionString = configuration.GetConnectionString("Default");

services.AddDbContext<DatabaseContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);
```

With the help of the shell scripts [`efcore_migration_add.sh`](./src/CurrentTimestamps/WebApi/efcore_migration_add.sh) and [`efcore_migration_remove.sh`](./src/CurrentTimestamps/WebApi/efcore_migration_remove.sh) you can now create and remove the associated migrations.

#### **Step five - extend class DatabaseContext and override method SaveChangesAsync()**
Now implement the core function of the EF Core extension. In the previously created class `DatabaseContext`, override the method `SaveChangesAsync()`, which saves all changes made in the `DbContext` into the underlying database.

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var dateTime = DateTime.UtcNow;

    var entriesAdded = ChangeTracker.Entries()
        .Where(entry => entry.State == EntityState.Added)
        .ToList();

    entriesAdded.ForEach(entry =>
        entry.Property(nameof(ICurrentTimestamps.CreatedAt)).CurrentValue = dateTime);

    var entriesModified = ChangeTracker.Entries()
        .Where(entry => entry.State == EntityState.Modified)
        .ToList();

    entriesModified.ForEach(entry =>
        entry.Property(nameof(ICurrentTimestamps.ChangedAt)).CurrentValue = dateTime);

    return base.SaveChangesAsync(cancellationToken);
}
```

Via the LINQ filter `Where()` on `ChangeTracker.Entries()` you can determine all newly added and changed data records in the associated `DbContext`. With the `ForEach()` calls you then set the standard timestamps, which you can integrate into any entities of `DatabaseContext` via the `ICurrentTimestamps` interface. Finally you call the method `SaveChangesAsync()` of the base class `DbContext` and save the changes into the database.

#### **Step six - insert and modify data records**
The following method `InsertNoteAsync()` shows you how to insert a new data record `note` in the `DatabaseContext`. When `SaveChangesAsync()` is called, the timestamp `CreatedAt` is set automatically.

```csharp
public async Task InsertNoteAsync(
    string message,
    DatabaseContext context,
    CancellationToken cancellationToken = default)
{
    var note = Note.Create(message);

    await context.Notes.AddAsync(note, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
}
```

The following method `UpdateNoteAsync()` shows you how to modify an existing data record. With the call of `SaveChangesAsync()` the timestamp `ChangedAt` is set automatically.

```csharp
public async Task UpdateNoteAsync(
    Guid noteId,
    string message,
    DatabaseContext context,
    CancellationToken cancellationToken = default)
{
    var note = await context.Notes.FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

    if (note is null)
    {
        return;
    }

    note.Message = message;

    await context.SaveChangesAsync(cancellationToken);
}
```

#### **Step seven - create database updater**
Finally you implement the class [`DatabaseUpdater`](./src/CurrentTimestamps/Core/Database/DatabaseUpdater.cs), which inserts and modifies sample data in the `DbContext`. So you can validate the functionality of the EF Core extension in the `SaveChangesAsync()` method. Furthermore, the `DatabaseUpdater` automatically executes the migration to create the database, when the application is started.

#### **Integration test**
You can test the application example discussed using a web-API. If you have installed [Docker](https://www.docker.com/) on your computer, you do not have to install and set up a local MySQL server. Just start the Docker engine and run the shell scripts [`run_mysql_server.sh`](./run_mysql_server.sh) and [`run_efcore_webapi.sh`](./run_efcore_webapi.sh) one after the other. Make sure that the Docker container with the MySQL server is fully started, before you then start the web-API.

The web-API has a [Swagger UI](https://swagger.io/tools/swagger-ui/) which you open in the browser via the URL `https://localhost:5001/swagger/`. The corresponding `curl` commands for testing via a terminal can be found [here](./tools/curl).

The following HTTP POST request inserts a new data record into the database.

```sh
#!/bin/sh

# Request
curl -X 'POST' \
  'https://localhost:5001/api/notes' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "message": "Arthur Dent"
}'

# Response body
# {
#   "id":"86e4871d-c39c-481f-ba55-6cc095e3f5ec",
#   "message":"Arthur Dent",
#   "createdAt":"2022-01-02T16:35:00.143135Z",
#   "changedAt":null
# }
```

The property `CreatedAt` was automatically set, when the new data record was created by the EF Core extension and shows the time of creation. The property `ChangedAt` is `null` (unprocessed), since the data record has only been created and not yet modified.

The following HTTP PUT request modifies the previously created data record in the database.

```sh
#!/bin/sh

# Request
curl -X 'PUT' \
  'https://localhost:5001/api/notes' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "id": "86e4871d-c39c-481f-ba55-6cc095e3f5ec",
  "message": "The answer to the ultimate question of life, the universe, and everything is 42."
}'

# Response body
# {
#   "id":"86e4871d-c39c-481f-ba55-6cc095e3f5ec",
#   "message":"The answer to the ultimate question of life, the universe, and everything is 42.",
#   "createdAt":"2022-01-02T16:35:00.143135Z",
#   "changedAt":"2022-01-03T17:02:25.452145Z",
# }
```

The property `CreatedAt` remains unchanged. The property `ChangedAt` has now been set automatically by the EF Core extension and shows the time of the last modification. If the data record is modified again, `ChangedAt` is also set again.

#### **Conclusion**
The example discussed shows you a possible solution how you can automate the setting of timestamps in your EF Core application. The main effort lies in the implementation of the interface `ICurrentTimestamps` in the desired entities and the overriding of the `SaveChangesAsync()` method from the EF Core base class `DbContext`.

By using the `ChangeTracker` class, other useful EF Core extensions can be implemented, which further automate your application. For example, database entries could be specifically monitored by the `ChangeTracker` and a desired event could be generated, when they are modified.

You can find the complete code in this GitHub repository.

Happy Coding!
