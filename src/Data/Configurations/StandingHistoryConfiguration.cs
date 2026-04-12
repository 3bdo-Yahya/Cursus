using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class StandingHistoryConfiguration : IEntityTypeConfiguration<StandingHistory>
    {
        public void Configure(EntityTypeBuilder<StandingHistory> builder)
        {
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
