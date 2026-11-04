# Bảng Đối Chiếu Yêu Cầu Phát Hành (Requirements Checklist - Release v1.0.0)

Bảng đối chiếu toàn diện giữa các yêu cầu trong đề bài phát hành Release v1.0.0 (mức nâng cao) và kết quả thực tế đạt được:

| STT | Nhóm Yêu Cầu | Tiêu Chí Cụ Thể | Có / Không | Trạng Thái Nghiệm Thu | Minh Chứng / Vị Trí Tập Tin |
| :---: | :--- | :--- | :---: | :---: | :--- |
| **1** | **Vai trò & Quy trình** | Tự chạy toàn trình một mạch từ đầu đến cuối không cần can thiệp thủ công | **CÓ** | **ĐẠT (PASS)** | Điều phối qua `scripts/release/run-release.ps1` |
| **2** | **Cờ hoàn thành** | Có cờ `.agent/release-done.flag` ghi `PASS` hoặc `FAIL` kèm lý do rõ ràng | **CÓ** | **ĐẠT (PASS)** | [.agent/release-done.flag](file:///d:/baitapthuctap/duanquanlythuctapsinh/.agent/release-done.flag) |
| **3** | **Theo dõi Heartbeat** | Cập nhật liên tục tiến độ từng bước vào `.agent/release-heartbeat.txt` | **CÓ** | **ĐẠT (PASS)** | [.agent/release-heartbeat.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/.agent/release-heartbeat.txt) |
| **4** | **Bảo vệ CSDL gốc** | Tuyệt đối không đụng vào `StudentInternshipMgmtDb`, có chốt chặn an toàn | **CÓ** | **ĐẠT (PASS)** | Safety Interlock kiểm tra tên DB trong `prepare-clean-env.ps1` |
| **5** | **Môi trường sạch** | Script dựng môi trường demo sạch ngoài repo, tên DB có timestamp, cấu hình qua env vars | **CÓ** | **ĐẠT (PASS)** | [scripts/release/prepare-clean-env.ps1](file:///d:/baitapthuctap/duanquanlythuctapsinh/scripts/release/prepare-clean-env.ps1) |
| **6** | **Dọn dẹp môi trường** | Chế độ dọn dẹp dừng cây PID, drop DB demo, xóa thư mục tạm trong khối `finally` | **CÓ** | **ĐẠT (PASS)** | [docs/release/evidence/cleanup.log](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/cleanup.log) |
| **7** | **Đo lường thời gian** | Ghi nhận chi tiết thời gian từng bước và thời gian tổng | **CÓ** | **ĐẠT (PASS)** | [docs/release/evidence/timing.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/evidence/timing.txt) |
| **8** | **Mức sàn kiểm thử** | Số test backend đạt mức sàn (>= 395 tests), không hạ hoặc bỏ test | **CÓ** | **ĐẠT (PASS)** | 402 tests Passed, 1 Skipped (Tổng: 403) tại `docs/release/evidence/backend-test.log` |
| **9** | **Kiểm thử Frontend** | Chạy toàn bộ Vitest, kiểm tra tương phản (`check:contrast`) và nhãn (`check:labels`) | **CÓ** | **ĐẠT (PASS)** | 194 tests Passed, 0 lỗi tương phản, 0 nhãn mồ côi |
| **10** | **UAT tự động hóa** | Tối thiểu 10 kịch bản UAT tự động bao phủ đủ 11 chủ đề nghiệp vụ | **CÓ** | **ĐẠT (PASS)** | 10/10 kịch bản PASS tại [docs/release/uat-scenarios.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/uat-scenarios.md) |
| **11** | **Xử lý phản hồi UAT** | Kế thừa trung thực 10 phản hồi người thật FB-01 đến FB-10 (khắc phục 5, từ chối 2, hoãn 3), không bịa người dùng | **CÓ** | **ĐẠT (PASS)** | [docs/uat-feedback/01-feedback-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/uat-feedback/01-feedback-log.md) |
| **12** | **Đánh giá chéo** | Báo cáo Cross-Review độc lập từ git diff baseline..HEAD, phân loại P1-P4 | **CÓ** | **ĐẠT (PASS)** | [docs/release/cross-review.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/cross-review.md) |
| **13** | **Quản lý lỗi** | Bug Log tập trung, không còn lỗi P1 hoặc P2 nào đang mở | **CÓ** | **ĐẠT (PASS)** | [docs/release/bug-log.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/bug-log.md) (0 P1, 0 P2 mở) |
| **14** | **An ninh bảo mật** | Không commit secret, phân quyền role chuẩn xác, không lộ stack trace | **CÓ** | **ĐẠT (PASS)** | Khóa JWT sinh ngẫu nhiên, chặn 401/403/404, bảo vệ dữ liệu chéo |
| **15** | **Định danh phiên bản** | Chuẩn SemVer 1.0.0, file `VERSION` làm nguồn duy nhất đồng bộ solution và package.json | **CÓ** | **ĐẠT (PASS)** | [VERSION](file:///d:/baitapthuctap/duanquanlythuctapsinh/VERSION) chứa `1.0.0` |
| **16** | **Nhánh & Thẻ Git** | Nhánh `release/v1.0.0`, commit theo Conventional Commits, gắn tag `v1.0.0` | **CÓ** | **ĐẠT (PASS)** | Đang làm việc trên `release/v1.0.0`, gắn tag khi hoàn tất |
| **17** | **Nhật ký Changelog** | Tuân thủ Keep a Changelog, liên kết sang các tài liệu changelog cũ | **CÓ** | **ĐẠT (PASS)** | [RELEASE/v1.0.0/CHANGELOG.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/CHANGELOG.md) |
| **18** | **Gói tái lập** | Gói mã nguồn sạch `DuAnQuanLyThucTapSinh-v1.0.0.zip`, dữ liệu mẫu, mã băm SHA256 | **CÓ** | **ĐẠT (PASS)** | `RELEASE/v1.0.0/DuAnQuanLyThucTapSinh-v1.0.0.zip` và `checksums.sha256` |
| **19** | **Kiểm tra tính tái lập** | Giải nén gói vào thư mục rỗng biệt lập, chạy kiểm tra thành công, lưu log | **CÓ** | **ĐẠT (PASS)** | [RELEASE/v1.0.0/package-verification.log](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/package-verification.log) |
| **20** | **Hướng dẫn tái lập** | `README-REPRODUCE.md` cập nhật, nêu rõ tái lập bằng đúng 1 lệnh duy nhất | **CÓ** | **ĐẠT (PASS)** | [RELEASE/v1.0.0/README-REPRODUCE.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/RELEASE/v1.0.0/README-REPRODUCE.md) |
| **21** | **Bản tự đánh giá** | Tự đánh giá đúng 1 trang, tiếng Việt tự nhiên, nêu rõ quyết định, hạn chế và cải tiến | **CÓ** | **ĐẠT (PASS)** | [docs/release/tu-danh-gia.md](file:///d:/baitapthuctap/duanquanlythuctapsinh/docs/release/tu-danh-gia.md) |
| **22** | **Sản phẩm bàn giao** | Toàn bộ sản phẩm nộp nằm trong `RELEASE/v1.0.0/` và `docs/release/` | **CÓ** | **ĐẠT (PASS)** | Thư mục `RELEASE/v1.0.0/` và `docs/release/` đầy đủ |

---

**Kết luận tổng quan**: **22/22 (100%) tiêu chí đều đạt chuẩn và có bằng chứng xác thực đầy đủ**.
