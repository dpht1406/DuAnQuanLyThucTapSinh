# Báo cáo bộ script sao lưu và khôi phục SQL Server

**Thời điểm thực hiện:** 27/10/2026  
**Cơ sở dữ liệu:** `StudentInternshipMgmtDb`  
**Thư mục kết quả:** `C:\BackupDemo`

## 1. Phạm vi công việc

Đã tạo bộ script PowerShell 5.1 để chụp baseline, sao lưu và xác minh SQL Server, mô phỏng mất dữ liệu có chốt an toàn, khôi phục, đối chiếu dữ liệu và kiểm thử chọn bản sao lưu dự phòng. Script kết nối bằng `System.Data.SqlClient`, Windows Authentication, kiểm tra database và SQL Server local, ghi log/metrics và không in giá trị bí mật.

Không chỉnh sửa mã nguồn ứng dụng, migration hoặc các tệp `appsettings*.json`. Không chèn hay sửa dữ liệu mẫu trong cơ sở dữ liệu.

## 2. Tệp đã tạo

| Tệp | Mục đích |
|---|---|
| `HUONG-DAN.md` | Hướng dẫn môi trường, thứ tự chạy, khôi phục trên máy khác, xử lý lỗi và lưu ý bảo mật. |
| `scripts/00-common.ps1` | Cấu hình dùng chung, kết nối SQL, log, snapshot, xác minh backup, manifest và metrics. |
| `scripts/01-baseline.ps1` | Chụp baseline dữ liệu trước sao lưu. |
| `scripts/02-backup.ps1` | Tạo backup nén, xác minh, hash, hai gói cấu hình và manifest. |
| `scripts/03-simulate-loss.ps1` | Mô phỏng xóa dữ liệu theo transaction với các chốt xác nhận. |
| `scripts/04-restore.ps1` | Xác minh và khôi phục backup; lưu bản COPY_ONLY trước khôi phục. |
| `scripts/05-verify.ps1` | Đối chiếu baseline với dữ liệu sau khôi phục và sau mô phỏng mất dữ liệu. |
| `scripts/06-restore-with-fallback.ps1` | Chọn backup hợp lệ gần nhất hoặc khôi phục theo lựa chọn đó. |
| `scripts/07-test-fallback.ps1` | Kiểm tra fallback bằng một bản sao backup bị làm hỏng, không khôi phục database. |

Cả tám tệp `.ps1` được lưu UTF-8 có BOM và đã kiểm tra cú pháp bằng PowerShell parser.

## 3. Kết quả chạy thử an toàn

| Script | Kết quả cuối cùng | Thời gian | Kích thước / chi tiết |
|---|---|---:|---|
| `01-baseline.ps1` | PASS | 0,977 giây | Snapshot 10 bảng; database 16.777.216 byte theo `sp_spaceused`. |
| `02-backup.ps1`, lần 1 | PASS | 1,144 giây tổng; backup 0,164 giây | 1.117.696 byte; xác minh 0,051 giây. |
| `02-backup.ps1`, lần 2 | PASS | 1,303 giây tổng; backup 0,211 giây | 1.117.696 byte; xác minh 0,050 giây. |
| `07-test-fallback.ps1`, lần chạy cuối | PASS | 2,605 giây | Bỏ qua bản thử nghiệm bị hỏng và chọn backup hợp lệ mới nhất. |

Hai bản sao lưu còn trong `C:\BackupDemo\backups`:

- `StudentInternshipMgmtDb_FULL_20261027_090332.bak` — 1.117.696 byte.
- `StudentInternshipMgmtDb_FULL_20261027_090355.bak` — 1.117.696 byte.

Tỷ lệ kích thước backup so với dữ liệu đã cấp phát được ghi nhận là 14,44%. Hash SHA256 của backup và các gói cấu hình khớp với manifest. Gói cấu hình SUBMIT đã được kiểm tra để xác nhận bí mật được che; gói FULL có chứa bản sao `secrets.json` gốc và cần được giữ riêng. Log đã được rà soát để bảo đảm không chứa các giá trị bí mật đã kiểm tra.

Baseline cuối cùng nằm tại `C:\BackupDemo\evidence\baseline-before.csv`. Bản backup lỗi dùng trong bài kiểm thử fallback đã được chuyển vào `C:\BackupDemo\evidence\corrupt-sample`; các mẫu thừa từ những lần kiểm thử trước đã được dọn, chỉ giữ mẫu của lần chạy cuối.

## 4. Lỗi gặp phải và cách xử lý

1. Lần chạy baseline đầu tiên thất bại với thông báo `Keyword not supported: 'InitialCatalog'.` Nguyên nhân là cách đặt thuộc tính connection string trong PowerShell. Đã điều chỉnh cách thiết lập catalog `master`, sau đó baseline chạy thành công.
2. Lần kiểm thử fallback đầu tiên chọn nhầm bản backup hỏng vì lỗi SQL trong `InfoMessage` chưa được ném ra như exception. Đã điều chỉnh xử lý sự kiện và chạy lại thành công; bản hỏng được bỏ qua.
3. Có một lần kiểm tra gói ZIP bị lỗi do phép so khớp đường dẫn ZIP trong lệnh kiểm tra. Đây là lỗi của lệnh xác minh phụ, không phải lỗi tạo gói. Sau khi đọc đúng entry ZIP, hash gói và nội dung che bí mật đã được xác nhận.

Không gặp lỗi quyền SQL Server hoặc lỗi quyền ghi thư mục backup. Không tự cấp quyền hệ thống.

## 5. Các bước chưa thực hiện

Theo yêu cầu an toàn, **không chạy** `03-simulate-loss.ps1`, `04-restore.ps1` hoặc `06-restore-with-fallback.ps1` ở chế độ khôi phục. Vì vậy, chưa có `after-loss.csv`, `after-restore.csv`, chưa đo RTO khôi phục thực tế và chưa chạy đối chiếu cuối cùng bằng `05-verify.ps1`.

Khi sẵn sàng tự thực hiện, mở PowerShell tại thư mục gốc repository và chạy lần lượt:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\03-simulate-loss.ps1 -IUnderstand
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\04-restore.ps1 -IUnderstand
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\InternshipManagement\docs\backup-restore\scripts\05-verify.ps1
```

Script mô phỏng mất dữ liệu sẽ còn yêu cầu nhập chính xác `XOA DU LIEU`. Lệnh khôi phục sẽ ghi đè database đích sau khi xác minh backup và tạo bản COPY_ONLY dự phòng; chỉ chạy khi đã sẵn sàng thực hiện khôi phục.

## 6. Chỉ số kỹ thuật

| Chỉ số | Mục tiêu | Giá trị đo được |
|---|---:|---|
| RPO | 24 giờ | Chưa đo trong tình huống khôi phục thực tế. |
| RTO | Dưới 5 phút | Chưa đo; metrics sẽ được ghi khi chạy quy trình khôi phục. |
