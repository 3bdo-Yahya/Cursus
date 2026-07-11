using Cursus.Domain.Entities;

namespace Cursus.BLL.Services;

/// <summary>
/// Advisor-facing course labels for impact reports and recommendations.
/// </summary>
public static class CourseDisplayHelper
{
    public static string Label(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return name;

        if (string.IsNullOrWhiteSpace(name) || string.Equals(name, code, StringComparison.OrdinalIgnoreCase))
            return code;

        return $"{name} ({code})";
    }

    public static string Label(Course course) => Label(course.Code, course.Name);

    public static bool IsVirtualPlaceholder(Course course) => course.Id < 0;

    public static bool IsVirtualPlaceholderCode(string code) =>
        code.StartsWith("ELEC-", StringComparison.OrdinalIgnoreCase);
}
