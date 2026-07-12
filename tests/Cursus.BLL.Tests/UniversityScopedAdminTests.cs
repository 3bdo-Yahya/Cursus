using Cursus.BLL.Services;
using Cursus.BLL.Services.Implementation;
using Cursus.DAL.Database;
using Cursus.DAL.Repositories;
using Cursus.Domain;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cursus.BLL.Tests;

public sealed class UniversityScopedAdminTests
{
    [Fact]
    public async Task CourseService_GetAllAsync_ReturnsOnlyScopedUniversityCourses()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var sut = CreateCourseService(db);

        var scoped = (await sut.GetAllAsync(universityId: 1)).ToList();
        var all = (await sut.GetAllAsync()).ToList();

        Assert.Single(scoped);
        Assert.Equal("CS101", scoped[0].Code);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CourseService_GetByIdAsync_ReturnsNull_ForOtherUniversity()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var sut = CreateCourseService(db);

        var foreign = await sut.GetByIdAsync(id: 2, universityId: 1);
        var own = await sut.GetByIdAsync(id: 1, universityId: 1);

        Assert.Null(foreign);
        Assert.NotNull(own);
    }

    [Fact]
    public async Task CourseService_AddAsync_RejectsDepartmentFromOtherUniversity()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var sut = CreateCourseService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.AddAsync(
            new CreateCourseDto
            {
                Code = "X1",
                Name = "Cross",
                DepartmentId = 2,
                CreditHours = 3,
                CourseType = CourseType.Core,
                SemesterAvailability = SemesterAvailability.FallSpring,
                PassingGradeThreshold = "D",
                IsActive = true
            },
            universityId: 1));
    }

    [Fact]
    public async Task CourseService_UpdateAsync_Throws_WhenCourseOutsideScope()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var sut = CreateCourseService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.UpdateAsync(
            new EditCourseDto
            {
                Id = 2,
                Code = "Hacked",
                Name = "No",
                DepartmentId = 2,
                CreditHours = 3,
                CourseType = CourseType.Core,
                SemesterAvailability = SemesterAvailability.FallSpring,
                PassingGradeThreshold = "D",
                IsActive = true
            },
            universityId: 1));
    }

    [Fact]
    public async Task DepartmentService_GetAllAsync_FiltersByUniversity()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var sut = CreateDepartmentService(db);

        var scoped = (await sut.GetAllAsync(universityId: 1)).ToList();

        Assert.Single(scoped);
        Assert.Equal(1, scoped[0].UniversityId);
    }

    [Fact]
    public async Task StudentManagement_GetStudentsAsync_FiltersByUniversity()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        await SeedStudentRoleAsync(db);
        var (sut, _) = CreateStudentSut(db);

        Assert.True((await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "a@u1.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        }, universityId: 1)).IsSuccess);

        Assert.True((await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "b@u2.edu",
            Password = "TempPass1!",
            DepartmentId = 2,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        }, universityId: 2)).IsSuccess);

        var uni1Students = (await sut.GetStudentsAsync(null, null, universityId: 1)).ToList();
        Assert.Single(uni1Students);
        Assert.Equal("a@u1.edu", uni1Students[0].Email);
    }

    [Fact]
    public async Task StudentManagement_CreateStudentAsync_RejectsOtherUniversityDepartment()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        await SeedStudentRoleAsync(db);
        var (sut, _) = CreateStudentSut(db);

        var result = await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "x@test.edu",
            Password = "TempPass1!",
            DepartmentId = 2,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        }, universityId: 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(CreateStudentRequest.DepartmentId), result.Field);
    }

    [Fact]
    public async Task StudentManagement_GetStudentDetailAsync_HidesOtherUniversity()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        await SeedStudentRoleAsync(db);
        var (sut, userManager) = CreateStudentSut(db);

        Assert.True((await sut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "other@u2.edu",
            Password = "TempPass1!",
            DepartmentId = 2,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        }, universityId: 2)).IsSuccess);

        var user = await userManager.FindByEmailAsync("other@u2.edu");
        Assert.NotNull(user);

        Assert.Null(await sut.GetStudentDetailAsync(user!.Id, universityId: 1));
        Assert.NotNull(await sut.GetStudentDetailAsync(user.Id, universityId: 2));
    }

    [Fact]
    public async Task AdminDashboard_GetAdminDashboardAsync_ScopesCounts()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        await SeedStudentRoleAsync(db);
        var (studentSut, _) = CreateStudentSut(db);
        Assert.True((await studentSut.CreateStudentAsync(new CreateStudentRequest
        {
            Email = "s1@u1.edu",
            Password = "TempPass1!",
            DepartmentId = 1,
            AcademicYear = "2025-2026",
            CurrentSemester = SemesterType.Fall
        }, universityId: 1)).IsSuccess);

        var dashboard = CreateDashboardService(db);
        var scoped = await dashboard.GetAdminDashboardAsync(universityId: 1);
        var global = await dashboard.GetAdminDashboardAsync();

        Assert.Equal(1, scoped.TotalUniversities);
        Assert.Equal(1, scoped.TotalDepartments);
        Assert.Equal(1, scoped.TotalCourses);
        Assert.Equal(1, scoped.TotalStudents);

        Assert.Equal(2, global.TotalUniversities);
        Assert.Equal(2, global.TotalDepartments);
        Assert.Equal(2, global.TotalCourses);
    }

    [Fact]
    public async Task UniversityService_AddAsync_CreatesUniversity()
    {
        await using var db = CreateDb();
        var sut = new UniversityService(new GenericRepository<University>(db));

        await sut.AddAsync(new CreateUniversityDto { Name = "New Tech U" });

        Assert.True(await db.Universities.AnyAsync(u => u.Name == "New Tech U"));
    }

    [Fact]
    public async Task AdminScopeService_ResolveAsync_RequiresUniversityForAdmin()
    {
        await using var db = CreateDb();
        await SeedTwoUniversitiesAsync(db);
        var userManager = CreateUserManager(db);
        var roleManager = CreateRoleManager(db);
        await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        await roleManager.CreateAsync(new IdentityRole(Roles.SuperAdmin));

        var admin = new AppUser
        {
            UserName = "admin@u1.edu",
            Email = "admin@u1.edu",
            EmailConfirmed = true,
            UniversityId = 1
        };
        Assert.True((await userManager.CreateAsync(admin, "TempPass1!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(admin, Roles.Admin)).Succeeded);

        var super = new AppUser
        {
            UserName = "super@cursus.com",
            Email = "super@cursus.com",
            EmailConfirmed = true
        };
        Assert.True((await userManager.CreateAsync(super, "TempPass1!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(super, Roles.SuperAdmin)).Succeeded);

        var scopeService = new AdminScopeService(userManager);
        var adminScope = await scopeService.ResolveAsync(admin.Id);
        var superScope = await scopeService.ResolveAsync(super.Id);

        Assert.True(adminScope.CanManageCatalog);
        Assert.False(adminScope.CanManageUniversities);
        Assert.Equal(1, adminScope.RequireUniversityId());

        Assert.True(superScope.CanManageUniversities);
        Assert.False(superScope.CanManageCatalog);
        Assert.Throws<InvalidOperationException>(() => superScope.RequireUniversityId());
    }

    [Fact]
    public void AdminScope_RequireUniversityId_Throws_WhenMissing()
    {
        var scope = new AdminScope(IsSuperAdmin: false, IsUniversityAdmin: true, UniversityId: null);
        Assert.Throws<InvalidOperationException>(() => scope.RequireUniversityId());
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedTwoUniversitiesAsync(ApplicationDbContext db)
    {
        db.Universities.AddRange(
            new University { Id = 1, Name = "Uni One" },
            new University { Id = 2, Name = "Uni Two" });
        db.Departments.AddRange(
            new Department
            {
                Id = 1,
                Name = "CS1",
                UniversityId = 1,
                TotalCreditsRequired = 120,
                MinGpaForGraduation = 2.0m,
                IsActive = true
            },
            new Department
            {
                Id = 2,
                Name = "CS2",
                UniversityId = 2,
                TotalCreditsRequired = 120,
                MinGpaForGraduation = 2.0m,
                IsActive = true
            });
        db.Courses.AddRange(
            new Course
            {
                Id = 1,
                Code = "CS101",
                Name = "Intro One",
                DepartmentId = 1,
                CreditHours = 3,
                CourseType = CourseType.Core,
                SemesterAvailability = SemesterAvailability.FallSpring,
                PassingGradeThreshold = "D",
                IsActive = true
            },
            new Course
            {
                Id = 2,
                Code = "CS201",
                Name = "Intro Two",
                DepartmentId = 2,
                CreditHours = 3,
                CourseType = CourseType.Core,
                SemesterAvailability = SemesterAvailability.FallSpring,
                PassingGradeThreshold = "D",
                IsActive = true
            });
        await db.SaveChangesAsync();
    }

    private static async Task SeedStudentRoleAsync(ApplicationDbContext db)
    {
        var roleManager = CreateRoleManager(db);
        if (!await roleManager.RoleExistsAsync(Roles.Student))
            await roleManager.CreateAsync(new IdentityRole(Roles.Student));
    }

    private static CourseService CreateCourseService(ApplicationDbContext db) =>
        new(new GenericRepository<Course>(db), new GenericRepository<Department>(db));

    private static DepartmentService CreateDepartmentService(ApplicationDbContext db) =>
        new(new GenericRepository<Department>(db));

    private static AdminDashboardService CreateDashboardService(ApplicationDbContext db) =>
        new(
            new GenericRepository<University>(db),
            new GenericRepository<GraduationRequirement>(db),
            new GenericRepository<Department>(db),
            new GenericRepository<Course>(db),
            CreateUserManager(db));

    private static (StudentManagementService Sut, UserManager<AppUser> UserManager) CreateStudentSut(
        ApplicationDbContext db)
    {
        var userManager = CreateUserManager(db);
        var sut = new StudentManagementService(db, new AcademicMetricsService(db), userManager);
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
            store, options, hasher, validators, passwordValidators, normalizer, errors, services, logger);
    }

    private static RoleManager<IdentityRole> CreateRoleManager(ApplicationDbContext db)
    {
        var store = new RoleStore<IdentityRole>(db);
        var validators = new List<IRoleValidator<IdentityRole>> { new RoleValidator<IdentityRole>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var logger = services.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();

        return new RoleManager<IdentityRole>(store, validators, normalizer, errors, logger);
    }
}
