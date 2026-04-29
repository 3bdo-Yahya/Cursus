using Cursus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.DAL.Configurations
{
    public class CreditHourRuleConfiguration : IEntityTypeConfiguration<CreditHourRule>
    {
        public void Configure(EntityTypeBuilder<CreditHourRule> builder)
        {
            builder.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CreditHourRules_MinCredits_MaxCredits",
                    "[MinCredits] >= 0 AND [MaxCredits] >= [MinCredits]");
            });

            builder.HasOne(rule => rule.Department)
                .WithMany(department => department.CreditHourRules)
                .HasForeignKey(rule => rule.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rule => rule.DepartmentId);

            builder.HasIndex(rule => new { rule.DepartmentId, rule.Standing })
                .IsUnique();
        }
    }
}
