using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PYS.Core.Entities;

namespace PYS.Data.Configurations;

public sealed class ProjectResourceConfiguration : IEntityTypeConfiguration<ProjectResource>
{
    public void Configure(EntityTypeBuilder<ProjectResource> builder)
    {
        builder.ToTable("ProjectResources");

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(r => r.Project)
            .WithMany(p => p.Resources)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Klasör hiyerarşisi (self-reference). SQL Server self-ref cascade'e izin vermez → Restrict.
        builder.HasOne(r => r.ParentFolder)
            .WithMany(r => r.Children)
            .HasForeignKey(r => r.ParentFolderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ProjectId);
        builder.HasIndex(r => r.ParentFolderId);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
