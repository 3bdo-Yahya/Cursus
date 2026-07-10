using Cursus.Domain.Entities;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Services;

/// <summary>
/// Maps plan-of-study <see cref="Course.RecommendedSemester"/> (1–8) to year/term labels.
/// 1 = Year 1 Fall, 2 = Year 1 Spring, … 8 = Year 4 Spring.
/// </summary>
public static class SemesterMath
{
    public static int GetYearNumber(int recommendedSemester) =>
        (int)Math.Ceiling(recommendedSemester / 2.0);

    public static SemesterType GetTermType(int recommendedSemester) =>
        recommendedSemester % 2 == 1 ? SemesterType.Fall : SemesterType.Spring;

    public static string ToPlanLabel(int recommendedSemester) =>
        $"Year {GetYearNumber(recommendedSemester)} {GetTermType(recommendedSemester)}";

    /// <summary>
    /// Derives a fallback recommended semester from the first digit of the course code (CS3xx → Year 3 Fall).
    /// </summary>
    public static int? InferFromCourseCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        foreach (var ch in code)
        {
            if (char.IsDigit(ch))
            {
                var year = ch - '0';
                if (year is >= 1 and <= 4)
                    return year * 2 - 1;

                break;
            }
        }

        return null;
    }

    public static int ResolveRecommendedSemester(Course course) =>
        course.RecommendedSemester ?? InferFromCourseCode(course.Code) ?? 99;

    public static bool IsFallTerm(int? recommendedSemester) =>
        recommendedSemester is > 0 && recommendedSemester % 2 == 1;

    public static bool IsSpringTerm(int? recommendedSemester) =>
        recommendedSemester is > 0 && recommendedSemester % 2 == 0;
}
