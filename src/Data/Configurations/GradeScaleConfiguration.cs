using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
    {
        public void Configure(EntityTypeBuilder<GradeScale> builder)
        {
            builder.Property(gradeScale => gradeScale.LetterGrade)
                .IsRequired()
                .HasMaxLength(2);

            builder.Property(gradeScale => gradeScale.PointValue)
                .HasPrecision(3, 2);

            builder.HasOne(gradeScale => gradeScale.University)
                .WithMany(university => university.GradeScales)
                .HasForeignKey(gradeScale => gradeScale.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(gradeScale => new { gradeScale.UniversityId, gradeScale.LetterGrade })
                .IsUnique();
        }
    }
}
