using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class UniversityConfiguration : IEntityTypeConfiguration<University>
    {
        public void Configure(EntityTypeBuilder<University> builder)
        {
            builder.Property(university => university.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(university => university.Name)
                .IsUnique();
        }
    }
}
