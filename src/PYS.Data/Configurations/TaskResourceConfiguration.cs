using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PYS.Core.Entities;

namespace PYS.Data.Configurations;

public sealed class TaskResourceConfiguration : IEntityTypeConfiguration<TaskResource>
{
    public void Configure(EntityTypeBuilder<TaskResource> builder)
    {
        builder.ToTable("TaskResources");

        builder.HasOne(tr => tr.Task)
            .WithMany(t => t.ResourceLinks)
            .HasForeignKey(tr => tr.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // Çoklu cascade yolu (Project→Task ve Project→Resource) çakışmasını önlemek için Restrict.
        builder.HasOne(tr => tr.Resource)
            .WithMany(r => r.TaskLinks)
            .HasForeignKey(tr => tr.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tr => new { tr.TaskId, tr.ResourceId });

        builder.HasQueryFilter(tr => !tr.IsDeleted);
    }
}
