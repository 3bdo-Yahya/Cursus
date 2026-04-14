using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(user => user.AcademicYear)
                .HasMaxLength(10);

            builder.HasOne(user => user.Department)
                .WithMany(department => department.Students)
                .HasForeignKey(user => user.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(user => user.DepartmentId);
            builder.HasIndex(user => new { user.DepartmentId, user.AcademicYear, user.CurrentSemester });
        }
    }
}
