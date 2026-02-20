# HierarchScraper - Web Scraping Application

Une application .NET 8 pour le web scraping générique suivant les principes SOLID.

## Architecture

L'application suit une architecture en couches avec :

- **HierarchScraper.Core** : Logique métier, modèles et interfaces
- **HierarchScraper.Infrastructure** : Implémentation concrète (scraping, persistence)
- **HierarchScraper.Web** : API REST pour gérer les sources et les offres

## Fonctionnalités

### 1. Gestion des sources de scraping
- Ajout/Modification/Suppression de sources
- Configuration JSON pour les règles de scraping
- Activation/Désactivation des sources

### 2. Règles de scraping configurables
- **ListSelector** : Sélecteur CSS pour trouver la liste des éléments
- **ItemSelector** : Sélecteur CSS pour trouver les items individuels
- **ExclusionRules** : Règles pour filtrer les éléments indésirables (publicités, etc.)
- **TitleSelector** : Sélecteur CSS pour extraire le titre de l'offre
- **DetailSelector** : Sélecteur CSS pour extraire l'URL de détail
- **NextPageSelector** : Sélecteur CSS pour la pagination

#### Extraire et enrichir depuis la page de détail
Lorsque le listing ne contient pas toutes les informations, vous pouvez indiquer un
`DetailConfig` complet.
- **MainSelector** : (optionnel) élément qui doit exister sur la page de détail avant de commencer l'extraction.
- **FieldSelectors** : dictionnaire `nomChamp -> sélecteur` qui mappe HTML → propriétés du modèle.  - Un sélecteur peut être suivi de `|attribut` (ex. `a.apply|href`) pour lire une
    valeur d'attribut plutôt que le texte.  - Les champs reconnus (companyName, location, jobDescription, contractType, salary,
    remotePolicy, applyLink, postedDateRaw, etc.) sont affectés directement.
  - Les noms inconnus sont stockés sous forme JSON dans `Vacancy.AdditionalDataJson`.

Le service tente de charger chaque détail puis appelle `VacancyDetailParser` pour
peupler l'objet. Si une offre existante (même `JobId`+`SourcePlatform`) est trouvée,
les nouveaux champs non vides sont fusionnés dans la ligne existante : cela permet
d'abord d'enregistrer les annonces légères puis d'ajouter les informations détaillées
plus tard sans créer de doublons.

### 3. Règles d'exclusion
Chaque règle d'exclusion contient :
- **Selector** : Sélecteur CSS pour chercher la règle
- **MustExist** : Boolean indiquant si l'élément doit être présent ou non

Logique d'exclusion :
- Si `MustExist=true` et l'élément est trouvé → Exclure (ex: publicité trouvée)
- Si `MustExist=true` et l'élément n'est pas trouvé → Inclure (ex: pas de publicité)
- Si `MustExist=false` et l'élément est trouvé → Inclure (ex: titre trouvé)
- Si `MustExist=false` et l'élément n'est pas trouvé → Exclure (ex: titre manquant)

## Configuration

### Base de données
L'application utilise SQLite par défaut. La configuration se trouve dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=HierarchScraper.db"
  }
}
```

### Configuration Puppeteer
PuppeteerSharp est utilisé pour le scraping. Vous pouvez configurer ses options dans `appsettings.json` :

```json
{
  "Puppeteer": {
    "Headless": true,                    // Exécuter en mode headless (sans interface)
    "Timeout": 30000,                    // Timeout en millisecondes
    "WaitForSelectorTimeout": 10000,    // Timeout pour l'attente des sélecteurs
    "ExecutablePath": "",               // Chemin vers l'exécutable Chromium (laisser vide pour téléchargement automatique)
    "UserAgent": "Mozilla/5.0..."        // User Agent pour éviter la détection
  }
}
```

### Exemple de configuration de scraping

#### Pour les sites statiques simples :

```json
{
  "ListSelector": "#job-listings",
  "NextPageSelector": "a[aria-label='Suivant']",
  "ItemConfig": {
    "ItemSelector": ".job-item",
    "ExclusionRules": [
      {
        "Selector": ".advertisement",
        "MustExist": true
      },
      {
        "Selector": ".job-title",
        "MustExist": false
      }
    ],
    "TitleSelector": ".job-title",
    "DetailSelector": ".job-link",
    "DetailConfig": {
      "MainSelector": "#job-detail",           // élément attendu avant de lire les champs
      "FieldSelectors": {
        "description": "#job-description",
        "company": ".company-name",
        "location": ".job-location",
        "contractType": ".contract-type",
        "salary": ".salary-range",
        "postedDate": ".posted-date",
        "applyLink": ".apply-button a"
      }
    }
  }
}
```

#### Pour Indeed (site dynamique) :

```json
{
  "ListSelector": "#mosaic-provider-jobcards",
  "NextPageSelector": "a[data-testid='pagination-page-next']",
  "ItemConfig": {
    "ItemSelector": ".job_seen_beacon",
    "ExclusionRules": [
      {
        "Selector": ".sponsored",
        "MustExist": true
      }
    ],
    "TitleSelector": "h2.jobTitle",
    "DetailSelector": "a[jk]"
  }
}
```

**Note** : Avec PuppeteerSharp, vous pouvez utiliser les mêmes sélecteurs pour les sites statiques et dynamiques. Le navigateur headless charge le contenu JavaScript avant l'analyse.

## API Endpoints

### Sources de scraping
- `GET /api/scraping/sources` - Liste toutes les sources
- `POST /api/scraping/sources` - Crée une nouvelle source
- `POST /api/scraping/sources/{id}/scrape` - Lance le scraping pour une source spécifique
- `POST /api/scraping/scrape-all` - Lance le scraping pour toutes les sources actives

### Offres d'emploi
- `GET /api/vacancies` - Liste toutes les offres
- `GET /api/vacancies/{id}` - Récupère une offre spécifique

## Technologies utilisées

- **.NET 8**
- **Entity Framework Core** (avec SQLite)
- **PuppeteerSharp** (pour le scraping des pages statiques et dynamiques)
- **ASP.NET Core Web API**
- **SOLID Principles**
- **Repository Pattern**

## Exigences système

Pour exécuter cette application avec PuppeteerSharp, vous avez besoin de :

- **.NET 8 SDK**
- **Chromium** (téléchargé automatiquement par PuppeteerSharp)
- **Mémoire** : Minimum 2GB (4GB recommandé pour le scraping intensif)
- **Espace disque** : 500MB pour Chromium + espace pour la base de données

## Installation et exécution

1. Cloner le dépôt
2. Exécuter `dotnet restore`
3. Exécuter `dotnet build`
4. Appliquer les migrations : `dotnet ef database update --project HierarchScraper.Infrastructure --startup-project HierarchScraper.API`
5. Démarrer l'application : `dotnet run --project HierarchScraper.API`

L'API sera disponible sur `https://localhost:5001` (ou `http://localhost:5000`).

**Première exécution** : PuppeteerSharp téléchargera automatiquement Chromium (environ 200MB).

## Tests

L'architecture suit les principes SOLID pour faciliter les tests unitaires. Vous pouvez facilement mock les interfaces pour tester les différents composants.

## 📁 Structure du projet

```
HierarchScraper/
├── HierarchScraper.Core/          # Modèles et interfaces
├── HierarchScraper.Infrastructure/ # Implémentation (scraping, DB)
├── HierarchScraper.API/           # API REST
└── HierarchScraper.db             # Base de données SQLite
```

## Futur projet MVC

Si vous souhaitez ajouter une interface utilisateur MVC plus tard, vous pourrez créer un nouveau projet `HierarchScraper.Web` ou `HierarchScraper.MVC` qui consommera l'API REST existante. Cela permettra une séparation claire entre :

- **Backend** : HierarchScraper.API (API REST)
- **Frontend** : HierarchScraper.Web (MVC/Blazor)

Cette architecture permet une meilleure scalabilité et une séparation des préoccupations.

## Contribution

Les contributions sont les bienvenues ! Veuillez suivre les principes SOLID et maintenir une bonne couverture de tests.