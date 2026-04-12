using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cursus.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDomainSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSemester",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStanding",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StandingHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SemesterGpa = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    CumulativeGpa = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
                    Standing = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandingHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandingHistories_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Universities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UniversityId = table.Column<int>(type: "int", nullable: false),
                    TotalCreditsRequired = table.Column<int>(type: "int", nullable: false),
                    MinGpaForGraduation = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeScales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniversityId = table.Column<int>(type: "int", nullable: false),
                    LetterGrade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    PointValue = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeScales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeScales_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreditHours = table.Column<int>(type: "int", nullable: false),
                    CourseType = table.Column<int>(type: "int", nullable: false),
                    SemesterAvailability = table.Column<int>(type: "int", nullable: false),
                    PassingGradeThreshold = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditHourRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Standing = table.Column<int>(type: "int", nullable: false),
                    MinCredits = table.Column<int>(type: "int", nullable: false),
                    MaxCredits = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditHourRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditHourRules_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GraduationRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CategoryType = table.Column<int>(type: "int", nullable: false),
                    RequiredCredits = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraduationRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GraduationRequirements_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    PrerequisiteId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => new { x.CourseId, x.PrerequisiteId });
                    table.CheckConstraint("CK_CoursePrerequisite_CourseId_PrerequisiteId", "[CourseId] <> [PrerequisiteId]");
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_PrerequisiteId",
                        column: x => x.PrerequisiteId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentCourses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Semester = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCourses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCourses_AspNetUsers_StudentId",
                        column: x => x.StudentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GraduationRequirementCourses",
                columns: table => new
                {
                    GraduationRequirementId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraduationRequirementCourses", x => new { x.GraduationRequirementId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_GraduationRequirementCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraduationRequirementCourses_GraduationRequirements_GraduationRequirementId",
                        column: x => x.GraduationRequirementId,
                        principalTable: "GraduationRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId_AcademicYear_CurrentSemester",
                table: "AspNetUsers",
                columns: new[] { "DepartmentId", "AcademicYear", "CurrentSemester" });

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_PrerequisiteId",
                table: "CoursePrerequisites",
                column: "PrerequisiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId_Code",
                table: "Courses",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditHourRules_DepartmentId",
                table: "CreditHourRules",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditHourRules_DepartmentId_Standing",
                table: "CreditHourRules",
                columns: new[] { "DepartmentId", "Standing" });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UniversityId",
                table: "Departments",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UniversityId_Name",
                table: "Departments",
                columns: new[] { "UniversityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeScales_UniversityId_LetterGrade",
                table: "GradeScales",
                columns: new[] { "UniversityId", "LetterGrade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraduationRequirementCourses_CourseId",
                table: "GraduationRequirementCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_GraduationRequirements_DepartmentId",
                table: "GraduationRequirements",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StandingHistories_StudentId_AcademicYear",
                table: "StandingHistories",
                columns: new[] { "StudentId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_StandingHistories_StudentId_Semester_AcademicYear",
                table: "StandingHistories",
                columns: new[] { "StudentId", "Semester", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_CourseId",
                table: "StudentCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_StudentId_AcademicYear_Semester",
                table: "StudentCourses",
                columns: new[] { "StudentId", "AcademicYear", "Semester" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCourses_StudentId_CourseId_AcademicYear_Semester",
                table: "StudentCourses",
                columns: new[] { "StudentId", "CourseId", "AcademicYear", "Semester" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "CreditHourRules");

            migrationBuilder.DropTable(
                name: "GradeScales");

            migrationBuilder.DropTable(
                name: "GraduationRequirementCourses");

            migrationBuilder.DropTable(
                name: "StandingHistories");

            migrationBuilder.DropTable(
                name: "StudentCourses");

            migrationBuilder.DropTable(
                name: "GraduationRequirements");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Universities");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartmentId_AcademicYear_CurrentSemester",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentSemester",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CurrentStanding",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AspNetUsers");
        }
    }
}
