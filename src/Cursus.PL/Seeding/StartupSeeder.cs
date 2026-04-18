using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cursus.PL.Seeding;

public static class StartupSeeder
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
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

    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        const string adminRoleName = "Admin";
        const string seededAdminEmail = "admin@cursus.local";
        const string seededAdminUserName = "admin@cursus.local";
        const string seededAdminPassword = "ChangeMe123!";

        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(adminRoleName));
            if (!createRoleResult.Succeeded && !await roleManager.RoleExistsAsync(adminRoleName))
            {
                throw new InvalidOperationException(
                    $"Unable to create the '{adminRoleName}' role: {string.Join(", ", createRoleResult.Errors.Select(error => error.Description))}");
            }
        }

        var adminUser = await userManager.FindByEmailAsync(seededAdminEmail)
            ?? await userManager.FindByNameAsync(seededAdminUserName);

        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = seededAdminUserName,
                Email = seededAdminEmail,
                EmailConfirmed = true
            };

            var createUserResult = await userManager.CreateAsync(adminUser, seededAdminPassword);
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

    public static async Task SeedSampleCatalogAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var seedDataRoot = ResolveSeedDataRoot();
        var universityFolders = Directory.GetDirectories(seedDataRoot);

        var universitiesByName = await context.Universities
            .ToDictionaryAsync(university => university.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var folder in universityFolders)
        {
            var slug = Path.GetFileName(folder);
            var universityName = GetUniversityNameFromSlug(slug);

            if (!universitiesByName.ContainsKey(universityName))
            {
                var university = new University { Name = universityName };
                context.Universities.Add(university);
                universitiesByName[universityName] = university;
            }
        }

        await context.SaveChangesAsync();

        var departmentsByKey = await context.Departments
            .ToDictionaryAsync(
                department => $"{department.UniversityId}:{department.Name}",
                StringComparer.OrdinalIgnoreCase);

        foreach (var folder in universityFolders)
        {
            var slug = Path.GetFileName(folder);
            var universityName = GetUniversityNameFromSlug(slug);
            var university = universitiesByName[universityName];

            var curriculumPath = Path.Combine(folder, "curriculum-courses.json");
            if (!File.Exists(curriculumPath))
            {
                continue;
            }

            var graduationRequirementsPath = Path.Combine(folder, "graduation-requirements.json");
            var graduationRules = LoadGraduationRules(graduationRequirementsPath);
            var curriculumCourses = LoadCurriculumCourses(curriculumPath);

            var majors = curriculumCourses
                .SelectMany(course => course.ProgramRules)
                .Select(rule => rule.Major)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var major in majors)
            {
                var departmentName = MapMajorToDepartmentName(major);
                var departmentKey = $"{university.Id}:{departmentName}";

                if (departmentsByKey.ContainsKey(departmentKey))
                {
                    continue;
                }

                var (requiredCredits, minimumGpa) = graduationRules.GetDepartmentDefaults(major);

                var department = new Department
                {
                    Name = departmentName,
                    UniversityId = university.Id,
                    TotalCreditsRequired = requiredCredits,
                    MinGpaForGraduation = minimumGpa,
                    IsActive = true
                };

                context.Departments.Add(department);
                departmentsByKey[departmentKey] = department;
            }

            await context.SaveChangesAsync();

            var universityDepartmentIds = departmentsByKey
                .Where(entry => entry.Value.UniversityId == university.Id)
                .Select(entry => entry.Value.Id)
                .ToHashSet();

            var existingCourseKeys = await context.Courses
                .Where(course => universityDepartmentIds.Contains(course.DepartmentId))
                .Select(course => $"{course.DepartmentId}:{course.Code}")
                .ToListAsync();

            var existingCourseKeySet = existingCourseKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var coursesToAdd = new List<Course>();

            foreach (var curriculumCourse in curriculumCourses)
            {
                foreach (var rule in curriculumCourse.ProgramRules)
                {
                    var departmentName = MapMajorToDepartmentName(rule.Major);
                    var department = departmentsByKey[$"{university.Id}:{departmentName}"];
                    var key = $"{department.Id}:{curriculumCourse.Code}";

                    if (existingCourseKeySet.Contains(key))
                    {
                        continue;
                    }

                    coursesToAdd.Add(new Course
                    {
                        Code = curriculumCourse.Code,
                        Name = curriculumCourse.Name,
                        CreditHours = curriculumCourse.CreditHours,
                        CourseType = ParseCourseType(rule.CourseType),
                        SemesterAvailability = ParseSemesterAvailability(curriculumCourse.SemesterAvailability),
                        PassingGradeThreshold = NormalizePassingGrade(curriculumCourse.PassingGradeThreshold),
                        DepartmentId = department.Id,
                        IsActive = true
                    });

                    existingCourseKeySet.Add(key);
                }
            }

            if (coursesToAdd.Count > 0)
            {
                context.Courses.AddRange(coursesToAdd);
                await context.SaveChangesAsync();
            }

            var prerequisitePath = Path.Combine(folder, "seed_prereqs.json");
            if (!File.Exists(prerequisitePath))
            {
                continue;
            }

            var prerequisites = LoadPrerequisites(prerequisitePath);

            var coursesByDepartmentAndCode = await context.Courses
                .Where(course => universityDepartmentIds.Contains(course.DepartmentId))
                .ToDictionaryAsync(
                    course => $"{course.DepartmentId}:{course.Code}",
                    StringComparer.OrdinalIgnoreCase);

            var existingPrerequisiteKeys = await context.CoursePrerequisites
                .Select(prerequisite => $"{prerequisite.CourseId}:{prerequisite.PrerequisiteId}")
                .ToListAsync();

            var existingPrerequisiteSet = existingPrerequisiteKeys.ToHashSet(StringComparer.Ordinal);
            var prerequisitesToAdd = new List<CoursePrerequisite>();

            foreach (var department in departmentsByKey.Values.Where(department => department.UniversityId == university.Id))
            {
                foreach (var prerequisite in prerequisites)
                {
                    if (!coursesByDepartmentAndCode.TryGetValue($"{department.Id}:{prerequisite.CourseCode}", out var course))
                    {
                        continue;
                    }

                    if (!coursesByDepartmentAndCode.TryGetValue($"{department.Id}:{prerequisite.PrerequisiteCourseCode}", out var prerequisiteCourse))
                    {
                        continue;
                    }

                    var key = $"{course.Id}:{prerequisiteCourse.Id}";
                    if (existingPrerequisiteSet.Contains(key))
                    {
                        continue;
                    }

                    prerequisitesToAdd.Add(new CoursePrerequisite
                    {
                        CourseId = course.Id,
                        PrerequisiteId = prerequisiteCourse.Id
                    });

                    existingPrerequisiteSet.Add(key);
                }
            }

            if (prerequisitesToAdd.Count > 0)
            {
                context.CoursePrerequisites.AddRange(prerequisitesToAdd);
                await context.SaveChangesAsync();
            }
        }
    }

    private static string ResolveSeedDataRoot()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src", "Cursus.DAL", "Database", "SeedData"))
        };

        var probe = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (probe is not null)
        {
            candidates.Add(Path.Combine(probe.FullName, "src", "Cursus.DAL", "Database", "SeedData"));
            candidates.Add(Path.Combine(probe.FullName, "Cursus.DAL", "Database", "SeedData"));
            probe = probe.Parent;
        }

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"SeedData folder not found. Probed {candidates.Count} locations from current directory '{Directory.GetCurrentDirectory()}' and base directory '{AppContext.BaseDirectory}'.");
    }

    private static string GetUniversityNameFromSlug(string slug)
    {
        return slug switch
        {
            "south-valley-university" => "South Valley National University",
            "american-university-in-cairo" => "AUC",
            "sinai-university" => "Sinai University",
            _ => string.Join(" ", slug.Split('-', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
    }

    private static string MapMajorToDepartmentName(string major)
    {
        return major.ToUpperInvariant() switch
        {
            "CS" => "Computer Science",
            "IT" => "Information Technology",
            "IS" => "Information Systems",
            "AI" => "Artificial Intelligence",
            "DS" => "Data Science",
            "SE" => "Software Engineering",
            "CE" => "Computer Engineering",
            "CSSE" => "Computer Science and Software Engineering",
            "IDSS" => "Information and Decision Support Systems",
            _ => major
        };
    }

    private static CourseType ParseCourseType(string courseType)
    {
        if (Enum.TryParse<CourseType>(courseType, true, out var parsedType))
        {
            return parsedType;
        }

        return CourseType.Core;
    }

    private static SemesterAvailability ParseSemesterAvailability(IEnumerable<string> values)
    {
        var normalized = values.Select(value => value.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasFall = normalized.Contains("Fall");
        var hasSpring = normalized.Contains("Spring");

        if (hasFall && hasSpring)
        {
            return SemesterAvailability.FallSpring;
        }

        if (hasFall)
        {
            return SemesterAvailability.Fall;
        }

        if (hasSpring)
        {
            return SemesterAvailability.Spring;
        }

        return SemesterAvailability.All;
    }

    private static string NormalizePassingGrade(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "D";
        }

        return value.Trim().ToUpperInvariant();
    }

    private static GraduationRules LoadGraduationRules(string path)
    {
        if (!File.Exists(path))
        {
            return new GraduationRules(132, 2.00m, new Dictionary<string, (int, decimal)>(StringComparer.OrdinalIgnoreCase));
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        var defaultCredits = TryGetInt(root, "totalCreditsRequired") ?? 132;
        var defaultGpa = TryGetDecimal(root, "minimumGraduationGPA") ?? 2.00m;
        var perMajor = new Dictionary<string, (int, decimal)>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("majors", out var majorsElement) && majorsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var majorProperty in majorsElement.EnumerateObject())
            {
                var majorCredits = TryGetInt(majorProperty.Value, "totalCreditsRequired") ?? defaultCredits;
                var majorGpa = TryGetDecimal(majorProperty.Value, "minimumGraduationGPA") ?? defaultGpa;
                perMajor[majorProperty.Name] = (majorCredits, majorGpa);
            }
        }

        return new GraduationRules(defaultCredits, defaultGpa, perMajor);
    }

    private static List<CurriculumCourseSeed> LoadCurriculumCourses(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new List<CurriculumCourseSeed>();

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var code = item.TryGetProperty("code", out var codeElement) ? codeElement.GetString() : null;
            var name = item.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var creditHours = 3;
            if (item.TryGetProperty("credits", out var creditsElement))
            {
                creditHours = TryGetInt(creditsElement, "creditHours") ?? 3;
            }

            var passingGrade = "D";
            if (item.TryGetProperty("gatingRequirements", out var gatingElement))
            {
                passingGrade = gatingElement.TryGetProperty("passingGradeThreshold", out var thresholdElement)
                    ? thresholdElement.GetString() ?? "D"
                    : "D";
            }

            var semesterAvailability = new List<string>();
            if (item.TryGetProperty("semesterAvailability", out var semesterElement) && semesterElement.ValueKind == JsonValueKind.Array)
            {
                semesterAvailability.AddRange(semesterElement.EnumerateArray()
                    .Where(value => value.ValueKind == JsonValueKind.String)
                    .Select(value => value.GetString()!)
                    .Where(value => !string.IsNullOrWhiteSpace(value)));
            }

            var programRules = new List<ProgramRuleSeed>();
            if (item.TryGetProperty("programRules", out var rulesElement) && rulesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var ruleProperty in rulesElement.EnumerateObject())
                {
                    var courseType = ruleProperty.Value.TryGetProperty("courseType", out var courseTypeElement)
                        ? courseTypeElement.GetString() ?? nameof(CourseType.Core)
                        : nameof(CourseType.Core);

                    programRules.Add(new ProgramRuleSeed(ruleProperty.Name, courseType));
                }
            }

            if (programRules.Count == 0)
            {
                continue;
            }

            result.Add(new CurriculumCourseSeed(
                code.Trim(),
                name.Trim(),
                Math.Max(1, Math.Min(6, creditHours)),
                NormalizePassingGrade(passingGrade),
                semesterAvailability,
                programRules));
        }

        return result;
    }

    private static List<PrerequisiteSeed> LoadPrerequisites(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var result = new List<PrerequisiteSeed>();

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var courseCode = item.TryGetProperty("courseCode", out var courseCodeElement)
                ? courseCodeElement.GetString()
                : null;
            var prerequisiteCode = item.TryGetProperty("prerequisiteCourseCode", out var prerequisiteCodeElement)
                ? prerequisiteCodeElement.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(courseCode) || string.IsNullOrWhiteSpace(prerequisiteCode))
            {
                continue;
            }

            result.Add(new PrerequisiteSeed(courseCode.Trim(), prerequisiteCode.Trim()));
        }

        return result;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return null;
    }

    private static decimal? TryGetDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        return null;
    }

    private sealed record ProgramRuleSeed(string Major, string CourseType);

    private sealed record CurriculumCourseSeed(
        string Code,
        string Name,
        int CreditHours,
        string PassingGradeThreshold,
        IReadOnlyCollection<string> SemesterAvailability,
        IReadOnlyCollection<ProgramRuleSeed> ProgramRules);

    private sealed record PrerequisiteSeed(string CourseCode, string PrerequisiteCourseCode);

    private sealed record GraduationRules(
        int DefaultCredits,
        decimal DefaultGpa,
        Dictionary<string, (int Credits, decimal Gpa)> PerMajor)
    {
        public (int RequiredCredits, decimal MinimumGpa) GetDepartmentDefaults(string major)
        {
            if (PerMajor.TryGetValue(major, out var values))
            {
                return (values.Credits, values.Gpa);
            }

            return (DefaultCredits, DefaultGpa);
        }
    }
}
