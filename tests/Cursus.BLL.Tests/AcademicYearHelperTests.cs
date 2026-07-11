using Cursus.BLL.Services;
using Xunit;

namespace Cursus.BLL.Tests;

public sealed class AcademicYearHelperTests
{
    [Theory]
    [InlineData("2025-2026", 2025)]
    [InlineData("5")]
    public void ParseCalendarYearStart_HandlesCalendarAndOrdinal(string academicYear, int? expectedStart = null)
    {
        var expected = expectedStart ?? DateTime.UtcNow.Year - 4 + int.Parse(academicYear);
        Assert.Equal(expected, AcademicYearHelper.ParseCalendarYearStart(academicYear));
    }
}
