using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cursus.BLL.Tests;

public sealed class UniversityAdminServiceTests
{
    [Fact]
    public async Task CreateAdminAsync_LinksAdminToUniversity()
    {
        await using var db = CreateDb();
        await SeedUniversityAndRolesAsync(db);
        var (sut, userManager) = CreateSut(db);

        var result = await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "admin@uni1.edu",
            Password = "TempPass1!",
            UniversityId = 1
        });

        Assert.True(result.IsSuccess);
        var user = await userManager.FindByEmailAsync("admin@uni1.edu");
        Assert.NotNull(user);
        Assert.Equal(1, user!.UniversityId);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.Admin));
        Assert.False(await userManager.IsInRoleAsync(user, Roles.SuperAdmin));
    }

    [Fact]
    public async Task CreateAdminAsync_RejectsInvalidUniversity()
    {
        await using var db = CreateDb();
        await SeedUniversityAndRolesAsync(db);
        var (sut, _) = CreateSut(db);

        var result = await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "x@test.edu",
            Password = "TempPass1!",
            UniversityId = 99
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(nameof(CreateUniversityAdminRequest.UniversityId), result.Field);
    }

    [Fact]
    public async Task GetAdminsAsync_FiltersByUniversity()
    {
        await using var db = CreateDb();
        await SeedUniversityAndRolesAsync(db);
        db.Universities.Add(new University { Id = 2, Name = "Uni Two" });
        await db.SaveChangesAsync();
        var (sut, _) = CreateSut(db);

        Assert.True((await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "a@u1.edu",
            Password = "TempPass1!",
            UniversityId = 1
        })).IsSuccess);

        Assert.True((await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "b@u2.edu",
            Password = "TempPass1!",
            UniversityId = 2
        })).IsSuccess);

        var uni1 = await sut.GetAdminsAsync(universityId: 1);
        Assert.Single(uni1);
        Assert.Equal("a@u1.edu", uni1[0].Email);
        Assert.Equal(2, (await sut.GetAdminsAsync()).Count);
    }

    [Fact]
    public async Task UpdateUniversityAsync_ReassignsAdmin()
    {
        await using var db = CreateDb();
        await SeedUniversityAndRolesAsync(db);
        db.Universities.Add(new University { Id = 2, Name = "Uni Two" });
        await db.SaveChangesAsync();
        var (sut, userManager) = CreateSut(db);

        Assert.True((await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "move@test.edu",
            Password = "TempPass1!",
            UniversityId = 1
        })).IsSuccess);

        var user = await userManager.FindByEmailAsync("move@test.edu");
        Assert.NotNull(user);

        var result = await sut.UpdateUniversityAsync(user!.Id, universityId: 2);
        Assert.True(result.IsSuccess);

        user = await userManager.FindByIdAsync(user.Id);
        Assert.Equal(2, user!.UniversityId);
    }

    [Fact]
    public async Task DeleteAdminAsync_RemovesUniversityAdmin()
    {
        await using var db = CreateDb();
        await SeedUniversityAndRolesAsync(db);
        var (sut, userManager) = CreateSut(db);

        Assert.True((await sut.CreateAdminAsync(new CreateUniversityAdminRequest
        {
            Email = "gone@test.edu",
            Password = "TempPass1!",
            UniversityId = 1
        })).IsSuccess);

        var user = await userManager.FindByEmailAsync("gone@test.edu");
        Assert.NotNull(user);

        var result = await sut.DeleteAdminAsync(user!.Id);
        Assert.True(result.IsSuccess);
        Assert.Null(await userManager.FindByEmailAsync("gone@test.edu"));
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedUniversityAndRolesAsync(ApplicationDbContext db)
    {
        db.Universities.Add(new University { Id = 1, Name = "Uni One" });
        await db.SaveChangesAsync();

        var roleManager = CreateRoleManager(db);
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
            await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
        if (!await roleManager.RoleExistsAsync(Roles.SuperAdmin))
            await roleManager.CreateAsync(new IdentityRole(Roles.SuperAdmin));
    }

    private static (UniversityAdminService Sut, UserManager<AppUser> UserManager) CreateSut(
        ApplicationDbContext db)
    {
        var userManager = CreateUserManager(db);
        return (new UniversityAdminService(db, userManager), userManager);
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
