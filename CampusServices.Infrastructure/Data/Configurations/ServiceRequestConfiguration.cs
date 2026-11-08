using CampusServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CampusServices.Infrastructure.Data.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.Property(r => r.Title).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Description).IsRequired().HasMaxLength(1000);

        // Save the status as text ("InProgress") instead of a number, so the table is easy to read.
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        // Category 1 --- * ServiceRequest
        builder.HasOne(r => r.Category)
            .WithMany(c => c.ServiceRequests)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Student 1 --- * ServiceRequest (the creator)
        builder.HasOne(r => r.Student)
            .WithMany(u => u.CreatedRequests)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Staff 1 --- * ServiceRequest (the person working on it, optional)
        builder.HasOne(r => r.AssignedTo)
            .WithMany(u => u.AssignedRequests)
            .HasForeignKey(r => r.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
