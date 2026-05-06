using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
    {
        public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
        {
            builder.HasKey(coursePrerequisite => new
            {
                coursePrerequisite.CourseId,
                coursePrerequisite.PrerequisiteId
            });

            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CoursePrerequisite_CourseId_PrerequisiteId",
                    "[CourseId] <> [PrerequisiteId]");
            });

            builder.HasOne(coursePrerequisite => coursePrerequisite.Course)
                .WithMany(course => course.Prerequisites)
                .HasForeignKey(coursePrerequisite => coursePrerequisite.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(coursePrerequisite => coursePrerequisite.Prerequisite)
                .WithMany(course => course.IsPrerequisiteFor)
                .HasForeignKey(coursePrerequisite => coursePrerequisite.PrerequisiteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(coursePrerequisite => coursePrerequisite.PrerequisiteId);
        }
    }
}
