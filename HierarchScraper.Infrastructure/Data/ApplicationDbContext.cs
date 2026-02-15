using HierarchScraper.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace HierarchScraper.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Vacancy> Vacancies { get; set; }
    public DbSet<ScrapingSource> ScrapingSources { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Vacancy entity
        modelBuilder.Entity<Vacancy>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Name).IsRequired().HasMaxLength(500);
            entity.Property(v => v.DetailUrl).IsRequired().HasMaxLength(1000);
            entity.Property(v => v.JobDescription).HasMaxLength(5000);
            entity.Property(v => v.SourcePlatform).IsRequired().HasMaxLength(100);
            
            entity.HasOne<ScrapingSource>()
                .WithMany()
                .HasForeignKey(v => v.ScrapingSourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ScrapingSource entity
        modelBuilder.Entity<ScrapingSource>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Platform).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.Url).IsRequired().HasMaxLength(1000);
            entity.Property(s => s.ScrapingConfig).IsRequired();
        });
    }
}