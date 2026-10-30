namespace StudentInternshipMgmt.Application.Features.About;

public static class AboutContentDefaults
{
    public static AboutContentDto CreateDefault()
    {
        return new AboutContentDto
        {
            Name = "Công ty Cổ phần Công nghệ Cửu Long",
            ShortName = "Cửu Long Tech",
            Tagline = "Chúng tôi làm phần mềm cho doanh nghiệp vừa và nhỏ, và đào tạo người làm nghề ngay trong dự án thật.",
            Summary = "Từ năm 2014, Cửu Long Tech xây dựng phần mềm quản trị, ứng dụng web và di động cho hơn 200 doanh nghiệp trong nước. Chương trình thực tập của chúng tôi cho sinh viên làm việc cùng đội sản phẩm, có mentor kèm riêng và được ghi nhận bằng đánh giá cuối kỳ.",
            Footnote = null,
            Facts = new List<AboutFactDto>
            {
                new() { Icon = "pi pi-calendar", Label = "Thành lập", Value = "2014" },
                new() { Icon = "pi pi-users", Label = "Nhân sự", Value = "120 người" },
                new() { Icon = "pi pi-briefcase", Label = "Sinh viên mỗi kỳ", Value = "12–15 người" },
                new() { Icon = "pi pi-clock", Label = "Thời lượng thực tập", Value = "10–12 tuần" },
                new() { Icon = "pi pi-map-marker", Label = "Hình thức", Value = "Hybrid, 3 ngày tại văn phòng" },
                new() { Icon = "pi pi-wallet", Label = "Hỗ trợ", Value = "3–5 triệu đồng/tháng" }
            },
            History = new List<AboutHistoryDto>
            {
                new() { Year = "2014", Title = "Thành lập", Text = "Sáu người sáng lập bắt đầu trong một văn phòng 30 m², làm phần mềm quản lý bán hàng cho cửa hàng nhỏ." },
                new() { Year = "2017", Title = "Mở rộng sang quản trị doanh nghiệp", Text = "Ra mắt bộ ERP nhẹ cho doanh nghiệp vừa và nhỏ. Đội ngũ tăng lên 40 người." },
                new() { Year = "2019", Title = "Kỳ thực tập đầu tiên", Text = "Nhận 8 sinh viên, trong đó 5 bạn được mời làm chính thức sau khi tốt nghiệp." },
                new() { Year = "2022", Title = "Chuyển sang nền tảng web và đám mây", Text = "Toàn bộ sản phẩm chuyển sang kiến trúc web, triển khai trên đám mây. Đội ngũ vượt 100 người." },
                new() { Year = "2024", Title = "Chương trình mentor nội bộ", Text = "Mỗi sinh viên có một mentor riêng, họp 1–1 hằng tuần và được đánh giá theo bộ tiêu chí công khai." },
                new() { Year = "2026", Title = "Số hóa quản lý thực tập", Text = "Sinh viên nộp yêu cầu, theo dõi trạng thái và nhận thông báo ngay trên hệ thống quản lý thực tập này." }
            },
            Fields = new List<AboutFieldDto>
            {
                new()
                {
                    Icon = "pi pi-server",
                    Title = "Phần mềm quản trị doanh nghiệp",
                    Text = "Kế toán, kho, nhân sự và bán hàng cho doanh nghiệp vừa và nhỏ.",
                    Tags = new List<string> { "C#", ".NET", "SQL Server" }
                },
                new()
                {
                    Icon = "pi pi-desktop",
                    Title = "Ứng dụng web",
                    Text = "Cổng thông tin, trang quản trị và dashboard theo dõi số liệu.",
                    Tags = new List<string> { "Vue", "TypeScript", "REST API" }
                },
                new()
                {
                    Icon = "pi pi-mobile",
                    Title = "Ứng dụng di động",
                    Text = "Ứng dụng cho nhân viên hiện trường và khách hàng cuối.",
                    Tags = new List<string> { "Flutter", "Firebase" }
                },
                new()
                {
                    Icon = "pi pi-check-circle",
                    Title = "Kiểm thử và đảm bảo chất lượng",
                    Text = "Viết test case, kiểm thử tự động và đo chất lượng trước mỗi lần phát hành.",
                    Tags = new List<string> { "Playwright", "Postman", "Jira" }
                }
            },
            Environment = new AboutEnvironmentDto
            {
                Highlights = new List<AboutHighlightDto>
                {
                    new() { Icon = "pi pi-user", Title = "Một mentor cho mỗi sinh viên", Text = "Họp 1–1 30 phút mỗi tuần để xem tiến độ và góp ý." },
                    new() { Icon = "pi pi-sync", Title = "Làm việc theo sprint", Text = "Sprint 2 tuần, có buổi review cuối sprint để trình bày kết quả." },
                    new() { Icon = "pi pi-code", Title = "Code review bắt buộc", Text = "Mọi thay đổi đều có người đọc lại trước khi gộp vào nhánh chính." },
                    new() { Icon = "pi pi-clock", Title = "Giờ giấc rõ ràng", Text = "8:30–17:30, thứ Hai đến thứ Sáu. Có thể làm từ xa 2 ngày mỗi tuần." }
                },
                Week = new List<AboutWeekScheduleDto>
                {
                    new() { Day = "Thứ Hai", Text = "Họp lập kế hoạch tuần, nhận việc từ mentor." },
                    new() { Day = "Thứ Ba – Thứ Năm", Text = "Làm việc trong nhóm, hỏi đáp và code review hằng ngày." },
                    new() { Day = "Thứ Sáu", Text = "Demo kết quả tuần và nhận phản hồi từ mentor." }
                },
                Tools = new List<string> { "Git", "Jira", "Figma", "Microsoft Teams", "Postman" }
            },
            Values = new List<AboutValueDto>
            {
                new() { Icon = "pi pi-shield", Title = "Trung thực với dữ liệu", Text = "Nói đúng số liệu, kể cả khi kết quả chưa như mong đợi." },
                new() { Icon = "pi pi-book", Title = "Học bằng làm", Text = "Sinh viên nhận nhiệm vụ thật, có phạm vi rõ và có người hướng dẫn." },
                new() { Icon = "pi pi-verified", Title = "Chất lượng trước tốc độ", Text = "Ưu tiên chạy đúng, có kiểm thử, rồi mới tối ưu tốc độ giao." },
                new() { Icon = "pi pi-heart", Title = "Tôn trọng thời gian của nhau", Text = "Họp có mục tiêu, phản hồi có thời hạn, không giao việc phút chót." },
                new() { Icon = "pi pi-share-alt", Title = "Chia sẻ để cả đội giỏi lên", Text = "Người đi trước ghi lại cách làm để người đến sau khỏi phải tự mò." }
            },
            StudentInfo = new AboutStudentInfoDto
            {
                Eligibility = new List<string>
                {
                    "Sinh viên năm 3 trở lên thuộc ngành Công nghệ thông tin, Hệ thống thông tin hoặc ngành liên quan.",
                    "Có thể dành tối thiểu 24 giờ mỗi tuần trong 10–12 tuần liên tục.",
                    "Có trường hợp cần chuyển vị trí hoặc kéo dài thời gian: trao đổi với mentor và phòng quản lý thực tập."
                },
                Documents = new List<string>
                {
                    "CV một trang",
                    "Bảng điểm mới nhất",
                    "Giấy giới thiệu của trường",
                    "Liên kết GitHub hoặc portfolio (không bắt buộc)"
                },
                Process = new List<AboutProcessStepDto>
                {
                    new() { Status = "Đã giới thiệu", Text = "Phòng quản lý thực tập gửi hồ sơ của bạn tới công ty." },
                    new() { Status = "Đã phỏng vấn", Text = "Phỏng vấn 45 phút về kỹ năng nền tảng và mong muốn của bạn." },
                    new() { Status = "Đã nhận", Text = "Công ty gửi kết quả trong vòng 5 ngày làm việc sau phỏng vấn." },
                    new() { Status = "Đang thực tập", Text = "Buổi định hướng ngày đầu, nhận mentor và cấp tài khoản làm việc." },
                    new() { Status = "Hoàn thành", Text = "Đánh giá cuối kỳ và xác nhận kết quả thực tập gửi về trường." }
                },
                Checklist = new List<AboutChecklistItemDto>
                {
                    new() { Id = "cv", Label = "Đã cập nhật CV và kiểm tra lỗi chính tả" },
                    new() { Id = "transcript", Label = "Đã có bảng điểm và giấy giới thiệu của trường" },
                    new() { Id = "position", Label = "Đã chọn vị trí phù hợp với kỹ năng hiện có" },
                    new() { Id = "schedule", Label = "Đã sắp xếp lịch học để dành đủ 24 giờ mỗi tuần" },
                    new() { Id = "contact", Label = "Đã kiểm tra email và số điện thoại trong hồ sơ trên hệ thống" }
                },
                Faq = new List<AboutFaqItemDto>
                {
                    new()
                    {
                        Q = "Tôi chưa có kinh nghiệm làm dự án thật thì có ứng tuyển được không?",
                        A = "Được. Chúng tôi xét kiến thức nền tảng và tinh thần học hỏi. Bài tập trên lớp hoặc đồ án cá nhân đều được xem là minh chứng."
                    },
                    new()
                    {
                        Q = "Kỳ thực tập có được trả lương không?",
                        A = "Sinh viên nhận hỗ trợ từ 3 đến 5 triệu đồng mỗi tháng tùy vị trí và mức độ tham gia dự án."
                    },
                    new()
                    {
                        Q = "Tôi có thể ứng tuyển nhiều vị trí cùng lúc không?",
                        A = "Mỗi lượt gửi yêu cầu chọn một vị trí. Nếu muốn đổi, hãy liên hệ phòng quản lý thực tập để được hỗ trợ."
                    },
                    new()
                    {
                        Q = "Nếu bị từ chối thì sao?",
                        A = "Hệ thống sẽ thông báo lý do từ chối. Bạn có thể chỉnh hồ sơ và gửi lại theo hướng dẫn của phòng quản lý thực tập."
                    }
                }
            },
            Contact = new AboutContactDto
            {
                Address = "12 Đường số 5, Khu công nghệ phần mềm, TP. Cần Thơ",
                Email = "thuctap@cuulongtech.example",
                Phone = "0292 000 0000",
                Person = "Chị Lê Thu Hà, phụ trách chương trình thực tập",
                Hours = "Thứ Hai – Thứ Sáu, 8:30–17:30"
            }
        };
    }
}
