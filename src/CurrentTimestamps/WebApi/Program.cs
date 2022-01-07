var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;

// Connect to MySQL database.
var connectionString = configuration.GetConnectionString("Default");

services.AddDbContext<DatabaseContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Add services to the container.
services.AddScoped<INoteRepository, NoteRepository>();

// Add controllers.
services.AddControllers();

// Add Swagger/OpenAPI support.
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

// Create application builder and database updater.
var app = builder.Build();
var updater = DatabaseUpdater.Create(app);

// Update database.
await updater.RunAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
