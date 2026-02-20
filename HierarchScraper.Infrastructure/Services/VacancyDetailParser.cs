using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using HierarchScraper.Core.Configurations;
using HierarchScraper.Core.Models;

namespace HierarchScraper.Infrastructure.Services;

public static class VacancyDetailParser
{
    /// <summary>
    /// Populates known properties on <paramref name="vacancy" /> by querying the
    /// supplied document using the selectors defined in <paramref name="config" />.
    /// Unrecognized field names are placed in <see cref="Vacancy.AdditionalDataJson" />.
    /// </summary>
    public static void PopulateFromDocument(Vacancy vacancy, IParentNode document, DetailConfiguration config)
    {
        if (vacancy == null) throw new ArgumentNullException(nameof(vacancy));
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (config == null) return;

        var additional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in config.FieldSelectors ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            var field = kvp.Key?.Trim();
            var selector = kvp.Value?.Trim();
            if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(selector))
                continue;

            // allow the config to specify an attribute after a pipe (e.g. "a.apply|href")
            string? attr = null;
            if (selector.Contains("|"))
            {
                var parts = selector.Split('|', 2);
                selector = parts[0];
                attr = parts[1];
            }

            var element = document.QuerySelector(selector);
            if (element == null) continue;

            string? raw;
            if (!string.IsNullOrEmpty(attr))
            {
                raw = element.GetAttribute(attr)?.Trim();
            }
            else
            {
                raw = element.TextContent?.Trim();
            }

            if (string.IsNullOrEmpty(raw)) continue;

            switch (field.ToLowerInvariant())
            {
                case "companyname":
                    vacancy.CompanyName = raw;
                    break;
                case "location":
                    vacancy.Location = raw;
                    break;
                case "jobdescription":
                case "description":
                    vacancy.JobDescription = raw;
                    break;
                case "contracttype":
                    vacancy.ContractType = raw;
                    break;
                case "salary":
                    vacancy.Salary = raw;
                    break;
                case "remotepolicy":
                    vacancy.RemotePolicy = raw;
                    break;
                case "applylink":
                    // if attribute wasn't specified, try href
                    if (string.IsNullOrEmpty(attr) && element is IHtmlAnchorElement a)
                        vacancy.ApplyLink = a.Href;
                    else
                        vacancy.ApplyLink = raw;
                    break;
                case "posteddateraw":
                case "postdate":
                    vacancy.PostedDateRaw = raw;
                    break;
                default:
                    additional[field] = raw;
                    break;
            }
        }

        if (additional.Any())
        {
            try
            {
                vacancy.AdditionalDataJson = JsonSerializer.Serialize(additional);
            }
            catch
            {
                // ignore serialization errors; field data is best-effort
            }
        }
    }
}
