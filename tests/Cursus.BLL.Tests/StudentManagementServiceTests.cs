using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cursus.BLL.Tests;

public sealed class StudentManagementServiceTests
{
    [Fact]
    public async Task CreateStudentAsync_SetsUniversityId_AndStudentRole()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db);
        var (sut, userManager) = CreateSut(db);

        var result = await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "new.student@test.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall,
            EnrollmentDate = DateTime.Today
        });

        Assert.True(result.IsSuccess);
        var user = await userManager.FindByEmailAsync("new.student@test.edu");
        Assert.NotNull(user);
        Assert.Equal(1, user!.UniversityId);
        Assert.Equal(1, user.DepartmentId);
        Assert.True(user.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.Student));
    }

    [Fact]
    public async Task CreateStudentAsync_RejectsInactiveDepartment()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db, departmentActive: false);
        var (sut, _) = CreateSut(db);

        var result = await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "x@test.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(CreateStudentRequest.DepartmentId), result.Field);
    }

    [Fact]
    public async Task CreateStudentAsync_RejectsDuplicateEmail()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db);
        var (sut, _) = CreateSut(db);

        var request = new CreateStudentRequest
        {
            Email = "dup@test.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        };

        Assert.True((await sut.CreateStudentAsync(request)).IsSuccess);
        var second = await sut.CreateStudentAsync(request);

        Assert.False(second.IsSuccess);
        Assert.Equal(nameof(CreateStudentRequest.Email), second.Field);
    }

    [Fact]
    public async Task DeleteStudentAsync_RefusesNonStudentUsers()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db);
        var (sut, userManager) = CreateSut(db);

        var admin = new AppUser
        {
            UserName = "admin@test.edu",
            Email = "admin@test.edu",
            EmailConfirmed = true
        };
        Assert.True((await userManager.CreateAsync(admin, "TempPass1!")).Succeeded);

        var result = await sut.DeleteStudentAsync(admin.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Contains("Student role", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(await userManager.FindByIdAsync(admin.Id));
    }

    [Fact]
    public async Task DeleteStudentAsync_DeletesStudentAccounts()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db);
        var (sut, userManager) = CreateSut(db);

        var created = await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "gone@test.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        });
        Assert.True(created.IsSuccess);

        var user = await userManager.FindByEmailAsync("gone@test.edu");
        Assert.NotNull(user);

        var deleted = await sut.DeleteStudentAsync(user!.Id);
        Assert.True(deleted.IsSuccess);
        Assert.Null(await userManager.FindByEmailAsync("gone@test.edu"));
    }

    [Fact]
    public async Task GetStandingSummaryAsync_CountsByStanding()
    {
        await using var db = CreateDb();
        await SeedUniversityDepartmentAndRoleAsync(db);
        var (sut, userManager) = CreateSut(db);

        await CreateStudentWithStandingAsync(sut, userManager, "good@test.edu", AcademicStanding.Good);
        await CreateStudentWithStandingAsync(sut, userManager, "warn@test.edu", AcademicStanding.Warning);
        await CreateStudentWithStandingAsync(sut, userManager, "prob@test.edu", AcademicStanding.Probation);

        var summary = await sut.GetStandingSummaryAsync();

        Assert.Equal(3, summary.Total);
        Assert.Equal(1, summary.Good);
        Assert.Equal(2, summary.WarningOrProbation);
        Assert.Equal(0, summary.Dismissed);
    }

    private static async Task CreateStudentWithStandingAsync(
        StudentManagementService sut,
        UserManager<AppUser> userManager,
        string email,
        AcademicStanding standing)
    {
        var result = await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = email,
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        });
        Assert.True(result.IsSuccess);

        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        user!.CurrentStanding = standing;
        await userManager.UpdateAsync(user);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedUniversityDepartmentAndRoleAsync(
        ApplicationDbContext db,
        bool departmentActive = true)
    {
        db.Universities.Add(new University { Id = 1, Name = "Test U" });
        db.Departments.Add(new Department
        {
            Id = 1,
            Name = "CS",
            UniversityId = 1,
            TotalCreditsRequired = 132,
            MinGpaForGraduation = 2.0m,
            IsActive = departmentActive
        });
        await db.SaveChangesAsync();

        var roleManager = CreateRoleManager(db);
        if (!await roleManager.RoleExistsAsync(Roles.Student))
            await roleManager.CreateAsync(new IdentityRole(Roles.Student));
    }

    private static (StudentManagementService Sut, UserManager<AppUser> UserManager) CreateSut(
        ApplicationDbContext db)
    {
        var userManager = CreateUserManager(db);
        var sut = new StudentManagementService(
            db,
            new AcademicMetricsService(db),
            userManager);
        return (sut, userManager);
    }

    private static UserManager<AppUser> CreateUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<AppUser>(db);
        var options = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var hasher = new PasswordHasher<AppUser>();
        var validators = new List<IUserValidator<AppUser>> { new UserValidator<AppUser>() };
        var passwordValidators = new List<IPasswordValidator<AppUser>> { new PasswordValidator<AppUser>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<UserManager<AppUser>>>();

        return new UserManager<AppUser>(
            store,
            options,
            hasher,
            validators,
            passwordValidators,
            normalizer,
            errors,
            services,
            logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(ApplicationDbContext db)
    {
        var store = new RoleStore<IdentityRole>(db);
        var validators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(
            store,
            validators,
            normalizer,
            errors,
            logger);
    }
}
