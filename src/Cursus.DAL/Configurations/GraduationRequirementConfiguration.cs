using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class GraduationRequirementConfiguration : IEntityTypeConfiguration<GraduationRequirement>
    {
        public void Configure(EntityTypeBuilder<GraduationRequirement> builder)
        {
            // Allow zero-credit categories such as optional or placeholder requirements, but block negatives.
            builder.ToTable(tableBuilder =>
            {
                // Graduation requirements must never be negative.
                tableBuilder.HasCheckConstraint(
                    "CK_GraduationRequirements_RequiredCredits_Positive",
                    "[RequiredCredits] >= 0");
            });

            builder.HasOne(requirement => requirement.Department)
                .WithMany(department => department.GraduationRequirements)
                .HasForeignKey(requirement => requirement.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(requirement => requirement.DepartmentId);

            // Prevent duplicate category rules inside the same department.
            builder.HasIndex(requirement => new { requirement.DepartmentId, requirement.CategoryType })
                .IsUnique();
        }
    }
}
