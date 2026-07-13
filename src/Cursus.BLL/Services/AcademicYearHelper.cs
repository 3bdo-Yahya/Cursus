using Cursus.Domain.Enums;

namespace Cursus.BLL.Services;

/// <summary>
/// Normalizes academic year labels and derives display year numbers from student progress.
/// </summary>
public static class AcademicYearHelper
{
    /// <summary>
    /// Derives academic year number (1–4) from graded term count.
    /// </summary>
    public static int DeriveYearNumber(int gradedTermCount) =>
        Math.Max(1, Math.Min(4, (gradedTermCount / 2) + 1));

    /// <summary>
    /// Returns true when <paramref name="academicYear"/> uses calendar format (YYYY-YYYY).
    /// </summary>
    public static bool IsCalendarFormat(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return false;

        var parts = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 2
               && int.TryParse(parts[0], out var start)
               && int.TryParse(parts[1], out var end)
               && start > 1900
               && end == start + 1;
    }

    /// <summary>
    /// Resolves the calendar start year from an academic year label.
    /// Ordinal values (1–10) are mapped to a demo calendar anchor instead of being used literally.
    /// </summary>
    public static int ParseCalendarYearStart(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var part = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!int.TryParse(part, out var year))
            return DateTime.UtcNow.Year;

        if (year is >= 1 and <= 10)
            return DateTime.UtcNow.Year - 4 + year;

        return year > 1900 ? year : DateTime.UtcNow.Year;
    }

    /// <summary>
    /// Calendar year for the active term within an academic year (Spring/Summer use start+1).
    /// </summary>
    public static int TermCalendarYear(string? academicYear, SemesterType semester)
    {
        var start = ParseCalendarYearStart(academicYear);
        return semester == SemesterType.Fall ? start : start + 1;
    }
}

