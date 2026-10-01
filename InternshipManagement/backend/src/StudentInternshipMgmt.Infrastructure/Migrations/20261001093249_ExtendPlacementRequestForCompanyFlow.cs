using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPlacementRequestForCompanyFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChangedByType",
                table: "StatusHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationSource",
                table: "PlacementRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueAt",
                table: "PlacementRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewAt",
                table: "PlacementRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewLocation",
                table: "PlacementRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewNote",
                table: "PlacementRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultActorType",
                table: "PlacementRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultNote",
                table: "PlacementRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentInterviewNote",
                table: "PlacementRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StudentReportedInterviewAt",
                table: "PlacementRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [sh] " +
                "SET [ChangedByType] = CASE " +
                "WHEN [u].[Id] IS NULL THEN N'System' " +
                "WHEN [u].[Role] = N'Admin' THEN N'Admin' " +
                "WHEN [u].[Role] = N'User' THEN N'Student' " +
                "ELSE N'System' END " +
                "FROM [StatusHistories] AS [sh] " +
                "LEFT JOIN [Users] AS [u] ON [sh].[ChangedBy] = [u].[Id];");

            migrationBuilder.Sql(
                "UPDATE [PlacementRequests] " +
                "SET [DueAt] = DATEADD(day, 7, SYSUTCDATETIME()) " +
                "WHERE [Status] = N'Pending';");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedByType",
                table: "StatusHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedByType",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "ConfirmationSource",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "DueAt",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "InterviewAt",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "InterviewLocation",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "InterviewNote",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ResultActorType",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "ResultNote",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "StudentInterviewNote",
                table: "PlacementRequests");

            migrationBuilder.DropColumn(
                name: "StudentReportedInterviewAt",
                table: "PlacementRequests");
        }
    }
}
