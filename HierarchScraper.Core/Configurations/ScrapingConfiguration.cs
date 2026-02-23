namespace HierarchScraper.Core.Configurations;

/// <summary>
/// Top‑level configuration object that is stored as JSON per scraping source.
/// </summary>
public class ScrapingConfiguration
{
    /// <summary>CSS selector that matches the container holding all items to enumerate.</summary>
    public string ListSelector { get; set; } = string.Empty;

    /// <summary>Selector used to locate the "next page" link for pagination.</summary>
    public string NextPageSelector { get; set; } = string.Empty;

    /// <summary>Sub‑configuration that applies to each item in the list.</summary>
    public ItemConfiguration ItemConfig { get; set; } = new ItemConfiguration();
}

/// <summary>
/// Options applied to each individual item in the list returned by
/// <see cref="ScrapingConfiguration.ListSelector" />.
/// </summary>
public class ItemConfiguration
{
    /// <summary>CSS selector matching one vacancy element inside the parent list.</summary>
    public string ItemSelector { get; set; } = string.Empty;

    /// <summary>Collection of rules used to filter out unwanted items.</summary>
    public List<ExclusionRule> ExclusionRules { get; set; } = new List<ExclusionRule>();

    /// <summary>Selector used to obtain the string title of the job.</summary>
    public string TitleSelector { get; set; } = string.Empty;

    /// <summary>Selector for the element (usually &lt;a&gt;) containing the detail URL.
    /// The URL itself is obtained from the <see cref="JobKeyAttribute" /> or
    /// by taking the element's href.</summary>
    public string DetailSelector { get; set; } = string.Empty; // link element on the listing page

    /// <summary>Name of the attribute on the <see cref="DetailSelector" /> element that
    /// contains the job identifier (e.g. "jk" on Indeed).</summary>
    public string JobKeyAttribute { get; set; } = string.Empty;

    /// <summary>A template used to build the detail url when only the job key is
    /// available. Use '{0}' as placeholder for the key value.</summary>
    public string DetailUrlTemplate { get; set; } = string.Empty;

    /// <summary>Further configuration to parse the vacancy's detail page.</summary>
    public DetailConfiguration DetailConfig { get; set; } = new DetailConfiguration();
}

/// <summary>
/// Settings used when opening the detail page for a vacancy. Both properties
/// are optional; if <see cref="FieldSelectors" /> is empty the page will not be
/// requested at all.
/// </summary>
public class DetailConfiguration
{
    /// <summary>
    /// Selector which must be present on the detail page before extracting fields.
    /// Useful to ensure that dynamic content has finished loading.
    /// </summary>
    public string MainSelector { get; set; } = string.Empty;

    /// <summary>
    /// Maps a logical field name to a CSS selector (optionally with |attribute)
    /// used to pull data from the detail page. Known names are mapped to
    /// strongly typed properties; unknown names are serialized into
    /// <see cref="Models.Vacancy.AdditionalDataJson" />.
    /// </summary>
    public Dictionary<string,string> FieldSelectors { get; set; } = new Dictionary<string,string>();

    /// <summary>
    /// Contains logic on how to detect to detect wether or not a vacancy is active or not
    /// </summary> 
    public IsActiveConfiguration IsActiveConfig { get; set; } = new IsActiveConfiguration();
}

/// <summary>
/// Rule used to include or exclude an item based on the presence (or absence)
/// of an element matching <see cref="Selector" />.
/// </summary>
public class ExclusionRule
{
    /// <summary>CSS selector for the element inspected by the rule.</summary>
    public string Selector { get; set; } = string.Empty;

    /// <summary>If true, the item is excluded when the selector matches.</summary>
    public bool MustExist { get; set; } = true;
}

public class IsActiveConfiguration
{
    public List<string> BySentences { get; set; } = new List<string>();

    public bool IsNoDescriptionInactive { get; set; } = false;
}
