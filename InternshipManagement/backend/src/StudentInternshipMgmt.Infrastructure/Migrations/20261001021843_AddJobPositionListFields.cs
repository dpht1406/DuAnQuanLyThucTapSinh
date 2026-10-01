using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPositionListFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "JobPositions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "JobPositions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobPositions",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Deadline", "Department", "Location" },
                values: new object[] { new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Công nghệ", null });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Deadline", "Department", "Location" },
                values: new object[] { new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Kiểm thử", null });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Deadline", "Department", "Location" },
                values: new object[] { new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Thiết kế", null });

            migrationBuilder.UpdateData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Deadline", "Department", "Location" },
                values: new object[] { null, "Phòng Dữ liệu", null });

            migrationBuilder.InsertData(
                table: "JobPositions",
                columns: new[] { "Id", "CompanyId", "Deadline", "Department", "Description", "IsOpen", "Location", "Quantity", "Title" },
                values: new object[,]
                {
                    { 1001, 2, new DateTime(2026, 10, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Dữ liệu", "Hỗ trợ làm sạch dữ liệu và xây dựng báo cáo trực quan.", true, "Tòa nhà Công nghệ, Quận 3, TP.HCM", 2, "Thực tập sinh Phân tích dữ liệu" },
                    { 1002, 3, null, "Phòng Thiết kế", "Tham gia thiết kế trải nghiệm và giao diện sản phẩm số.", true, null, 1, "Thực tập sinh Thiết kế sản phẩm" },
                    { 1003, 1, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Kiểm thử", "Viết và thực thi kịch bản kiểm thử cho sản phẩm.", false, null, 2, "Thực tập sinh Kiểm thử phần mềm" },
                    { 1004, 3, new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Công nghệ", "Phát triển dịch vụ web với ASP.NET Core.", true, null, 3, "Thực tập sinh Phát triển .NET" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "JobPositions",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "JobPositions");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "JobPositions");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobPositions");
        }
    }
}
