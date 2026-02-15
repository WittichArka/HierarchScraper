namespace HierarchScraper.Core.Models;

public class Vacancy
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public int ScrapingSourceId { get; set; }
    public string SourcePlatform { get; set; } = string.Empty;
}