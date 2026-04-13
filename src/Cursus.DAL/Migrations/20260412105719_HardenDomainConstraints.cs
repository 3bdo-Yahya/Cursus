using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cursus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class HardenDomainConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recreate this index as unique so each department has only one rule per standing.
            migrationBuilder.DropIndex(
                name: "IX_CreditHourRules_DepartmentId_Standing",
                table: "CreditHourRules");

            // Prevent duplicate university rows that differ only by identity key.
            migrationBuilder.CreateIndex(
                name: "IX_Universities_Name",
                table: "Universities",
                column: "Name",
                unique: true);

            // Keep cumulative GPA values inside the supported academic range.
            migrationBuilder.AddCheckConstraint(
                name: "CK_StandingHistories_CumulativeGpa_Range",
                table: "StandingHistories",
                sql: "[CumulativeGpa] >= 0 AND [CumulativeGpa] <= 4");

            // Keep semester GPA values inside the supported academic range.
            migrationBuilder.AddCheckConstraint(
                name: "CK_StandingHistories_SemesterGpa_Range",
                table: "StandingHistories",
                sql: "[SemesterGpa] >= 0 AND [SemesterGpa] <= 4");

            // Prevent two graduation rows from representing the same category inside one department.
            migrationBuilder.CreateIndex(
                name: "IX_GraduationRequirements_DepartmentId_CategoryType",
                table: "GraduationRequirements",
                columns: new[] { "DepartmentId", "CategoryType" },
                unique: true);

            // Force each graduation requirement to add a positive number of credits.
            migrationBuilder.AddCheckConstraint(
                name: "CK_GraduationRequirements_RequiredCredits_Positive",
                table: "GraduationRequirements",
                sql: "[RequiredCredits] > 0");

            // Keep stored grade points aligned with the active GPA scale range.
            migrationBuilder.AddCheckConstraint(
                name: "CK_GradeScales_PointValue_Range",
                table: "GradeScales",
                sql: "[PointValue] >= 0 AND [PointValue] <= 4");

            // Stop invalid department GPA thresholds from entering the database.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_MinGpaForGraduation_Range",
                table: "Departments",
                sql: "[MinGpaForGraduation] >= 0 AND [MinGpaForGraduation] <= 4");

            // Require every department to declare a positive graduation-credit total.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Departments_TotalCreditsRequired_Positive",
                table: "Departments",
                sql: "[TotalCreditsRequired] > 0");

            // Enforce one credit-hour policy per department and standing combination.
            migrationBuilder.CreateIndex(
                name: "IX_CreditHourRules_DepartmentId_Standing",
                table: "CreditHourRules",
                columns: new[] { "DepartmentId", "Standing" },
                unique: true);

            // Prevent impossible credit ranges such as negative values or max below min.
            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditHourRules_MinCredits_MaxCredits",
                table: "CreditHourRules",
                sql: "[MinCredits] >= 0 AND [MaxCredits] >= [MinCredits]");

            // Match the SQL schema to the same course credit-hour limits enforced by the domain model.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Courses_CreditHours_Range",
                table: "Courses",
                sql: "[CreditHours] >= 1 AND [CreditHours] <= 6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the university-name uniqueness rule when rolling back the hardening migration.
            migrationBuilder.DropIndex(
                name: "IX_Universities_Name",
                table: "Universities");

            // Remove the cumulative GPA range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_StandingHistories_CumulativeGpa_Range",
                table: "StandingHistories");

            // Remove the semester GPA range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_StandingHistories_SemesterGpa_Range",
                table: "StandingHistories");

            // Remove the department-category uniqueness rule when rolling back.
            migrationBuilder.DropIndex(
                name: "IX_GraduationRequirements_DepartmentId_CategoryType",
                table: "GraduationRequirements");

            // Remove the positive-credit requirement when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_GraduationRequirements_RequiredCredits_Positive",
                table: "GraduationRequirements");

            // Remove the grade-point range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_GradeScales_PointValue_Range",
                table: "GradeScales");

            // Remove the department GPA threshold range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_MinGpaForGraduation_Range",
                table: "Departments");

            // Remove the department total-credit check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Departments_TotalCreditsRequired_Positive",
                table: "Departments");

            // Drop the unique version of the credit-hour rule index before restoring the old non-unique one.
            migrationBuilder.DropIndex(
                name: "IX_CreditHourRules_DepartmentId_Standing",
                table: "CreditHourRules");

            // Remove the credit-range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditHourRules_MinCredits_MaxCredits",
                table: "CreditHourRules");

            // Remove the course credit-hour range check when rolling back.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Courses_CreditHours_Range",
                table: "Courses");

            // Restore the original non-unique index shape from the previous migration state.
            migrationBuilder.CreateIndex(
                name: "IX_CreditHourRules_DepartmentId_Standing",
                table: "CreditHourRules",
                columns: new[] { "DepartmentId", "Standing" });
        }
    }
}
