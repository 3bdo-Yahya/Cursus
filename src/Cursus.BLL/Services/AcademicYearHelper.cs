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
}
