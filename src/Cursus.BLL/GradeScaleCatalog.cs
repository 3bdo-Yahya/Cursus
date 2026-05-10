using Cursus.Domain.Enums;

namespace Cursus.BLL;

public static class GradeScaleCatalog
{
    public static readonly IReadOnlyList<string> LetterGrades =
    [
        "A+",
        "A",
        "A-",
        "B+",
        "B",
        "B-",
        "C+",
        "C",
        "C-",
        "D+",
        "D",
        "F"
    ];

    public static string? Normalize(string? grade)
    {
        if (string.IsNullOrWhiteSpace(grade))
        {
            return null;
        }

        var normalizedGrade = grade.Trim().ToUpperInvariant();
        return LetterGrades.Contains(normalizedGrade, StringComparer.OrdinalIgnoreCase)
            ? normalizedGrade
            : null;
    }

    public static bool IsValid(string? grade) => Normalize(grade) is not null;

    public static StudentCourseStatus DetermineStatus(string grade, string passingThreshold)
    {
        var normalizedGrade = Normalize(grade) ?? grade.Trim().ToUpperInvariant();
        var normalizedThreshold = Normalize(passingThreshold) ?? "D";

        return GetRank(normalizedGrade) <= GetRank(normalizedThreshold)
            ? StudentCourseStatus.Completed
            : StudentCourseStatus.Failed;
    }

    private static int GetRank(string grade)
    {
        var index = Array.FindIndex(LetterGrades.ToArray(), value =>
            string.Equals(value, grade, StringComparison.OrdinalIgnoreCase));

        return index >= 0 ? index : LetterGrades.Count - 1;
    }
}
