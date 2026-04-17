using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            // Enforce valid graduation settings in SQL Server so direct inserts cannot create invalid departments.
            builder.ToTable(tableBuilder =>
            {
                // Require departments to have a positive graduation-credit total.
                tableBuilder.HasCheckConstraint(
                    "CK_Departments_TotalCreditsRequired_Positive",
                    "[TotalCreditsRequired] > 0");

                // Keep the minimum graduation GPA inside the common 0.00..4.00 academic scale.
                tableBuilder.HasCheckConstraint(
                    "CK_Departments_MinGpaForGraduation_Range",
                    "[MinGpaForGraduation] >= 0 AND [MinGpaForGraduation] <= 4");
            });

            builder.Property(department => department.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(department => department.MinGpaForGraduation)
                .HasPrecision(3, 2);

            builder.Property(department => department.IsActive)
                .HasDefaultValue(true);

            builder.HasOne(department => department.University)
                .WithMany(university => university.Departments)
                .HasForeignKey(department => department.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(department => department.UniversityId);

            builder.HasIndex(department => new { department.UniversityId, department.Name })
                .IsUnique();
        }
    }
}
