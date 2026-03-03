using HierarchScraper.Core.Models;

namespace HierarchScraper.Core.Interfaces;

public interface IVacancyRepository
{
    Task<Vacancy> AddAsync(Vacancy vacancy);
    Task<IEnumerable<Vacancy>> GetAllAsync();
    Task<IEnumerable<string>> GetJobIdsByPlatform(string platform);
    Task<Vacancy?> GetByIdAsync(int id);
    Task UpdateAsync(Vacancy vacancy);
    Task DeleteAsync(int id);
    Task<IEnumerable<Vacancy>> GetBySourceAsync(int sourceId);
}