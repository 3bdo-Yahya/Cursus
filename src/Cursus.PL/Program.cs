using Cursus.Domain.Entities;
using Cursus.PL.Models.Options;
using Microsoft.AspNetCore.Identity;
using Cursus.PL.Seeding;
using Microsoft.Extensions.Options;
using System;
using Cursus.Domain.Constants;
using Cursus.DAL.Database;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.DTOs;
using Cursus.Domain.Interfaces.Services;
using Cursus.BLL.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Cursus.PL.Services;

namespace Cursus.PL;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

        // Email Settings
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IEmailSender, IdentityEmailSender>();
        builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(5);
        });

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            await StartupSeeder.InitializeDatabaseAsync(app.Services);
            await SeedRolesAsync(app.Services);
            await StartupSeeder.SeedSampleCatalogAsync(app.Services);
            await StartupSeeder.SeedGradeScaleAsync(app.Services);
            await SeedDefaultAdminAsync(app.Services);
            await StartupSeeder.SeedDemoStudentsAsync(app.Services);
        }

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();


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

        foreach (var roleName in new[] { Roles.Admin, Roles.Student })
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
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
            throw new InvalidOperationException("IdentitySeed:AdminPassword must be configured.");

        var adminEmail = string.IsNullOrWhiteSpace(options.AdminEmail)
            ? "admin@cursus.com"
            : options.AdminEmail.Trim();

        // Resolve admin university
        var adminUniversity = await ResolveAdminUniversityAsync(context, options.AdminUniversityName);
        if (adminUniversity is null)
        {
            throw new InvalidOperationException(
                $"Unable to find admin university: {options.AdminUniversityName}");
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail)
            ?? await userManager.FindByNameAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                UniversityId = adminUniversity.Id  // Admin linked to specific university
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

        // Ensure admin UniversityId is set to the configured university
        if (adminUser.UniversityId != adminUniversity.Id)
        {
            adminUser.UniversityId = adminUniversity.Id;
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to update seeded admin user (set university link): {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, Roles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            if (!addRoleResult.Succeeded)
                throw new InvalidOperationException(
                    $"Unable to assign 'Admin' role to seeded admin user: {string.Join(", ", addRoleResult.Errors.Select(error => error.Description))}");
        }

        Console.WriteLine($"[Seeding] Admin user seeded and linked to {adminUniversity.Name} university");
    }

    private static async Task<University?> ResolveAdminUniversityAsync(ApplicationDbContext context, string? universityName)
    {
        if (string.IsNullOrWhiteSpace(universityName))
        {
            return null;
        }

        return await context.Universities
            .FirstOrDefaultAsync(u => u.Name == universityName.Trim());
    }
}

