using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForceAdminPasswordChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "MustChangePassword",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "MustChangePassword",
                value: false);
        }
    }
}
