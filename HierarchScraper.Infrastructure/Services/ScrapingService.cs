using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using HierarchScraper.Core.Configurations;
using HierarchScraper.Core.Interfaces;
using HierarchScraper.Core.Models;
using Microsoft.Extensions.Logging;

namespace HierarchScraper.Infrastructure.Services;

public class ScrapingService : IScrapingService
{
    private readonly IVacancyRepository _vacancyRepository;
    private readonly IScrapingSourceRepository _sourceRepository;
    private readonly ILogger<ScrapingService> _logger;
    private readonly IBrowsingContext _browsingContext;

    public ScrapingService(
        IVacancyRepository vacancyRepository,
        IScrapingSourceRepository sourceRepository,
        ILogger<ScrapingService> logger)
    {
        _vacancyRepository = vacancyRepository;
        _sourceRepository = sourceRepository;
        _logger = logger;
        
        // Configure AngleSharp for web scraping
        var config = Configuration.Default.WithDefaultLoader();
        _browsingContext = BrowsingContext.New(config);
    }

    public async Task<IEnumerable<Vacancy>> ScrapeSourceAsync(ScrapingSource source)
    {
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

            while (!string.IsNullOrEmpty(currentUrl) && !processedUrls.Contains(currentUrl))
            {
                processedUrls.Add(currentUrl);
                _logger.LogInformation("Processing page: {Url}", currentUrl);

                var document = await _browsingContext.OpenAsync(currentUrl);
                var listElement = document.QuerySelector(config.ListSelector);
                
                if (listElement == null)
                {
                    _logger.LogWarning("List element not found with selector: {Selector}", config.ListSelector);
                    break;
                }

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

                // Get next page URL
                currentUrl = GetNextPageUrl(document, config.NextPageSelector, source.Url);
            }

            // Save all vacancies to database
            foreach (var vacancy in vacancies)
            {
                await _vacancyRepository.AddAsync(vacancy);
            }

            _logger.LogInformation("Completed scraping for source: {SourceName}. Found {Count} vacancies", source.Name, vacancies.Count);
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
            await ScrapeSourceAsync(source);
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
            
            // If elements found and MustExist is true -> exclude (ad found)
            // If elements found and MustExist is false -> don't exclude (valid item)
            // If no elements found and MustExist is true -> don't exclude (valid item)
            // If no elements found and MustExist is false -> exclude (missing required element)
            
            if ((hasElements && rule.MustExist) || (!hasElements && !rule.MustExist))
            {
                return true; // Exclude this item
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
                return null;
            }

            var title = titleElement.TextContent.Trim();
            var detailUrl = detailElement.GetAttribute("href") ?? string.Empty;
            
            // Handle relative URLs
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

    private string? GetNextPageUrl(IDocument document, string nextPageSelector, string baseUrl)
    {
        if (string.IsNullOrEmpty(nextPageSelector))
        {
            return null;
        }

        var nextPageElement = document.QuerySelector(nextPageSelector);
        if (nextPageElement == null)
        {
            return null;
        }

        var nextUrl = nextPageElement.GetAttribute("href");
        if (string.IsNullOrEmpty(nextUrl))
        {
            return null;
        }

        // Handle relative URLs
        if (!nextUrl.StartsWith("http"))
        {
            var baseUri = new Uri(baseUrl);
            nextUrl = new Uri(baseUri, nextUrl).AbsoluteUri;
        }

        return nextUrl;
    }
}