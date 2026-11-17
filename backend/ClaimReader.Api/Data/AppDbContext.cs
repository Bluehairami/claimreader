using ClaimReader.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimReader.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Claim> Claims => Set<Claim>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Claim>(e =>
        {
            e.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(c => c.ClaimAmount).HasPrecision(12, 2);
            e.Property(c => c.RawModelJson).HasColumnType("jsonb");
            e.HasIndex(c => c.CreatedAt); // Add this line to create an index
        });
    }
}