using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Cursus.BLL.Tests;
using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cursus.PL.Tests;

public sealed class StudentPlannerEndpointTests
{
    [Fact]
    public async Task PlannerPlan_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var unauthenticatedFactory = new UnauthenticatedCursusWebApplicationFactory();
        var client = unauthenticatedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            $"/Student/PlannerPlan?academicYear={PlannerTestData.AcademicYear}&semester={SemesterType.Fall}");

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect,
            $"Expected unauthorized or login redirect, got {response.StatusCode}.");
    }

    [Fact]
    public async Task PlannerPlan_ReturnsPlan_WithAntiforgery()
    {
        using var factory = await CreateAuthenticatedFactoryAsync();
        var client = factory.CreateClient();

        var token = await CursusWebApplicationFactory.GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/Student/PlannerPlan?academicYear={PlannerTestData.AcademicYear}&semester={SemesterType.Fall}");
        CursusWebApplicationFactory.AddAntiforgeryHeader(request, token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("plannedCourses", out _));
        Assert.True(json.TryGetProperty("capacity", out var capacity));
        Assert.True(capacity.TryGetProperty("remainingRoom", out _));
    }

    [Fact]
    public async Task AddAndRemovePlannedCourse_RoundTrip_WithAntiforgery()
    {
        using var factory = await CreateAuthenticatedFactoryAsync();
        var client = factory.CreateClient();
        await SeedCourseAsync(factory, 1, "CS101");

        var token = await CursusWebApplicationFactory.GetAntiforgeryTokenAsync(client);

        using (var addRequest = new HttpRequestMessage(HttpMethod.Post, "/Student/AddPlannedCourse"))
        {
            addRequest.Content = JsonContent.Create(new
            {
                courseId = 1,
                academicYear = PlannerTestData.AcademicYear,
                semester = SemesterType.Fall
            });
            CursusWebApplicationFactory.AddAntiforgeryHeader(addRequest, token);

            var addResponse = await client.SendAsync(addRequest);
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        }

        using (var removeRequest = new HttpRequestMessage(HttpMethod.Post, "/Student/RemovePlannedCourse"))
        {
            removeRequest.Content = JsonContent.Create(new
            {
                courseId = 1,
                academicYear = PlannerTestData.AcademicYear,
                semester = SemesterType.Fall
            });
            CursusWebApplicationFactory.AddAntiforgeryHeader(removeRequest, token);

            var removeResponse = await client.SendAsync(removeRequest);
            Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        }
    }

    [Fact]
    public async Task AddPlannedCourse_ReturnsBadRequest_WhenForcedPlusPlannedExceedsLimit()
    {
        using var factory = await CreateAuthenticatedFactoryAsync();
        var client = factory.CreateClient();
        await SeedForcedCourseAsync(factory, 100, "FORCED", 15);
        await SeedCourseAsync(factory, 1, "CS101", credits: 6);

        var token = await CursusWebApplicationFactory.GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Student/AddPlannedCourse");
        request.Content = JsonContent.Create(new
        {
            courseId = 1,
            academicYear = PlannerTestData.AcademicYear,
            semester = SemesterType.Fall
        });
        CursusWebApplicationFactory.AddAntiforgeryHeader(request, token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SimulateFailure_ReturnsImpact_ForCourseWithoutStudentRecord()
    {
        using var factory = await CreateAuthenticatedFactoryAsync();
        var client = factory.CreateClient();
        await SeedCourseAsync(factory, 1, "CS101");

        var token = await CursusWebApplicationFactory.GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/Student/SimulateFailure");
        request.Content = JsonContent.Create(new { courseId = 1 });
        CursusWebApplicationFactory.AddAntiforgeryHeader(request, token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CS101", json.GetProperty("failedCourseCode").GetString());
    }

    private static async Task<CursusWebApplicationFactory> CreateAuthenticatedFactoryAsync()
    {
        var factory = new CursusWebApplicationFactory();
        await factory.SeedStudentAsync();
        return factory;
    }

    private static async Task SeedCourseAsync(CursusWebApplicationFactory factory, int id, string code, int credits = 3)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.Courses.AnyAsync(c => c.Id == id))
            return;

        db.Courses.Add(PlannerTestData.Course(id, code, credits));
        await db.SaveChangesAsync();
    }

    private static async Task SeedForcedCourseAsync(CursusWebApplicationFactory factory, int id, string code, int credits)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.StudentCourses.AnyAsync(sc => sc.CourseId == id && sc.StudentId == PlannerTestData.StudentId))
            return;

        var course = PlannerTestData.Course(id, code, credits);
        db.Courses.Add(course);
        db.StudentCourses.Add(new StudentCourse
        {
            StudentId = PlannerTestData.StudentId,
            CourseId = course.Id,
            Status = StudentCourseStatus.InProgress,
            Semester = SemesterType.Fall,
            AcademicYear = PlannerTestData.AcademicYear,
            Course = course
        });
        await db.SaveChangesAsync();
    }
}
