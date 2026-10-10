using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPositionApprovalStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalNote",
                table: "JobPositions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "JobPositions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Approved");

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1001,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1002,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1003,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1004,
                columns: new[] { "ApprovalNote", "ApprovalStatus" },
                values: new object[] { null, "Approved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalNote",
                table: "JobPositions");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "JobPositions");
        }
    }
}
