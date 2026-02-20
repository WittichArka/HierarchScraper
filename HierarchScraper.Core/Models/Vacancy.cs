namespace HierarchScraper.Core.Models;

public class Vacancy
{
    public int Id { get; set; }
    
    // Identification
    public string JobId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Titre du poste
    public string CompanyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    // Détails de l'offre
    public string JobDescription { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty; // Temps plein, CDI...
    public string Salary { get; set; } = string.Empty; // Texte brut du salaire
    public string RemotePolicy { get; set; } = string.Empty; // Hybride, Télétravail...

    // URLs
    public string DetailUrl { get; set; } = string.Empty; // URL propre viewjob?jk=...
    public string ApplyLink { get; set; } = string.Empty; // Lien direct si différent

    // Dates
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow; // Date du scrap
    public string? PostedDateRaw { get; set; } // "Il y a 3 jours"
    public DateTime? ApplyDate { get; set; } = null;

    // Source & Tracking
    public int ScrapingSourceId { get; set; }
    public string SourcePlatform { get; set; } = string.Empty; // "Indeed", "LinkedIn"...
    public bool IsActive { get; set; } = true;

    // Nouveau : Un champ JSON pour stocker des infos spécifiques sans changer la DB
    // Utile pour stocker les "Avantages" (Chèques repas, voiture, etc.)
    public string? AdditionalDataJson { get; set; } 

    // Facile à consommer côté API : désérialise automatiquement le JSON
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public Dictionary<string,string>? AdditionalData
    {
        get => string.IsNullOrEmpty(AdditionalDataJson)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(AdditionalDataJson);
        set => AdditionalDataJson = value == null
                ? null
                : System.Text.Json.JsonSerializer.Serialize(value);
    }
}