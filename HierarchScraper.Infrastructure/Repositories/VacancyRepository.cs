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

    public async Task<IEnumerable<string>> GetJobIdsByPlatform(string platform)
    {
        return await _context
        .Vacancies
        .Where(v => v.SourcePlatform == platform)
        .Select(v => v.JobId)
        .ToListAsync();
    }

    public async Task<Vacancy> AddAsync(Vacancy vacancy)
    {
        // Do not duplicate; if an entry already exists we may still merge new details
        var existingVacancy = await _context.Vacancies
            .SingleOrDefaultAsync(v => v.SourcePlatform == vacancy.SourcePlatform && v.JobId == vacancy.JobId);
        if (existingVacancy != null)
        {
            bool changed = false;

            if (string.IsNullOrEmpty(existingVacancy.Name) && !string.IsNullOrEmpty(vacancy.Name))
            {
                existingVacancy.Name = vacancy.Name;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.CompanyName) && !string.IsNullOrEmpty(vacancy.CompanyName))
            {
                existingVacancy.CompanyName = vacancy.CompanyName;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.Location) && !string.IsNullOrEmpty(vacancy.Location))
            {
                existingVacancy.Location = vacancy.Location;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.JobDescription) && !string.IsNullOrEmpty(vacancy.JobDescription))
            {
                existingVacancy.JobDescription = vacancy.JobDescription;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.ContractType) && !string.IsNullOrEmpty(vacancy.ContractType))
            {
                existingVacancy.ContractType = vacancy.ContractType;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.Salary) && !string.IsNullOrEmpty(vacancy.Salary))
            {
                existingVacancy.Salary = vacancy.Salary;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.RemotePolicy) && !string.IsNullOrEmpty(vacancy.RemotePolicy))
            {
                existingVacancy.RemotePolicy = vacancy.RemotePolicy;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.ApplyLink) && !string.IsNullOrEmpty(vacancy.ApplyLink))
            {
                existingVacancy.ApplyLink = vacancy.ApplyLink;
                changed = true;
            }
            if (string.IsNullOrEmpty(existingVacancy.PostedDateRaw) && !string.IsNullOrEmpty(vacancy.PostedDateRaw))
            {
                existingVacancy.PostedDateRaw = vacancy.PostedDateRaw;
                changed = true;
            }
            if (vacancy.ApplyDate.HasValue && !existingVacancy.ApplyDate.HasValue)
            {
                existingVacancy.ApplyDate = vacancy.ApplyDate;
                changed = true;
            }

            // merge additional data JSON if both exist (simply overwrite for now)
            if (!string.IsNullOrEmpty(vacancy.AdditionalDataJson))
            {
                if (string.IsNullOrEmpty(existingVacancy.AdditionalDataJson) ||
                    existingVacancy.AdditionalDataJson != vacancy.AdditionalDataJson)
                {
                    existingVacancy.AdditionalDataJson = vacancy.AdditionalDataJson;
                    changed = true;
                }
            }

            if (changed)
            {
                _context.Vacancies.Update(existingVacancy);
                await _context.SaveChangesAsync();
            }

            return existingVacancy;
        }

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