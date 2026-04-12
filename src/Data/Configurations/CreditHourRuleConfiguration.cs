using Cursus.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cursus.Data.Configurations
{
    public class CreditHourRuleConfiguration : IEntityTypeConfiguration<CreditHourRule>
    {
        public void Configure(EntityTypeBuilder<CreditHourRule> builder)
        {
            // Protect the table from impossible credit-limit rows such as negative values or a max below the min.
            builder.ToTable(tableBuilder =>
            {
                // Require both bounds to be non-negative and ordered correctly for scheduling logic.
                tableBuilder.HasCheckConstraint(
                    "CK_CreditHourRules_MinCredits_MaxCredits",
                    "[MinCredits] >= 0 AND [MaxCredits] >= [MinCredits]");
            });

            builder.HasOne(rule => rule.Department)
                .WithMany(department => department.CreditHourRules)
                .HasForeignKey(rule => rule.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rule => rule.DepartmentId);

            // Prevent duplicate credit-hour policies for the same department and academic standing.
            builder.HasIndex(rule => new { rule.DepartmentId, rule.Standing })
                .IsUnique();
        }
    }
}
