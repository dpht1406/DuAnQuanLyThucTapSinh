-- Script tạo 505 công ty phụ vụ kiểm thử biên (TC-BOUND-02, TC-BOUND-03, TC-FILTER-05)
-- Database: StudentInternshipMgmtDb_BulkCompanyTest

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT N'Bắt đầu chèn 505 công ty phục vụ kiểm thử biên 500 records...';

DECLARE @i INT = 1;
WHILE @i <= 505
BEGIN
    INSERT INTO [Companies] ([Name], [Address], [Industry], [ContactPerson], [ContactPhone], [ContactEmail], [ContactPosition])
    VALUES (
        N'Công ty Biên Test #' + CAST(@i AS NVARCHAR(10)),
        N'Địa chỉ kiểm thử biên số ' + CAST(@i AS NVARCHAR(10)),
        N'Boundary_500_Industry',
        N'Người liên hệ #' + CAST(@i AS NVARCHAR(10)),
        N'090' + RIGHT('0000000' + CAST(@i AS NVARCHAR(10)), 7),
        N'bulk.test.' + CAST(@i AS NVARCHAR(10)) + N'@boundary-test.vn',
        N'Vị trí #' + CAST(@i AS NVARCHAR(10))
    );
    SET @i = @i + 1;
END

PRINT N'Hoàn tất chèn 505 công ty biên.';
