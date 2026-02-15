using HierarchScraper.Core.Models;

namespace HierarchScraper.Core.Interfaces;

public interface IScrapingSourceRepository
{
    Task<ScrapingSource> AddAsync(ScrapingSource source);
    Task<IEnumerable<ScrapingSource>> GetAllAsync();
    Task<ScrapingSource?> GetByIdAsync(int id);
    Task UpdateAsync(ScrapingSource source);
    Task DeleteAsync(int id);
    Task<IEnumerable<ScrapingSource>> GetActiveSourcesAsync();
}