# Sao lưu và khôi phục SQL Server

Bộ script này tạo bản sao lưu đầy đủ, kiểm tra khả năng đọc của bản sao lưu, lưu dấu vết so sánh dữ liệu và hỗ trợ khôi phục cơ sở dữ liệu `StudentInternshipMgmtDb`. Mọi thao tác SQL đều dùng `System.Data.SqlClient` có sẵn trong Windows PowerShell 5.1, Windows Authentication và chỉ chấp nhận SQL Server trên máy local. Script đọc chuỗi kết nối từ `backend/src/StudentInternshipMgmt.Api/appsettings.json`, nhưng chuyển kết nối sang `master` cho các lệnh sao lưu và khôi phục.

Các thư mục `C:\BackupDemo\backups`, `config`, `logs` và `evidence` được tạo tự động. Chạy các lệnh trong PowerShell hoặc `powershell.exe` từ thư mục gốc repository; không cần cài `sqlcmd` hay module bổ sung. Tài khoản Windows chạy script cần kết nối được SQL Server và có quyền sao lưu/khôi phục phù hợp. Riêng dịch vụ SQL Server phải có quyền đọc/ghi vào thư mục bản sao lưu.

## Quy trình thực hiện

1. Chụp trạng thái dữ liệu ban đầu bằng lệnh:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\01-baseline.ps1
   ```

   Script ghi `baseline-before.csv`, in số dòng và checksum từng bảng người dùng, đồng thời ghi log và metric. Nếu kết nối, đường dẫn cấu hình hoặc quyền SQL chưa đúng, lệnh kết thúc với mã lỗi 1.

2. Tạo hai bản sao lưu độc lập bằng cách chạy lệnh sau hai lần:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\02-backup.ps1
   ```

   Mỗi lần chạy chụp lại baseline ngay trước khi backup, tạo file `.bak` có nén và checksum, xác minh bằng `RESTORE VERIFYONLY`, tính SHA256, thời gian, kích thước và tỷ lệ kích thước file so với dữ liệu đã cấp phát. Manifest được cập nhật tại `C:\BackupDemo\backups\manifest.json`. Script cũng tạo một gói cấu hình FULL và một gói SUBMIT, giữ tối đa bảy file backup FULL mới nhất và ghi lại các bước vào log. Kết quả thành công có mã thoát 0.

3. Kiểm tra cơ chế chọn backup dự phòng mà không khôi phục database:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\07-test-fallback.ps1
   ```

   Cần có ít nhất hai bản backup đã xác minh. Script chỉ tạo một bản sao thử nghiệm bị hỏng, gọi `06-restore-with-fallback.ps1 -VerifyOnly`, xác nhận bản hỏng bị bỏ qua rồi chuyển mẫu hỏng vào `evidence\corrupt-sample`. Bản backup gốc không bị sửa và không có dữ liệu database nào bị thay đổi.

4. Chỉ khi đã kiểm tra kỹ các file backup và sẵn sàng mô phỏng sự cố, chạy mô phỏng mất dữ liệu bằng lệnh:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\03-simulate-loss.ps1 -IUnderstand
   ```

   Trước khi xóa, script bắt buộc phải tìm được bản backup đã xác minh trong 24 giờ gần nhất và yêu cầu nhập chính xác `XOA DU LIEU`. Các thao tác xóa nằm trong một transaction; script không tắt khóa ngoại, không dùng `TRUNCATE` và không xóa tài khoản Admin. Kết quả số dòng bị xóa và snapshot sau mô phỏng được ghi ra console và `after-loss.csv`. Nếu phát hiện vòng phụ thuộc khóa ngoại hoặc lệnh xóa bị chặn, transaction được rollback.

5. Khôi phục từ bản mới nhất bằng lệnh:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\04-restore.ps1 -IUnderstand
   ```

   Có thể chỉ định file cụ thể bằng `-BackupFile "C:\BackupDemo\backups\StudentInternshipMgmtDb_FULL_yyyyMMdd_HHmmss.bak"`. Script xác minh bản được chọn, tạo một bản sao dự phòng COPY_ONLY của trạng thái database hiện tại trong `backups\pre-restore`, chuyển database sang `SINGLE_USER`, khôi phục và luôn thử đặt lại `MULTI_USER` trong `finally`. Thời gian workflow được ghi vào metrics như RTO thực tế; snapshot sau khôi phục nằm trong `after-restore.csv`.

6. Đối chiếu kết quả khôi phục với baseline bằng lệnh:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\05-verify.ps1
   ```

   `verify-result.csv` ghi kết quả theo từng bảng. PASS đòi hỏi số dòng và checksum đều khớp; checksum `N/A` không được xem là bằng chứng đầy đủ. Nếu có `after-loss.csv`, script cũng in rõ những bảng đã lệch so với baseline để chứng minh mô phỏng mất dữ liệu.

7. Khi cần tự động chọn bản hợp lệ gần nhất, có thể chỉ xem lựa chọn mà không khôi phục:

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\06-restore-with-fallback.ps1 -VerifyOnly
   ```

   Để thật sự khôi phục theo cơ chế dự phòng, phải chủ động thêm `-IUnderstand`. Script kiểm tra các bản sao từ mới đến cũ; nếu không còn bản hợp lệ, mã thoát là 2.

## Khôi phục trên máy khác

Chuyển file `.bak` đã xác minh sang máy đích qua kênh lưu trữ an toàn, kiểm tra SHA256 đối chiếu manifest và đặt file ở vị trí mà dịch vụ SQL Server máy đích có thể đọc. Tạo database cùng tên `StudentInternshipMgmtDb` nếu quy trình SQL Server tại máy đích yêu cầu, rồi dùng `04-restore.ps1 -BackupFile "<đường dẫn bản sao lưu>" -IUnderstand`. Bộ script chỉ hoạt động với SQL Server local trên chính máy đang chạy; không dùng chuỗi kết nối trỏ sang server từ xa. Nếu SQL instance tại máy mới khác cấu hình `localhost` trong appsettings, cần cấu hình môi trường triển khai phù hợp trước khi chạy, không đưa credentials vào log hoặc tham số dòng lệnh.

User Secrets không nằm trong file backup database. Gói cấu hình FULL có thể chứa `secrets.json` nếu file User Secrets tồn tại trên máy sao lưu, nhưng đây là gói nhạy cảm cần chuyển riêng qua kho bí mật được kiểm soát. Trên máy mới, tạo lại User Secrets với đúng UserSecretsId `172049aa-b3b1-4c2f-a2f3-538028103edf` và nạp lại `Jwt:SecretKey` cùng `Smtp:Password` từ nguồn bí mật được quản lý; không dùng giá trị đã thay thế trong gói SUBMIT làm bí mật hoạt động.

## Lỗi thường gặp và cách xử lý

- Nếu SQL Server báo lỗi 3201 kèm `Operating system error 5`, dịch vụ SQL Server không được quyền ghi vào thư mục backup. Script đọc tài khoản dịch vụ qua `sys.dm_server_services` và in lệnh `icacls` tương ứng để quản trị viên tự xem xét, chạy bằng quyền Administrator. Script không tự cấp quyền và sẽ dừng.
- Nếu không kết nối được SQL Server, xác nhận dịch vụ đang chạy, Windows Authentication được bật, `DefaultConnection` trỏ tới đúng database và server là local. Script từ chối database khác hoặc server từ xa.
- Nếu khôi phục báo database đang được sử dụng, thao tác `SINGLE_USER WITH ROLLBACK IMMEDIATE` sẽ ngắt session khác. Đóng ứng dụng/API và công cụ đang mở kết nối trước khi thử lại; lưu ý các session đang chạy có thể bị rollback.
- Nếu `WITH COMPRESSION` không được hỗ trợ bởi phiên bản/edition SQL Server, dùng Developer hoặc Standard tương thích như môi trường dự kiến. Không sửa lệnh để bỏ compression nếu yêu cầu bài thực hành vẫn yêu cầu nén; hãy xác minh phiên bản và edition trước.
- Nếu checksum của một bảng không hỗ trợ `BINARY_CHECKSUM(*)`, script ghi cảnh báo và lưu `N/A`, nhưng vẫn ghi số dòng. Khi đó bước verify không thể đánh dấu PASS đầy đủ cho bảng đó; cần đánh giá dữ liệu bằng phương pháp kiểm tra khác trước khi kết luận.
- Nếu database thay đổi trong lúc chạy snapshot, hãy dừng các tiến trình ứng dụng ghi dữ liệu và chụp lại baseline/backup theo đúng thứ tự. Snapshot này là dấu vết kiểm chứng, không thay thế transactionally consistent backup của SQL Server.

## Bảo mật và lưu giữ

File `.bak` chứa dữ liệu database, còn `config-backup-FULL_*.zip` có thể chứa khóa JWT, mật khẩu SMTP và cấu hình môi trường. Giới hạn quyền truy cập, mã hóa ổ đĩa hoặc kho lưu trữ, không gửi chúng qua chat/email không an toàn và không đưa gói FULL vào bài nộp. Gói SUBMIT đã che giá trị bí mật theo quy tắc trong script nhưng vẫn cần được rà soát trước khi nộp. Không chia sẻ file log hoặc manifest nếu quy định dự án xem tên máy, đường dẫn hay dấu vết hệ thống là thông tin nội bộ.

## Chỉ số kỹ thuật

| Chỉ số | Mục tiêu | Giá trị đo được |
|---|---:|---|
| RPO | 24 giờ | Điền sau khi chạy thử |
| RTO | Dưới 5 phút | Điền thời gian restore đo trong `metrics.csv` |

SEO và accessibility không áp dụng cho quy trình sao lưu này vì đây là công cụ PowerShell chạy cục bộ và không tạo giao diện web hay nội dung được lập chỉ mục.
