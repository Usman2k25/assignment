using CampusServices.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusServices.Infrastructure.Data;

// The bridge between our C# classes and the PostgreSQL tables.
// IdentityDbContext adds the Identity tables (users, roles, user-roles, ...) for us.
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Each DbSet is one table.
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Loads every class that implements IEntityTypeConfiguration (see the Configurations folder).
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
