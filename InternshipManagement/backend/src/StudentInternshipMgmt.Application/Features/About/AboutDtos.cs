namespace StudentInternshipMgmt.Application.Features.About;

public class AboutFactDto
{
    public string Icon { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AboutHistoryDto
{
    public string Year { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AboutFieldDto
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public class AboutHighlightDto
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AboutWeekScheduleDto
{
    public string Day { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AboutEnvironmentDto
{
    public List<AboutHighlightDto> Highlights { get; set; } = new();
    public List<AboutWeekScheduleDto> Week { get; set; } = new();
    public List<string> Tools { get; set; } = new();
}

public class AboutValueDto
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AboutProcessStepDto
{
    public string Status { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public class AboutChecklistItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class AboutFaqItemDto
{
    public string Q { get; set; } = string.Empty;
    public string A { get; set; } = string.Empty;
}

public class AboutStudentInfoDto
{
    public List<string> Eligibility { get; set; } = new();
    public List<string> Documents { get; set; } = new();
    public List<AboutProcessStepDto> Process { get; set; } = new();
    public List<AboutChecklistItemDto> Checklist { get; set; } = new();
    public List<AboutFaqItemDto> Faq { get; set; } = new();
}

public class AboutContactDto
{
    public string? Person { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Hours { get; set; }
}

public class AboutContentDto
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string Tagline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Footnote { get; set; }
    public List<AboutFactDto> Facts { get; set; } = new();
    public List<AboutHistoryDto> History { get; set; } = new();
    public List<AboutFieldDto> Fields { get; set; } = new();
    public AboutEnvironmentDto Environment { get; set; } = new();
    public List<AboutValueDto> Values { get; set; } = new();
    public AboutStudentInfoDto StudentInfo { get; set; } = new();
    public AboutContactDto Contact { get; set; } = new();
}

public class AboutPageDto
{
    public AboutContentDto Content { get; set; } = new();
    public DateTime? UpdatedAt { get; set; }
}
