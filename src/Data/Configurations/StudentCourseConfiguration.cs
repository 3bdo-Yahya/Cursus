using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class StudentCourseConfiguration : IEntityTypeConfiguration<StudentCourse>
    {
        public void Configure(EntityTypeBuilder<StudentCourse> builder)
        {
            builder.Property(studentCourse => studentCourse.StudentId)
                .IsRequired();

            builder.Property(studentCourse => studentCourse.Grade)
                .HasMaxLength(2);

            builder.Property(studentCourse => studentCourse.AcademicYear)
                .IsRequired()
                .HasMaxLength(10);

            builder.HasOne(studentCourse => studentCourse.Student)
                .WithMany(student => student.StudentCourses)
                .HasForeignKey(studentCourse => studentCourse.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(studentCourse => studentCourse.Course)
                .WithMany(course => course.StudentCourses)
                .HasForeignKey(studentCourse => studentCourse.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(studentCourse => new
            {
                studentCourse.StudentId,
                studentCourse.CourseId,
                studentCourse.AcademicYear,
                studentCourse.Semester
            }).IsUnique();

            builder.HasIndex(studentCourse => new
            {
                studentCourse.StudentId,
                studentCourse.AcademicYear,
                studentCourse.Semester
            });

            builder.HasIndex(studentCourse => studentCourse.CourseId);
        }
    }
}
