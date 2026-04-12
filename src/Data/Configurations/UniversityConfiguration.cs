using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class UniversityConfiguration : IEntityTypeConfiguration<University>
    {
        public void Configure(EntityTypeBuilder<University> builder)
        {
            // Keep the university name mapping explicit in Fluent API so schema rules live with the rest of the EF configuration.
            builder.Property(university => university.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Prevent duplicate university names because the current domain treats each university as a distinct catalog owner.
            builder.HasIndex(university => university.Name)
                .IsUnique();
        }
    }
}
