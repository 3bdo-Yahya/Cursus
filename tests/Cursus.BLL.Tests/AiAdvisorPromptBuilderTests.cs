using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Tests;

public sealed class AiAdvisorPromptBuilderTests
{
    [Fact]
    public void BuildSystemPrompt_IncludesGroundingDataAndCourseGrades()
    {
        var context = new AiAdvisorContextDto
        {
            DisplayName = "Ahmed Kamal",
            DepartmentName = "Computer\nScience",
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Spring,
            AcademicStanding = AcademicStanding.Good,
            Cgpa = 3.24m,
            MinGpaForGraduation = 2.0m,
            CreditsCompleted = 84,
            CreditsRequired = 132,
            CreditsRemaining = 48,
            OverallProgressPercentage = 64,
            IsOverloadEligible = true,
            IsOnTrack = false,
            ProjectedGraduation = "Spring 2027",
            CategoryProgress =
            [
                new AiAdvisorCategoryProgressDto
                {
                    Label = "Core Courses",
                    RequiredCredits = 72,
                    EarnedCredits = 60,
                    InProgressCredits = 3,
                    Percentage = 83,
                    IsSatisfied = false
                }
            ],
            CompletedCourses =
            [
                new AiAdvisorCourseDto
                {
                    Code = "CS201",
                    Name = "Data Structures",
                    Grade = "B+",
                    CreditHours = 3
                }
            ],
            InProgressCourses =
            [
                new AiAdvisorCourseDto
                {
                    Code = "CS301",
                    Name = "Operating Systems",
                    CreditHours = 3
                }
            ],
            FailedOrLowGradeCourses =
            [
                new AiAdvisorCourseDto
                {
                    Code = "MTH102",
                    Name = "Calculus II",
                    Grade = "D",
                    CreditHours = 3
                }
            ],
            AvailableCourses =
            [
                new AiAdvisorCourseDto
                {
                    Code = "CS302",
                    Name = "Databases",
                    CreditHours = 3
                }
            ],
            LockedCourses =
            [
                new AiAdvisorCourseDto
                {
                    Code = "CS401",
                    Name = "Capstone Project",
                    CreditHours = 3
                }
            ]
        };

        var prompt = AiAdvisorPromptBuilder.BuildSystemPrompt(context);

        Assert.Contains("Ahmed Kamal", prompt);
        Assert.Contains("Department: Computer Science", prompt);
        Assert.Contains("Cumulative GPA: 3.24", prompt);
        Assert.Contains("Minimum graduation GPA: 2.00", prompt);
        Assert.Contains("Credits completed: 84/132", prompt);
        Assert.Contains("Credits remaining: 48", prompt);
        Assert.Contains("Overall progress: 64%", prompt);
        Assert.Contains("Overload eligible: Yes", prompt);
        Assert.Contains("On track to graduate: No", prompt);
        Assert.Contains("Core Courses: 60/72 credits earned, 3 in progress, 83% (remaining)", prompt);
        Assert.Contains("CS201: Data Structures, grade B+", prompt);
        Assert.Contains("CS301: Operating Systems", prompt);
        Assert.Contains("MTH102: Calculus II, grade D", prompt);
        Assert.Contains("CS302: Databases", prompt);
        Assert.Contains("CS401: Capstone Project", prompt);
        Assert.Contains("Use AVAILABLE COURSES for next-semester suggestions", prompt);
        Assert.Contains("instead of inventing courses", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_UsesExplicitFallbacksForMissingData()
    {
        var prompt = AiAdvisorPromptBuilder.BuildSystemPrompt(new AiAdvisorContextDto());

        Assert.Contains("Name: Student", prompt);
        Assert.Contains("Department: Not assigned", prompt);
        Assert.Contains("Academic year: Unknown", prompt);
        Assert.Contains("Current semester: Unknown", prompt);
        Assert.Contains("Cumulative GPA: Unknown", prompt);
        Assert.Contains("Minimum graduation GPA: Unknown", prompt);
        Assert.Contains("Academic standing: Unknown", prompt);
        Assert.Contains("Credits completed: Unknown", prompt);
        Assert.Contains("Credits remaining: Unknown", prompt);
        Assert.Contains("Overall progress: Unknown", prompt);
        Assert.Contains("Overload eligible: Unknown", prompt);
        Assert.Contains("On track to graduate: Unknown", prompt);
        Assert.Contains("Projected graduation: Unknown", prompt);
        Assert.Equal(6, CountOccurrences(prompt, "- None provided"));
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }
}
