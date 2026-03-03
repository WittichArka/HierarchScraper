# HierarchScraper - Project Context & Rules

Ce fichier sert de base de connaissances pour Gemini CLI afin de maintenir la cohérence architecturale et technique du projet.

## 🎯 Project Overview
Application de Web Scraping générique en .NET 8. Capable de scraper des sites statiques et dynamiques (SPAs) en utilisant PuppeteerSharp pour le rendu et AngleSharp pour le parsing DOM.

## 🏗️ Architecture & Patterns
- **Architecture N-Tier** : Core (Abstractions/Modèles), Infrastructure (Implémentations), API (Endpoints).
- **Repository Pattern** : Toute interaction avec la base de données SQLite doit passer par `IVacancyRepository` ou `IScrapingSourceRepository`.
- **Dependency Injection** : Les services sont enregistrés dans `Program.cs` de l'API.
- **SOLID Principles** : Maintenir le découplage entre le moteur de scraping (Puppeteer) et la logique de parsing (`VacancyDetailParser`).

## 🛠️ Tech Stack & Key Libraries
- **Runtime** : .NET 8
- **Browser Automation** : PuppeteerSharp (gestion des SPAs, simulation humaine).
- **HTML Parsing** : AngleSharp (utilisé sur le contenu récupéré par Puppeteer).
- **Database** : SQLite via Entity Framework Core.
- **Logging** : Custom `FileLoggerProvider` enregistrant dans le dossier `Logs/`.

## 📜 Coding Rules & Conventions
- **Modèle Vacancy** : 
    - Toujours utiliser `JobId` + `SourcePlatform` comme identifiant unique pour éviter les doublons.
    - Utiliser `AdditionalDataJson` (via la propriété non-mappée `AdditionalData`) pour stocker des données spécifiques à une source sans modifier le schéma SQL.
- **Configuration de Scraping** :
    - La configuration est stockée en JSON dans `ScrapingSource.ScrapingConfig`.
    - Supporte la syntaxe `selector|attribute` pour extraire des attributs spécifiques (ex: `a|href`).
- **Simulation Humaine** : Toujours inclure des délais aléatoires et masquer le flag `webdriver` lors des interactions Puppeteer (déjà implémenté dans `PuppeteerScrapingService`).

## 🔍 Points d'Attention
- Le scraping d'Indeed est particulièrement sensible ; il nécessite l'usage du `JobKeyAttribute` (souvent `jk`) pour identifier correctement les offres avant même de cliquer sur le détail.
- La détection d'inactivité (`IsActive`) se base soit sur des phrases clés dans la page, soit sur l'absence de description.

## 🚀 Workflows Communs
- **Migrations** : `dotnet ef migrations add <Name> --project HierarchScraper.Infrastructure --startup-project HierarchScraper.API`
- **Exécution** : `dotnet run --project HierarchScraper.API`
