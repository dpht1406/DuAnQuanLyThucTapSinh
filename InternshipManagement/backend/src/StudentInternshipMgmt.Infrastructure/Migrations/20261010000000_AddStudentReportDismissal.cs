using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    public partial class AddStudentReportDismissal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StudentReportDismissedAt",
                table: "PlacementRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentReportDismissNote",
                table: "PlacementRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentReportDismissSource",
                table: "PlacementRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StudentReportDismissSource",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "StudentReportDismissNote",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "StudentReportDismissedAt",
                table: "PlacementRequests");
        }
    }
}
