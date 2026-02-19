namespace HierarchScraper.Core.Configurations;

public class ScrapingConfiguration
{
    public string ListSelector { get; set; } = string.Empty;
    public string NextPageSelector { get; set; } = string.Empty;
    public ItemConfiguration ItemConfig { get; set; } = new ItemConfiguration();
}

public class ItemConfiguration
{
    public string ItemSelector { get; set; } = string.Empty;
    public List<ExclusionRule> ExclusionRules { get; set; } = new List<ExclusionRule>();
    public string TitleSelector { get; set; } = string.Empty;
    public string DetailSelector { get; set; } = string.Empty;
    public string JobKeyAttribute { get; set; } = string.Empty;
    public string DetailUrlTemplate { get; set; } = string.Empty;
}

public class ExclusionRule
{
    public string Selector { get; set; } = string.Empty;
    public bool MustExist { get; set; } = true;
}