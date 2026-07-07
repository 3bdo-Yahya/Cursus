using System.Text.RegularExpressions;
using Cursus.BLL.Tests;
using Cursus.DAL.Database;
using Cursus.Domain.Constants;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cursus.PL.Tests;

public sealed class CursusWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Testing:DatabaseName", _databaseName);

        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }

    public async Task SeedStudentAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        if (!await roleManager.RoleExistsAsync(Roles.Student))
            await roleManager.CreateAsync(new IdentityRole(Roles.Student));

        if (await db.Universities.AnyAsync(u => u.Id == PlannerTestData.UniversityId))
            return;

        var university = new University { Id = PlannerTestData.UniversityId, Name = "Test University" };
        var department = new Department
        {
            Id = PlannerTestData.DepartmentId,
            Name = "Computer Science",
            UniversityId = PlannerTestData.UniversityId,
            TotalCreditsRequired = 132,
            MinGpaForGraduation = 2.0m,
            IsActive = true
        };

        db.Universities.Add(university);
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var user = new AppUser
        {
            Id = PlannerTestData.StudentId,
            UserName = "student@test.edu",
            Email = "student@test.edu",
            UniversityId = PlannerTestData.UniversityId,
            DepartmentId = PlannerTestData.DepartmentId,
            AcademicYear = PlannerTestData.AcademicYear,
            CurrentSemester = SemesterType.Fall,
            CurrentStanding = AcademicStanding.Good,
            EmailConfirmed = true
        };

        if (await userManager.FindByIdAsync(user.Id) is null)
        {
            await userManager.CreateAsync(user, "Password1!");
            await userManager.AddToRoleAsync(user, Roles.Student);
        }
    }

    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/Student/Planner");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(
            html,
            @"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(match.Success, "Antiforgery token was not found on Planner page.");
        return match.Groups[1].Value;
    }

    public static void AddAntiforgeryHeader(HttpRequestMessage request, string token)
    {
        request.Headers.Add("RequestVerificationToken", token);
    }
}

public sealed class UnauthenticatedCursusWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Testing:DatabaseName", Guid.NewGuid().ToString());
    }
}
