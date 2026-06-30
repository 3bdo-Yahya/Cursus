using Cursus.Domain.Enums;

namespace Cursus.BLL.Services;

/// <summary>
/// Demo-scoped graduation delay estimate based on retake availability,
/// standing credit caps, and remaining blocked credits.
/// </summary>
internal static class GraduationDelayCalculator
{
    private const int DefaultMaxCreditsPerSemester = 18;

    internal sealed record Result(
        int GraduationDelaySemesters,
        int RetakeDelaySemesters,
        int RecoverySemesters,
        int MaxCreditsPerSemester,
        string RetakeSemesterLabel,
        string ProjectedGraduationLabel);

    internal static Result Calculate(
        SemesterType currentSemester,
        string? academicYear,
        AcademicStanding standing,
        decimal cgpa,
        SemesterAvailability failedCourseAvailability,
        int blockedCredits,
        int cascadeDepth)
    {
        var maxCredits = GetMaxCreditsPerSemester(standing, cgpa);
        var retakeDelay = SemestersUntilOffering(currentSemester, failedCourseAvailability);

        var creditSemesters = blockedCredits > 0
            ? (int)Math.Ceiling((double)blockedCredits / maxCredits)
            : 0;
        var recoverySemesters = Math.Max(creditSemesters, cascadeDepth);

        var graduationDelay = retakeDelay + recoverySemesters;

        var retakeSemesterLabel = FormatSemesterAfter(
            currentSemester, academicYear, retakeDelay);
        var projectedGraduationLabel = FormatSemesterAfter(
            currentSemester, academicYear, graduationDelay);

        return new Result(
            GraduationDelaySemesters: graduationDelay,
            RetakeDelaySemesters: retakeDelay,
            RecoverySemesters: recoverySemesters,
            MaxCreditsPerSemester: maxCredits,
            RetakeSemesterLabel: retakeSemesterLabel,
            ProjectedGraduationLabel: projectedGraduationLabel);
    }

    internal static int GetMaxCreditsPerSemester(AcademicStanding standing, decimal cgpa) =>
        standing switch
        {
            AcademicStanding.Probation => 12,
            AcademicStanding.Warning => 15,
            _ => cgpa >= 3.0m ? 21 : DefaultMaxCreditsPerSemester
        };

    internal static int SemestersUntilOffering(
        SemesterType currentSemester,
        SemesterAvailability availability)
    {
        if (availability == SemesterAvailability.All)
            return 1;

        var semester = currentSemester;
        for (var wait = 1; wait <= 4; wait++)
        {
            semester = NextSemester(semester);
            if (IsOfferedIn(semester, availability))
                return wait;
        }

        return 1;
    }

    private static bool IsOfferedIn(SemesterType semester, SemesterAvailability availability) =>
        availability switch
        {
            SemesterAvailability.All => true,
            SemesterAvailability.FallSpring =>
                semester is SemesterType.Fall or SemesterType.Spring,
            SemesterAvailability.Fall => semester == SemesterType.Fall,
            SemesterAvailability.Spring => semester == SemesterType.Spring,
            _ => true
        };

    private static SemesterType NextSemester(SemesterType semester) =>
        semester switch
        {
            SemesterType.Fall => SemesterType.Spring,
            SemesterType.Spring => SemesterType.Summer,
            _ => SemesterType.Fall
        };

    private static string FormatSemesterAfter(
        SemesterType currentSemester,
        string? academicYear,
        int semestersAhead)
    {
        var yearStart = ParseAcademicYearStart(academicYear);
        var semester = currentSemester;
        var year = currentSemester switch
        {
            SemesterType.Fall => yearStart,
            _ => yearStart + 1
        };

        for (var i = 0; i < semestersAhead; i++)
            (semester, year) = AdvanceSemester(semester, year);

        return $"{semester} {year}";
    }

    private static (SemesterType semester, int year) AdvanceSemester(
        SemesterType semester, int year) =>
        semester switch
        {
            SemesterType.Fall => (SemesterType.Spring, year + 1),
            SemesterType.Spring => (SemesterType.Summer, year),
            _ => (SemesterType.Fall, year)
        };

    private static int ParseAcademicYearStart(string? academicYear)
    {
        if (string.IsNullOrWhiteSpace(academicYear))
            return DateTime.UtcNow.Year;

        var part = academicYear.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return int.TryParse(part, out var year) ? year : DateTime.UtcNow.Year;
    }
}
