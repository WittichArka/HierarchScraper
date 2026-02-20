using HierarchScraper.Core.Configurations;
using HierarchScraper.Core.Models;

namespace HierarchScraper.Core.Interfaces;

public interface IScrapingService
{
    Task<IEnumerable<Vacancy>> ScrapeSourceAsync(ScrapingSource source);
    Task ProcessAllActiveSourcesAsync();

    /// <summary>
    /// Visits the detail page specified on <paramref name="vacancy" /> and
    /// uses the provided configuration to fill-in any missing values. The
    /// vacancy argument is modified in-place and returned (or <c>null</c> if
    /// the request failed).
    /// </summary>
    Task<Vacancy?> UpdateVacancyDetailAsync(Vacancy vacancy, DetailConfiguration detailConfig);
}