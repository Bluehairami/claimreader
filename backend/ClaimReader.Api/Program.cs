using ClaimReader.Api.Data;
using ClaimReader.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<AppDbContext>((sp, o) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    if (config.GetValue<bool>("UseInMemoryDb"))
        o.UseInMemoryDatabase("claims-tests");
    else
        o.UseNpgsql(config.GetConnectionString("Default"));
});
builder.Services.AddDbContext<AppDbContext>((sp, o) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    if (config.GetValue<bool>("UseInMemoryDb"))
        o.UseInMemoryDatabase("claims-tests");
    else
        o.UseNpgsql(config.GetConnectionString("Default"),
            npgsqlOptions => npgsqlOptions.CommandTimeout(180)); // Increase timeout to 180 seconds
});

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.AddHttpClient<IExtractionService, GeminiExtractionService>(c =>
    c.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<FileStorage>();

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
              ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("RunMigrations"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }