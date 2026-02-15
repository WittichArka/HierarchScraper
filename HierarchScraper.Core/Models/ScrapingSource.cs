namespace HierarchScraper.Core.Models;

public class ScrapingSource
{
    public int Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ScrapingConfig { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}