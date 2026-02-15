using System.Text.Json;
using HierarchScraper.Core.Configurations;
using HierarchScraper.Core.Interfaces;
using HierarchScraper.Core.Models;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace HierarchScraper.Infrastructure.Services;

/// <summary>
/// Service de scraping utilisant PuppeteerSharp pour gérer à la fois les pages statiques et dynamiques
/// </summary>
public class PuppeteerScrapingService : IScrapingService
{
    private readonly IVacancyRepository _vacancyRepository;
    private readonly IScrapingSourceRepository _sourceRepository;
    private readonly ILogger<PuppeteerScrapingService> _logger;
    private readonly PuppeteerOptions _puppeteerOptions;
    private IBrowser? _browser;

    public PuppeteerScrapingService(
        IVacancyRepository vacancyRepository,
        IScrapingSourceRepository sourceRepository,
        ILogger<PuppeteerScrapingService> logger,
        IConfiguration configuration)
    {
        _vacancyRepository = vacancyRepository;
        _sourceRepository = sourceRepository;
        _logger = logger;

        // Charger la configuration Puppeteer
        _puppeteerOptions = configuration.GetSection("Puppeteer").Get<PuppeteerOptions>() ?? new PuppeteerOptions();
    }

    public async Task<IEnumerable<Vacancy>> ScrapeSourceAsync(ScrapingSource source)
    {
        if (!source.IsActive)
        {
            _logger.LogInformation("Source {SourceName} is not active, skipping", source.Name);
            return Enumerable.Empty<Vacancy>();
        }

        try
        {
            _logger.LogInformation("Starting scraping for source: {SourceName}", source.Name);

            var config = JsonSerializer.Deserialize<ScrapingConfiguration>(source.ScrapingConfig);
            if (config == null)
            {
                _logger.LogError("Invalid scraping configuration for source: {SourceName}", source.Name);
                return Enumerable.Empty<Vacancy>();
            }

            var vacancies = new List<Vacancy>();
            var currentUrl = source.Url;
            var processedUrls = new HashSet<string>();

            // Initialiser le navigateur si ce n'est pas déjà fait
            await EnsureBrowserInitialized();

            while (!string.IsNullOrEmpty(currentUrl) && !processedUrls.Contains(currentUrl))
            {
                processedUrls.Add(currentUrl);
                _logger.LogInformation("Processing page: {Url}", currentUrl);

                // Charger la page avec Puppeteer
                var pageHtml = await LoadPageWithPuppeteer(currentUrl, config.ListSelector);
                if (string.IsNullOrEmpty(pageHtml))
                {
                    _logger.LogWarning("Could not load page content for: {Url}", currentUrl);
                    break;
                }

                // Parser le HTML avec AngleSharp pour l'analyse
                var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
                var document = await context.OpenAsync(req => req.Content(pageHtml));

                // Trouver la liste principale
                var listElement = document.QuerySelector(config.ListSelector);
                if (listElement == null)
                {
                    _logger.LogWarning("List element not found with selector: {Selector}", config.ListSelector);
                    break;
                }

                // Traiter chaque item
                var items = listElement.QuerySelectorAll(config.ItemConfig.ItemSelector);
                _logger.LogInformation("Found {Count} items on page", items.Length);

                foreach (var item in items)
                {
                    if (ShouldExcludeItem(item, config.ItemConfig.ExclusionRules))
                    {
                        continue;
                    }

                    var vacancy = ExtractVacancyFromItem(item, config.ItemConfig, source);
                    if (vacancy != null)
                    {
                        vacancies.Add(vacancy);
                    }
                }

                // Trouver l'URL de la page suivante
                currentUrl = await GetNextPageUrlWithPuppeteer(currentUrl, config.NextPageSelector);
            }

            // Sauvegarder toutes les offres dans la base de données
            foreach (var vacancy in vacancies)
            {
                await _vacancyRepository.AddAsync(vacancy);
            }

            _logger.LogInformation("Completed scraping for source: {SourceName}. Found {Count} vacancies", 
                source.Name, vacancies.Count);
            
            return vacancies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping source: {SourceName}", source.Name);
            return Enumerable.Empty<Vacancy>();
        }
    }

    public async Task ProcessAllActiveSourcesAsync()
    {
        var sources = await _sourceRepository.GetActiveSourcesAsync();
        
        foreach (var source in sources)
        {
            try
            {
                await ScrapeSourceAsync(source);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing source: {SourceName}", source.Name);
            }
        }
    }

    private async Task EnsureBrowserInitialized()
    {
        if (_browser != null) return;

        try
        {
            _logger.LogInformation("Initializing Puppeteer browser...");
            
            // Télécharger Chromium si nécessaire
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = _puppeteerOptions.Headless,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-accelerated-2d-canvas",
                    "--disable-gpu",
                    "--window-size=1920,1080"
                },
                ExecutablePath = _puppeteerOptions.ExecutablePath,
                Timeout = _puppeteerOptions.Timeout
            });

            _logger.LogInformation("Puppeteer browser initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Puppeteer browser");
            throw;
        }
    }

    private async Task<string> LoadPageWithPuppeteer(string url, string mainContentSelector)
    {
        if (_browser == null)
        {
            _logger.LogError("Browser not initialized");
            return string.Empty;
        }

        try
        {
            using var page = await _browser.NewPageAsync();

            // Configurer la page pour éviter la détection
            await page.SetUserAgentAsync(_puppeteerOptions.UserAgent);
            await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Accept-Language"] = "fr-FR,fr;q=0.9,en-US;q=0.8,en;q=0.7"
            });

            // Naviguer vers la page
            _logger.LogDebug("Navigating to: {Url}", url);
            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = _puppeteerOptions.Timeout
            });

            // Attendre que le contenu principal soit chargé
            if (!string.IsNullOrEmpty(mainContentSelector))
            {
                try
                {
                    await page.WaitForSelectorAsync(mainContentSelector, new WaitForSelectorOptions
                    {
                        Timeout = _puppeteerOptions.WaitForSelectorTimeout
                    });
                    _logger.LogDebug("Main content loaded successfully");
                }
                catch (WaitTaskTimeoutException)
                {
                    _logger.LogWarning("Main content selector not found within timeout");
                }
            }

            // Attendre un peu pour permettre le chargement des éléments dynamiques
            await Task.Delay(1000);

            // Récupérer le HTML complet
            return await page.GetContentAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading page with Puppeteer: {Url}", url);
            return string.Empty;
        }
    }

    private async Task<string?> GetNextPageUrlWithPuppeteer(string currentUrl, string nextPageSelector)
    {
        if (string.IsNullOrEmpty(nextPageSelector) || _browser == null)
        {
            return null;
        }

        try
        {
            using var page = await _browser.NewPageAsync();
            
            // Naviguer vers la page actuelle
            await page.GoToAsync(currentUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = _puppeteerOptions.Timeout
            });

            // Attendre que le sélecteur de page suivante soit disponible
            await page.WaitForSelectorAsync(nextPageSelector, new WaitForSelectorOptions
            {
                Timeout = _puppeteerOptions.WaitForSelectorTimeout,
                Visible = true
            });

            // Récupérer l'URL du bouton suivant
            var nextPageUrl = await page.EvaluateFunctionAsync<string>(
                "(selector) => document.querySelector(selector)?.href", 
                nextPageSelector);

            if (string.IsNullOrEmpty(nextPageUrl))
            {
                _logger.LogDebug("Next page button found but href is empty");
                return null;
            }

            // Gérer les URLs relatives
            if (!nextPageUrl.StartsWith("http"))
            {
                var baseUri = new Uri(currentUrl);
                nextPageUrl = new Uri(baseUri, nextPageUrl).AbsoluteUri;
            }

            return nextPageUrl;
        }
        catch (WaitTaskTimeoutException)
        {
            _logger.LogDebug("No next page button found - assuming this is the last page");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next page URL");
            return null;
        }
    }

    private bool ShouldExcludeItem(IElement item, List<ExclusionRule> exclusionRules)
    {
        if (exclusionRules == null || exclusionRules.Count == 0)
        {
            return false;
        }

        foreach (var rule in exclusionRules)
        {
            var elements = item.QuerySelectorAll(rule.Selector);
            bool hasElements = elements.Length > 0;

            // Logique d'exclusion comme spécifié
            if ((hasElements && rule.MustExist) || (!hasElements && !rule.MustExist))
            {
                return true; // Exclure cet item
            }
        }

        return false;
    }

    private Vacancy? ExtractVacancyFromItem(IElement item, ItemConfiguration itemConfig, ScrapingSource source)
    {
        try
        {
            var titleElement = item.QuerySelector(itemConfig.TitleSelector);
            var detailElement = item.QuerySelector(itemConfig.DetailSelector);

            if (titleElement == null || detailElement == null)
            {
                _logger.LogDebug("Item missing required elements (title or detail)");
                return null;
            }

            var title = titleElement.TextContent.Trim();
            var detailUrl = detailElement.GetAttribute("href") ?? string.Empty;

            // Gérer les URLs relatives
            if (!string.IsNullOrEmpty(detailUrl) && !detailUrl.StartsWith("http"))
            {
                var baseUri = new Uri(source.Url);
                detailUrl = new Uri(baseUri, detailUrl).AbsoluteUri;
            }

            return new Vacancy
            {
                Name = title,
                DetailUrl = detailUrl,
                SourcePlatform = source.Platform,
                ScrapingSourceId = source.Id,
                CreatedDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting vacancy from item");
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            try
            {
                await _browser.CloseAsync();
                _logger.LogInformation("Puppeteer browser closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing Puppeteer browser");
            }
        }
    }
}

public class PuppeteerOptions
{
    public bool Headless { get; set; } = true;
    public int Timeout { get; set; } = 30000;
    public int WaitForSelectorTimeout { get; set; } = 10000;
    public string ExecutablePath { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
}