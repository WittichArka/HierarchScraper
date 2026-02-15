using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("Starting HierarchScraper API...");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add database context
builder.Services.AddDbContext<HierarchScraper.Infrastructure.Data.ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add application services - Using Puppeteer implementation
builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IScrapingService, HierarchScraper.Infrastructure.Services.PuppeteerScrapingService>();

// Add repository services (to be implemented)
builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IVacancyRepository, HierarchScraper.Infrastructure.Repositories.VacancyRepository>();
builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IScrapingSourceRepository, HierarchScraper.Infrastructure.Repositories.ScrapingSourceRepository>();

// Add database context (to be implemented)
builder.Services.AddDbContext<HierarchScraper.Infrastructure.Data.ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add logging
builder.Services.AddLogging();

// Add Serilog to the logging pipeline
builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Scraping API endpoints
app.MapGet("/api/scraping/sources", async (HierarchScraper.Core.Interfaces.IScrapingSourceRepository sourceRepo) =>
{
    return await sourceRepo.GetAllAsync();
})
.WithName("GetScrapingSources")
.WithOpenApi();

app.MapPost("/api/scraping/sources", async (HierarchScraper.Core.Models.ScrapingSource source, HierarchScraper.Core.Interfaces.IScrapingSourceRepository sourceRepo) =>
{
    var createdSource = await sourceRepo.AddAsync(source);
    return Results.Created($"/api/scraping/sources/{createdSource.Id}", createdSource);
})
.WithName("CreateScrapingSource")
.WithOpenApi();

app.MapPost("/api/scraping/sources/{id}/scrape", async (int id, HierarchScraper.Core.Interfaces.IScrapingService scrapingService, HierarchScraper.Core.Interfaces.IScrapingSourceRepository sourceRepo) =>
{
    var source = await sourceRepo.GetByIdAsync(id);
    if (source == null)
    {
        return Results.NotFound();
    }
    
    var vacancies = await scrapingService.ScrapeSourceAsync(source);
    return Results.Ok(vacancies);
})
.WithName("ScrapeSource")
.WithOpenApi();

app.MapPost("/api/scraping/scrape-all", async (HierarchScraper.Core.Interfaces.IScrapingService scrapingService) =>
{
    await scrapingService.ProcessAllActiveSourcesAsync();
    return Results.Ok(new { message = "Scraping started for all active sources" });
})
.WithName("ScrapeAllSources")
.WithOpenApi();

// Vacancies API endpoints
app.MapGet("/api/vacancies", async (HierarchScraper.Core.Interfaces.IVacancyRepository vacancyRepo) =>
{
    return await vacancyRepo.GetAllAsync();
})
.WithName("GetAllVacancies")
.WithOpenApi();

app.MapGet("/api/vacancies/{id}", async (int id, HierarchScraper.Core.Interfaces.IVacancyRepository vacancyRepo) =>
{
    var vacancy = await vacancyRepo.GetByIdAsync(id);
    return vacancy == null ? Results.NotFound() : Results.Ok(vacancy);
})
.WithName("GetVacancyById")
.WithOpenApi();

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
