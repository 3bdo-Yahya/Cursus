using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class GraduationRequirementConfiguration : IEntityTypeConfiguration<GraduationRequirement>
    {
        public void Configure(EntityTypeBuilder<GraduationRequirement> builder)
        {
            builder.HasOne(requirement => requirement.Department)
                .WithMany(department => department.GraduationRequirements)
                .HasForeignKey(requirement => requirement.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(requirement => requirement.DepartmentId);
        }
    }
}
