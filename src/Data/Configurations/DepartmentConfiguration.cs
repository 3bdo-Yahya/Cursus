using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.Property(department => department.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(department => department.MinGpaForGraduation)
                .HasPrecision(3, 2);

            builder.HasOne(department => department.University)
                .WithMany(university => university.Departments)
                .HasForeignKey(department => department.UniversityId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(department => department.UniversityId);

            builder.HasIndex(department => new { department.UniversityId, department.Name })
                .IsUnique();
        }
    }
}
