# HierarchScraper - Technical Documentation

## .NET 10 Migration (March 2026)

The solution has been upgraded from .NET 8 to .NET 10 to benefit from the latest performance improvements and language features.

### Key Changes
- **Target Framework:** All projects (`Core`, `Infrastructure`, `API`) now target `net10.0`.
- **NuGet Packages:** Updated all `Microsoft.*` and `EntityFrameworkCore.*` packages to version `10.0.4`.
- **Cross-Platform Path Resolution:** Added a `ResolvePath` helper in `Program.cs` and `PuppeteerScrapingService.cs` that handles:
  - Windows-style environment variables (`%VAR%`).
  - Linux/Unix-style environment variables (`$VAR` or `${VAR}`).
  - Automatic path separator normalization (`/` vs `\`).
- **OpenAPI/Swagger:** Removed deprecated `.WithOpenApi()` calls in favor of default .NET 10 metadata generation.
- **Vulnerability Fix:** Resolved a high-severity vulnerability in `Microsoft.Extensions.Caching.Memory` by upgrading to version `10.0.4`.

### Configuration
The `appsettings.json` now supports environment variables for paths:
- `Logging:FilePath`: Set to `Logs/log-` by default.
- `Puppeteer:UserDataSavePath`: Set to `%TEMP%/HierarchScraper/PuppeteerProfile` for cross-platform compatibility.

### Database
The SQLite database (`HierarchScraper.db`) has been synchronized with the latest EF migrations. To reset or update the database manually:
```bash
dotnet ef database update --project HierarchScraper.Infrastructure --startup-project HierarchScraper.API
```

### Running the Project
The API defaults to port 5000 when run via DLL or `dotnet run`:
```bash
dotnet run --project HierarchScraper.API
```
- **Swagger UI:** http://localhost:5000/swagger
- **Scrape All Endpoint:** POST http://localhost:5000/api/scraping/scrape-all
