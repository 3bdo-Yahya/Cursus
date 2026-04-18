using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

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

        await InitializeDatabaseAsync(app.Services);

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        await SeedIdentityAsync(app.Services, builder.Configuration);

        app.MapStaticAssets();
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();
        app.MapRazorPages();

        app.Run();
    }

    private static async Task SeedIdentityAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        const string adminRoleName = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(adminRoleName));
            if (!createRoleResult.Succeeded && !await roleManager.RoleExistsAsync(adminRoleName))
            {
                throw new InvalidOperationException(
                    $"Unable to create the '{adminRoleName}' role: {string.Join(", ", createRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        var adminUserSection = configuration.GetSection("IdentityBootstrap:AdminUser");
        var email = adminUserSection["Email"];
        var password = adminUserSection["Password"];
        var userName = adminUserSection["UserName"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("IdentityBootstrap:AdminUser requires Email and Password when bootstrap is enabled.");
        }

        var normalizedUserName = string.IsNullOrWhiteSpace(userName) ? email : userName;

        var adminUser = await userManager.FindByEmailAsync(email)
            ?? await userManager.FindByNameAsync(normalizedUserName);

        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = normalizedUserName,
                Email = email,
                EmailConfirmed = true
            };

            var createUserResult = await userManager.CreateAsync(adminUser, password);
            if (!createUserResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the initial admin user: {string.Join(", ", createUserResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, adminRoleName))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the '{adminRoleName}' role to the initial admin user: {string.Join(", ", addRoleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var hasMigrations = context.Database.GetMigrations().Any();

        if (hasMigrations)
        {
            await context.Database.MigrateAsync();
            return;
        }

        await context.Database.EnsureCreatedAsync();
    }
}
