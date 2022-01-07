namespace Core.Database;

public class DatabaseUpdater
{
    private const string _MODIFY_NOTE = "Sample note A";

    private readonly IApplicationBuilder _applicationBuilder;

    private DatabaseUpdater(IApplicationBuilder applicationBuilder)
    {
        _applicationBuilder = applicationBuilder;
    }

    public static DatabaseUpdater Create(IApplicationBuilder applicationBuilder)
    {
        return new DatabaseUpdater(applicationBuilder);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _applicationBuilder.ApplicationServices.CreateScope();

        var context = scope.ServiceProvider.GetService<DatabaseContext>()
                      ?? throw new Exception("Could not get database context.");

        await MigrateDatabaseAsync(context, cancellationToken);

        await InsertSampleDataAsync(context, cancellationToken);
        await UpdateSampleDataAsync(context, cancellationToken);
    }

    private static async Task MigrateDatabaseAsync(DbContext context, CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);

        Console.WriteLine("Migrating database ... done!");
    }

    private static async Task InsertSampleDataAsync(DatabaseContext context, CancellationToken cancellationToken)
    {
        if (context.Notes.Any())
        {
            return;
        }

        var notes = new List<Note>
        {
            Note.Create(_MODIFY_NOTE),
            Note.Create("Sample note B"),
            Note.Create("Sample note C")
        };

        await context.Notes.AddRangeAsync(notes, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        Console.WriteLine("Inserting sample data ... done!");
    }

    private static async Task UpdateSampleDataAsync(DatabaseContext context, CancellationToken cancellationToken)
    {
        var note = await context.Notes.FirstOrDefaultAsync(n => n.Message == _MODIFY_NOTE, cancellationToken);

        if (note is null)
        {
            return;
        }

        note.Message = $"{_MODIFY_NOTE} -- modified";
        await context.SaveChangesAsync(cancellationToken);

        Console.WriteLine("Modifying sample data ... done!");
    }
}
