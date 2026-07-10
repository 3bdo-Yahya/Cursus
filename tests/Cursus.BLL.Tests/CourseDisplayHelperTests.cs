using Cursus.BLL.Services;
using Xunit;

namespace Cursus.BLL.Tests;

public sealed class CourseDisplayHelperTests
{
    [Fact]
    public void Label_UsesNameWithCodeInParentheses()
    {
        var label = CourseDisplayHelper.Label("CS241", "Data Structures");
        Assert.Equal("Data Structures (CS241)", label);
    }

    [Fact]
    public void Label_FallsBackToCodeWhenNameMissing()
    {
        Assert.Equal("CS241", CourseDisplayHelper.Label("CS241", ""));
    }

    [Theory]
    [InlineData("ELEC-CORE-1", true)]
    [InlineData("CS241", false)]
    public void IsVirtualPlaceholderCode_DetectsSimulationElectives(string code, bool expected)
    {
        Assert.Equal(expected, CourseDisplayHelper.IsVirtualPlaceholderCode(code));
    }
}
