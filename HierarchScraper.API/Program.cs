using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

try
{
    Console.WriteLine("Starting HierarchScraper API...");

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add CORS policy to allow requests from any origin (for development)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
    });

    // Add database context
    builder.Services.AddDbContext<HierarchScraper.Infrastructure.Data.ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add application services - Using Puppeteer implementation
    builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IScrapingService, HierarchScraper.Infrastructure.Services.PuppeteerScrapingService>();

    // Add repository services
    builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IVacancyRepository, HierarchScraper.Infrastructure.Repositories.VacancyRepository>();
    builder.Services.AddScoped<HierarchScraper.Core.Interfaces.IScrapingSourceRepository, HierarchScraper.Infrastructure.Repositories.ScrapingSourceRepository>();

    // Add logging with file and console output
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
    // Add custom file logger
    builder.Logging.AddProvider(new FileLoggerProvider("Logs/log-"));
    
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning); 
    // On ajoute cette ligne pour forcer l'affichage du démarrage :
    builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Information);
    builder.Logging.AddFilter("System", LogLevel.Warning);
    builder.Logging.AddFilter("HierarchScraper", LogLevel.Debug);
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Enable CORS
    app.UseCors("AllowAll");

    app.MapGet("/", () => "Hello World!");

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
    Console.WriteLine("Application terminated unexpectedly: " + ex);
}

// Custom File Logger Provider
public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePrefix;
    private readonly object _lock = new object();
    private int _fileCounter = 0;
    
    public FileLoggerProvider(string filePrefix)
    {
        _filePrefix = filePrefix;
        EnsureLogsDirectoryExists();
    }
    
    private void EnsureLogsDirectoryExists()
    {
        string logsDir = Path.GetDirectoryName(_filePrefix);
        if (!string.IsNullOrEmpty(logsDir) && !Directory.Exists(logsDir))
        {
            Directory.CreateDirectory(logsDir);
        }
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePrefix, _lock, () => Interlocked.Increment(ref _fileCounter));
    }
    
    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePrefix;
    private readonly object _lock;
    private readonly Func<int> _getFileCounter;
    
    public FileLogger(string categoryName, string filePrefix, object lockObj, Func<int> getFileCounter)
    {
        _categoryName = categoryName;
        _filePrefix = filePrefix;
        _lock = lockObj;
        _getFileCounter = getFileCounter;
    }
    
    public IDisposable BeginScope<TState>(TState state) => null;
    
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        string message = formatter(state, exception);
        string logLine = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {logLevel}] {_categoryName}: {message}";
        
        if (exception != null)
        {
            logLine += $"\n{exception}";
        }
        
        lock (_lock)
        {
            string fileName = $"{_filePrefix}{DateTime.UtcNow:yyyyMMdd}.txt";
            File.AppendAllText(fileName, logLine + "\n");
        }
    }
}
