using HierarchScraper.Core.Models;

namespace HierarchScraper.Core.Interfaces;

public interface IScrapingService
{
    Task<IEnumerable<Vacancy>> ScrapeSourceAsync(ScrapingSource source);
    Task ProcessAllActiveSourcesAsync();
}