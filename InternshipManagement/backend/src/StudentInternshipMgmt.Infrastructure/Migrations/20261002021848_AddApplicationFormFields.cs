using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicantEmail",
                table: "PlacementRequests",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantFullName",
                table: "PlacementRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantMajor",
                table: "PlacementRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantPhone",
                table: "PlacementRequests",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicantSchool",
                table: "PlacementRequests",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverLetter",
                table: "PlacementRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvUrl",
                table: "PlacementRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantEmail",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ApplicantFullName",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ApplicantMajor",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ApplicantPhone",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ApplicantSchool",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "CoverLetter",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "CvUrl",
                table: "PlacementRequests");
        }
    }
}
