namespace StudentInternshipMgmt.Application.Features.About;

public static class AboutIcons
{
    public static readonly HashSet<string> AllowedIcons = new(StringComparer.Ordinal)
    {
        "pi pi-calendar",
        "pi pi-users",
        "pi pi-briefcase",
        "pi pi-clock",
        "pi pi-map-marker",
        "pi pi-wallet",
        "pi pi-server",
        "pi pi-desktop",
        "pi pi-mobile",
        "pi pi-check-circle",
        "pi pi-user",
        "pi pi-sync",
        "pi pi-code",
        "pi pi-shield",
        "pi pi-book",
        "pi pi-verified",
        "pi pi-heart",
        "pi pi-share-alt",
        "pi pi-building",
        "pi pi-globe",
        "pi pi-star",
        "pi pi-award",
        "pi pi-graduation-cap",
        "pi pi-lightbulb",
        "pi pi-bolt",
        "pi pi-compass",
        "pi pi-cog",
        "pi pi-chart-bar",
        "pi pi-comments",
        "pi pi-folder"
    };

    public static bool IsValid(string? icon)
    {
        return !string.IsNullOrWhiteSpace(icon) && AllowedIcons.Contains(icon.Trim());
    }
}
