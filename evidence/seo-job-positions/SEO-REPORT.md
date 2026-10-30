# BÁO CÁO ĐO BASELINE SEO & ACCESSIBILITY (TRƯỚC KHI TỐI ƯU)
**Trang mục tiêu:** Vị trí thực tập (Route `/job-positions`)  
**Dự án:** Quản lý sinh viên thực tập (`InternshipManagement`)  
**Vai trò:** Senior QA / SEO Engineer  
**Thời điểm thực hiện:** 26/10/2026 09:30:00 (GMT+7)  
**Quy tắc tuân thủ:** Đo baseline nguyên trạng, **KHÔNG chỉnh sửa bất kỳ file source nào**, mọi số liệu đều có bằng chứng tái lập được.

---

## 1. MÔI TRƯỜNG & CÁCH TÁI LẬP (REPRODUCIBILITY)

### 1.1. Thông số môi trường đo
- **Hệ điều hành:** Windows 11 Pro (x64)
- **Node.js:** `v22.19.0`
- **npm:** `10.9.3`
- **.NET SDK / Runtime:** `9.0.308` (Hosting backend ASP.NET Core 8 Web API)
- **Trình duyệt kiểm thử:** Google Chrome `155.0.8059.86` (Official Build, x64)
- **Công cụ đo tự động:** 
  - Puppeteer Core `24.x` + Lighthouse `12.x` (Programmatic User Flow API)
  - PrimeVue Contrast Check (`scripts/check-contrast.mjs`) & Label Check (`scripts/check-labels.mjs`)
- **URLs kiểm thử:**
  - Frontend: `http://localhost:5173` (Vite 8.3.0)
  - Backend API: `http://localhost:5018/api` (Kestrel ASP.NET Core 8)
- **Tài khoản kiểm thử:**
  - Sinh viên / User: `PI02099` (Xác thực JWT bearer)
  - Quản trị viên / Admin: `admin` (Xác thực JWT bearer)

### 1.2. Các bước tái lập kết quả
1. Khởi động backend API tại thư mục `InternshipManagement/backend`:
   ```powershell
   dotnet run --project src/StudentInternshipMgmt.Api
   ```
   *Kiểm tra cổng 5018 phản hồi Swagger: `http://localhost:5018/swagger/v1/swagger.json`.*
2. Khởi động frontend tại thư mục `InternshipManagement/frontend`:
   ```powershell
   npm run dev
   ```
   *Kiểm tra giao diện Vite sẵn sàng tại `http://localhost:5173`.*
3. Chạy kiểm tra tương phản màu có sẵn trong source:
   ```powershell
   npm run check:contrast
   ```
4. Thực thi script đo baseline và thu thập chứng cứ trong thư mục `evidence/`:
   ```powershell
   node evidence/capture-evidence.mjs
   ```
   *Toàn bộ kết quả HTML, JSON và ảnh chụp màn hình được lưu trữ độc lập trong thư mục `evidence/seo-job-positions/`.*

---

## 2. BẢNG ĐIỂM BASELINE LIGHTHOUSE

Các phép đo Lighthouse được thực hiện trên chính phiên làm việc đã xác thực (`localStorage` chứa `accessToken` hợp lệ), truy cập vào trang `/job-positions` sau khi toàn bộ dữ liệu 12 vị trí thực tập đã render hoàn tất.

### 2.1. Tổng hợp điểm số (0 - 100)

| Danh mục kiểm thử | User Desktop | User Mobile | Admin Desktop | Ghi chú & Đánh giá QA |
| :--- | :---: | :---: | :---: | :--- |
| **SEO** | **100** | **100** | **100** | Đạt 100 điểm công cụ (nhưng có rủi ro kiến trúc SPA) |
| **Accessibility (A11y)** | **100** | **100** | **100** | Đạt điểm tối đa trên desktop; mobile có 1 cảnh báo |
| **Best Practices** | **100** | **100** | **96** | Admin bị 96 do console network warning |
| **Performance** | **33** | **32** | **25** | Môi trường Dev Server unbundled Vite + slow 4G throttling |

### 2.2. Chỉ số Core Web Vitals & Tải trang

| Chỉ số kỹ thuật | User Desktop | User Mobile | Admin Desktop | Ngưỡng Google khuyến nghị |
| :--- | :---: | :---: | :---: | :---: |
| **Largest Contentful Paint (LCP)** | 43.7 s | 43.3 s | 44.3 s | $\le$ 2.5 s (Tốt) |
| **First Contentful Paint (FCP)** | 16.4 s | 16.4 s | 16.5 s | $\le$ 1.8 s (Tốt) |
| **Total Blocking Time (TBT)** | 540 ms | 1,110 ms | 2,270 ms | $\le$ 200 ms (Tốt) |
| **Cumulative Layout Shift (CLS)** | **0.001** | **0.000** | **0.001** | $\le$ 0.1 (Tốt - ĐẠT RẤT TỐT) |
| **Speed Index (SI)** | 16.4 s | 16.4 s | 16.5 s | $\le$ 3.4 s (Tốt) |

> [!NOTE]
> Chỉ số LCP, FCP và TBT cao là đặc thù khi chạy Lighthouse trên **Vite Dev Server** (mã nguồn chưa qua bundling/minification, hàng trăm file ES module riêng lẻ tải qua giả lập mạng hạn chế Slow 4G / 4x CPU slowdown). Tuy nhiên, chỉ số **CLS = 0.000 - 0.001** chứng minh bố cục trang cực kỳ ổn định, không bị giật/nhảy layout khi nạp danh sách.

### 2.3. Danh sách Audit bị Fail / Warning từ công cụ
1. **Lighthouse Mobile - Accessibility:**
   - Audit: `label-content-name-mismatch` (Elements with visible text labels do not have matching accessible names).
   - Phần tử vi phạm: `header.mobile-header > button.mobile-avatar` (`aria-label="Tài khoản của tôi"`, nhưng hiển thị text `"P\nPI02099"`).
2. **Lighthouse Mobile/Desktop - Performance & Diagnostics:**
   - `total-byte-weight`: Tổng dung lượng tải lớn (~7,163 KiB do dev mode chưa nén).
   - `unminified-javascript`: Chưa minify JS (tiết kiệm ước tính ~5,270 KiB khi build production).
   - `unused-javascript`: Chưa tree-shake (~780 KiB JS chưa dùng).
3. **Lighthouse - Agentic Browsing / SEO Emerging:**
   - `llms-txt`: Trang chưa có file Markdown `llms.txt` cho các bot AI/LLM.
   - `ard-schema`: Chưa có manifest `ai-catalog.json`.

---

## 3. BẢNG CHECKLIST SEO & ACCESSIBILITY CHO TRANG `/job-positions`

Mỗi tiêu chí được thẩm định trực tiếp trên mã nguồn DOM kết xuất, luồng mạng và so sánh với HTML thô từ máy chủ.

| STT | Tiêu chuẩn kiểm tra | Kết quả | Mức độ | Bằng chứng tái lập | Phân tích chi tiết của Senior QA/SEO |
| :---: | :--- | :---: | :---: | :---: | :--- |
| **I** | **HEAD & METADATA** | | | | |
| 1 | Thẻ `<title>` theo route | **PASS** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Render đúng `Danh sách vị trí thực tập \| QLTT` (32 ký tự, nằm trong dải 30-60 ký tự). Gán động chuẩn qua `pageTitle.js`. |
| 2 | Meta Description riêng | **FAIL** | **Trung bình** | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Trang dùng chung description từ `index.html` (75 ký tự). Không có thẻ description riêng cho từng route việc làm. |
| 3 | `<html lang="vi">` | **PASS** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Khai báo ngôn ngữ tiếng Việt chính xác trong thẻ `<html>`. |
| 4 | Charset & Viewport | **PASS** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | `<meta charset="UTF-8">` và `<meta name="viewport" content="width=device-width, initial-scale=1.0">` đầy đủ. |
| 5 | Thẻ Canonical URL | **FAIL** | **Trung bình** | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Hoàn toàn thiếu `<link rel="canonical" href="...">`, dễ dẫn đến trùng lặp nội dung khi URL có query params (`?search=...&availability=...`). |
| 6 | Thẻ Meta Robots | **FAIL** | **Trung bình** | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Thiếu thẻ `<meta name="robots">`. Vì trang yêu cầu đăng nhập, chuẩn SEO khuyến nghị bắt buộc phải có `noindex, nofollow` để tránh lãng phí crawl budget. |
| 7 | Open Graph & Twitter Cards | **FAIL** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Thiếu toàn bộ các thẻ mạng xã hội (`og:title`, `og:description`, `og:image`, `twitter:card`). |
| 8 | Favicon | **PASS** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Có `<link rel="icon" type="image/svg+xml" href="/favicon.svg">`, file tồn tại và trả về 200 OK. |
| 9 | `robots.txt` | **FAIL** | **Trung bình** | [checklist-evidence.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/checklist-evidence.json) | File tồn tại nhưng chứa `Allow: /` cho phép crawl toàn bộ các trang nội bộ cần đăng nhập thay vì chặn. |
| 10 | `sitemap.xml` | **FAIL** | **Cao** | [checklist-evidence.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/checklist-evidence.json) | Không có sitemap.xml. Server Vite SPA fallback trả về `index.html` (Content-Type: `text/html`, 200 OK), gây lỗi Soft 404 / Invalid XML trong GSC. |
| 11 | Structured Data (JSON-LD) | **N/A** | Thấp | [05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png) | Không có schema `JobPosting`. Vì trang bị khóa sau login wall, bot không thể index schema; chỉ có ý nghĩa nếu có trang tin công khai. |
| **II**| **NỘI DUNG & CẤU TRÚC** | | | | |
| 12 | Số lượng thẻ `<h1>` | **PASS** | Thấp | [07-heading-outline.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/07-heading-outline.png) | Duy nhất 1 thẻ `<h1>Vị trí thực tập</h1>`, thể hiện chính xác chủ đề trang. |
| 13 | Cấu trúc phân cấp Heading | **PASS** | Thấp | [07-heading-outline.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/07-heading-outline.png) | Thứ tự: H1 → H2 (Card vị trí) → H3 (Mô tả chi tiết trong Drawer). Không bị nhảy cấp heading. |
| 14 | Semantic Landmarks | **PASS** | Thấp | [04-page-job-positions-full.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/04-page-job-positions-full.png) | Có đúng 1 thẻ `<main id="main-content">`, các thẻ `<header>` và `<nav>` phân cấp rõ ràng. |
| 15 | Crawlability thẻ Card vị trí | **FAIL** | **Cao** | [04-page-job-positions-full.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/04-page-job-positions-full.png) | Tiêu đề card KHÔNG phải liên kết `<a href>`. Nút xem chi tiết dùng `<button @click>`, bot tìm kiếm không thể crawl hoặc lập chỉ mục từng tin tuyển dụng. |
| 16 | Anchor text & nhãn nút | **PASS** | Thấp | [04-page-job-positions-full.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/04-page-job-positions-full.png) | Các nút có `aria-label` mô tả rõ ngữ cảnh (vd: `Xem chi tiết Thực tập sinh .NET`), không dùng nhãn chung chung. |
| 17 | Thẻ Alt & Icon SVG/Font | **PASS** | Thấp | [04-page-job-positions-full.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/04-page-job-positions-full.png) | Icon font `pi pi-*` và icon trang trí có `aria-hidden="true"`. |
| 18 | Nhãn ô nhập & bộ lọc | **PASS** | Thấp | [checklist-evidence.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/checklist-evidence.json) | Cả ô `#position-search` và `#position-availability` đều có `<label for="...">` liên kết chuẩn xác. |
| 19 | Trợ năng Drawer chi tiết | **PASS** | Thấp | [08-accessibility-tree-drawer.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/08-accessibility-tree-drawer.png) | PrimeVue Drawer có `role="dialog"`, `aria-modal="true"`, nút đóng rõ ràng, đóng bằng phím ESC. |
| 20 | Tương phản màu sắc | **PASS** | Thấp | Console Log contrast | Lệnh `npm run check:contrast` đạt 11/11 cặp màu, 0 lỗi tương phản (đạt chuẩn WCAG 2.1 AA $\ge$ 4.5:1). |
| **III**| **HÀNH VI SPA** | | | | |
| 21 | So sánh HTML thô vs Rendered | **FAIL** | **Cao** | [06-view-source-raw-html.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/06-view-source-raw-html.png) | HTML thô chỉ có `<div id="app"></div>` (587 bytes). Toàn bộ danh sách tuyển dụng tải qua CSR -> Rủi ro SEO rất lớn nếu bot không chạy JS. |
| 22 | Cập nhật Title khi F5 reload | **PASS** | Thấp | [checklist-evidence.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/checklist-evidence.json) | F5 giữ nguyên chính xác tiêu đề `Danh sách vị trí thực tập \| QLTT` nhờ `router.afterEach`. |
| 23 | Điều hướng khi chưa login | **PASS** | Thấp | [checklist-evidence.json](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/checklist-evidence.json) | Navigation guard chặn truy cập chưa login, chuyển hướng client về `/login`, title đổi thành `Đăng nhập \| QLTT`. |
| 24 | Xử lý lỗi API (Fallback UI) | **PASS** | Thấp | [09-error-fallback.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/09-error-fallback.png) | Khi API trả về 500, hiển thị banner lỗi thân thiện kèm nút "Thử lại", tiêu đề H1 và toolbar không bị vỡ/trắng trang. |
| 25 | Nhật ký mạng & Console | **PASS** | Thấp | [10-console-network.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/10-console-network.png) | Gọi API danh sách hoàn tất trong ~1.2s, 0 lỗi JavaScript fatal. |

---

## 4. BẰNG CHỨNG HÌNH ẢNH (EVIDENCE SCREENSHOTS)

Toàn bộ các file ảnh đã được tạo với chất lượng cao (Retina resolution, `deviceScaleFactor: 2`) và lưu tại thư mục `evidence/seo-job-positions/`:

| Tên file hình ảnh | Chú thích 1 dòng chứng minh |
| :--- | :--- |
| **[01-lighthouse-seo-desktop.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/01-lighthouse-seo-desktop.png)** | Chứng minh điểm số SEO đạt 100/100 trên Desktop cùng danh sách các audit SEO kỹ thuật cơ bản đã vượt qua. |
| **[02-lighthouse-seo-mobile.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/02-lighthouse-seo-mobile.png)** | Chứng minh điểm số SEO đạt 100/100 trên thiết bị di động (mobile viewport) với cấu hình throttling chuẩn. |
| **[03-lighthouse-accessibility.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/03-lighthouse-accessibility.png)** | Chứng minh danh mục Trợ năng (Accessibility) đạt điểm tuyệt đối 100/100 đối với các phần tử form và màu sắc. |
| **[04-page-job-positions-full.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/04-page-job-positions-full.png)** | Chụp toàn bộ giao diện trang `/job-positions` đã xác thực tài khoản User, danh sách 12 card việc làm render đầy đủ và an toàn. |
| **[05-devtools-head-elements.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/05-devtools-head-elements.png)** | Thể hiện chi tiết các thẻ trong `<head>` sau khi render: xác nhận title động đạt chuẩn nhưng thiếu description riêng, canonical và robots. |
| **[06-view-source-raw-html.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/06-view-source-raw-html.png)** | Chứng minh sự khác biệt CSR: HTML thô trả về từ server chỉ là vỏ rỗng `<div id="app"></div>`, hoàn toàn không có dữ liệu việc làm. |
| **[07-heading-outline.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/07-heading-outline.png)** | Trực quan hóa cấu trúc phân cấp Heading của trang: 1 thẻ H1 duy nhất, các thẻ H2 cho từng vị trí và H3 trong Drawer không nhảy cấp. |
| **[08-accessibility-tree-drawer.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/08-accessibility-tree-drawer.png)** | Chứng minh Drawer xem chi tiết vị trí thực tập tuân thủ chuẩn Accessible Dialog (`role="dialog"`, `aria-modal="true"`, đóng bằng ESC). |
| **[09-error-fallback.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/09-error-fallback.png)** | Bằng chứng UI xử lý lỗi khi API `/api/job-positions` bị gián đoạn (500): hiển thị thông báo lỗi thân thiện, có nút thử lại và không trắng trang. |
| **[10-console-network.png](file:///d:/baitapthuctap/duanquanlythuctapsinh/evidence/seo-job-positions/10-console-network.png)** | Giám sát Network và Console: hiển thị thời gian phản hồi API, trạng thái HTTP 200 và xác nhận không có ngoại lệ JavaScript phát sinh. |

---

## 5. DANH SÁCH LỖI THEO THỨ TỰ ƯU TIÊN & ĐỀ XUẤT SỬA ĐỔI

> [!IMPORTANT]
> Đây là các ĐỀ XUẤT KỸ THUẬT phục vụ giai đoạn tối ưu tiếp theo; **chưa áp dụng vào source code** theo đúng quy tắc đo baseline.

### Ưu tiên 1 (Nghiêm trọng - Cao): Thiếu liên kết crawlable (`<a href>`) trên Card vị trí
- **Mô tả hiện trạng:** Tiêu đề công việc trong [JobPositionCard.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/components/job-positions/JobPositionCard.vue#L31) chỉ là thẻ `<h2>` văn bản thuần. Thao tác xem chi tiết được kích hoạt bằng nút PrimeVue `<Button @click="emit('view', position)">`. Bot tìm kiếm của Google/Bing không bấm nút JavaScript `@click` để thu thập dữ liệu; do đó các vị trí thực tập không thể được crawl hay lập chỉ mục URL riêng.
- **Vị trí file nghi ngờ:** 
  - [JobPositionCard.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/components/job-positions/JobPositionCard.vue#L31-L47)
  - [JobPositionsView.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/views/job-positions/JobPositionsView.vue#L202)
- **Đề xuất sửa đổi:**
  - Bọc tiêu đề `<h2>` bằng thẻ `<router-link :to="{ name: 'job-position-detail', params: { id: position.id } }">` (kết xuất ra `<a :href="...">` thật trong DOM).
  - Bổ sung URL fallback dạng query param `?positionId=123` nếu giữ trải nghiệm mở Drawer để người dùng có thể chia sẻ trực tiếp liên kết vị trí tuyển dụng.

### Ưu tiên 2 (Nghiêm trọng - Cao): Sự cố "Soft 404" do thiếu file `sitemap.xml`
- **Mô tả hiện trạng:** Request `GET /sitemap.xml` trả về mã HTTP `200 OK` nhưng trả về nội dung `index.html` của SPA. Điều này khiến Google Search Console báo lỗi sitemap không hợp lệ và ảnh hưởng tiêu cực đến chất lượng kỹ thuật toàn trang.
- **Vị trí file nghi ngờ:** Thư mục `InternshipManagement/frontend/public/`
- **Đề xuất sửa đổi:**
  - Nếu dự án hoàn toàn là ứng dụng nội bộ (Internal Intranet/Portal): Cấu hình backend/nginx hoặc Vite trả về mã `404 Not Found` chuẩn cho `/sitemap.xml`.
  - Nếu dự án dự kiến công khai danh sách vị trí thực tập: Sinh file XML tĩnh `public/sitemap.xml` liệt kê các URL hợp lệ.

### Ưu tiên 3 (Trung bình): Thiếu thẻ Meta Description riêng theo route
- **Mô tả hiện trạng:** Thẻ `<meta name="description">` chỉ có 1 thẻ cố định trong [index.html](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/index.html#L7). File [pageTitle.js](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/pageTitle.js) và [router/index.js](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/router/index.js#L346) chỉ cập nhật `document.title`, bỏ quên meta description.
- **Vị trí file nghi ngờ:**
  - [pageTitle.js](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/utils/pageTitle.js)
  - [router/index.js](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/router/index.js#L346)
- **Đề xuất sửa đổi:**
  - Bổ sung hàm cập nhật meta description trong hook `router.afterEach`:
    ```javascript
    const descEl = document.querySelector('meta[name="description"]')
    if (descEl && to.meta.description) {
      descEl.setAttribute('content', to.meta.description)
    }
    ```
  - Khai báo `meta.description` tương ứng cho route `job-positions`: *"Khám phá danh sách các vị trí thực tập doanh nghiệp mới nhất, yêu cầu tuyển dụng và nộp hồ sơ ứng tuyển trực tuyến."* (khoảng 140 ký tự).

### Ưu tiên 4 (Trung bình): Thiếu thẻ Meta Robots (`noindex, nofollow`) cho trang yêu cầu đăng nhập
- **Mô tả hiện trạng:** Route `/job-positions` yêu cầu đăng nhập (`requiresAuth: true`). Tuy nhiên thẻ `<meta name="robots">` hoàn toàn vắng bóng, trong khi `robots.txt` lại để `Allow: /`. Điều này khiến bot tìm kiếm cố gắng crawl và lập chỉ mục các trang chuyển hướng.
- **Vị trí file nghi ngờ:**
  - [index.html](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/index.html)
  - [public/robots.txt](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/public/robots.txt)
- **Đề xuất sửa đổi:**
  - Gắn thẻ `<meta name="robots" content="noindex, nofollow" />` cho các route yêu cầu đăng nhập.
  - Cập nhật `robots.txt`:
    ```txt
    User-agent: *
    Disallow: /job-positions
    Disallow: /dashboard
    Disallow: /profile
    Disallow: /placement-requests
    ```

### Ưu tiên 5 (Thấp - A11y): Lỗi `label-content-name-mismatch` trên Mobile Header
- **Mô tả hiện trạng:** Nút avatar tài khoản trên Mobile Header:
  `<button type="button" class="mobile-avatar" aria-label="Tài khoản của tôi">` chứa text hiển thị trực quan là username (`PI02099`). Theo tiêu chuẩn WCAG 2.5.3, tên truy cập được (`aria-label`) phải chứa chuỗi văn bản hiển thị trên màn hình.
- **Vị trí file nghi ngờ:** [MainLayout.vue](file:///d:/baitapthuctap/duanquanlythuctapsinh/InternshipManagement/frontend/src/layouts/MainLayout.vue#L124-L126)
- **Đề xuất sửa đổi:** Đổi thuộc tính thành `aria-label="Tài khoản: " + authStore.user?.username` hoặc bỏ `aria-label` nếu văn bản và avatar đã đủ ý nghĩa ngữ nghĩa.

---

## 6. HẠN CHẾ CỦA PHÉP ĐO (MEASUREMENT LIMITATIONS)

1. **Bản chất SPA Client-Side Rendering (CSR):**
   Phép đo Lighthouse chạy trực tiếp trên trình duyệt Chromium có nạp và thực thi JavaScript đầy đủ. Tuy nhiên, các web crawler đơn giản (hoặc các bot không render JS, các công cụ tổng hợp RSS/feed) khi gửi request HTTP thông thường sẽ chỉ nhận được tài liệu HTML rỗng (587 bytes). Vì vậy, điểm SEO 100 của Lighthouse chỉ phản ánh chất lượng DOM sau khi hydrate, không đảm bảo khả năng lập chỉ mục của các công cụ tìm kiếm truyền thống nếu không có Server-Side Rendering (SSR) hoặc Prerendering (SSG).
2. **Trang được bảo vệ bởi tường xác thực (Authentication Gate):**
   Mọi dữ liệu việc làm trên route `/job-positions` chỉ truy cập được sau khi người dùng đăng nhập thành công. Do Googlebot và các công cụ tìm kiếm công khai không có phiên đăng nhập, chúng sẽ bị chuyển hướng về `/login`. Do đó, mục tiêu SEO của trang này nên là ngăn chặn chỉ mục rác (`noindex`), trừ khi hệ thống có định hướng mở cổng tra cứu việc làm công khai.
3. **Môi trường đo là Localhost Dev Server (Vite Development Mode):**
   Các chỉ số Core Web Vitals (LCP ~43s, FCP ~16s) bị ảnh hưởng nặng nề bởi việc Vite chạy ở chế độ dev (không bundling, không nén gzip/brotli, giữ nguyên source maps, giả lập độ trễ mạng mạng Slow 4G và CPU throttling). Khi build production (`npm run build`), hiệu năng thực tế sẽ tối ưu hơn rất nhiều.

---

## 7. KẾT LUẬN ĐÁNH GIÁ (EXECUTIVE SUMMARY)

Bản đo baseline cho thấy trang "Vị trí thực tập" (`/job-positions`) đạt nền tảng kỹ thuật trợ năng và cấu trúc ngữ nghĩa ban đầu rất tốt: điểm số Lighthouse SEO và Accessibility đều đạt 100/100, cấu trúc phân cấp thẻ Heading (H1 → H2 → H3) chuẩn mực, độ ổn định bố cục đạt mức lý tưởng (CLS = 0.001) và hệ thống màu sắc vượt qua 100% tiêu chuẩn tương phản WCAG 2.1 AA. Trang cũng có khả năng xử lý lỗi API (Fallback UI) và khôi phục tiêu đề khi F5 rất mượt mà.

Tuy nhiên, dưới góc độ chuyên gia SEO cao cấp, trang vẫn tồn tại 3 khiếm khuyết kiến trúc trọng yếu: (1) nội dung việc làm bị khóa trong mô hình Client-Side Rendering thuần khiến HTML thô hoàn toàn trống rỗng; (2) các Card vị trí thiếu liên kết crawlable (`<a href>`) mà phụ thuộc hoàn toàn vào sự kiện click JavaScript; và (3) chưa có thẻ meta description riêng, thẻ canonical cũng như chưa thiết lập chỉ thị `noindex` phù hợp cho trang yêu cầu đăng nhập. Đây là các cơ sở dữ liệu thực nghiệm chuẩn xác, độc lập để đội ngũ kỹ thuật tiến hành tối ưu hóa có định hướng trong giai đoạn tiếp theo.

---

### Bảng danh mục file bằng chứng đã tạo
Toàn bộ các file phục vụ báo cáo được lưu trữ tập trung tại thư mục `evidence/` (hoàn toàn không đụng vào source code dự án):

1. `evidence/seo-job-positions/SEO-REPORT.md` (Báo cáo đánh giá chi tiết này)
2. `evidence/seo-job-positions/01-lighthouse-seo-desktop.png`
3. `evidence/seo-job-positions/02-lighthouse-seo-mobile.png`
4. `evidence/seo-job-positions/03-lighthouse-accessibility.png`
5. `evidence/seo-job-positions/04-page-job-positions-full.png`
6. `evidence/seo-job-positions/05-devtools-head-elements.png`
7. `evidence/seo-job-positions/06-view-source-raw-html.png`
8. `evidence/seo-job-positions/07-heading-outline.png`
9. `evidence/seo-job-positions/08-accessibility-tree-drawer.png`
10. `evidence/seo-job-positions/09-error-fallback.png`
11. `evidence/seo-job-positions/10-console-network.png`
12. `evidence/seo-job-positions/lighthouse-user-desktop.html` & `.json`
13. `evidence/seo-job-positions/lighthouse-user-mobile.html` & `.json`
14. `evidence/seo-job-positions/lighthouse-admin-desktop.html` & `.json`
15. `evidence/seo-job-positions/lighthouse-summary.json`
16. `evidence/seo-job-positions/checklist-evidence.json`
17. `evidence/seo-job-positions/raw-html.html`
18. `evidence/tokens.json`
19. `evidence/capture-evidence.mjs`
20. `evidence/generate-audit.mjs`
21. `evidence/package.json`
