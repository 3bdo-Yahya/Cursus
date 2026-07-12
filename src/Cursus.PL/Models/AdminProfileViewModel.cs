using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.PL.Models;

/// <summary>
/// Real data available for the Admin/Profile page without any new DB tables.
/// Sources: AspNetUsers (UserManager), AdminDashboardDto, StudentManagementService.
/// </summary>
public class AdminProfileViewModel
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public string DisplayName    { get; init; } = "Administrator";
    public string Email          { get; init; } = string.Empty;
    public string UserId         { get; init; } = string.Empty;
    public bool   EmailConfirmed { get; init; }

    /// <summary>Two-letter initials derived from the display name.</summary>
    public string Initials { get; init; } = "AD";

    // ── System-wide stats (from AdminDashboardDto) ────────────────────────────
    public int TotalStudents    { get; init; }
    public int TotalCourses     { get; init; }
    public int ActiveCourses    { get; init; }
    public int TotalDepartments { get; init; }
    public int ActiveDepartments{ get; init; }
    public int TotalUniversities{ get; init; }

    // ── Student standing breakdown ────────────────────────────────────────────
    public int GoodStanding       { get; init; }
    public int WarningOrProbation { get; init; }
    public int Dismissed          { get; init; }

    /// <summary>Percentage of students in good standing (0–100).</summary>
    public double GoodStandingPct => TotalStudents > 0
        ? Math.Round((double)GoodStanding / TotalStudents * 100, 1)
        : 0;

    // ── Course breakdown ──────────────────────────────────────────────────────
    public int InactiveCourses { get; init; }

    // ── Helpers ───────────────────────────────────────────────────────────────
    /// <summary>Bar width (0–100) for a given value out of total.</summary>
    public double BarPct(int value, int total) =>
        total > 0 ? Math.Min(100, Math.Round((double)value / total * 100, 1)) : 0;
}
