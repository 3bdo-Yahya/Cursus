using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Entities;

namespace Cursus.DAL.Database
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<University> Universities => Set<University>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
        public DbSet<GraduationRequirement> GraduationRequirements => Set<GraduationRequirement>();
        public DbSet<GraduationRequirementCourse> GraduationRequirementCourses => Set<GraduationRequirementCourse>();
        public DbSet<GradeScale> GradeScales => Set<GradeScale>();
        public DbSet<CreditHourRule> CreditHourRules => Set<CreditHourRule>();
        public DbSet<StudentCourse> StudentCourses => Set<StudentCourse>();
        public DbSet<StandingHistory> StandingHistories => Set<StandingHistory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}