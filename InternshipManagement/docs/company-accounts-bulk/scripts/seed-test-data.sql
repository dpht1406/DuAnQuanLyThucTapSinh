-- Script tạo dữ liệu mẫu cho kiểm thử chức năng Tạo tài khoản doanh nghiệp hàng loạt
-- Database: StudentInternshipMgmtDb_BulkCompanyTest

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT N'Bắt đầu chèn dữ liệu mẫu cho kiểm thử Tạo tài khoản DN hàng loạt...';

-- 1. Tạo các công ty hợp lệ chưa có tài khoản (đủ các ngành, tên tiếng Việt, có dấu phẩy, nháy kép, ký tự công thức)
INSERT INTO [Companies] ([Name], [Address], [Industry], [ContactPerson], [ContactPhone], [ContactEmail], [ContactPosition])
VALUES
-- Hợp lệ 1: CNTT
(N'Công ty TNHH Giải pháp Phần mềm FPT Test', N'Tòa nhà FPT, Cầu Giấy, Hà Nội', N'Công nghệ thông tin', N'Nguyễn Văn An', N'0901234561', N'an.nguyen@fpt-test.vn', N'Trưởng phòng HR'),

-- Hợp lệ 2: Ngân hàng / Tài chính
(N'Ngân hàng TMCP Ngoại Thương Chi nhánh Test', N'198 Trần Quang Khải, Hoàn Kiếm, Hà Nội', N'Tài chính - Ngân hàng', N'Trần Thị Bích', N'0901234562', N'bich.tran@bank-test.vn', N'Giám đốc Nhân sự'),

-- Hợp lệ 3: Viễn thông - Tên có dấu phẩy và nháy kép, tiếng Việt phong phú
(N'Công ty Cổ phần "Truyền Thông, Công Nghệ & Dịch Vụ" Số 1', N'123 Nguyễn Huệ, Quận 1, TP.HCM', N'Viễn thông', N'Lê Hoàng Long', N'0901234563', N'long.le@telecom-commas.vn', N'Phó Giám đốc'),

-- Hợp lệ 4: Tên bắt đầu bằng ký tự formula (=, +, -, @) để kiểm tra chống CSV Injection
(N'=SUM(1+1) Phân Tích Dữ Liệu & AI Corp', N'456 Lê Duẩn, Đà Nẵng', N'Công nghệ thông tin', N'Phạm Minh Cường', N'0901234564', N'cuong.pm@data-formula.vn', N'Team Lead'),

-- Hợp lệ 5: Cơ khí - Chế tạo
(N'Tập đoàn Cơ khí Chính xác & Tự động hóa Á Châu', N'KCN Biên Hòa 2, Đồng Nai', N'Cơ khí - Tự động hóa', N'Hoàng Trọng Đức', N'0901234565', N'duc.hoang@automation-test.vn', N'Quản lý Đào tạo'),

-- 2. Công ty ĐÃ CÓ TÀI KHOẢN (sẽ liên kết User ở bước sau)
(N'Công ty Cổ phần Đã Có Tài Khoản VIP', N'789 Cách Mạng Tháng 8, TP.HCM', N'Công nghệ thông tin', N'Vũ Quốc Toàn', N'0901234566', N'toan.vu@already-account.vn', N'HR Manager'),

-- 3. Công ty THIẾU EMAIL (ContactEmail là NULL hoặc rỗng)
(N'Công ty TNHH Thiếu Email Liên Hệ 1', N'101 Võ Văn Tần, Quận 3, TP.HCM', N'Thương mại điện tử', N'Đỗ Mai Lan', N'0901234567', NULL, N'Phụ trách thực tập'),
(N'Công ty TNHH Thiếu Email Rỗng 2', N'102 Võ Văn Tần, Quận 3, TP.HCM', N'Thương mại điện tử', N'Đỗ Mai Hương', N'0901234568', N'', N'Phụ trách tuyển dụng'),

-- 4. Công ty EMAIL SAI ĐỊNH DẠNG
(N'Công ty TNHH Email Không Có At', N'202 Nguyễn Trãi, Thanh Xuân, Hà Nội', N'Giáo dục - Đào tạo', N'Bùi Thanh Hằng', N'0901234569', N'invalid-email-without-at.com', N'Chuyên viên Nhân sự'),
(N'Công ty Cổ phần Email Sai Ký Tự', N'203 Nguyễn Trãi, Thanh Xuân, Hà Nội', N'Giáo dục - Đào tạo', N'Ngô Gia Bảo', N'0901234570', N'invalid..email@@domain..com', N'Tuyển dụng');

-- Lấy Id của Công ty Đã Có Tài Khoản và tạo User Role Company cho nó
DECLARE @ExistingCompanyId INT;
SELECT @ExistingCompanyId = Id FROM [Companies] WHERE [ContactEmail] = N'toan.vu@already-account.vn';

IF @ExistingCompanyId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [Users] WHERE [CompanyId] = @ExistingCompanyId)
BEGIN
    INSERT INTO [Users] ([Username], [PasswordHash], [Role], [IsActive], [MustChangePassword], [CreatedAt], [CompanyId])
    VALUES (
        N'cty' + RIGHT('0000' + CAST(@ExistingCompanyId AS VARCHAR(10)), 4),
        N'$2a$11$eAcc0untHashDummYBCryptStringForTest123456789012345678901234', -- BCrypt hash dummy
        N'Company',
        1,
        0,
        GETUTCDATE(),
        @ExistingCompanyId
    );
    PRINT N'Đã liên kết tài khoản Company cho công ty Id: ' + CAST(@ExistingCompanyId AS VARCHAR(10));
END

PRINT N'Hoàn tất nạp dữ liệu mẫu ban đầu.';
