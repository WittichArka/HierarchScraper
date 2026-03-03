# HierarchScraper - Web Scraping Application

Une application .NET 8 robuste pour le web scraping générique suivant les principes SOLID, capable de gérer des sites statiques et dynamiques (SPA) grâce à Puppeteer.

## Architecture

L'application suit une architecture en couches (N-Tier) :

- **HierarchScraper.Core** : Cœur métier contenant les modèles (Vacancy, ScrapingSource), les interfaces et les configurations.
- **HierarchScraper.Infrastructure** : Implémentations concrètes.
  - `Data` : Context Entity Framework Core pour SQLite.
  - `Repositories` : Accès aux données pour les sources et les offres.
  - `Services` : Moteur de scraping basé sur **PuppeteerSharp** et parseur HTML via **AngleSharp**.
- **HierarchScraper.API** : API REST minimaliste exposant les fonctionnalités de gestion et de scraping.

## Fonctionnalités

### 1. Gestion des sources de scraping
- CRUD complet sur les sources de scraping via l'API.
- Chaque source possède sa propre configuration JSON de règles.
- Activation/Désactivation globale d'une source.

### 2. Moteur de Scraping Avancé
- **Gestion du JS** : Utilisation d'un navigateur headless pour charger les contenus dynamiques.
- **Simulation Humaine** : Masquage du mode automation (`webdriver`), User-Agent réaliste, gestion du viewport et délais aléatoires entre les actions pour éviter la détection.
- **Pagination** : Support de la pagination via sélecteur CSS.
- **Extraction en deux temps** :
  1. Récupération des informations de base depuis la liste.
  2. (Optionnel) Navigation vers la page de détail pour enrichir l'offre.
- **Détection des doublons** : Identification unique basée sur le couple `JobId` + `SourcePlatform`.

### 3. Règles de scraping configurables
La configuration JSON d'une source permet de définir :
- **ListSelector** : Conteneur global des offres.
- **ItemSelector** : Sélecteur pour chaque bloc d'offre.
- **JobKeyAttribute** : Attribut HTML contenant l'identifiant unique de l'offre (ex: `data-jk` sur Indeed).
- **DetailUrlTemplate** : Template pour reconstruire l'URL de détail si elle n'est pas présente directement (ex: `https://site.com/job/{0}`).
- **ExclusionRules** : Filtrage des items (ex: publicités) selon la présence ou l'absence d'éléments CSS.
- **DetailConfig** : Mapping `Champ -> Sélecteur CSS` pour la page de détail. Supporte l'extraction d'attributs via la syntaxe `selector|attribute`.

### 4. Détection d'inactivité (IsActive)
L'application peut détecter si une offre est expirée via `IsActiveConfig` :
- **BySentences** : Liste de phrases (ex: "Cette offre n'est plus disponible") dont la présence sur la page marque l'offre comme inactive.
- **IsNoDescriptionInactive** : Marque l'offre comme inactive si le sélecteur de description ne renvoie aucun contenu.

## Modèle de Données (Vacancy)

Les offres extraites contiennent :
- Informations de base : Titre, Entreprise, Localisation, Description.
- Métadonnées : Type de contrat, Salaire, Politique de télétravail.
- Liens : URL de détail, Lien de postulation direct.
- **AdditionalData** : Un dictionnaire JSON flexible permettant de stocker n'importe quel champ supplémentaire extrait sans modifier le schéma de la base de données.

## Configuration

### Base de données (SQLite)
Configurez la chaîne de connexion dans `appsettings.json` :
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=HierarchScraper.db"
}
```

### Puppeteer (Navigateur)
```json
"Puppeteer": {
  "Headless": true,
  "Timeout": 30000,
  "WaitForSelectorTimeout": 10000,
  "UserDataSavePath": "C:\\Temp\\PuppeteerData" // Pour conserver les sessions/cookies
}
```

### Logs
L'application intègre un **FileLoggerProvider** personnalisé qui enregistre les logs quotidiens dans le dossier `Logs/`.

## API Endpoints

### Scraping
- `GET /api/scraping/sources` : Liste des sources.
- `POST /api/scraping/sources` : Ajouter une source.
- `POST /api/scraping/sources/{id}/scrape` : Lancer le scraping pour une source.
- `POST /api/scraping/scrape-all` : Lancer toutes les sources actives.
- `POST /api/scraping/vacancy/{id}/update` : **Nouveau** - Met à jour les détails d'une offre spécifique à partir de son URL de détail déjà enregistrée.

### Vacancies
- `GET /api/vacancies` : Liste des offres.
- `GET /api/vacancies/{id}` : Détails d'une offre.

## Installation

1. **Prérequis** : .NET 8 SDK.
2. **Installation** :
   ```bash
   dotnet restore
   dotnet build
   ```
3. **Base de données** :
   ```bash
   dotnet ef database update --project HierarchScraper.Infrastructure --startup-project HierarchScraper.API
   ```
4. **Exécution** :
   ```bash
   dotnet run --project HierarchScraper.API
   ```

## Structure du Projet

```
HierarchScraper/
├── HierarchScraper.API/           # Points d'entrée REST
├── HierarchScraper.Core/          # Logique métier et abstractions
├── HierarchScraper.Infrastructure/# Persistance et Moteur de Scraping
└── Logs/                          # Logs de l'application
```
