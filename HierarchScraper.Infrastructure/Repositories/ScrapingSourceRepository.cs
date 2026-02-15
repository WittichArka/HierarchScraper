using HierarchScraper.Core.Interfaces;
using HierarchScraper.Core.Models;
using HierarchScraper.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HierarchScraper.Infrastructure.Repositories;

public class ScrapingSourceRepository : IScrapingSourceRepository
{
    private readonly ApplicationDbContext _context;

    public ScrapingSourceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ScrapingSource> AddAsync(ScrapingSource source)
    {
        _context.ScrapingSources.Add(source);
        await _context.SaveChangesAsync();
        return source;
    }

    public async Task DeleteAsync(int id)
    {
        var source = await _context.ScrapingSources.FindAsync(id);
        if (source != null)
        {
            _context.ScrapingSources.Remove(source);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<ScrapingSource>> GetAllAsync()
    {
        return await _context.ScrapingSources
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ScrapingSource>> GetActiveSourcesAsync()
    {
        return await _context.ScrapingSources
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<ScrapingSource?> GetByIdAsync(int id)
    {
        return await _context.ScrapingSources
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task UpdateAsync(ScrapingSource source)
    {
        _context.ScrapingSources.Update(source);
        await _context.SaveChangesAsync();
    }
}