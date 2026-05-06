using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class StandingHistoryConfiguration : IEntityTypeConfiguration<StandingHistory>
    {
        public void Configure(EntityTypeBuilder<StandingHistory> builder)
        {
            // Keep GPA snapshots valid in SQL Server so reporting logic never reads impossible academic values.
            builder.ToTable(tableBuilder =>
            {
                // Restrict semester GPA to the supported 0.00..4.00 range.
                tableBuilder.HasCheckConstraint(
                    "CK_StandingHistories_SemesterGpa_Range",
                    "[SemesterGpa] >= 0 AND [SemesterGpa] <= 4");

                // Restrict cumulative GPA to the supported 0.00..4.00 range.
                tableBuilder.HasCheckConstraint(
                    "CK_StandingHistories_CumulativeGpa_Range",
                    "[CumulativeGpa] >= 0 AND [CumulativeGpa] <= 4");
            });

            builder.Property(history => history.StudentId)
                .IsRequired();

            builder.Property(history => history.AcademicYear)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(history => history.SemesterGpa)
                .HasPrecision(3, 2);

            builder.Property(history => history.CumulativeGpa)
                .HasPrecision(3, 2);

            builder.HasOne(history => history.Student)
                .WithMany(student => student.StandingHistories)
                .HasForeignKey(history => history.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(history => new
            {
                history.StudentId,
                history.Semester,
                history.AcademicYear
            }).IsUnique();

            builder.HasIndex(history => new
            {
                history.StudentId,
                history.AcademicYear
            });
        }
    }
}
