using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations;

public class PlannedCourseConfiguration : IEntityTypeConfiguration<PlannedCourse>
{
    public void Configure(EntityTypeBuilder<PlannedCourse> builder)
    {
        builder.Property(pc => pc.StudentId)
            .IsRequired();

        builder.Property(pc => pc.AcademicYear)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasOne(pc => pc.Student)
            .WithMany(student => student.PlannedCourses)
            .HasForeignKey(pc => pc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Course)
            .WithMany(course => course.PlannedCourses)
            .HasForeignKey(pc => pc.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pc => new
        {
            pc.StudentId,
            pc.CourseId,
            pc.AcademicYear,
            pc.Semester
        }).IsUnique();

        builder.HasIndex(pc => new
        {
            pc.StudentId,
            pc.AcademicYear,
            pc.Semester
        });
    }
}
