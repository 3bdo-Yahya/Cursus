using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            // Enforce valid credit-hour values in SQL Server even when rows are inserted outside the application.
            builder.ToTable(tableBuilder =>
            {
                // Keep course credit hours inside the same 1..6 range defined by the entity validation attribute.
                tableBuilder.HasCheckConstraint(
                    "CK_Courses_CreditHours_Range",
                    "[CreditHours] >= 1 AND [CreditHours] <= 6");
            });

            builder.Property(course => course.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(course => course.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(course => course.PassingGradeThreshold)
                .IsRequired()
                .HasMaxLength(2);

            builder.HasOne(course => course.Department)
                .WithMany(department => department.Courses)
                .HasForeignKey(course => course.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(course => course.DepartmentId);

            builder.HasIndex(course => new { course.DepartmentId, course.Code })
                .IsUnique();
        }
    }
}
