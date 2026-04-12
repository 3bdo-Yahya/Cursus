using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class CreditHourRuleConfiguration : IEntityTypeConfiguration<CreditHourRule>
    {
        public void Configure(EntityTypeBuilder<CreditHourRule> builder)
        {
            builder.HasOne(rule => rule.Department)
                .WithMany(department => department.CreditHourRules)
                .HasForeignKey(rule => rule.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rule => rule.DepartmentId);

            builder.HasIndex(rule => new { rule.DepartmentId, rule.Standing });
        }
    }
}
