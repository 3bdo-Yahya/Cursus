using System;
using System.Collections.Generic;
using Cursus.BLL.Services;
using Cursus.Domain.DTOs;
using Cursus.Domain.Enums;
using Xunit;

namespace Cursus.BLL.Tests
{
    public sealed class GeminiPromptBuilderTests
    {
        [Fact]
        public void BuildPrompt_ConstructsFormattedPromptWithAllStudentContext()
        {
            // Arrange
            var audit = new GraduationAuditDto
            {
                StudentId = "student-123",
                StudentName = "Ahmed Kamal",
                DepartmentName = "Computer Science",
                AcademicYear = "2025-2026",
                CurrentSemester = SemesterType.Spring,
                CurrentStanding = AcademicStanding.Good,
                TotalCreditsEarned = 84,
                TotalCreditsRequired = 132,
                Cgpa = 3.24m,
                MinGpaForGraduation = 2.0m,
                EstimatedGradSemester = "Spring 2027",
                IsOnTrack = true,
                Categories = new List<CategoryProgressDto>
                {
                    new CategoryProgressDto
                    {
                        CourseType = CourseType.Core,
                        Label = "Core Courses",
                        Description = "Mandatory curriculum courses",
                        RequiredCredits = 72,
                        EarnedCredits = 60,
                        InProgressCredits = 3,
                        Courses = new List<CourseAuditItemDto>
                        {
                            new CourseAuditItemDto
                            {
                                CourseId = 1,
                                Code = "CS201",
                                Name = "Data Structures",
                                CreditHours = 3,
                                Grade = "B+",
                                Status = CourseAuditStatus.Completed
                            },
                            new CourseAuditItemDto
                            {
                                CourseId = 2,
                                Code = "CS301",
                                Name = "Operating Systems",
                                CreditHours = 3,
                                Grade = null,
                                Status = CourseAuditStatus.InProgress
                            },
                            new CourseAuditItemDto
                            {
                                CourseId = 3,
                                Code = "MTH102",
                                Name = "Calculus II",
                                CreditHours = 3,
                                Grade = "F",
                                Status = CourseAuditStatus.Failed
                            }
                        }
                    }
                }
            };

            var request = new ChatRequestDto
            {
                Message = "Am I on track to graduate?",
                History = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "user", Content = "Hello" },
                    new ChatMessageDto { Role = "model", Content = "Hello! I am your AI academic advisor. How can I help you today?" }
                }
            };

            // Act
            var prompt = GeminiPromptBuilder.BuildPrompt(audit, request);

            // Assert
            Assert.Contains("Ahmed Kamal", prompt);
            Assert.Contains("Computer Science", prompt);
            Assert.Contains("Cumulative GPA: 3.24", prompt);
            Assert.Contains("Credits Remaining: 48", prompt);
            Assert.Contains("Estimated Graduation: Spring 2027", prompt);
            Assert.Contains("Core Courses", prompt);
            Assert.Contains("CS201 - Data Structures (3cr, Grade: B+)", prompt);
            Assert.Contains("CS301 - Operating Systems (3cr)", prompt);
            Assert.Contains("MTH102 - Calculus II (3cr, Grade: F)", prompt);
            Assert.Contains("Student: Hello", prompt);
            Assert.Contains("Advisor: Hello! I am your AI academic advisor", prompt);
            Assert.Contains("Student: Am I on track to graduate?", prompt);
            Assert.Contains("Advisor: ", prompt);
        }

        [Fact]
        public void BuildPrompt_ThrowsArgumentNullExceptionWhenAuditIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GeminiPromptBuilder.BuildPrompt(null!, new ChatRequestDto()));
        }

        [Fact]
        public void BuildPrompt_ThrowsArgumentNullExceptionWhenRequestIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                GeminiPromptBuilder.BuildPrompt(new GraduationAuditDto
                {
                    StudentId = "student-1",
                    StudentName = "Student",
                    DepartmentName = "CS",
                    AcademicYear = "2025-2026",
                    CurrentSemester = SemesterType.Fall,
                    CurrentStanding = AcademicStanding.Good,
                    EstimatedGradSemester = "Spring 2029",
                    MinGpaForGraduation = 2.0m,
                    Categories = new List<CategoryProgressDto>()
                }, null!));
        }
    }
}
