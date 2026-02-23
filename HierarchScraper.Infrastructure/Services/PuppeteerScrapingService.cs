using System.Text.Json;
using System.Threading;
using AngleSharp;
using AngleSharp.Dom;
using HierarchScraper.Core.Configurations;
using HierarchScraper.Core.Interfaces;
using HierarchScraper.Core.Models;
using HierarchScraper.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using HierarchScraper.Core.DTOs;

namespace HierarchScraper.Infrastructure.Services;

public class PuppeteerScrapingService : IScrapingService, IAsyncDisposable
{
    private readonly IVacancyRepository _vacancyRepository;
    private readonly IScrapingSourceRepository _sourceRepository;
    private readonly ILogger<PuppeteerScrapingService> _logger;
    private readonly PuppeteerOptions _puppeteerOptions;
    private readonly SemaphoreSlim _browserSemaphore = new SemaphoreSlim(1, 1);
    private IBrowser? _browser;
    private readonly Random _random = new Random();

    public PuppeteerScrapingService(
        IVacancyRepository vacancyRepository,
        IScrapingSourceRepository sourceRepository,
        ILogger<PuppeteerScrapingService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _vacancyRepository = vacancyRepository;
        _sourceRepository = sourceRepository;
        _logger = logger;
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
            if (config == null) return Enumerable.Empty<Vacancy>();

            var vacancies = new List<Vacancy>();
            var currentUrl = source.Url;
            var processedUrls = new HashSet<string>();

            // Limite de sécurité pour éviter les boucles infinies
            int pageCount = 0;
            while (!string.IsNullOrEmpty(currentUrl) && !processedUrls.Contains(currentUrl) && pageCount < 10)
            {
                processedUrls.Add(currentUrl);
                pageCount++;

                var page = await LoadPageWithPuppeteer(currentUrl, config.ListSelector); 

                var pageHtml = await page.GetContentAsync();                
                if (string.IsNullOrEmpty(pageHtml)) 
                {
                    _logger.LogWarning("Page content is empty for URL: {Url}", currentUrl);
                    break;
                }
                await SaveToFile(pageHtml);

                var context = BrowsingContext.New(AngleSharp.Configuration.Default.WithDefaultLoader());
                var document = await context.OpenAsync(req => req.Content(pageHtml));
                var listElement = document.QuerySelector(config.ListSelector);

                if (listElement == null)
                {
                    _logger.LogWarning("Selector {Selector} not found on page", config.ListSelector);
                    break;
                }

                var items = listElement.QuerySelectorAll(config.ItemConfig.ItemSelector);
                _logger.LogInformation("Found {Count} items on page {Page}", items.Length, pageCount);

                foreach (var item in items)
                {
                    Console.WriteLine(item.InnerHtml);
                    if (ShouldExcludeItem(item, config.ItemConfig.ExclusionRules)) continue;
                    var vacancy = await ExtractVacancyFromItem(item, config.ItemConfig, source);
                    if (vacancy != null)
                    {
                        vacancies.Add(vacancy);
                    }
                }

                currentUrl = await GetNextPageUrlWithPuppeteer(page, config.NextPageSelector);

                if (page != null) await page.CloseAsync();
                page = null;
            }

            foreach (var v in vacancies) await _vacancyRepository.AddAsync(v);
            return vacancies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping source: {SourceName}", source.Name);
            return Enumerable.Empty<Vacancy>();
        }
    }

    private async Task SaveToFile(string someText)
    {
        var dt = DateTime.Now;
        using (var fs = new FileStream($".\\{dt.ToFileTime()}.html", FileMode.CreateNew))
        using (var sw = new StreamWriter(fs))
            sw.WriteLine(someText);
    }

    private async Task EnsureBrowserInitialized()
    {
        if (_browser != null && !_browser.IsClosed) return;

        await _browserSemaphore.WaitAsync();
        try
        {
            if (_browser != null && !_browser.IsClosed) return;

            _logger.LogInformation("Initializing browser...");
            var browserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PuppeteerSharp");
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = browserPath });
            
            var installedBrowsers = browserFetcher.GetInstalledBrowsers();
            if (!installedBrowsers.Any())
            {
                _logger.LogInformation("Downloading browser...");
                await browserFetcher.DownloadAsync();
                installedBrowsers = browserFetcher.GetInstalledBrowsers();
            }

            var executablePath = installedBrowsers.First().GetExecutablePath();

            string userDataDir = _puppeteerOptions.UserDataSavePath;
            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = _puppeteerOptions.Headless,
                ExecutablePath = executablePath,
                UserDataDir = userDataDir,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-blink-features=AutomationControlled", // Cache le mode automation
                    "--window-size=1280,800"
                },
                Timeout = _puppeteerOptions.Timeout
            });

            _logger.LogInformation("Browser initialized!");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        finally
        {
            _browserSemaphore.Release();
        }
    }

    private async Task<IPage?> LoadPageWithPuppeteer(string url, string mainContentSelector)
    {
        try{
            await EnsureBrowserInitialized();
        }
        catch(Exception ex)
        {
            _logger.LogError("Error initializing browser : {0}", ex.Message);
        }
        if (_browser == null) return null;
        IPage? page = null;
        int retryCount = 0;
        const int maxRetries = 3;
        
        while (retryCount < maxRetries)
        {
            try
            {
                page = await _browser.NewPageAsync();
                
                // Simulation humaine
                await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");
                await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 800 });

                // Injection de script pour masquer Puppeteer
                await page.EvaluateFunctionOnNewDocumentAsync("() => { Object.defineProperty(navigator, 'webdriver', { get: () => undefined }); }");

                _logger.LogInformation("Loading URL: {Url} (Attempt {Attempt})", url, retryCount + 1);
                await page.GoToAsync(url, new NavigationOptions {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                    Timeout = _puppeteerOptions.Timeout
                });

                // Délai aléatoire entre 2 et 5 secondes
                await Task.Delay(_random.Next(2000, 5000));

                if (!string.IsNullOrEmpty(mainContentSelector))
                {
                    try 
                    {
                        await page.WaitForSelectorAsync(mainContentSelector, new WaitForSelectorOptions { Timeout = _puppeteerOptions.WaitForSelectorTimeout }); 
                    }
                    catch 
                    {
                        _logger.LogWarning("Timeout waiting for selector: {Sel}", mainContentSelector); 
                    }
                }
                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading page with Puppeteer (Attempt {Attempt})", retryCount + 1);
                if (retryCount < maxRetries - 1)
                {
                    await Task.Delay(_random.Next(2000, 5000));
                }
                if (page != null) await page.CloseAsync();
            }
            
            retryCount++;
        }
        
        _logger.LogError("Failed to load page after {MaxRetries} attempts", maxRetries);
        return null;
    }

    private async Task<string?> GetNextPageUrlWithPuppeteer(IPage page, string nextPageSelector)
    {
        // 1. Validation de base
        if (string.IsNullOrEmpty(nextPageSelector) || page == null) return null;

        try
        {
            // 2. Extraction de l'attribut href via le navigateur
            // On utilise 'property' plutôt que 'attribute' pour avoir l'URL absolue directement via le DOM
            var nextPageUrl = await page.EvaluateFunctionAsync<string>(
                @"(s) => { 
                    const el = document.querySelector(s);
                    return el ? el.href : null; 
                }", 
                nextPageSelector
            );

            if (string.IsNullOrEmpty(nextPageUrl)) return null;

            // 3. Puppeteer renvoie souvent l'URL absolue via .href, 
            // mais au cas où, on sécurise la reconstruction
            if (!nextPageUrl.StartsWith("http"))
            {
                return new Uri(new Uri(page.Url), nextPageUrl).AbsoluteUri;
            }

            return nextPageUrl;
        }
        catch (Exception ex)
        {
            // Optionnel : logger l'erreur ex pour le debug
            return null;
        }
        // Note : On ne ferme PAS la page ici, car le scraper en aura besoin pour la suite.
    }

    private bool ShouldExcludeItem(IElement item, List<ExclusionRule> rules)
    {
        if (rules == null || !rules.Any()) return false;
        return rules.Any(r => (item.QuerySelectorAll(r.Selector).Length > 0 && r.MustExist) || (item.QuerySelectorAll(r.Selector).Length == 0 && !r.MustExist));
    }

    private async Task PopulateVacancyDetailAsync(Vacancy vacancy, DetailConfiguration detailConfig)
    {
        if (vacancy == null || string.IsNullOrEmpty(vacancy.DetailUrl) || detailConfig == null)
            return;

        var page = await LoadPageWithPuppeteer(vacancy.DetailUrl, detailConfig.MainSelector);
        if (page == null) return;

        var html = await page.GetContentAsync();
        if (string.IsNullOrEmpty(html))
        {
            await page.CloseAsync();
            return;
        }

        var context = BrowsingContext.New(AngleSharp.Configuration.Default.WithDefaultLoader());
        var document = await context.OpenAsync(req => req.Content(html));

        VacancyDetailParser.PopulateFromDocument(vacancy, document, detailConfig);

        if (detailConfig?.IsActiveConfig?.BySentences?.Any() ?? false)
        {
            string sentences = string.Join("`,`", detailConfig.IsActiveConfig.BySentences);
            string jsFunction = $@"() => {{const messages = [`{sentences}`];const pageText = document.body.innerText;return messages.some(msg => pageText.includes(msg));}}";
            bool isExpired = await page.EvaluateFunctionAsync<bool>(jsFunction);
            vacancy.IsActive = !isExpired;
        }


        await page.CloseAsync();
    }

    private async Task<Vacancy?> ExtractVacancyFromItem(IElement item, ItemConfiguration config, ScrapingSource source)
    {
        var titleEl = item.QuerySelector(config.TitleSelector);
        if (titleEl == null) return null;

        string jobId = string.Empty;
        string detailUrl = string.Empty;

        // if a link selector is provided, try to read from it first
        if (!string.IsNullOrEmpty(config.DetailSelector))
        {
            var linkEl = item.QuerySelector(config.DetailSelector);
            if (linkEl != null)
            {
                // use href when available (anchor element)
                if (linkEl is AngleSharp.Html.Dom.IHtmlAnchorElement anchor && !string.IsNullOrEmpty(anchor.Href))
                {
                    detailUrl = anchor.Href;
                }
                else if (!string.IsNullOrEmpty(config.JobKeyAttribute))
                {
                    jobId = linkEl.GetAttribute(config.JobKeyAttribute) ?? string.Empty;
                }
            }
        }

        // fallback: search for any element bearing the job key attribute
        if (string.IsNullOrEmpty(jobId) && !string.IsNullOrEmpty(config.JobKeyAttribute))
        {
            var keyEl = item.QuerySelector($"[{config.JobKeyAttribute}]");
            if (keyEl != null)
            {
                jobId = keyEl.GetAttribute(config.JobKeyAttribute) ?? string.Empty;
            }
        }

        // build URL from template if necessary
        if (string.IsNullOrEmpty(detailUrl) && !string.IsNullOrEmpty(config.DetailUrlTemplate) && !string.IsNullOrEmpty(jobId))
        {
            detailUrl = string.Format(config.DetailUrlTemplate, jobId);
        }

        if (string.IsNullOrEmpty(jobId) && string.IsNullOrEmpty(detailUrl))
            return null;

        if (string.IsNullOrEmpty(jobId) && !string.IsNullOrEmpty(detailUrl))
        {
            jobId = detailUrl; // fallback: use URL as identifier
        }

        if (!string.IsNullOrEmpty(detailUrl) && !detailUrl.StartsWith("http"))
        {
            detailUrl = new Uri(new Uri(source.Url), detailUrl).AbsoluteUri;
        }

        var vacancy = new Vacancy
        {
            Name = titleEl.TextContent.Trim(),
            JobId = jobId,
            DetailUrl = detailUrl,
            SourcePlatform = source.Platform,
            ScrapingSourceId = source.Id,
            CreatedDate = DateTime.UtcNow
        };

        if (config.DetailConfig?.FieldSelectors?.Any() == true)
        {
            await PopulateVacancyDetailAsync(vacancy, config.DetailConfig);
        }

        return vacancy;
    }

    public async Task ProcessAllActiveSourcesAsync()
    {
        var sources = await _sourceRepository.GetActiveSourcesAsync();
        foreach (var s in sources) await ScrapeSourceAsync(s);
    }

    /// <inheritdoc />
    public async Task<ScrapingUpdateVacancyDetailResult> UpdateVacancyDetailAsync(int vacancyId)
    {
        var vacancy = await _vacancyRepository.GetByIdAsync(vacancyId);
        if (vacancy == null)
            return new ScrapingUpdateVacancyDetailResult { ErrorMessage = $"Vacancy #{vacancyId} has not been found" };

        if (string.IsNullOrEmpty(vacancy.DetailUrl))
            return new ScrapingUpdateVacancyDetailResult { ErrorMessage = "Vacancy does not have a detail URL" };

        var source = await _sourceRepository.GetByIdAsync(vacancy.ScrapingSourceId);
        if (source == null)
            return new ScrapingUpdateVacancyDetailResult { ErrorMessage = "Associated scraping source not found" };

        DetailConfiguration? detailConfig = null;
        try
        {
            if (!string.IsNullOrEmpty(source.ScrapingConfig))
            {
                var config = System.Text.Json.JsonSerializer.Deserialize<HierarchScraper.Core.Configurations.ScrapingConfiguration>(source.ScrapingConfig);
                detailConfig = config?.ItemConfig?.DetailConfig;
            }
        }
        catch (Exception ex)
        {
            // ignore parsing issues; detailConfig will remain null
            Console.WriteLine("Failed to parse scraping config: " + ex.Message);
        }

        if (detailConfig == null || detailConfig.FieldSelectors == null || !detailConfig.FieldSelectors.Any())
            return new ScrapingUpdateVacancyDetailResult { ErrorMessage = "No detail configuration available for this source" };

        // reuse existing helper which loads the page and runs the parser
        await PopulateVacancyDetailAsync(vacancy, detailConfig);
        await _vacancyRepository.UpdateAsync(vacancy);

        return new ScrapingUpdateVacancyDetailResult { Data = vacancy };
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null) await _browser.CloseAsync();
        _browserSemaphore.Dispose();
    }
}


public class PuppeteerOptions
{
    public bool Headless { get; set; } = true;
    public int Timeout { get; set; } = 30000;
    public int WaitForSelectorTimeout { get; set; } = 10000;
    public string ExecutablePath { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";
    public string UserDataSavePath { get; set; } = string.Empty;
}
