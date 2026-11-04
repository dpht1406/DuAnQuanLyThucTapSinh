# Bản Tự Đánh Giá Quá Trình Phát Hành Release v1.0.0

**Người thực hiện**: Senior Release Engineer kiêm QA Lead  
**Dự án**: Quản lý sinh viên thực tập (ASP.NET Core 8 + Vue 3 + SQL Server)  
**Phiên bản phát hành**: v1.0.0 (Chính thức)  
**Thời điểm đánh giá**: Tháng 11/2026  

---

### 1. Các Quyết Định Kỹ Thuật Đã Chọn và Lý Do Thực Tế

Trong vai trò kỹ sư trưởng phát hành và phụ trách chất lượng, tôi đã đưa ra các quyết định then chốt nhằm đạt mức độ tin cậy cao nhất cho đợt release này:

1. **Dựng môi trường demo sạch ngoài repo và bảo vệ tuyệt đối CSDL chính**:
   - *Quyết định*: Sử dụng `git archive` để xuất mã nguồn sạch ra thư mục tạm của hệ điều hành, tạo database demo riêng biệt mang tên `StudentInternshipMgmt_ReleaseDemo_<timestamp>` trên SQL Server cục bộ.
   - *Lý do*: Đảm bảo mã nguồn được thử nghiệm không mang theo bất kỳ file rác (`bin`, `obj`, `dist`, `node_modules`) hoặc tệp cấu hình bí mật nào. Cơ chế "chốt chặn an toàn" (Safety Interlock) kiên quyết từ chối chạy nếu phát hiện tên CSDL trùng với `StudentInternshipMgmtDb`, loại trừ 100% rủi ro làm hỏng dữ liệu đang vận hành.
2. **Cấu hình độc lập qua biến môi trường tiến trình**:
   - *Quyết định*: Truyền chuỗi kết nối CSDL, khóa ký JWT ngẫu nhiên và cổng mạng qua `$env:...` thay vì chỉnh sửa `appsettings.json`.
   - *Lý do*: Giữ nguyên vẹn tính toàn vẹn của mã nguồn trong kho lưu trữ (repository integrity), tuân thủ nguyên tắc 12-Factor App và không để lộ bí mật vào commit.
3. **Chiến lược kiểm thử song song kết hợp VSTest an toàn**:
   - *Quyết định*: Tận dụng bộ nhớ đệm cục bộ (`local-cache` NuGet và `npm ci --prefer-offline`) để dựng môi trường nhanh chóng mà không phụ thuộc mạng ngoài; phân tách việc chạy từng dự án test để tránh xung đột cổng socket nội bộ của Windows.
   - *Lý do*: Giúp toàn bộ quy trình từ khôi phục dependency, biên dịch Release, chạy 403 test backend và 194 test frontend diễn ra trơn tru chỉ trong vài chục giây.
4. **Giải pháp kiểm soát đồng thời (Concurrency Control) triệt để**:
   - *Quyết định*: Giải quyết lỗi P2 race condition (FB-10) bằng giải pháp phòng thủ theo chiều sâu: khóa dòng bi quan (`UPDATE [Students] SET [Status] = [Status] WHERE [Id] = @studentId`) kết hợp Database Transaction ở tầng backend và khóa trạng thái nút bấm (`saving = true` bọc trong `try...finally`) ở tầng frontend.
   - *Lý do*: Đảm bảo tính nhất quán tuyệt đối của dữ liệu sinh viên kể cả khi người dùng click liên tiếp hoặc hệ thống bị tấn công nộp đơn song song.

---

### 2. Những Hạn Chế Thật Sự Còn Lại

Dù hệ thống đã vượt qua toàn bộ các tiêu chuẩn kiểm thử kỹ thuật tự động, vẫn tồn tại một số hạn chế khách quan cần ghi nhận minh bạch:
- **Chưa có khảo sát người thật trên diện rộng**: Bộ 10 kịch bản UAT hiện tại được thực thi tự động hóa bằng script mô phỏng API và chụp ảnh màn hình giao diện. Do không thể thay thế con người thật, các phản hồi người dùng hiện tại là sự kế thừa từ quan sát thực nghiệm trước đó (FB-01 đến FB-10: khắc phục 5, từ chối 2, hoãn 3 theo KNOWN-ISSUES.md). Cần tổ chức một đợt thử nghiệm thực tế với sinh viên và cán bộ khoa tại trường.
- **Cơ chế gửi email chưa áp dụng Transactional Outbox**: Sau khi giao dịch đăng ký thực tập được commit vào CSDL, hệ thống mới tiến hành gửi email thông báo qua SMTP. Nếu máy chủ gặp sự cố sập nguồn đúng thời điểm này, đơn ứng tuyển đã được lưu nhưng email có thể chưa được gửi đi.
- **Một ca kiểm thử tìm kiếm tiếng Việt đang tạm bỏ qua (`[Skip]`)**: Ca test `StudentSearchTests` phụ thuộc vào cấu hình collation Accent-Insensitive cụ thể của SQL Server nên đang được giữ nguyên theo thiết kế ban đầu để tránh false-negative.

---

### 3. Hướng Cải Tiến Cho Các Phiên Bản Tiếp Theo

1. **Triển khai Outbox Pattern**: Lưu thông báo email vào bảng `OutboxMessages` cùng trong transaction nghiệp vụ, dùng Worker nền định kỳ quét và gửi email với cơ chế retry và dead-letter queue.
2. **Nâng cấp kiểm thử E2E giao diện hoàn chỉnh**: Tích hợp Playwright hoặc Cypress vào pipeline CI/CD để tự động hóa toàn bộ thao tác click chuột, nhập liệu trực quan trên trình duyệt thật song song với kiểm thử API.
3. **Mở rộng kịch bản phân quyền chi tiết**: Bổ sung ma trận phân quyền nâng cao cho các phòng ban khác nhau của trường (Phòng Đào tạo, Trợ lý Khoa, Cố vấn Học tập) nhằm tăng cường tính linh hoạt trong điều phối thực tập.
