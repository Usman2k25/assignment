using CampusServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusServices.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);

        // The three fixed categories. HasData puts them into the migration,
        // so they are inserted into the database when you run "database update".
        builder.HasData(
            new Category { Id = 1, Name = "IT Support" },
            new Category { Id = 2, Name = "Facilities" },
            new Category { Id = 3, Name = "Administration" });
    }
}
