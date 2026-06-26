using System;
using System.Linq;
using System.Text;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;

namespace Cursus.BLL.Services
{
    public static class GeminiPromptBuilder
    {
        public static string BuildPrompt(GraduationAuditDto audit, ChatRequestDto request)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var sb = new StringBuilder();

            // 1. System Instructions & Grounding Data
            sb.AppendLine("You are the Cursus AI Academic Advisor, a friendly and supportive advisor at a credit-hour university.");
            sb.AppendLine("Provide concise, realistic, and supportive guidance grounded strictly in the student's profile data.");
            sb.AppendLine("If the student asks about consequences of failing or dropping a course, recommend they use the Cursus Impact Analyzer.");
            sb.AppendLine("Keep responses focused and concise (maximum of 3-5 short paragraphs). Do not invent courses, grades, or requirements.");
            sb.AppendLine();
            
            sb.AppendLine("=== STUDENT PROFILE ===");
            sb.AppendLine($"- Name: {audit.StudentName}");
            sb.AppendLine($"- Department: {audit.DepartmentName}");
            sb.AppendLine($"- Current Term: {audit.CurrentSemester} {audit.AcademicYear}");
            sb.AppendLine($"- Academic Standing: {audit.CurrentStanding}");
            sb.AppendLine($"- Cumulative GPA: {audit.Cgpa} (Min required to graduate: {audit.MinGpaForGraduation})");
            sb.AppendLine($"- Overload Eligible (CGPA >= 3.0): {(audit.IsOverloadEligible ? "Yes" : "No")}");
            sb.AppendLine($"- Total Credits Earned: {audit.TotalCreditsEarned} / {audit.TotalCreditsRequired} ({audit.OverallPercentage}%)");
            sb.AppendLine($"- Credits Remaining: {audit.CreditsRemaining}");
            sb.AppendLine($"- Estimated Graduation: {audit.EstimatedGradSemester}");
            sb.AppendLine($"- On Track to Graduate: {(audit.IsOnTrack ? "Yes" : "No")}");
            sb.AppendLine();

            sb.AppendLine("=== DEGREE REQUIREMENTS BREAKDOWN ===");
            foreach (var category in audit.Categories)
            {
                sb.AppendLine();
                sb.AppendLine($"--- {category.Label} ({category.CourseType}) ---");
                sb.AppendLine(category.Description);
                sb.AppendLine($"Progress: {category.EarnedCredits}/{category.RequiredCredits} credits earned ({category.Percentage}%), {category.InProgressCredits} credits in progress. Satisfied: {(category.IsSatisfied ? "Yes" : "No")}");

                var completed = category.Courses.Where(c => c.Status == CourseAuditStatus.Completed).ToList();
                var inProgress = category.Courses.Where(c => c.Status == CourseAuditStatus.InProgress).ToList();
                var failed = category.Courses.Where(c => c.Status == CourseAuditStatus.Failed).ToList();
                var available = category.Courses.Where(c => c.Status == CourseAuditStatus.Available).ToList();
                var locked = category.Courses.Where(c => c.Status == CourseAuditStatus.Locked).ToList();

                if (completed.Count > 0)
                    sb.AppendLine("Completed: " + string.Join(", ", completed.Select(c => $"{c.Code} - {c.Name} ({c.CreditHours}cr, Grade: {c.Grade})")));

                if (inProgress.Count > 0)
                    sb.AppendLine("In Progress: " + string.Join(", ", inProgress.Select(c => $"{c.Code} - {c.Name} ({c.CreditHours}cr)")));

                if (failed.Count > 0)
                    sb.AppendLine("Failed (needs retake): " + string.Join(", ", failed.Select(c => $"{c.Code} - {c.Name} ({c.CreditHours}cr, Grade: {c.Grade})")));

                if (available.Count > 0)
                    sb.AppendLine("Available now: " + string.Join(", ", available.Select(c => $"{c.Code} - {c.Name} ({c.CreditHours}cr)")));

                if (locked.Count > 0)
                    sb.AppendLine("Locked (prerequisites not met): " + string.Join(", ", locked.Select(c => $"{c.Code} - {c.Name} ({c.CreditHours}cr)")));
            }
            sb.AppendLine();

            // 2. Chat History
            sb.AppendLine("=== CONVERSATION HISTORY ===");
            if (request.History != null)
            {
                foreach (var message in request.History)
                {
                    var role = string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase) ? "Student" : "Advisor";
                    sb.AppendLine($"{role}: {message.Content}");
                }
            }

            // 3. Current Turn
            sb.AppendLine($"Student: {request.Message}");
            sb.Append("Advisor: ");

            return sb.ToString();
        }
    }
}
