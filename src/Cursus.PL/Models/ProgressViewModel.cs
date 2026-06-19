using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.PL.Models;

/// <summary>
/// ViewModel for the Student Progress Tracker page.
/// Wraps <see cref="GraduationAuditDto"/> with additional UI helpers
/// so the Razor view stays free of display logic.
/// </summary>
public sealed class ProgressViewModel
{
    public required GraduationAuditDto Audit { get; init; }

    // ── Category UI metadata ──────────────────────────────────────────────

    /// <summary>Returns the CSS color-modifier suffix for a category card bar / badge.</summary>
    public static string GetCategoryColorSuffix(CourseType type) => type switch
    {
        CourseType.Core          => "blue",
        CourseType.DeptElective  => "purple",
        CourseType.FreeElective  => "amber",
        CourseType.UniversityReq => "green",
        _                        => "blue"
    };

    /// <summary>Returns the Material Symbols icon name for a category.</summary>
    public static string GetCategoryIcon(CourseType type) => type switch
    {
        CourseType.Core          => "menu_book",
        CourseType.DeptElective  => "hub",
        CourseType.FreeElective  => "auto_awesome",
        CourseType.UniversityReq => "account_balance",
        _                        => "school"
    };

    /// <summary>Returns the inline hex/var color for a category icon.</summary>
    public static string GetCategoryIconColor(CourseType type) => type switch
    {
        CourseType.Core          => "var(--c-primary)",
        CourseType.DeptElective  => "#7c3aed",
        CourseType.FreeElective  => "#b45309",
        CourseType.UniversityReq => "#047857",
        _                        => "var(--c-primary)"
    };

    /// <summary>Returns the background color CSS value for the category icon box.</summary>
    public static string GetCategoryIconBg(CourseType type) => type switch
    {
        CourseType.Core          => "var(--icon-blue-bg)",
        CourseType.DeptElective  => "var(--icon-purple-bg)",
        CourseType.FreeElective  => "var(--icon-amber-bg)",
        CourseType.UniversityReq => "rgba(16,185,129,.12)",
        _                        => "var(--icon-blue-bg)"
    };

    // ── Course status UI helpers ──────────────────────────────────────────

    public static string GetStatusIconName(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed  => "check",
        CourseAuditStatus.InProgress => "cached",
        CourseAuditStatus.Failed     => "close",
        CourseAuditStatus.Available  => "circle",
        CourseAuditStatus.Locked     => "lock",
        _                            => "circle"
    };

    public static string GetStatusCssClass(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed  => "status-done",
        CourseAuditStatus.InProgress => "status-progress",
        CourseAuditStatus.Failed     => "status-failed",
        CourseAuditStatus.Available  => "status-open",
        CourseAuditStatus.Locked     => "status-locked",
        _                            => "status-open"
    };

    public static string GetStatusIconColor(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed  => "#10b981",
        CourseAuditStatus.InProgress => "var(--c-primary)",
        CourseAuditStatus.Failed     => "#ef4444",
        _                            => "var(--c-muted)"
    };

    public static string GetStatusIconVariation(CourseAuditStatus status) => status switch
    {
        CourseAuditStatus.Completed  => "'FILL' 1,'wght' 500",
        CourseAuditStatus.InProgress => "'FILL' 1,'wght' 500",
        CourseAuditStatus.Failed     => "'FILL' 1,'wght' 500",
        _                            => "'FILL' 0,'wght' 300"
    };

    // ── Grade pill CSS class ───────────────────────────────────────────────

    /// <summary>Maps a letter grade to one of the grade-pill CSS classes.</summary>
    public static string GetGradePillClass(string? grade) => grade switch
    {
        "A+" or "A" or "A-" => "grade-a",
        "B+" or "B" or "B-" => "grade-b",
        "C+" or "C" or "C-" => "grade-c",
        _                    => "grade-d"
    };
}
