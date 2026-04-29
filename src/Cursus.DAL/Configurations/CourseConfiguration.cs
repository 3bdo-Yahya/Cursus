using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.Property(course => course.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(course => course.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Courses_CreditHours_Range",
                    "[CreditHours] >= 1 AND [CreditHours] <= 6");
            });

            builder.Property(c => c.CourseType)
                .IsRequired();

            builder.Property(c => c.SemesterAvailability)
                .IsRequired();

            builder.Property(c => c.PassingGradeThreshold)
                .IsRequired()
                .HasDefaultValue("D")
                .HasMaxLength(2)
                .IsFixedLength();

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
