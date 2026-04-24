using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cursus.PL.Seeding;
using Microsoft.Extensions.Options;

namespace Cursus.PL;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        });
        builder.Services.Configure<IdentitySeedOptions>(builder.Configuration.GetSection("IdentitySeed"));

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
            {
                continue;
            }

            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': {string.Join(", ", createRoleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task SeedDefaultAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentitySeedOptions>>().Value;

        if (string.IsNullOrWhiteSpace(options.AdminPassword))
        {
            throw new InvalidOperationException("IdentitySeed:AdminPassword must be configured.");
        }

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
                throw new InvalidOperationException(
                    $"Unable to create default admin user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign 'Admin' role to seeded admin user: {string.Join(", ", addRoleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}

public sealed class IdentitySeedOptions
{
    public string AdminEmail { get; set; } = "admin@cursus.com";
    public string AdminPassword { get; set; } = string.Empty;
}
