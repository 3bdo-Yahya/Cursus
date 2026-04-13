using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class GraduationRequirementConfiguration : IEntityTypeConfiguration<GraduationRequirement>
    {
        public void Configure(EntityTypeBuilder<GraduationRequirement> builder)
        {
            // Block invalid graduation rows that would require zero or negative credits.
            builder.ToTable(tableBuilder =>
            {
                // Force every graduation requirement to contribute a positive credit amount.
                tableBuilder.HasCheckConstraint(
                    "CK_GraduationRequirements_RequiredCredits_Positive",
                    "[RequiredCredits] > 0");
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
