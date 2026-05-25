using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PYS.Core.Entities;

namespace PYS.Data.Configurations;

public sealed class ProjectInvitationConfiguration : IEntityTypeConfiguration<ProjectInvitation>
{
    public void Configure(EntityTypeBuilder<ProjectInvitation> builder)
    {
        builder.ToTable("ProjectInvitations");

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasOne(i => i.Project)
            .WithMany(p => p.Invitations)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.ProjectId, i.Email }).IsUnique();
        builder.HasIndex(i => i.Email);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
