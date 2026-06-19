using Cursus.BLL.Services;
using Cursus.BLL.Services.Implementation;
using Cursus.DAL.Database;
using Cursus.DAL.Repositories;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Repositories;
using Cursus.Domain.Interfaces.Services;
using Cursus.PL.Models.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cursus.PL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(
                this IServiceCollection services,
                IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddIdentity<AppUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            services.Configure<IdentitySeedOptions>(
                configuration.GetSection("IdentitySeed"));

            services.AddControllersWithViews();
            services.AddRazorPages();

            #region Repository
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            #endregion

            #region Services
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IUniversityService, UniversityService>();
            services.AddScoped<IDepartmentService, DepartmentService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
            services.AddScoped<IStudentManagementService, StudentManagementService>();
            services.AddScoped<ICourseMapService, CourseMapService>();
            services.AddScoped<IProgressService, ProgressService>();
            services.AddScoped<IStudentDashboardService, StudentDashboardService>();
            #endregion


            return services;
        }
    }
}