# Chỉ số kỹ thuật trước và sau

| Chỉ số | Trước | Mục tiêu | Sau |
|---|---:|---:|---:|
| Số cặp màu fail | 6 | 0 | 0 (11/11 cặp đạt) |
| Màu computed của nút “Quay lại” trên `#F6F4EE` | `#78716C` / 4.36:1 | ≥4.5:1 | `rgb(95, 101, 95)` (`#5F655F`) / 5.43:1 |
| Số thẻ `<main>` trên trang form (ví dụ `/students/new`) | 2 | 1 | 1 |
| Có skip link | Không | Có | Có |
| Số route có title riêng | 0 / 37 | Tất cả route | 37 / 37 |
| Số chỗ tắt focus (`outline: none`, `--apply-focus-ring: none`, rule `:focus` chỉ đổi border) | 1 | 0 | 0 |
| Số label mồ côi | 0 | 0 | 0 |
| Backend build | — | Pass | Pass, 0 warning / 0 error |
| Backend tests | — | Pass | 291 passed, 1 skipped, 0 failed |
| Frontend build | — | Pass | Pass |
| Frontend tests | — | Pass | 133 passed, 0 failed |
| Lighthouse Accessibility — login |  | Đo bằng Lighthouse |  |
| Lighthouse SEO — login |  | Đo bằng Lighthouse |  |
| Lighthouse Accessibility — `/companies/new` |  | Đo bằng Lighthouse |  |
| Lighthouse SEO — `/companies/new` |  | Đo bằng Lighthouse |  |

Đo lặp lại từ `InternshipManagement/frontend` bằng `npm run check:contrast` và `npm run check:labels`; output nằm trong `logs/`. Màu nút được kiểm tra trực tiếp trên `/companies/new` bằng `getComputedStyle` sau khi render PrimeVue thật; nền được lấy từ tổ tiên hiển thị đầu tiên không trong suốt. Số main được kiểm tra trên route form có layout, trước khi tối ưu là main của `MainLayout` cộng main trong `FormPage`.
