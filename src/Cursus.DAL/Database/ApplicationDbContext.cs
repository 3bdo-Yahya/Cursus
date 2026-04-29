using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Cursus.Domain.Entities;

namespace Cursus.DAL.Database
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<University> Universities { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
        public DbSet<GraduationRequirement> GraduationRequirements { get; set; }
        public DbSet<GraduationRequirementCourse> GraduationRequirementCourses { get; set; }
        public DbSet<GradeScale> GradeScales { get; set; }
        public DbSet<CreditHourRule> CreditHourRules { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<StandingHistory> StandingHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}