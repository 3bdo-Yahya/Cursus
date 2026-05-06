using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class GraduationRequirementCourseConfiguration : IEntityTypeConfiguration<GraduationRequirementCourse>
    {
        public void Configure(EntityTypeBuilder<GraduationRequirementCourse> builder)
        {
            builder.HasKey(requirementCourse => new
            {
                requirementCourse.GraduationRequirementId,
                requirementCourse.CourseId
            });

            builder.HasOne(requirementCourse => requirementCourse.GraduationRequirement)
                .WithMany(requirement => requirement.GraduationRequirementCourses)
                .HasForeignKey(requirementCourse => requirementCourse.GraduationRequirementId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(requirementCourse => requirementCourse.Course)
                .WithMany(course => course.GraduationRequirementCourses)
                .HasForeignKey(requirementCourse => requirementCourse.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(requirementCourse => requirementCourse.CourseId);
        }
    }
}
