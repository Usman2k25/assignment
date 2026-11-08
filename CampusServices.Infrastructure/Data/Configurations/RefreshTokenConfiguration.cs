using CampusServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusServices.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(100);

        // We look tokens up by their hash, so the index makes that fast (and keeps hashes unique).
        builder.HasIndex(t => t.TokenHash).IsUnique();

        // If a user is deleted, their refresh tokens are deleted too.
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
