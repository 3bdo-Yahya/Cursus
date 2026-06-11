using Cursus.Domain.Entities;
using Cursus.PL.Models.Options;
using Microsoft.AspNetCore.Identity;
using Cursus.PL.Seeding;
using Microsoft.Extensions.Options;

namespace Cursus.PL;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationServices(builder.Configuration);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        await StartupSeeder.InitializeDatabaseAsync(app.Services);
        await SeedRolesAsync(app.Services);
        await SeedDefaultAdminAsync(app.Services);

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        await StartupSeeder.SeedSampleCatalogAsync(app.Services);

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();
        app.MapRazorPages();

        app.Run();
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in new[] { "Admin", "Student" })
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (createRoleResult.Succeeded)
                continue;

            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            if (!createRoleResult.Succeeded)
                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': {string.Join(", ", createRoleResult.Errors.Select(error => error.Description))}");
        }
    }

    private static async Task SeedDefaultAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
            throw new InvalidOperationException("IdentitySeed:AdminPassword must be configured.");

        var adminEmail = string.IsNullOrWhiteSpace(options.AdminEmail)
            ? "admin@cursus.com"
            : options.AdminEmail.Trim();

        var adminUser = await userManager.FindByEmailAsync(adminEmail)
            ?? await userManager.FindByNameAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(adminUser, options.AdminPassword);
            if (!createResult.Succeeded)
            {
                var isDuplicateUserFailure = createResult.Errors.Any(error =>
                    string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateUserName), StringComparison.Ordinal) ||
                    string.Equals(error.Code, nameof(IdentityErrorDescriber.DuplicateEmail), StringComparison.Ordinal));

                if (isDuplicateUserFailure)
                    adminUser = await userManager.FindByEmailAsync(adminEmail)
                        ?? await userManager.FindByNameAsync(adminEmail);

                if (adminUser is null)
                    throw new InvalidOperationException(
                        $"Unable to create default admin user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!addRoleResult.Succeeded)
                throw new InvalidOperationException(
                    $"Unable to assign 'Admin' role to seeded admin user: {string.Join(", ", addRoleResult.Errors.Select(error => error.Description))}");
        }
    }
}
