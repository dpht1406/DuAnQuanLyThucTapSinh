using System.Text.RegularExpressions;
using StudentInternshipMgmt.Application.Common;

namespace StudentInternshipMgmt.Application.Features.About;

public static class AboutContentValidator
{
    private static readonly Regex ChecklistIdRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

    public static List<string> Validate(AboutContentDto? content)
    {
        var errors = new List<string>();

        if (content is null)
        {
            errors.Add("Dữ liệu nội dung không được để trống.");
            return errors;
        }

        // 1. Thông tin chung
        ValidateGeneralInfo(content, errors);

        // 2. Facts
        ValidateFacts(content.Facts, errors);

        // 3. History
        ValidateHistory(content.History, errors);

        // 4. Fields
        ValidateFields(content.Fields, errors);

        // 5. Environment
        ValidateEnvironment(content.Environment, errors);

        // 6. Values
        ValidateValues(content.Values, errors);

        // 7. Student Info
        ValidateStudentInfo(content.StudentInfo, errors);

        // 8. Contact
        ValidateContact(content.Contact, errors);

        return errors;
    }

    public static AboutContentDto TrimContent(AboutContentDto content)
    {
        return new AboutContentDto
        {
            Name = content.Name?.Trim() ?? string.Empty,
            ShortName = NormalizeOptional(content.ShortName),
            Tagline = content.Tagline?.Trim() ?? string.Empty,
            Summary = content.Summary?.Trim() ?? string.Empty,
            Footnote = NormalizeOptional(content.Footnote),
            Facts = content.Facts?.Select(f => new AboutFactDto
            {
                Icon = f.Icon?.Trim() ?? string.Empty,
                Label = f.Label?.Trim() ?? string.Empty,
                Value = f.Value?.Trim() ?? string.Empty
            }).ToList() ?? new(),
            History = content.History?.Select(h => new AboutHistoryDto
            {
                Year = h.Year?.Trim() ?? string.Empty,
                Title = h.Title?.Trim() ?? string.Empty,
                Text = h.Text?.Trim() ?? string.Empty
            }).ToList() ?? new(),
            Fields = content.Fields?.Select(f => new AboutFieldDto
            {
                Icon = f.Icon?.Trim() ?? string.Empty,
                Title = f.Title?.Trim() ?? string.Empty,
                Text = f.Text?.Trim() ?? string.Empty,
                Tags = f.Tags?.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new()
            }).ToList() ?? new(),
            Environment = new AboutEnvironmentDto
            {
                Highlights = content.Environment?.Highlights?.Select(h => new AboutHighlightDto
                {
                    Icon = h.Icon?.Trim() ?? string.Empty,
                    Title = h.Title?.Trim() ?? string.Empty,
                    Text = h.Text?.Trim() ?? string.Empty
                }).ToList() ?? new(),
                Week = content.Environment?.Week?.Select(w => new AboutWeekScheduleDto
                {
                    Day = w.Day?.Trim() ?? string.Empty,
                    Text = w.Text?.Trim() ?? string.Empty
                }).ToList() ?? new(),
                Tools = content.Environment?.Tools?.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new()
            },
            Values = content.Values?.Select(v => new AboutValueDto
            {
                Icon = v.Icon?.Trim() ?? string.Empty,
                Title = v.Title?.Trim() ?? string.Empty,
                Text = v.Text?.Trim() ?? string.Empty
            }).ToList() ?? new(),
            StudentInfo = new AboutStudentInfoDto
            {
                Eligibility = content.StudentInfo?.Eligibility?.Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e)).ToList() ?? new(),
                Documents = content.StudentInfo?.Documents?.Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToList() ?? new(),
                Process = content.StudentInfo?.Process?.Select(p => new AboutProcessStepDto
                {
                    Status = p.Status?.Trim() ?? string.Empty,
                    Text = p.Text?.Trim() ?? string.Empty
                }).ToList() ?? new(),
                Checklist = content.StudentInfo?.Checklist?.Select(c => new AboutChecklistItemDto
                {
                    Id = c.Id?.Trim() ?? string.Empty,
                    Label = c.Label?.Trim() ?? string.Empty
                }).ToList() ?? new(),
                Faq = content.StudentInfo?.Faq?.Select(faq => new AboutFaqItemDto
                {
                    Q = faq.Q?.Trim() ?? string.Empty,
                    A = faq.A?.Trim() ?? string.Empty
                }).ToList() ?? new()
            },
            Contact = new AboutContactDto
            {
                Person = NormalizeOptional(content.Contact?.Person),
                Address = NormalizeOptional(content.Contact?.Address),
                Email = NormalizeOptional(content.Contact?.Email),
                Phone = NormalizeOptional(content.Contact?.Phone),
                Hours = NormalizeOptional(content.Contact?.Hours)
            }
        };
    }

    private static void ValidateGeneralInfo(AboutContentDto content, List<string> errors)
    {
        // Name
        if (string.IsNullOrWhiteSpace(content.Name))
            errors.Add("Thông tin chung: tên đơn vị tiếp nhận (name) là bắt buộc.");
        else
        {
            var trimmed = content.Name.Trim();
            if (trimmed.Length > 200)
                errors.Add("Thông tin chung: tên đơn vị tiếp nhận không được quá 200 ký tự.");
            if (HasInvalidControlChars(content.Name))
                errors.Add("Thông tin chung: tên đơn vị tiếp nhận không được chứa ký tự điều khiển.");
        }

        // ShortName
        if (!string.IsNullOrWhiteSpace(content.ShortName))
        {
            var trimmed = content.ShortName.Trim();
            if (trimmed.Length > 100)
                errors.Add("Thông tin chung: tên viết tắt không được quá 100 ký tự.");
            if (HasInvalidControlChars(content.ShortName))
                errors.Add("Thông tin chung: tên viết tắt không được chứa ký tự điều khiển.");
        }

        // Tagline
        if (string.IsNullOrWhiteSpace(content.Tagline))
            errors.Add("Thông tin chung: khẩu hiệu (tagline) là bắt buộc.");
        else
        {
            var trimmed = content.Tagline.Trim();
            if (trimmed.Length > 300)
                errors.Add("Thông tin chung: khẩu hiệu không được quá 300 ký tự.");
            if (HasInvalidControlChars(content.Tagline))
                errors.Add("Thông tin chung: khẩu hiệu không được chứa ký tự điều khiển.");
        }

        // Summary (allow line breaks)
        if (string.IsNullOrWhiteSpace(content.Summary))
            errors.Add("Thông tin chung: giới thiệu tóm tắt (summary) là bắt buộc.");
        else
        {
            var trimmed = content.Summary.Trim();
            if (trimmed.Length > 1000)
                errors.Add("Thông tin chung: giới thiệu tóm tắt không được quá 1000 ký tự.");
            if (HasInvalidControlChars(content.Summary, allowLineBreaks: true))
                errors.Add("Thông tin chung: giới thiệu tóm tắt không được chứa ký tự điều khiển không hợp lệ.");
        }

        // Footnote
        if (!string.IsNullOrWhiteSpace(content.Footnote))
        {
            var trimmed = content.Footnote.Trim();
            if (trimmed.Length > 300)
                errors.Add("Thông tin chung: ghi chú cuối trang không được quá 300 ký tự.");
            if (HasInvalidControlChars(content.Footnote))
                errors.Add("Thông tin chung: ghi chú cuối trang không được chứa ký tự điều khiển.");
        }
    }

    private static void ValidateFacts(List<AboutFactDto>? facts, List<string> errors)
    {
        if (facts is null || facts.Count < 1 || facts.Count > 8)
        {
            errors.Add("Thông tin nhanh: phải có từ 1 đến 8 mục.");
            if (facts is null) return;
        }

        for (int i = 0; i < facts.Count; i++)
        {
            var fact = facts[i];
            int pos = i + 1;

            if (!AboutIcons.IsValid(fact.Icon))
                errors.Add($"Thông tin nhanh, mục thứ {pos}: biểu tượng (icon) không hợp lệ.");

            if (string.IsNullOrWhiteSpace(fact.Label))
                errors.Add($"Thông tin nhanh, mục thứ {pos}: nhãn không được để trống.");
            else
            {
                var trimmed = fact.Label.Trim();
                if (trimmed.Length > 50)
                    errors.Add($"Thông tin nhanh, mục thứ {pos}: nhãn không được quá 50 ký tự.");
                if (HasInvalidControlChars(fact.Label))
                    errors.Add($"Thông tin nhanh, mục thứ {pos}: nhãn không được chứa ký tự điều khiển.");
            }

            if (string.IsNullOrWhiteSpace(fact.Value))
                errors.Add($"Thông tin nhanh, mục thứ {pos}: giá trị không được để trống.");
            else
            {
                var trimmed = fact.Value.Trim();
                if (trimmed.Length > 100)
                    errors.Add($"Thông tin nhanh, mục thứ {pos}: giá trị không được quá 100 ký tự.");
                if (HasInvalidControlChars(fact.Value))
                    errors.Add($"Thông tin nhanh, mục thứ {pos}: giá trị không được chứa ký tự điều khiển.");
            }
        }
    }

    private static void ValidateHistory(List<AboutHistoryDto>? history, List<string> errors)
    {
        if (history is null) return;

        if (history.Count > 20)
            errors.Add("Lịch sử phát triển: không được quá 20 mục.");

        for (int i = 0; i < history.Count; i++)
        {
            var item = history[i];
            int pos = i + 1;

            if (string.IsNullOrWhiteSpace(item.Year))
                errors.Add($"Lịch sử, mục thứ {pos}: năm không được để trống.");
            else
            {
                var trimmed = item.Year.Trim();
                if (trimmed.Length > 20)
                    errors.Add($"Lịch sử, mục thứ {pos}: năm không được quá 20 ký tự.");
                if (HasInvalidControlChars(item.Year))
                    errors.Add($"Lịch sử, mục thứ {pos}: năm không được chứa ký tự điều khiển.");
            }

            if (string.IsNullOrWhiteSpace(item.Title))
                errors.Add($"Lịch sử, mục thứ {pos}: tiêu đề không được để trống.");
            else
            {
                var trimmed = item.Title.Trim();
                if (trimmed.Length > 150)
                    errors.Add($"Lịch sử, mục thứ {pos}: tiêu đề không được quá 150 ký tự.");
                if (HasInvalidControlChars(item.Title))
                    errors.Add($"Lịch sử, mục thứ {pos}: tiêu đề không được chứa ký tự điều khiển.");
            }

            if (string.IsNullOrWhiteSpace(item.Text))
                errors.Add($"Lịch sử, mục thứ {pos}: nội dung không được để trống.");
            else
            {
                var trimmed = item.Text.Trim();
                if (trimmed.Length > 500)
                    errors.Add($"Lịch sử, mục thứ {pos}: nội dung không được quá 500 ký tự.");
                if (HasInvalidControlChars(item.Text))
                    errors.Add($"Lịch sử, mục thứ {pos}: nội dung không được chứa ký tự điều khiển.");
            }
        }
    }

    private static void ValidateFields(List<AboutFieldDto>? fields, List<string> errors)
    {
        if (fields is null) return;

        if (fields.Count > 8)
            errors.Add("Lĩnh vực hoạt động: không được quá 8 mục.");

        for (int i = 0; i < fields.Count; i++)
        {
            var item = fields[i];
            int pos = i + 1;

            if (!AboutIcons.IsValid(item.Icon))
                errors.Add($"Lĩnh vực, mục thứ {pos}: biểu tượng (icon) không hợp lệ.");

            if (string.IsNullOrWhiteSpace(item.Title))
                errors.Add($"Lĩnh vực, mục thứ {pos}: tiêu đề không được để trống.");
            else
            {
                var trimmed = item.Title.Trim();
                if (trimmed.Length > 150)
                    errors.Add($"Lĩnh vực, mục thứ {pos}: tiêu đề không được quá 150 ký tự.");
                if (HasInvalidControlChars(item.Title))
                    errors.Add($"Lĩnh vực, mục thứ {pos}: tiêu đề không được chứa ký tự điều khiển.");
            }

            if (string.IsNullOrWhiteSpace(item.Text))
                errors.Add($"Lĩnh vực, mục thứ {pos}: mô tả không được để trống.");
            else
            {
                var trimmed = item.Text.Trim();
                if (trimmed.Length > 400)
                    errors.Add($"Lĩnh vực, mục thứ {pos}: mô tả không được quá 400 ký tự.");
                if (HasInvalidControlChars(item.Text))
                    errors.Add($"Lĩnh vực, mục thứ {pos}: mô tả không được chứa ký tự điều khiển.");
            }

            if (item.Tags is not null)
            {
                if (item.Tags.Count > 8)
                    errors.Add($"Lĩnh vực, mục thứ {pos}: không được quá 8 thẻ (tag).");

                for (int t = 0; t < item.Tags.Count; t++)
                {
                    var tag = item.Tags[t];
                    int tagPos = t + 1;

                    if (string.IsNullOrWhiteSpace(tag))
                        errors.Add($"Lĩnh vực, mục thứ {pos}, thẻ thứ {tagPos}: thẻ không được để trống.");
                    else
                    {
                        var trimmedTag = tag.Trim();
                        if (trimmedTag.Length > 30)
                            errors.Add($"Lĩnh vực, mục thứ {pos}, thẻ thứ {tagPos}: thẻ không được quá 30 ký tự.");
                        if (HasInvalidControlChars(tag))
                            errors.Add($"Lĩnh vực, mục thứ {pos}, thẻ thứ {tagPos}: thẻ không được chứa ký tự điều khiển.");
                    }
                }
            }
        }
    }

    private static void ValidateEnvironment(AboutEnvironmentDto? env, List<string> errors)
    {
        if (env is null) return;

        // Highlights
        if (env.Highlights is not null)
        {
            if (env.Highlights.Count > 8)
                errors.Add("Môi trường làm việc - Điểm nổi bật: không được quá 8 mục.");

            for (int i = 0; i < env.Highlights.Count; i++)
            {
                var item = env.Highlights[i];
                int pos = i + 1;

                if (!AboutIcons.IsValid(item.Icon))
                    errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: biểu tượng không hợp lệ.");

                if (string.IsNullOrWhiteSpace(item.Title))
                    errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: tiêu đề không được để trống.");
                else
                {
                    var trimmed = item.Title.Trim();
                    if (trimmed.Length > 150)
                        errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: tiêu đề không được quá 150 ký tự.");
                    if (HasInvalidControlChars(item.Title))
                        errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: tiêu đề không được chứa ký tự điều khiển.");
                }

                if (string.IsNullOrWhiteSpace(item.Text))
                    errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: mô tả không được để trống.");
                else
                {
                    var trimmed = item.Text.Trim();
                    if (trimmed.Length > 300)
                        errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: mô tả không được quá 300 ký tự.");
                    if (HasInvalidControlChars(item.Text))
                        errors.Add($"Môi trường làm việc, điểm nổi bật thứ {pos}: mô tả không được chứa ký tự điều khiển.");
                }
            }
        }

        // Week
        if (env.Week is not null)
        {
            if (env.Week.Count > 7)
                errors.Add("Môi trường làm việc - Lịch tuần: không được quá 7 mục.");

            for (int i = 0; i < env.Week.Count; i++)
            {
                var item = env.Week[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item.Day))
                    errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: ngày/thời gian không được để trống.");
                else
                {
                    var trimmed = item.Day.Trim();
                    if (trimmed.Length > 50)
                        errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: ngày/thời gian không được quá 50 ký tự.");
                    if (HasInvalidControlChars(item.Day))
                        errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: ngày/thời gian không được chứa ký tự điều khiển.");
                }

                if (string.IsNullOrWhiteSpace(item.Text))
                    errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: nội dung không được để trống.");
                else
                {
                    var trimmed = item.Text.Trim();
                    if (trimmed.Length > 300)
                        errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: nội dung không được quá 300 ký tự.");
                    if (HasInvalidControlChars(item.Text))
                        errors.Add($"Môi trường làm việc, lịch tuần mục thứ {pos}: nội dung không được chứa ký tự điều khiển.");
                }
            }
        }

        // Tools
        if (env.Tools is not null)
        {
            if (env.Tools.Count > 12)
                errors.Add("Môi trường làm việc - Công cụ: không được quá 12 công cụ.");

            for (int i = 0; i < env.Tools.Count; i++)
            {
                var tool = env.Tools[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(tool))
                    errors.Add($"Môi trường làm việc, công cụ thứ {pos}: tên công cụ không được để trống.");
                else
                {
                    var trimmed = tool.Trim();
                    if (trimmed.Length > 40)
                        errors.Add($"Môi trường làm việc, công cụ thứ {pos}: tên công cụ không được quá 40 ký tự.");
                    if (HasInvalidControlChars(tool))
                        errors.Add($"Môi trường làm việc, công cụ thứ {pos}: tên công cụ không được chứa ký tự điều khiển.");
                }
            }
        }
    }

    private static void ValidateValues(List<AboutValueDto>? values, List<string> errors)
    {
        if (values is null) return;

        if (values.Count > 10)
            errors.Add("Giá trị cốt lõi: không được quá 10 mục.");

        for (int i = 0; i < values.Count; i++)
        {
            var item = values[i];
            int pos = i + 1;

            if (!AboutIcons.IsValid(item.Icon))
                errors.Add($"Giá trị cốt lõi, mục thứ {pos}: biểu tượng không hợp lệ.");

            if (string.IsNullOrWhiteSpace(item.Title))
                errors.Add($"Giá trị cốt lõi, mục thứ {pos}: tiêu đề không được để trống.");
            else
            {
                var trimmed = item.Title.Trim();
                if (trimmed.Length > 150)
                    errors.Add($"Giá trị cốt lõi, mục thứ {pos}: tiêu đề không được quá 150 ký tự.");
                if (HasInvalidControlChars(item.Title))
                    errors.Add($"Giá trị cốt lõi, mục thứ {pos}: tiêu đề không được chứa ký tự điều khiển.");
            }

            if (string.IsNullOrWhiteSpace(item.Text))
                errors.Add($"Giá trị cốt lõi, mục thứ {pos}: mô tả không được để trống.");
            else
            {
                var trimmed = item.Text.Trim();
                if (trimmed.Length > 300)
                    errors.Add($"Giá trị cốt lõi, mục thứ {pos}: mô tả không được quá 300 ký tự.");
                if (HasInvalidControlChars(item.Text))
                    errors.Add($"Giá trị cốt lõi, mục thứ {pos}: mô tả không được chứa ký tự điều khiển.");
            }
        }
    }

    private static void ValidateStudentInfo(AboutStudentInfoDto? info, List<string> errors)
    {
        if (info is null) return;

        // Eligibility
        if (info.Eligibility is not null)
        {
            if (info.Eligibility.Count > 10)
                errors.Add("Thông tin sinh viên - Điều kiện: không được quá 10 mục.");

            for (int i = 0; i < info.Eligibility.Count; i++)
            {
                var item = info.Eligibility[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item))
                    errors.Add($"Thông tin sinh viên, điều kiện thứ {pos}: nội dung không được để trống.");
                else
                {
                    var trimmed = item.Trim();
                    if (trimmed.Length > 300)
                        errors.Add($"Thông tin sinh viên, điều kiện thứ {pos}: nội dung không được quá 300 ký tự.");
                    if (HasInvalidControlChars(item))
                        errors.Add($"Thông tin sinh viên, điều kiện thứ {pos}: nội dung không được chứa ký tự điều khiển.");
                }
            }
        }

        // Documents
        if (info.Documents is not null)
        {
            if (info.Documents.Count > 10)
                errors.Add("Thông tin sinh viên - Hồ sơ: không được quá 10 mục.");

            for (int i = 0; i < info.Documents.Count; i++)
            {
                var item = info.Documents[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item))
                    errors.Add($"Thông tin sinh viên, hồ sơ thứ {pos}: tên giấy tờ không được để trống.");
                else
                {
                    var trimmed = item.Trim();
                    if (trimmed.Length > 150)
                        errors.Add($"Thông tin sinh viên, hồ sơ thứ {pos}: tên giấy tờ không được quá 150 ký tự.");
                    if (HasInvalidControlChars(item))
                        errors.Add($"Thông tin sinh viên, hồ sơ thứ {pos}: tên giấy tờ không được chứa ký tự điều khiển.");
                }
            }
        }

        // Process
        if (info.Process is not null)
        {
            if (info.Process.Count > 8)
                errors.Add("Thông tin sinh viên - Quy trình: không được quá 8 bước.");

            for (int i = 0; i < info.Process.Count; i++)
            {
                var item = info.Process[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item.Status))
                    errors.Add($"Thông tin sinh viên, quy trình bước {pos}: trạng thái không được để trống.");
                else
                {
                    var trimmed = item.Status.Trim();
                    if (trimmed.Length > 50)
                        errors.Add($"Thông tin sinh viên, quy trình bước {pos}: trạng thái không được quá 50 ký tự.");
                    if (HasInvalidControlChars(item.Status))
                        errors.Add($"Thông tin sinh viên, quy trình bước {pos}: trạng thái không được chứa ký tự điều khiển.");
                }

                if (string.IsNullOrWhiteSpace(item.Text))
                    errors.Add($"Thông tin sinh viên, quy trình bước {pos}: mô tả không được để trống.");
                else
                {
                    var trimmed = item.Text.Trim();
                    if (trimmed.Length > 300)
                        errors.Add($"Thông tin sinh viên, quy trình bước {pos}: mô tả không được quá 300 ký tự.");
                    if (HasInvalidControlChars(item.Text))
                        errors.Add($"Thông tin sinh viên, quy trình bước {pos}: mô tả không được chứa ký tự điều khiển.");
                }
            }
        }

        // Checklist
        if (info.Checklist is not null)
        {
            if (info.Checklist.Count > 10)
                errors.Add("Thông tin sinh viên - Checklist: không được quá 10 mục.");

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < info.Checklist.Count; i++)
            {
                var item = info.Checklist[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item.Id) || item.Id.Trim().Length > 40 || !ChecklistIdRegex.IsMatch(item.Id.Trim()))
                {
                    errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: mã định danh (id) phải từ 1 đến 40 ký tự và chỉ chứa chữ thường, số, dấu gạch ngang.");
                }
                else
                {
                    var id = item.Id.Trim();
                    if (HasInvalidControlChars(item.Id))
                        errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: mã định danh (id) không được chứa ký tự điều khiển.");
                    else if (!seenIds.Add(id))
                        errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: mã định danh (id) '{id}' bị trùng lặp.");
                }

                if (string.IsNullOrWhiteSpace(item.Label))
                    errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: nội dung không được để trống.");
                else
                {
                    var trimmed = item.Label.Trim();
                    if (trimmed.Length > 200)
                        errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: nội dung không được quá 200 ký tự.");
                    if (HasInvalidControlChars(item.Label))
                        errors.Add($"Thông tin sinh viên, checklist mục thứ {pos}: nội dung không được chứa ký tự điều khiển.");
                }
            }
        }

        // FAQ
        if (info.Faq is not null)
        {
            if (info.Faq.Count > 15)
                errors.Add("Thông tin sinh viên - Câu hỏi thường gặp: không được quá 15 câu.");

            for (int i = 0; i < info.Faq.Count; i++)
            {
                var item = info.Faq[i];
                int pos = i + 1;

                if (string.IsNullOrWhiteSpace(item.Q))
                    errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu hỏi không được để trống.");
                else
                {
                    var trimmed = item.Q.Trim();
                    if (trimmed.Length > 200)
                        errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu hỏi không được quá 200 ký tự.");
                    if (HasInvalidControlChars(item.Q))
                        errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu hỏi không được chứa ký tự điều khiển.");
                }

                if (string.IsNullOrWhiteSpace(item.A))
                    errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu trả lời không được để trống.");
                else
                {
                    var trimmed = item.A.Trim();
                    if (trimmed.Length > 1000)
                        errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu trả lời không được quá 1000 ký tự.");
                    if (HasInvalidControlChars(item.A, allowLineBreaks: true))
                        errors.Add($"Thông tin sinh viên, câu hỏi thường gặp thứ {pos}: câu trả lời không được chứa ký tự điều khiển không hợp lệ.");
                }
            }
        }
    }

    private static void ValidateContact(AboutContactDto? contact, List<string> errors)
    {
        if (contact is null) return;

        // Person
        if (!string.IsNullOrWhiteSpace(contact.Person))
        {
            var trimmed = contact.Person.Trim();
            if (trimmed.Length > 150)
                errors.Add("Liên hệ: người phụ trách không được quá 150 ký tự.");
            if (HasInvalidControlChars(contact.Person))
                errors.Add("Liên hệ: người phụ trách không được chứa ký tự điều khiển.");
        }

        // Address
        if (!string.IsNullOrWhiteSpace(contact.Address))
        {
            var trimmed = contact.Address.Trim();
            if (trimmed.Length > 300)
                errors.Add("Liên hệ: địa chỉ không được quá 300 ký tự.");
            if (HasInvalidControlChars(contact.Address))
                errors.Add("Liên hệ: địa chỉ không được chứa ký tự điều khiển.");
        }

        // Hours
        if (!string.IsNullOrWhiteSpace(contact.Hours))
        {
            var trimmed = contact.Hours.Trim();
            if (trimmed.Length > 100)
                errors.Add("Liên hệ: giờ làm việc không được quá 100 ký tự.");
            if (HasInvalidControlChars(contact.Hours))
                errors.Add("Liên hệ: giờ làm việc không được chứa ký tự điều khiển.");
        }

        // Email
        if (!string.IsNullOrWhiteSpace(contact.Email))
        {
            var trimmed = contact.Email.Trim();
            if (trimmed.Length > 150)
                errors.Add("Liên hệ: email không được quá 150 ký tự.");
            else if (HasInvalidControlChars(contact.Email))
                errors.Add("Liên hệ: email không được chứa ký tự điều khiển.");
            else if (!EmailAddressValidator.TryValidate(trimmed, out var emailError))
                errors.Add($"Liên hệ: {emailError ?? "Email không đúng định dạng."}");
        }

        // Phone
        if (!string.IsNullOrWhiteSpace(contact.Phone))
        {
            var trimmed = contact.Phone.Trim();
            if (trimmed.Length > 20)
                errors.Add("Liên hệ: số điện thoại không được quá 20 ký tự.");
            else if (HasInvalidControlChars(contact.Phone))
                errors.Add("Liên hệ: số điện thoại không được chứa ký tự điều khiển.");
            else
            {
                bool validChars = trimmed.All(c => char.IsDigit(c) || c is ' ' or '+' or '-' or '(' or ')' or '.');
                int digitCount = trimmed.Count(char.IsDigit);
                if (!validChars || digitCount < 8)
                    errors.Add("Liên hệ: số điện thoại chỉ gồm chữ số, dấu cách, +, -, (, ), . và phải có ít nhất 8 chữ số.");
            }
        }
    }

    private static bool HasInvalidControlChars(string? text, bool allowLineBreaks = false)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.Any(c => char.IsControl(c) && !(allowLineBreaks && c is '\r' or '\n'));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
