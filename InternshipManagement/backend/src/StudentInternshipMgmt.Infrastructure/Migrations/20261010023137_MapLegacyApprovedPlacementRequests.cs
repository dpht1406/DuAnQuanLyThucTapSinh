using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentInternshipMgmt.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapLegacyApprovedPlacementRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [pr] " +
                "SET [Status] = N'Accepted', " +
                "[ResultActorType] = N'Admin', " +
                "[ResultNote] = N'Chuyển đổi dữ liệu cũ: đơn đã duyệt theo luồng cũ.', " +
                "[ConfirmationSource] = N'Chuyển đổi dữ liệu cũ (Approved → Accepted)' " +
                "FROM [PlacementRequests] AS [pr] " +
                "INNER JOIN [Students] AS [s] ON [s].[Id] = [pr].[StudentId] " +
                "WHERE [pr].[Status] = N'Approved' " +
                "AND [s].[IsDeleted] = 0 " +
                "AND [s].[CompanyId] = [pr].[CompanyId];");

            migrationBuilder.Sql(
                "UPDATE [PlacementRequests] " +
                "SET [Status] = N'Rejected', " +
                "[ResultActorType] = N'Admin', " +
                "[ResultNote] = N'Chuyển đổi dữ liệu cũ: đơn đã duyệt nhưng sinh viên không còn gắn doanh nghiệp.', " +
                "[ConfirmationSource] = N'Chuyển đổi dữ liệu cũ (Approved → Rejected)' " +
                "WHERE [Status] = N'Approved';");

            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM [PlacementRequests] WHERE [Status] = N'Approved') " +
                "THROW 50001, N'Vẫn còn đơn Approved sau khi chuyển đổi.', 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down chỉ hoàn lại chính xác nếu các dòng này chưa bị sửa tiếp sau migration.
            migrationBuilder.Sql(
                "UPDATE [PlacementRequests] " +
                "SET [Status] = N'Approved', " +
                "[ResultActorType] = NULL, " +
                "[ResultNote] = NULL, " +
                "[ConfirmationSource] = NULL " +
                "WHERE [ConfirmationSource] IN (" +
                "N'Chuyển đổi dữ liệu cũ (Approved → Accepted)', " +
                "N'Chuyển đổi dữ liệu cũ (Approved → Rejected)');");
        }
    }
}
