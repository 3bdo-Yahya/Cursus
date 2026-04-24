using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class GradeScaleConfiguration : IEntityTypeConfiguration<GradeScale>
    {
        public void Configure(EntityTypeBuilder<GradeScale> builder)
        {
            // Keep grade points inside a realistic GPA range even if data is loaded directly through SQL scripts.
            builder.ToTable(tableBuilder =>
            {
                // Restrict point values to the standard 0.00..4.00 range used by the current domain model.
                tableBuilder.HasCheckConstraint(
                    "CK_GradeScales_PointValue_Range",
                    "[PointValue] >= 0 AND [PointValue] <= 4");
            });

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
