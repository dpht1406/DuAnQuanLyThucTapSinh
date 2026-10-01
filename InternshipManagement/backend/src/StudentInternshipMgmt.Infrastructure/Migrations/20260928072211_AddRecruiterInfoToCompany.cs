using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterInfoToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Companies",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPosition",
                table: "Companies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ContactPosition",
                table: "Companies");
        }
    }
}
