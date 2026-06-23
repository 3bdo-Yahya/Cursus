using System.Reflection;
using System.Security.Claims;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Controllers;
using Cursus.PL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cursus.PL.Tests;

public sealed class StudentControllerAiAdvisorTests
{
    [Fact]
    public void AiAdvisorChat_RequiresPostAndAntiforgeryValidation()
    {
        var method = typeof(StudentController).GetMethod(
            nameof(StudentController.AiAdvisorChat));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task AiAdvisorChat_ReturnsSuccessWithMappedAcademicContext()
    {
        var audit = CreateAudit();
        var progressService = new FakeProgressService { Audit = audit };
        var advisorService = new FakeAiAdvisorService
        {
            Response = AiAdvisorResponseDto.Success("You are making good progress.")
        };
        var controller = CreateController(progressService, advisorService);

        var result = await controller.AiAdvisorChat(
            new AiAdvisorChatRequest { Message = "  Am I on track?  " },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AiAdvisorResponseDto>(ok.Value);
        Assert.True(response.Succeeded);
        Assert.Equal("student-1", progressService.ReceivedStudentId);
        Assert.Equal("Am I on track?", advisorService.ReceivedMessage);
        Assert.Equal("Test Student", advisorService.ReceivedContext?.DisplayName);
        Assert.Equal(4, advisorService.ReceivedContext?.CategoryProgress.Count);
        Assert.Contains(
            advisorService.ReceivedContext!.FailedOrLowGradeCourses,
            course => course.Code == "MTH102");
    }

    [Fact]
    public async Task AiAdvisorChat_ReturnsBadRequestForBlankMessage()
    {
        var controller = CreateController(
            new FakeProgressService(),
            new FakeAiAdvisorService());

        var result = await controller.AiAdvisorChat(
            new AiAdvisorChatRequest { Message = " " },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AiAdvisorResponseDto>(badRequest.Value);
        Assert.Equal("invalid_message", response.ErrorCode);
    }

    [Fact]
    public async Task AiAdvisorChat_ReturnsUnauthorizedWithoutStudentClaim()
    {
        var controller = CreateController(
            new FakeProgressService(),
            new FakeAiAdvisorService(),
            studentId: null);

        var result = await controller.AiAdvisorChat(
            new AiAdvisorChatRequest { Message = "Hello" },
            CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<AiAdvisorResponseDto>(unauthorized.Value);
        Assert.Equal("student_not_authenticated", response.ErrorCode);
    }

    [Fact]
    public async Task AiAdvisorChat_ReturnsUnprocessableEntityWhenAuditIsMissing()
    {
        var controller = CreateController(
            new FakeProgressService { Audit = null },
            new FakeAiAdvisorService());

        var result = await controller.AiAdvisorChat(
            new AiAdvisorChatRequest { Message = "Hello" },
            CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var response = Assert.IsType<AiAdvisorResponseDto>(unprocessable.Value);
        Assert.Equal("student_context_unavailable", response.ErrorCode);
    }

    [Fact]
    public async Task AiAdvisorChat_ReturnsServiceUnavailableForProviderFailure()
    {
        var controller = CreateController(
            new FakeProgressService { Audit = CreateAudit() },
            new FakeAiAdvisorService
            {
                Response = AiAdvisorResponseDto.Failure(
                    "openai_request_failed",
                    "The AI advisor is temporarily unavailable.")
            });

        var result = await controller.AiAdvisorChat(
            new AiAdvisorChatRequest { Message = "Hello" },
            CancellationToken.None);

        var unavailable = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, unavailable.StatusCode);
    }

    private static StudentController CreateController(
        IProgressService progressService,
        IAiAdvisorService advisorService,
        string? studentId = "student-1")
    {
        var controller = new StudentController(
            CreateUserManager(),
            null!,
            progressService,
            new FakeDashboardService(),
            advisorService,
            null!);

        var claims = studentId is null
            ? []
            : new[] { new Claim(ClaimTypes.NameIdentifier, studentId) };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    private static UserManager<AppUser> CreateUserManager() =>
        new(
            new TestUserStore(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<AppUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<AppUser>>.Instance);

    private static GraduationAuditDto CreateAudit() =>
        new()
        {
            StudentId = "student-1",
            StudentName = "Test Student",
            DepartmentName = "Computer Science",
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Spring,
            CurrentStanding = AcademicStanding.Good,
            TotalCreditsEarned = 60,
            TotalCreditsRequired = 120,
            Cgpa = 3.1m,
            EstimatedGradSemester = "Spring 2028/2029",
            IsOnTrack = true,
            MinGpaForGraduation = 2m,
            Categories =
            [
                Category(CourseType.Core, "Core Courses",
                [
                    Course(1, "CS201", "Data Structures", "B+", CourseAuditStatus.Completed),
                    Course(2, "MTH102", "Calculus II", "F", CourseAuditStatus.Failed)
                ]),
                Category(CourseType.DeptElective, "Department Elective", []),
                Category(CourseType.FreeElective, "Free Elective", []),
                Category(CourseType.UniversityReq, "University Requirements", [])
            ]
        };

    private static CategoryProgressDto Category(
        CourseType type,
        string label,
        IReadOnlyList<CourseAuditItemDto> courses) =>
        new()
        {
            CourseType = type,
            Label = label,
            Description = label,
            RequiredCredits = 30,
            EarnedCredits = 15,
            InProgressCredits = 3,
            Courses = courses
        };

    private static CourseAuditItemDto Course(
        int id,
        string code,
        string name,
        string? grade,
        CourseAuditStatus status) =>
        new()
        {
            CourseId = id,
            Code = code,
            Name = name,
            CreditHours = 3,
            Grade = grade,
            Status = status
        };

    private sealed class FakeProgressService : IProgressService
    {
        public GraduationAuditDto? Audit { get; init; }
        public string? ReceivedStudentId { get; private set; }

        public Task<GraduationAuditDto?> GetGraduationAuditAsync(string studentId)
        {
            ReceivedStudentId = studentId;
            return Task.FromResult(Audit);
        }
    }

    private sealed class FakeDashboardService : IStudentDashboardService
    {
        public Task<StudentDashboardDto?> GetDashboardDataAsync(string studentId) =>
            Task.FromResult<StudentDashboardDto?>(null);
    }

    private sealed class FakeAiAdvisorService : IAiAdvisorService
    {
        public AiAdvisorResponseDto Response { get; init; } =
            AiAdvisorResponseDto.Success("Test response");
        public AiAdvisorContextDto? ReceivedContext { get; private set; }
        public string? ReceivedMessage { get; private set; }

        public Task<AiAdvisorResponseDto> GetAdvisorResponseAsync(
            AiAdvisorContextDto studentContext,
            string userMessage,
            CancellationToken cancellationToken = default)
        {
            ReceivedContext = studentContext;
            ReceivedMessage = userMessage;
            return Task.FromResult(Response);
        }
    }

    private sealed class TestUserStore : IUserStore<AppUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(
            AppUser user,
            string? userName,
            CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(
            AppUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(
            AppUser user,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(
            AppUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(
            AppUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(
            AppUser user,
            CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<AppUser?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AppUser?>(null);

        public Task<AppUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken) =>
            Task.FromResult<AppUser?>(null);
    }
}
