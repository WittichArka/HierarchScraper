using HierarchScraper.Core.Interfaces;
using HierarchScraper.Core.Models;
using HierarchScraper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HierarchScraper.Infrastructure.Repositories;

public class VacancyRepository : IVacancyRepository
{
    private readonly ApplicationDbContext _context;

    public VacancyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vacancy> AddAsync(Vacancy vacancy)
    {
        _context.Vacancies.Add(vacancy);
        await _context.SaveChangesAsync();
        return vacancy;
    }

    public async Task DeleteAsync(int id)
    {
        var vacancy = await _context.Vacancies.FindAsync(id);
        if (vacancy != null)
        {
            _context.Vacancies.Remove(vacancy);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Vacancy>> GetAllAsync()
    {
        return await _context.Vacancies
            .AsNoTracking()
            .OrderByDescending(v => v.CreatedDate)
            .ToListAsync();
    }

    public async Task<Vacancy?> GetByIdAsync(int id)
    {
        return await _context.Vacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<Vacancy>> GetBySourceAsync(int sourceId)
    {
        return await _context.Vacancies
            .AsNoTracking()
            .Where(v => v.ScrapingSourceId == sourceId)
            .OrderByDescending(v => v.CreatedDate)
            .ToListAsync();
    }

    public async Task UpdateAsync(Vacancy vacancy)
    {
        _context.Vacancies.Update(vacancy);
        await _context.SaveChangesAsync();
    }
}