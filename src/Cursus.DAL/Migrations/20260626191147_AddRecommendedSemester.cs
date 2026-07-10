using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cursus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendedSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecommendedSemester",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Courses_RecommendedSemester_Range",
                table: "Courses",
                sql: "[RecommendedSemester] IS NULL OR ([RecommendedSemester] >= 1 AND [RecommendedSemester] <= 8)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Courses_RecommendedSemester_Range",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "RecommendedSemester",
                table: "Courses");
        }
    }
}
