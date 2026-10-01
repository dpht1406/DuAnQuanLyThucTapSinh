// Dữ liệu SEED (giả lập) cho trang Giới thiệu doanh nghiệp.
// Đây là đơn vị tiếp nhận thực tập hư cấu, chỉ dùng để trình diễn.
// Khi có API thật, chỉ cần thay thân hàm getCompanyProfile() ở cuối file.

export const companyProfileSeed = {
  name: 'Công ty Cổ phần Công nghệ Cửu Long',
  shortName: 'Cửu Long Tech',
  tagline: 'Chúng tôi làm phần mềm cho doanh nghiệp vừa và nhỏ, và đào tạo người làm nghề ngay trong dự án thật.',
  summary:
    'Từ năm 2014, Cửu Long Tech xây dựng phần mềm quản trị, ứng dụng web và di động cho hơn 200 doanh nghiệp trong nước. Chương trình thực tập của chúng tôi cho sinh viên làm việc cùng đội sản phẩm, có mentor kèm riêng và được ghi nhận bằng đánh giá cuối kỳ.',
  facts: [
    { icon: 'pi pi-calendar', label: 'Thành lập', value: '2014' },
    { icon: 'pi pi-users', label: 'Nhân sự', value: '120 người' },
    { icon: 'pi pi-briefcase', label: 'Sinh viên mỗi kỳ', value: '12–15 người' },
    { icon: 'pi pi-clock', label: 'Thời lượng thực tập', value: '10–12 tuần' },
    { icon: 'pi pi-map-marker', label: 'Hình thức', value: 'Hybrid, 3 ngày tại văn phòng' },
    { icon: 'pi pi-wallet', label: 'Hỗ trợ', value: '3–5 triệu đồng/tháng' }
  ],
  history: [
    { year: '2014', title: 'Thành lập', text: 'Sáu người sáng lập bắt đầu trong một văn phòng 30 m², làm phần mềm quản lý bán hàng cho cửa hàng nhỏ.' },
    { year: '2017', title: 'Mở rộng sang quản trị doanh nghiệp', text: 'Ra mắt bộ ERP nhẹ cho doanh nghiệp vừa và nhỏ. Đội ngũ tăng lên 40 người.' },
    { year: '2019', title: 'Kỳ thực tập đầu tiên', text: 'Nhận 8 sinh viên, trong đó 5 bạn được mời làm chính thức sau khi tốt nghiệp.' },
    { year: '2022', title: 'Chuyển sang nền tảng web và đám mây', text: 'Toàn bộ sản phẩm chuyển sang kiến trúc web, triển khai trên đám mây. Đội ngũ vượt 100 người.' },
    { year: '2024', title: 'Chương trình mentor nội bộ', text: 'Mỗi sinh viên có một mentor riêng, họp 1–1 hằng tuần và được đánh giá theo bộ tiêu chí công khai.' },
    { year: '2026', title: 'Số hóa quản lý thực tập', text: 'Sinh viên nộp yêu cầu, theo dõi trạng thái và nhận thông báo ngay trên hệ thống quản lý thực tập này.' }
  ],
  fields: [
    { icon: 'pi pi-server', title: 'Phần mềm quản trị doanh nghiệp', text: 'Kế toán, kho, nhân sự và bán hàng cho doanh nghiệp vừa và nhỏ.', tags: ['C#', '.NET', 'SQL Server'] },
    { icon: 'pi pi-desktop', title: 'Ứng dụng web', text: 'Cổng thông tin, trang quản trị và dashboard theo dõi số liệu.', tags: ['Vue', 'TypeScript', 'REST API'] },
    { icon: 'pi pi-mobile', title: 'Ứng dụng di động', text: 'Ứng dụng cho nhân viên hiện trường và khách hàng cuối.', tags: ['Flutter', 'Firebase'] },
    { icon: 'pi pi-check-circle', title: 'Kiểm thử và đảm bảo chất lượng', text: 'Viết test case, kiểm thử tự động và đo chất lượng trước mỗi lần phát hành.', tags: ['Playwright', 'Postman', 'Jira'] }
  ],
  environment: {
    highlights: [
      { icon: 'pi pi-user', title: 'Một mentor cho mỗi sinh viên', text: 'Họp 1–1 30 phút mỗi tuần để xem tiến độ và góp ý.' },
      { icon: 'pi pi-sync', title: 'Làm việc theo sprint', text: 'Sprint 2 tuần, có buổi review cuối sprint để trình bày kết quả.' },
      { icon: 'pi pi-code', title: 'Code review bắt buộc', text: 'Mọi thay đổi đều có người đọc lại trước khi gộp vào nhánh chính.' },
      { icon: 'pi pi-clock', title: 'Giờ giấc rõ ràng', text: '8:30–17:30, thứ Hai đến thứ Sáu. Có thể làm từ xa 2 ngày mỗi tuần.' }
    ],
    week: [
      { day: 'Thứ Hai', text: 'Họp lập kế hoạch tuần, nhận việc từ mentor.' },
      { day: 'Thứ Ba – Thứ Năm', text: 'Làm việc trong nhóm, hỏi đáp và code review hằng ngày.' },
      { day: 'Thứ Sáu', text: 'Demo kết quả tuần và nhận phản hồi từ mentor.' }
    ],
    tools: ['Git', 'Jira', 'Figma', 'Microsoft Teams', 'Postman']
  },
  values: [
    { icon: 'pi pi-shield', title: 'Trung thực với dữ liệu', text: 'Nói đúng số liệu, kể cả khi kết quả chưa như mong đợi.' },
    { icon: 'pi pi-book', title: 'Học bằng làm', text: 'Sinh viên nhận nhiệm vụ thật, có phạm vi rõ và có người hướng dẫn.' },
    { icon: 'pi pi-verified', title: 'Chất lượng trước tốc độ', text: 'Ưu tiên chạy đúng, có kiểm thử, rồi mới tối ưu tốc độ giao.' },
    { icon: 'pi pi-heart', title: 'Tôn trọng thời gian của nhau', text: 'Họp có mục tiêu, phản hồi có thời hạn, không giao việc phút chót.' },
    { icon: 'pi pi-share-alt', title: 'Chia sẻ để cả đội giỏi lên', text: 'Người đi trước ghi lại cách làm để người đến sau khỏi phải tự mò.' }
  ],
  positions: [
    { title: 'Lập trình viên Frontend (Vue)', quantity: 3, isOpen: true, skills: 'HTML, CSS, JavaScript, Vue cơ bản' },
    { title: 'Lập trình viên Backend (.NET)', quantity: 3, isOpen: true, skills: 'C#, SQL, hiểu REST API' },
    { title: 'Kiểm thử phần mềm (QA)', quantity: 2, isOpen: true, skills: 'Viết test case, đọc hiểu yêu cầu' },
    { title: 'Phân tích nghiệp vụ (BA)', quantity: 2, isOpen: true, skills: 'Vẽ sơ đồ quy trình, viết tài liệu yêu cầu' },
    { title: 'Thiết kế UI/UX', quantity: 1, isOpen: false, skills: 'Figma, nguyên tắc thiết kế giao diện' }
  ],
  studentInfo: {
    eligibility: [
      'Sinh viên năm 3 trở lên thuộc ngành Công nghệ thông tin, Hệ thống thông tin hoặc ngành liên quan.',
      'Có thể dành tối thiểu 24 giờ mỗi tuần trong 10–12 tuần liên tục.',
      'Có trường hợp cần chuyển vị trí hoặc kéo dài thời gian: trao đổi với mentor và phòng quản lý thực tập.'
    ],
    documents: ['CV một trang', 'Bảng điểm mới nhất', 'Giấy giới thiệu của trường', 'Liên kết GitHub hoặc portfolio (không bắt buộc)'],
    // Các bước khớp với trạng thái sinh viên trong hệ thống (utils/studentStatus.js)
    process: [
      { status: 'Đã giới thiệu', text: 'Phòng quản lý thực tập gửi hồ sơ của bạn tới công ty.' },
      { status: 'Đã phỏng vấn', text: 'Phỏng vấn 45 phút về kỹ năng nền tảng và mong muốn của bạn.' },
      { status: 'Đã nhận', text: 'Công ty gửi kết quả trong vòng 5 ngày làm việc sau phỏng vấn.' },
      { status: 'Đang thực tập', text: 'Buổi định hướng ngày đầu, nhận mentor và cấp tài khoản làm việc.' },
      { status: 'Hoàn thành', text: 'Đánh giá cuối kỳ và xác nhận kết quả thực tập gửi về trường.' }
    ],
    checklist: [
      { id: 'cv', label: 'Đã cập nhật CV và kiểm tra lỗi chính tả' },
      { id: 'transcript', label: 'Đã có bảng điểm và giấy giới thiệu của trường' },
      { id: 'position', label: 'Đã chọn vị trí phù hợp với kỹ năng hiện có' },
      { id: 'schedule', label: 'Đã sắp xếp lịch học để dành đủ 24 giờ mỗi tuần' },
      { id: 'contact', label: 'Đã kiểm tra email và số điện thoại trong hồ sơ trên hệ thống' }
    ],
    faq: [
      { q: 'Tôi chưa có kinh nghiệm làm dự án thật thì có ứng tuyển được không?', a: 'Được. Chúng tôi xét kiến thức nền tảng và tinh thần học hỏi. Bài tập trên lớp hoặc đồ án cá nhân đều được xem là minh chứng.' },
      { q: 'Kỳ thực tập có được trả lương không?', a: 'Sinh viên nhận hỗ trợ từ 3 đến 5 triệu đồng mỗi tháng tùy vị trí và mức độ tham gia dự án.' },
      { q: 'Tôi có thể ứng tuyển nhiều vị trí cùng lúc không?', a: 'Mỗi lượt gửi yêu cầu chọn một vị trí. Nếu muốn đổi, hãy liên hệ phòng quản lý thực tập để được hỗ trợ.' },
      { q: 'Nếu bị từ chối thì sao?', a: 'Hệ thống sẽ thông báo lý do từ chối. Bạn có thể chỉnh hồ sơ và gửi lại theo hướng dẫn của phòng quản lý thực tập.' }
    ]
  },
  contact: {
    address: '12 Đường số 5, Khu công nghệ phần mềm, TP. Cần Thơ',
    email: 'thuctap@cuulongtech.example',
    phone: '0292 000 0000',
    person: 'Chị Lê Thu Hà, phụ trách chương trình thực tập',
    hours: 'Thứ Hai – Thứ Sáu, 8:30–17:30'
  }
}

/**
 * Trả về hồ sơ doanh nghiệp. Hiện tại đọc từ seed, có độ trễ nhỏ để
 * mô phỏng gọi API nên giao diện vẫn xử lý được trạng thái tải và lỗi.
 * Đặt `options.simulateError = true` để kiểm thử nhánh lỗi.
 */
export function getCompanyProfile(options = {}) {
  const { delay = 400, simulateError = false } = options
  return new Promise((resolve, reject) => {
    setTimeout(() => {
      if (simulateError) {
        reject(new Error('Không tải được thông tin doanh nghiệp. Vui lòng thử lại.'))
        return
      }
      resolve(structuredClone(companyProfileSeed))
    }, delay)
  })
}
