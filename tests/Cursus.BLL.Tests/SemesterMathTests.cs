using Cursus.BLL.Services;
using Cursus.Domain.Enums;
using Xunit;

namespace Cursus.BLL.Tests;

public sealed class SemesterMathTests
{
    [Theory]
    [InlineData(1, 1, SemesterType.Fall)]
    [InlineData(2, 1, SemesterType.Spring)]
    [InlineData(3, 2, SemesterType.Fall)]
    [InlineData(8, 4, SemesterType.Spring)]
    public void MapsRecommendedSemesterToYearAndTerm(int semester, int year, SemesterType term)
    {
        Assert.Equal(year, SemesterMath.GetYearNumber(semester));
        Assert.Equal(term, SemesterMath.GetTermType(semester));
        Assert.Contains($"Year {year}", SemesterMath.ToPlanLabel(semester));
    }

    [Theory]
    [InlineData("CS241", 5)]
    [InlineData("MATH101", 1)]
    [InlineData("ENG401", 7)]
    public void InfersSemesterFromCourseCode(string code, int expected)
    {
        Assert.Equal(expected, SemesterMath.InferFromCourseCode(code));
    }
}
