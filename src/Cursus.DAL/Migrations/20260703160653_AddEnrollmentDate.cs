using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cursus.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnrollmentDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrollmentDate",
                table: "AspNetUsers");
        }
    }
}
