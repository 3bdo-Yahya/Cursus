using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
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
