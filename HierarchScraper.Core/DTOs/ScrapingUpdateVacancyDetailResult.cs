using HierarchScraper.Core.Models;

namespace HierarchScraper.Core.DTOs;

public class ScrapingUpdateVacancyDetailResult
{
    public Vacancy? Data { get; set; } = null;

    public string? ErrorMessage { get; set; } = string.Empty;
}