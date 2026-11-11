using Microsoft.EntityFrameworkCore;

namespace SousChef.Data;

/// <summary>
/// Database context for SousChef.
/// </summary>
/// <param name="options">The database context options.</param>
public class SousChefDbContext(DbContextOptions<SousChefDbContext> options) : DbContext(options)
{
    /// <summary>
    /// The stored recipe cards.
    /// </summary>
    public DbSet<RecipeRecord> Recipes => Set<RecipeRecord>();

    /// <summary>
    /// The images extracted from the cards.
    /// </summary>
    public DbSet<RecipeImageRecord> Images => Set<RecipeImageRecord>();

    /// <summary>
    /// Configures indexes and relationships.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecipeRecord>().HasIndex(r => r.ProductCode);
        modelBuilder.Entity<RecipeRecord>().HasMany(r => r.Images).WithOne().HasForeignKey(i => i.RecipeRecordId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecipeImageRecord>().HasIndex(i => new { i.RecipeRecordId, i.Key }).IsUnique();
    }
}