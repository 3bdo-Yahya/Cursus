using Cursus.BLL.Services;
using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Cursus.Domain.Constants;

namespace Cursus.PL.Seeding;

public static class StartupSeeder
{
    private const string DemoStudentPassword = "Demo123!";
    private static readonly Dictionary<string, string[]> UniversityNameAliasesBySlug = new(StringComparer.OrdinalIgnoreCase)
    {
        ["south-valley-university"] = ["South Valley National University"],
        ["american-university-in-cairo"] = ["AUC", "The American University in Cairo"]
    };

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

    public static async Task SeedSampleCatalogAsync(IServiceProvider serviceProvider)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Console.WriteLine("[Seeding] Starting SeedSampleCatalogAsync...");

            var seedDataRoot = ResolveSeedDataRoot();
            Console.WriteLine($"[Seeding] Seed data root: {seedDataRoot}");
            var universityFolders = Directory.GetDirectories(seedDataRoot);
            Console.WriteLine($"[Seeding] Found {universityFolders.Length} university folders");

            var universitiesByName = await context.Universities
                .ToDictionaryAsync(university => university.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var folder in universityFolders)
            {
                var slug = Path.GetFileName(folder);
                var universityName = GetUniversityNameFromSlug(slug);

                if (!universitiesByName.TryGetValue(universityName, out var university)
                    && !TryRenameLegacyUniversity(universitiesByName, slug, universityName, out university))
                {
                    university = new University { Name = universityName };
                    context.Universities.Add(university);
                    universitiesByName[universityName] = university;
                    Console.WriteLine($"[Seeding] Added university: {universityName}");
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
                Console.WriteLine($"[Seeding] Processing university: {universityName}");

                var curriculumPath = Path.Combine(folder, "curriculum-courses.json");
                if (!File.Exists(curriculumPath))
                {
                    Console.WriteLine($"[Seeding] No curriculum file found for {universityName}, skipping");
                    continue;
                }

                var graduationRequirementsPath = Path.Combine(folder, "graduation-requirements.json");
                var graduationRules = LoadGraduationRules(graduationRequirementsPath);
                var curriculumCourses = LoadCurriculumCourses(curriculumPath);
                Console.WriteLine($"[Seeding] Loaded {curriculumCourses.Count} courses for {universityName}");

                await using var transaction = await context.Database.BeginTransactionAsync();

                var majors = curriculumCourses
                    .SelectMany(course => course.ProgramRules)
                    .Select(rule => rule.Major)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var major in majors)
                {
                    var departmentName = MapMajorToDepartmentName(major);
                    var departmentKey = $"{university.Id}:{departmentName}";
                    var (requiredCredits, minimumGpa) = graduationRules.GetDepartmentDefaults(major);
                    
                    // Validate GPA is in valid range
                    if (minimumGpa < 0m || minimumGpa > 4.0m)
                    {
                        Console.WriteLine($"[Seeding] WARNING: Department '{departmentName}' has invalid GPA {minimumGpa}, using 2.0");
                        minimumGpa = 2.00m;
                    }

                    if (departmentsByKey.TryGetValue(departmentKey, out var existingDepartment))
                    {
                        var departmentChanged = false;

                        if (existingDepartment.TotalCreditsRequired != requiredCredits)
                        {
                            existingDepartment.TotalCreditsRequired = requiredCredits;
                            departmentChanged = true;
                        }

                        if (existingDepartment.MinGpaForGraduation != minimumGpa)
                        {
                            existingDepartment.MinGpaForGraduation = minimumGpa;
                            departmentChanged = true;
                        }

                        if (!existingDepartment.IsActive)
                        {
                            existingDepartment.IsActive = true;
                            departmentChanged = true;
                        }

                        if (departmentChanged)
                        {
                            Console.WriteLine($"[Seeding] Updated department: {departmentName} (Credits: {requiredCredits}, Min GPA: {minimumGpa})");
                        }

                        continue;
                    }

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
                    Console.WriteLine($"[Seeding] Added department: {departmentName} (Credits: {requiredCredits}, Min GPA: {minimumGpa})");
                }

                await context.SaveChangesAsync();

                var universityDepartmentIds = departmentsByKey
                    .Where(entry => entry.Value.UniversityId == university.Id)
                    .Select(entry => entry.Value.Id)
                    .ToHashSet();

                var existingCourses = await context.Courses
                    .Where(course => universityDepartmentIds.Contains(course.DepartmentId))
                    .ToListAsync();

                var existingCoursesByKey = existingCourses.ToDictionary(
                    course => $"{course.DepartmentId}:{course.Code}",
                    StringComparer.OrdinalIgnoreCase);
                var desiredCourseKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var coursesToAdd = new List<Course>();
                var updatedCourseCount = 0;
                var duplicateSeedCourseCount = 0;

                foreach (var curriculumCourse in curriculumCourses)
                {
                    foreach (var rule in curriculumCourse.ProgramRules)
                    {
                        var departmentName = MapMajorToDepartmentName(rule.Major);
                        var department = departmentsByKey[$"{university.Id}:{departmentName}"];
                        var key = $"{department.Id}:{curriculumCourse.Code}";
                        var recommendedSemester = GetRecommendedSemester(curriculumCourse.Code, rule);

                        if (!desiredCourseKeySet.Add(key))
                        {
                            duplicateSeedCourseCount++;
                            Console.WriteLine($"[Seeding] WARNING: Duplicate seed course {curriculumCourse.Code} in {departmentName}, skipping");
                            continue;
                        }

                        // Validate credit hours
                        int validCreditHours = curriculumCourse.CreditHours;
                        if (validCreditHours < 1 || validCreditHours > 6)
                        {
                            Console.WriteLine($"[Seeding] WARNING: Course {curriculumCourse.Code} has invalid creditHours={validCreditHours}, clamping to valid range");
                            validCreditHours = Math.Max(1, Math.Min(6, validCreditHours));
                        }

                        var courseType = ParseCourseType(rule.CourseType);
                        var semesterAvailability = ParseSemesterAvailability(curriculumCourse.SemesterAvailability);

                        if (existingCoursesByKey.TryGetValue(key, out var existingCourse))
                        {
                            if (SyncSeededCourse(
                                    existingCourse,
                                    curriculumCourse,
                                    validCreditHours,
                                    courseType,
                                    semesterAvailability,
                                    recommendedSemester))
                            {
                                updatedCourseCount++;
                            }

                            continue;
                        }

                        coursesToAdd.Add(new Course
                        {
                            Code = curriculumCourse.Code,
                            Name = curriculumCourse.Name,
                            CreditHours = validCreditHours,
                            CourseType = courseType,
                            SemesterAvailability = semesterAvailability,
                            PassingGradeThreshold = NormalizePassingGrade(curriculumCourse.PassingGradeThreshold),
                            DepartmentId = department.Id,
                            IsActive = true,
                            RecommendedSemester = recommendedSemester
                        });

                    }
                }

                var deactivatedCourseCount = 0;
                foreach (var existingCourse in existingCourses)
                {
                    var key = $"{existingCourse.DepartmentId}:{existingCourse.Code}";
                    if (desiredCourseKeySet.Contains(key) || !existingCourse.IsActive)
                    {
                        continue;
                    }

                    existingCourse.IsActive = false;
                    deactivatedCourseCount++;
                }

                if (duplicateSeedCourseCount > 0)
                {
                    Console.WriteLine($"[Seeding] Skipped {duplicateSeedCourseCount} duplicate seed course entries");
                }

                if (coursesToAdd.Count > 0)
                {
                    context.Courses.AddRange(coursesToAdd);
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[Seeding] Added {coursesToAdd.Count} courses");
                }

                if (updatedCourseCount > 0 || deactivatedCourseCount > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[Seeding] Updated {updatedCourseCount} courses, deactivated {deactivatedCourseCount} stale courses");
                }

                var coursesByDepartmentAndCode = await context.Courses
                    .Where(course => universityDepartmentIds.Contains(course.DepartmentId) && course.IsActive)
                    .ToDictionaryAsync(
                        course => $"{course.DepartmentId}:{course.Code}",
                        StringComparer.OrdinalIgnoreCase);

                await SeedGraduationRequirementsAsync(
                    context,
                    folder,
                    university,
                    departmentsByKey,
                    universityDepartmentIds,
                    coursesByDepartmentAndCode);

                var prerequisitePath = Path.Combine(folder, "seed_prereqs.json");
                if (!File.Exists(prerequisitePath))
                {
                    Console.WriteLine($"[Seeding] No prerequisites file found for {universityName}, skipping");
                    await transaction.CommitAsync();
                    continue;
                }

                var prerequisites = LoadPrerequisites(prerequisitePath);
                Console.WriteLine($"[Seeding] Loaded {prerequisites.Count} prerequisite relationships");

                var universityCourseIds = await context.Courses
                    .Where(course => universityDepartmentIds.Contains(course.DepartmentId))
                    .Select(course => course.Id)
                    .ToHashSetAsync();

                var existingPrerequisites = await context.CoursePrerequisites
                    .Where(prerequisite => universityCourseIds.Contains(prerequisite.CourseId))
                    .ToListAsync();

                var existingPrerequisiteSet = existingPrerequisites
                    .Select(prerequisite => $"{prerequisite.CourseId}:{prerequisite.PrerequisiteId}")
                    .ToHashSet(StringComparer.Ordinal);
                var desiredPrerequisiteSet = new HashSet<string>(StringComparer.Ordinal);
                var prerequisitesToAdd = new List<CoursePrerequisite>();
                int prereqSkippedCount = 0;

                // FIX: Apply prerequisites only to departments that have BOTH the course and its prerequisite
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
                            // Both courses must exist in SAME department - if prerequisite not in department, skip
                            prereqSkippedCount++;
                            continue;
                        }

                        var key = $"{course.Id}:{prerequisiteCourse.Id}";
                        if (!desiredPrerequisiteSet.Add(key) || existingPrerequisiteSet.Contains(key))
                        {
                            continue;
                        }

                        prerequisitesToAdd.Add(new CoursePrerequisite
                        {
                            CourseId = course.Id,
                            PrerequisiteId = prerequisiteCourse.Id
                        });

                        Console.WriteLine($"[Seeding] Added prerequisite: {prerequisite.CourseCode} requires {prerequisite.PrerequisiteCourseCode} in {department.Name}");
                    }
                }

                var prerequisitesToRemove = existingPrerequisites
                    .Where(prerequisite => !desiredPrerequisiteSet.Contains(
                        $"{prerequisite.CourseId}:{prerequisite.PrerequisiteId}"))
                    .ToList();

                if (prereqSkippedCount > 0)
                {
                    Console.WriteLine($"[Seeding] Skipped {prereqSkippedCount} prerequisites (course not found in same department)");
                }

                if (prerequisitesToAdd.Count > 0)
                {
                    context.CoursePrerequisites.AddRange(prerequisitesToAdd);
                }

                if (prerequisitesToRemove.Count > 0)
                {
                    context.CoursePrerequisites.RemoveRange(prerequisitesToRemove);
                }

                if (prerequisitesToAdd.Count > 0 || prerequisitesToRemove.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($"[Seeding] Added {prerequisitesToAdd.Count} prerequisites, removed {prerequisitesToRemove.Count} stale prerequisites");
                }

                await transaction.CommitAsync();
            }

            Console.WriteLine("[Seeding] SeedSampleCatalogAsync completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Seeding] ERROR in SeedSampleCatalogAsync: {ex.Message}");
            Console.WriteLine($"[Seeding] Stack trace: {ex.StackTrace}");
            throw; // Re-throw to let caller handle
        }
    }

    public static async Task SeedGradeScaleAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var universities = await context.Universities.ToListAsync();

        foreach (var university in universities)
        {
            // Check if grade scales already exist for this university (idempotent)
            var hasExistingGradeScales = await context.GradeScales
                .AnyAsync(gs => gs.UniversityId == university.Id);

            if (hasExistingGradeScales)
            {
                continue;
            }

            // Define the 4.0 GPA scale from SRS Section 14.1
            var gradeScalesToAdd = new List<GradeScale>
            {
                new GradeScale { UniversityId = university.Id, LetterGrade = "A+", PointValue = 4.0m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "A", PointValue = 4.0m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "A-", PointValue = 3.7m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "B+", PointValue = 3.3m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "B", PointValue = 3.0m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "B-", PointValue = 2.7m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "C+", PointValue = 2.3m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "C", PointValue = 2.0m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "C-", PointValue = 1.7m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "D+", PointValue = 1.3m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "D", PointValue = 1.0m },
                new GradeScale { UniversityId = university.Id, LetterGrade = "F", PointValue = 0.0m }
            };

            context.GradeScales.AddRange(gradeScalesToAdd);
        }

        await context.SaveChangesAsync();
    }

    public static async Task SeedDemoStudentsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var departments = await context.Departments
            .Include(department => department.University)
            .Where(department => department.IsActive)
            .OrderBy(department => department.University!.Name)
            .ThenBy(department => department.Name)
            .ToListAsync();

        if (departments.Count == 0)
        {
            Console.WriteLine("[Seeding] No departments found for demo student seeding, skipping");
            return;
        }

        var profiles = GetDemoStudentProfiles();
        Console.WriteLine($"[Seeding] Starting demo student seeding for {profiles.Count} students");

        for (var index = 0; index < profiles.Count; index++)
        {
            var profile = profiles[index];
            var department = ResolveDemoDepartment(profile, departments, index);

            if (department is null)
            {
                Console.WriteLine($"[Seeding] WARNING: No department resolved for demo student {profile.Email}, skipping");
                continue;
            }

            var user = await userManager.FindByEmailAsync(profile.Email)
                ?? await userManager.FindByNameAsync(profile.Email);

            if (user is null)
            {
                user = new AppUser
                {
                    UserName = profile.Email,
                    Email = profile.Email,
                    EmailConfirmed = true,
                    UniversityId = department.UniversityId,
                    DepartmentId = department.Id,
                    AcademicYear = profile.AcademicYear,
                    CurrentSemester = profile.CurrentSemester,
                    CurrentStanding = profile.Standing,
                    EnrollmentDate = ResolveEnrollmentDate(profile)
                };

                var createResult = await userManager.CreateAsync(user, DemoStudentPassword);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create demo student '{profile.Email}': {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
                }
            }
            else
            {
                user.UniversityId = department.UniversityId;
                user.DepartmentId = department.Id;
                user.AcademicYear = profile.AcademicYear;
                user.CurrentSemester = profile.CurrentSemester;
                user.CurrentStanding = profile.Standing;
                user.EnrollmentDate = ResolveEnrollmentDate(profile) ?? user.EnrollmentDate;

                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to update demo student '{profile.Email}': {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, "Student"))
            {
                var roleResult = await userManager.AddToRoleAsync(user, "Student");
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to assign Student role to demo student '{profile.Email}': {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
                }
            }

            var courses = await context.Courses
                .Where(course => course.DepartmentId == department.Id && course.IsActive)
                .OrderBy(course => course.Code)
                .ToListAsync();

            if (courses.Count == 0)
            {
                Console.WriteLine($"[Seeding] WARNING: No courses found for demo student {profile.Email}, skipping history");
                continue;
            }

            var desiredCourses = BuildDemoStudentCourseHistory(user.Id, profile, courses);
            await SyncDemoStudentCoursesAsync(context, user.Id, desiredCourses);
            await SyncDemoStandingHistoryAsync(context, user.Id, profile, desiredCourses);
            await context.SaveChangesAsync();
        }

        Console.WriteLine("[Seeding] Demo student seeding completed successfully");
    }

    private static async Task SyncDemoStudentCoursesAsync(
        ApplicationDbContext context,
        string studentId,
        List<StudentCourse> desiredCourses)
    {
        var existingCourses = await context.StudentCourses
            .Where(studentCourse => studentCourse.StudentId == studentId)
            .ToListAsync();

        var desiredByKey = desiredCourses.ToDictionary(GetStudentCourseKey, StringComparer.Ordinal);
        var existingByKey = existingCourses.ToDictionary(GetStudentCourseKey, StringComparer.Ordinal);

        foreach (var existingCourse in existingCourses)
        {
            if (!desiredByKey.ContainsKey(GetStudentCourseKey(existingCourse)))
            {
                context.StudentCourses.Remove(existingCourse);
            }
        }

        foreach (var desiredCourse in desiredCourses)
        {
            if (existingByKey.TryGetValue(GetStudentCourseKey(desiredCourse), out var existingCourse))
            {
                existingCourse.Status = desiredCourse.Status;
                existingCourse.Grade = desiredCourse.Grade;
            }
            else
            {
                context.StudentCourses.Add(desiredCourse);
            }
        }
    }

    private static async Task SyncDemoStandingHistoryAsync(
        ApplicationDbContext context,
        string studentId,
        DemoStudentProfile profile,
        List<StudentCourse> desiredCourses)
    {
        var completedTerms = desiredCourses
            .Where(course => course.Status is StudentCourseStatus.Completed or StudentCourseStatus.Failed)
            .Select(course => new AcademicTerm(course.AcademicYear, course.Semester))
            .Distinct()
            .OrderBy(term => term.AcademicYear)
            .ThenBy(term => term.Semester)
            .ToList();

        var existingHistories = await context.StandingHistories
            .Where(history => history.StudentId == studentId)
            .ToListAsync();

        var desiredHistories = new List<StandingHistory>();
        for (var index = 0; index < completedTerms.Count; index++)
        {
            var term = completedTerms[index];
            var distanceFromLatest = completedTerms.Count - index - 1;
            var cumulativeGpa = ClampGpa(profile.CumulativeGpa - (distanceFromLatest * 0.05m));
            var semesterGpa = ClampGpa(profile.SemesterGpa - (distanceFromLatest % 3 * 0.04m));

            desiredHistories.Add(new StandingHistory
            {
                StudentId = studentId,
                AcademicYear = term.AcademicYear,
                Semester = term.Semester,
                SemesterGpa = semesterGpa,
                CumulativeGpa = cumulativeGpa,
                Standing = index == completedTerms.Count - 1
                    ? profile.Standing
                    : ResolveStanding(cumulativeGpa)
            });
        }

        var desiredByKey = desiredHistories.ToDictionary(GetStandingHistoryKey, StringComparer.Ordinal);
        var existingByKey = existingHistories.ToDictionary(GetStandingHistoryKey, StringComparer.Ordinal);

        foreach (var existingHistory in existingHistories)
        {
            if (!desiredByKey.ContainsKey(GetStandingHistoryKey(existingHistory)))
            {
                context.StandingHistories.Remove(existingHistory);
            }
        }

        foreach (var desiredHistory in desiredHistories)
        {
            if (existingByKey.TryGetValue(GetStandingHistoryKey(desiredHistory), out var existingHistory))
            {
                existingHistory.SemesterGpa = desiredHistory.SemesterGpa;
                existingHistory.CumulativeGpa = desiredHistory.CumulativeGpa;
                existingHistory.Standing = desiredHistory.Standing;
            }
            else
            {
                context.StandingHistories.Add(desiredHistory);
            }
        }
    }

    private static List<StudentCourse> BuildDemoStudentCourseHistory(
        string studentId,
        DemoStudentProfile profile,
        List<Course> courses)
    {
        if (IsMazenPresentationPersona(profile))
        {
            return BuildMazenPresentationCourseHistory(studentId, profile, courses);
        }

        var desiredCourses = new List<StudentCourse>();
        var historyTerms = GetDemoHistoryTerms();
        var completedCapacity = Math.Max(0, courses.Count - profile.FailedCourseCount - profile.CurrentCourseCount);
        var completedCount = Math.Min(profile.CompletedCourseCount, completedCapacity);
        var failedCount = Math.Min(profile.FailedCourseCount, Math.Max(0, courses.Count - completedCount - profile.CurrentCourseCount));
        var currentCount = Math.Min(profile.CurrentCourseCount, Math.Max(0, courses.Count - completedCount - failedCount));
        var gradeCycle = ResolveGradeCycle(profile.CumulativeGpa);

        for (var index = 0; index < completedCount; index++)
        {
            var course = courses[index];
            var term = historyTerms[Math.Min(index / 5, historyTerms.Count - 1)];

            desiredCourses.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = course.Id,
                Status = StudentCourseStatus.Completed,
                Grade = gradeCycle[index % gradeCycle.Length],
                AcademicYear = term.AcademicYear,
                Semester = term.Semester
            });
        }

        for (var index = 0; index < failedCount; index++)
        {
            var course = courses[completedCount + index];
            var term = historyTerms[Math.Min((completedCount + index) / 5, historyTerms.Count - 1)];

            desiredCourses.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = course.Id,
                Status = StudentCourseStatus.Failed,
                Grade = "F",
                AcademicYear = term.AcademicYear,
                Semester = term.Semester
            });
        }

        for (var index = 0; index < currentCount; index++)
        {
            var course = courses[completedCount + failedCount + index];

            desiredCourses.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = course.Id,
                Status = StudentCourseStatus.InProgress,
                Grade = null,
                AcademicYear = profile.AcademicYear,
                Semester = profile.CurrentSemester
            });
        }

        return desiredCourses;
    }

    private static Department? ResolveDemoDepartment(
        DemoStudentProfile profile,
        List<Department> departments,
        int profileIndex)
    {
        var exactDepartment = departments.FirstOrDefault(department =>
            department.University is not null &&
            string.Equals(department.University.Name, profile.UniversityName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(department.Name, profile.DepartmentName, StringComparison.OrdinalIgnoreCase));

        if (exactDepartment is not null)
        {
            return exactDepartment;
        }

        var matchingDepartment = departments.FirstOrDefault(department =>
            string.Equals(department.Name, profile.DepartmentName, StringComparison.OrdinalIgnoreCase));

        if (matchingDepartment is not null)
        {
            return matchingDepartment;
        }

        return departments.Count == 0
            ? null
            : departments[profileIndex % departments.Count];
    }

    private static IReadOnlyList<DemoStudentProfile> GetDemoStudentProfiles() =>
    [
        // Defense presentation persona — SVU CS sophomore, Spring Y2, CS211 keystone at risk
        new("mazen.hassan@cursus.demo", "South Valley University", "Computer Science", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 19, 5, 0, 2.85m, 2.90m),
        new("freshman.cs@cursus.demo", "South Valley University", "Computer Science", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 5, 5, 0, 3.40m, 3.48m),
        new("sophomore.it@cursus.demo", "South Valley University", "Information Technology", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 16, 5, 1, 2.86m, 2.94m),
        new("junior.ai@cursus.demo", "South Valley University", "Artificial Intelligence", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 28, 5, 1, 3.16m, 3.24m),
        new("senior.is@cursus.demo", "South Valley University", "Information Systems", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 42, 4, 0, 3.55m, 3.62m),
        new("probation.cs@cursus.demo", "South Valley University", "Computer Science", "2025-2026", SemesterType.Spring, AcademicStanding.Probation, 18, 4, 2, 1.88m, 1.94m),
        new("junior.csse@cursus.demo", "Sinai University", "Computer Science and Software Engineering", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 30, 5, 1, 3.08m, 3.18m),
        new("freshman.idss@cursus.demo", "Sinai University", "Information and Decision Support Systems", "2025-2026", SemesterType.Spring, AcademicStanding.Warning, 6, 5, 1, 2.05m, 2.12m),
        new("senior.auc@cursus.demo", "American University in Cairo", "Computer Science", "2025-2026", SemesterType.Spring, AcademicStanding.Good, 38, 4, 0, 3.68m, 3.74m)
    ];

    /// <summary>
    /// Presentation defense account — enrolled 1 Oct 2024, Year 2 Spring, CS211 in progress.
    /// </summary>
    private static bool IsMazenPresentationPersona(DemoStudentProfile profile) =>
        string.Equals(profile.Email, "mazen.hassan@cursus.demo", StringComparison.OrdinalIgnoreCase);

    private static DateTime? ResolveEnrollmentDate(DemoStudentProfile profile) =>
        IsMazenPresentationPersona(profile)
            ? new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc)
            : null;

    /// <summary>
    /// Curriculum-aligned transcript for Mazen: semesters 1–3 completed, semester-4 spring
    /// in progress with CS211 Data Structures I as the Impact Analyzer keystone.
    /// </summary>
    private static List<StudentCourse> BuildMazenPresentationCourseHistory(
        string studentId,
        DemoStudentProfile profile,
        List<Course> courses)
    {
        var byCode = courses
            .GroupBy(course => course.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var gradeCycle = ResolveGradeCycle(profile.CumulativeGpa);
        var desired = new List<StudentCourse>();
        var gradeIndex = 0;

        void AddCompleted(IEnumerable<string> codes, string academicYear, SemesterType semester)
        {
            foreach (var code in codes)
            {
                if (!byCode.TryGetValue(code, out var course))
                {
                    continue;
                }

                desired.Add(new StudentCourse
                {
                    StudentId = studentId,
                    CourseId = course.Id,
                    Status = StudentCourseStatus.Completed,
                    Grade = gradeCycle[gradeIndex % gradeCycle.Length],
                    AcademicYear = academicYear,
                    Semester = semester
                });
                gradeIndex++;
            }
        }

        // Enrolled Oct 2024 → Y1 Fall / Y1 Spring / Y2 Fall completed; Y2 Spring in progress
        AddCompleted(
            ["CS121", "EE101", "HU111", "HU141", "HU151", "HU153", "MA111"],
            "2024-2025",
            SemesterType.Fall);
        AddCompleted(
            ["CS141", "HU112", "HU122", "HU132", "MA112", "MA113", "MA121"],
            "2024-2025",
            SemesterType.Spring);
        AddCompleted(
            ["CS241", "EE201", "IS221", "IT231", "MA231"],
            "2025-2026",
            SemesterType.Fall);

        // Spring Y2 — CS211 is the keystone fail-seed for the defense cascade
        foreach (var code in new[] { "CS211", "MA222", "IS211", "CS242", "PH201" })
        {
            if (!byCode.TryGetValue(code, out var course))
            {
                continue;
            }

            desired.Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = course.Id,
                Status = StudentCourseStatus.InProgress,
                Grade = null,
                AcademicYear = profile.AcademicYear,
                Semester = profile.CurrentSemester
            });
        }

        return desired;
    }

    private static IReadOnlyList<AcademicTerm> GetDemoHistoryTerms() =>
    [
        new("2021-2022", SemesterType.Fall),
        new("2021-2022", SemesterType.Spring),
        new("2022-2023", SemesterType.Fall),
        new("2022-2023", SemesterType.Spring),
        new("2023-2024", SemesterType.Fall),
        new("2023-2024", SemesterType.Spring),
        new("2024-2025", SemesterType.Fall),
        new("2024-2025", SemesterType.Spring),
        new("2025-2026", SemesterType.Fall)
    ];

    private static string[] ResolveGradeCycle(decimal cumulativeGpa)
    {
        if (cumulativeGpa >= 3.5m)
        {
            return ["A", "A-", "B+", "A", "B+"];
        }

        if (cumulativeGpa >= 3.0m)
        {
            return ["B+", "B", "A-", "B", "C+"];
        }

        if (cumulativeGpa >= 2.2m)
        {
            return ["B", "C+", "C", "B-", "C+"];
        }

        return ["C", "D+", "D", "C-", "D"];
    }

    private static decimal ClampGpa(decimal value) => Math.Max(0.00m, Math.Min(4.00m, value));

    private static AcademicStanding ResolveStanding(decimal cumulativeGpa)
    {
        if (cumulativeGpa < 2.00m)
        {
            return AcademicStanding.Probation;
        }

        if (cumulativeGpa < 2.25m)
        {
            return AcademicStanding.Warning;
        }

        return AcademicStanding.Good;
    }

    private static string GetStudentCourseKey(StudentCourse studentCourse) =>
        $"{studentCourse.CourseId}:{studentCourse.AcademicYear}:{studentCourse.Semester}";

    private static string GetStandingHistoryKey(StandingHistory history) =>
        $"{history.AcademicYear}:{history.Semester}";

    private static bool SyncSeededCourse(
        Course course,
        CurriculumCourseSeed curriculumCourse,
        int validCreditHours,
        CourseType courseType,
        SemesterAvailability semesterAvailability,
        int? recommendedSemester)
    {
        var changed = false;
        var passingGradeThreshold = NormalizePassingGrade(curriculumCourse.PassingGradeThreshold);

        if (!string.Equals(course.Code, curriculumCourse.Code, StringComparison.Ordinal))
        {
            course.Code = curriculumCourse.Code;
            changed = true;
        }

        if (!string.Equals(course.Name, curriculumCourse.Name, StringComparison.Ordinal))
        {
            course.Name = curriculumCourse.Name;
            changed = true;
        }

        if (course.CreditHours != validCreditHours)
        {
            course.CreditHours = validCreditHours;
            changed = true;
        }

        if (course.CourseType != courseType)
        {
            course.CourseType = courseType;
            changed = true;
        }

        if (course.SemesterAvailability != semesterAvailability)
        {
            course.SemesterAvailability = semesterAvailability;
            changed = true;
        }

        if (!string.Equals(course.PassingGradeThreshold, passingGradeThreshold, StringComparison.Ordinal))
        {
            course.PassingGradeThreshold = passingGradeThreshold;
            changed = true;
        }

        if (course.RecommendedSemester != recommendedSemester)
        {
            course.RecommendedSemester = recommendedSemester;
            changed = true;
        }

        if (!course.IsActive)
        {
            course.IsActive = true;
            changed = true;
        }

        return changed;
    }

    private static async Task SeedGraduationRequirementsAsync(
        ApplicationDbContext context,
        string universityFolder,
        University university,
        Dictionary<string, Department> departmentsByKey,
        HashSet<int> universityDepartmentIds,
        Dictionary<string, Course> coursesByDepartmentAndCode)
    {
        var graduationRequirementPath = Path.Combine(universityFolder, "seed_graduation_reqs.json");
        if (!File.Exists(graduationRequirementPath))
        {
            Console.WriteLine($"[Seeding] No graduation requirement seed file found for {university.Name}, skipping");
            return;
        }

        var graduationRequirementSeeds = LoadGraduationRequirementSeeds(graduationRequirementPath);
        Console.WriteLine($"[Seeding] Loaded {graduationRequirementSeeds.Count} graduation requirement rows");

        if (graduationRequirementSeeds.Count == 0)
        {
            return;
        }

        var universityDepartments = departmentsByKey.Values
            .Where(department => department.UniversityId == university.Id)
            .ToList();

        var existingRequirements = await context.GraduationRequirements
            .Where(requirement => universityDepartmentIds.Contains(requirement.DepartmentId))
            .ToDictionaryAsync(
                requirement => $"{requirement.DepartmentId}:{requirement.CategoryType}",
                StringComparer.OrdinalIgnoreCase);

        var touchedRequirements = new List<(GraduationRequirement Requirement, Department Department, GraduationRequirementSeed Seed)>();
        var addedRequirementCount = 0;
        var updatedRequirementCount = 0;
        var skippedRequirementCount = 0;

        var catalogDepartmentIds = coursesByDepartmentAndCode.Values
            .Select(course => course.DepartmentId)
            .ToHashSet();

        foreach (var seed in graduationRequirementSeeds)
        {
            var targetDepartments = ResolveGraduationRequirementDepartments(
                seed,
                university,
                universityDepartments,
                departmentsByKey,
                catalogDepartmentIds);

            if (targetDepartments.Count == 0)
            {
                skippedRequirementCount++;
                Console.WriteLine($"[Seeding] WARNING: No department found for graduation requirement major '{seed.Major ?? "all"}'");
                continue;
            }

            foreach (var department in targetDepartments)
            {
                var key = $"{department.Id}:{seed.CategoryType}";

                if (!existingRequirements.TryGetValue(key, out var requirement))
                {
                    requirement = new GraduationRequirement
                    {
                        DepartmentId = department.Id,
                        CategoryType = seed.CategoryType,
                        RequiredCredits = seed.RequiredCredits
                    };

                    context.GraduationRequirements.Add(requirement);
                    existingRequirements[key] = requirement;
                    addedRequirementCount++;
                }
                else if (requirement.RequiredCredits != seed.RequiredCredits)
                {
                    requirement.RequiredCredits = seed.RequiredCredits;
                    updatedRequirementCount++;
                }

                touchedRequirements.Add((requirement, department, seed));
            }
        }

        await context.SaveChangesAsync();

        var touchedRequirementIds = touchedRequirements
            .Select(item => item.Requirement.Id)
            .Distinct()
            .ToHashSet();

        if (touchedRequirementIds.Count == 0)
        {
            Console.WriteLine($"[Seeding] Graduation requirements skipped: {skippedRequirementCount}");
            return;
        }

        var existingRequirementCourses = await context.GraduationRequirementCourses
            .Where(requirementCourse => touchedRequirementIds.Contains(requirementCourse.GraduationRequirementId))
            .ToListAsync();

        var existingRequirementCourseSet = existingRequirementCourses
            .Select(requirementCourse => $"{requirementCourse.GraduationRequirementId}:{requirementCourse.CourseId}")
            .ToHashSet(StringComparer.Ordinal);
        var desiredRequirementCourseSet = new HashSet<string>(StringComparer.Ordinal);
        var requirementCoursesToAdd = new List<GraduationRequirementCourse>();
        var missingEligibleCourseCount = 0;

        foreach (var (requirement, department, seed) in touchedRequirements)
        {
            foreach (var eligibleCourseCode in seed.EligibleCourseCodes)
            {
                if (!coursesByDepartmentAndCode.TryGetValue($"{department.Id}:{eligibleCourseCode}", out var course))
                {
                    missingEligibleCourseCount++;
                    continue;
                }

                var key = $"{requirement.Id}:{course.Id}";
                if (!desiredRequirementCourseSet.Add(key) || existingRequirementCourseSet.Contains(key))
                {
                    continue;
                }

                requirementCoursesToAdd.Add(new GraduationRequirementCourse
                {
                    GraduationRequirementId = requirement.Id,
                    CourseId = course.Id
                });
            }
        }

        var requirementCoursesToRemove = existingRequirementCourses
            .Where(requirementCourse => !desiredRequirementCourseSet.Contains(
                $"{requirementCourse.GraduationRequirementId}:{requirementCourse.CourseId}"))
            .ToList();

        if (requirementCoursesToAdd.Count > 0)
        {
            context.GraduationRequirementCourses.AddRange(requirementCoursesToAdd);
        }

        if (requirementCoursesToRemove.Count > 0)
        {
            context.GraduationRequirementCourses.RemoveRange(requirementCoursesToRemove);
        }

        if (requirementCoursesToAdd.Count > 0 || requirementCoursesToRemove.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        Console.WriteLine(
            $"[Seeding] Graduation requirements added: {addedRequirementCount}, updated: {updatedRequirementCount}, skipped: {skippedRequirementCount}, course links added: {requirementCoursesToAdd.Count}, course links removed: {requirementCoursesToRemove.Count}, missing eligible courses: {missingEligibleCourseCount}");
    }

    private static List<Department> ResolveGraduationRequirementDepartments(
        GraduationRequirementSeed seed,
        University university,
        List<Department> universityDepartments,
        Dictionary<string, Department> departmentsByKey,
        HashSet<int> catalogDepartmentIds)
    {
        if (string.IsNullOrWhiteSpace(seed.Major))
        {
            return universityDepartments
                .Where(department => catalogDepartmentIds.Contains(department.Id))
                .ToList();
        }

        var departmentName = MapMajorToDepartmentName(seed.Major);
        var departmentKey = $"{university.Id}:{departmentName}";

        if (!departmentsByKey.TryGetValue(departmentKey, out var department)
            || !catalogDepartmentIds.Contains(department.Id))
        {
            return [];
        }

        return [department];
    }

    private static string ResolveSeedDataRoot()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Published/deployed layout (e.g. MonsterASP wwwroot)
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Database", "SeedData")),

            // Local dev / source tree layout
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "src", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Cursus.DAL", "Database", "SeedData")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src", "Cursus.DAL", "Database", "SeedData"))
        };

        var probe = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (probe is not null)
        {
            candidates.Add(Path.Combine(probe.FullName, "Database", "SeedData"));
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

    private static bool TryRenameLegacyUniversity(
        Dictionary<string, University> universitiesByName,
        string slug,
        string canonicalName,
        out University university)
    {
        university = null!;

        if (!UniversityNameAliasesBySlug.TryGetValue(slug, out var aliases))
        {
            return false;
        }

        foreach (var alias in aliases)
        {
            if (!universitiesByName.TryGetValue(alias, out var legacyUniversity))
            {
                continue;
            }

            university = legacyUniversity;
            university.Name = canonicalName;
            universitiesByName.Remove(alias);
            universitiesByName[canonicalName] = university;
            Console.WriteLine($"[Seeding] Renamed university '{alias}' to '{canonicalName}'");
            return true;
        }

        return false;
    }

    private static string GetUniversityNameFromSlug(string slug)
    {
        return slug switch
        {
            "south-valley-university" => "South Valley University",
            "american-university-in-cairo" => "American University in Cairo",
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

    private static int? GetRecommendedSemester(string courseCode, ProgramRuleSeed rule)
    {
        if (rule.RecommendedSemesters.Count > 0)
            return rule.RecommendedSemesters[0];

        return SemesterMath.InferFromCourseCode(courseCode);
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

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            var defaultCredits = TryGetInt(root, "totalCreditsRequired") ?? 132;
            var defaultGpa = TryGetDecimal(root, "minimumGraduationGPA") ?? 2.00m;
            
            // Validate default GPA is in valid range (0.0 - 4.0)
            if (defaultGpa < 0m || defaultGpa > 4.0m)
            {
                Console.WriteLine($"[Seeding] WARNING: Default GPA {defaultGpa} is out of valid range, using 2.0");
                defaultGpa = 2.00m;
            }
            
            var perMajor = new Dictionary<string, (int, decimal)>(StringComparer.OrdinalIgnoreCase);

            if (root.TryGetProperty("majors", out var majorsElement) && majorsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var majorProperty in majorsElement.EnumerateObject())
                {
                    var majorCredits = TryGetInt(majorProperty.Value, "totalCreditsRequired") ?? defaultCredits;
                    var majorGpa = TryGetDecimal(majorProperty.Value, "minimumGraduationGPA") ?? defaultGpa;
                    
                    // Validate major GPA is in valid range (0.0 - 4.0)
                    if (majorGpa < 0m || majorGpa > 4.0m)
                    {
                        Console.WriteLine($"[Seeding] WARNING: GPA for {majorProperty.Name} is {majorGpa}, out of valid range, using {defaultGpa}");
                        majorGpa = defaultGpa;
                    }
                    
                    perMajor[majorProperty.Name] = (majorCredits, majorGpa);
                }
            }

            return new GraduationRules(defaultCredits, defaultGpa, perMajor);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse graduation rules from '{path}': {ex.Message}", ex);
        }
    }

    private static List<CurriculumCourseSeed> LoadCurriculumCourses(string path)
    {
        try
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

                        var recommendedSemesters = new List<int>();
                        if (ruleProperty.Value.TryGetProperty("recommendedSemesters", out var semestersElement)
                            && semestersElement.ValueKind == JsonValueKind.Array)
                        {
                            recommendedSemesters.AddRange(semestersElement.EnumerateArray()
                                .Where(value => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _))
                                .Select(value => value.GetInt32())
                                .Where(value => value >= 1 && value <= 8));
                        }

                        programRules.Add(new ProgramRuleSeed(ruleProperty.Name, courseType, recommendedSemesters));
                    }
                }

                if (programRules.Count == 0)
                {
                    continue;
                }

                // Validate credit hours before adding
                int validatedCreditHours = creditHours;
                if (creditHours < 1 || creditHours > 6)
                {
                    Console.WriteLine($"[Seeding] WARNING: Course {code} has invalid creditHours={creditHours}, will be clamped to valid range");
                    validatedCreditHours = Math.Max(1, Math.Min(6, creditHours));
                }

                result.Add(new CurriculumCourseSeed(
                    code.Trim(),
                    name.Trim(),
                    validatedCreditHours,
                    NormalizePassingGrade(passingGrade),
                    semesterAvailability,
                    programRules));
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse curriculum courses from '{path}': {ex.Message}", ex);
        }
    }

    private static List<PrerequisiteSeed> LoadPrerequisites(string path)
    {
        try
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
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse prerequisites from '{path}': {ex.Message}", ex);
        }
    }

    private static List<GraduationRequirementSeed> LoadGraduationRequirementSeeds(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var result = new List<GraduationRequirementSeed>();

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                var categoryTypeValue = item.TryGetProperty("categoryType", out var categoryTypeElement)
                    ? categoryTypeElement.GetString()
                    : null;

                if (!Enum.TryParse<CourseType>(categoryTypeValue, true, out var categoryType))
                {
                    Console.WriteLine($"[Seeding] WARNING: Graduation requirement has invalid categoryType '{categoryTypeValue}', skipping");
                    continue;
                }

                var requiredCredits = TryGetInt(item, "requiredCredits");
                if (!requiredCredits.HasValue || requiredCredits.Value < 0)
                {
                    Console.WriteLine($"[Seeding] WARNING: Graduation requirement '{categoryType}' has invalid requiredCredits, skipping");
                    continue;
                }

                var major = item.TryGetProperty("major", out var majorElement)
                    ? majorElement.GetString()
                    : null;

                var eligibleCourseCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (item.TryGetProperty("eligibleCourseCodes", out var eligibleCoursesElement) &&
                    eligibleCoursesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var codeElement in eligibleCoursesElement.EnumerateArray())
                    {
                        if (codeElement.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var courseCode = codeElement.GetString();
                        if (!string.IsNullOrWhiteSpace(courseCode))
                        {
                            eligibleCourseCodes.Add(courseCode.Trim());
                        }
                    }
                }

                result.Add(new GraduationRequirementSeed(
                    string.IsNullOrWhiteSpace(major) ? null : major.Trim(),
                    categoryType,
                    requiredCredits.Value,
                    eligibleCourseCodes.ToList()));
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse graduation requirements from '{path}': {ex.Message}", ex);
        }
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

    private sealed record ProgramRuleSeed(
        string Major,
        string CourseType,
        IReadOnlyList<int> RecommendedSemesters);

    private sealed record CurriculumCourseSeed(
        string Code,
        string Name,
        int CreditHours,
        string PassingGradeThreshold,
        IReadOnlyCollection<string> SemesterAvailability,
        IReadOnlyCollection<ProgramRuleSeed> ProgramRules);

    private sealed record PrerequisiteSeed(string CourseCode, string PrerequisiteCourseCode);

    private sealed record GraduationRequirementSeed(
        string? Major,
        CourseType CategoryType,
        int RequiredCredits,
        IReadOnlyCollection<string> EligibleCourseCodes);

    private sealed record DemoStudentProfile(
        string Email,
        string UniversityName,
        string DepartmentName,
        string AcademicYear,
        SemesterType CurrentSemester,
        AcademicStanding Standing,
        int CompletedCourseCount,
        int CurrentCourseCount,
        int FailedCourseCount,
        decimal SemesterGpa,
        decimal CumulativeGpa);

    private sealed record AcademicTerm(string AcademicYear, SemesterType Semester);

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


