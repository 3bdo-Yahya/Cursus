using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cursus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPlannedCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlannedCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedCourses_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedCourses_CourseId",
                table: "PlannedCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedCourses_StudentId_AcademicYear_Semester",
                table: "PlannedCourses",
                columns: new[] { "StudentId", "AcademicYear", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedCourses_StudentId_CourseId_AcademicYear_Semester",
                table: "PlannedCourses",
                columns: new[] { "StudentId", "CourseId", "AcademicYear", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedCourses");
        }
    }
}
