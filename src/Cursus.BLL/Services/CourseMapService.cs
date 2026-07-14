using Cursus.DAL.Database;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class CourseMapService : ICourseMapService
    {
        private readonly ApplicationDbContext _db;
        private readonly IGenericRepository<Course> _courseRepository;
        private readonly IGenericRepository<StudentCourse> _studentCourseRepository;
        private readonly IPlannerService _plannerService;
        private readonly IAcademicMetricsService _academicMetricsService;

        public CourseMapService(
            ApplicationDbContext db,
            IGenericRepository<Course> courseRepository,
            IGenericRepository<StudentCourse> studentCourseRepository,
            IPlannerService plannerService,
            IAcademicMetricsService academicMetricsService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _courseRepository = courseRepository;
            _studentCourseRepository = studentCourseRepository;
            _plannerService = plannerService;
            _academicMetricsService = academicMetricsService;
        }

        public async Task<CourseGraphDto> GetCourseGraphForStudentAsync(string studentId, int departmentId)
        {
            var courses = await _courseRepository.GetAll()
                .Where(c => c.DepartmentId == departmentId && c.IsActive)
                .Include(c => c.Department)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.Prerequisite)
                .AsNoTracking()
                .ToListAsync();

            var studentCourses = await _studentCourseRepository.GetAll()
                .Where(sc => sc.StudentId == studentId)
                .Include(sc => sc.Course)
                .AsNoTracking()
                .ToListAsync();

            var plannedCourseIds = await GetPrimaryTermPlannedCourseIdsAsync(studentId, departmentId, studentCourses);

            var studentCourseMap = studentCourses
                .GroupBy(sc => sc.CourseId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var bestAttempt = g.OrderBy(sc => sc.Status switch
                        {
                            StudentCourseStatus.Completed => 0,
                            StudentCourseStatus.InProgress => 1,
                            StudentCourseStatus.Failed => 2,
                            _ => 3
                        }).First();
                        return (bestAttempt.Status, bestAttempt.Grade);
                    });

            var nodes = courses.Select(c =>
            {
                var hasStudentRecord = studentCourseMap.TryGetValue(c.Id, out var record);
                var isPlanned = !hasStudentRecord && plannedCourseIds.Contains(c.Id);
                return new CourseNodeDto(
                    Id: c.Id,
                    Code: c.Code,
                    Name: c.Name,
                    CreditHours: c.CreditHours,
                    Status: hasStudentRecord ? record.Status : null,
                    Grade: hasStudentRecord ? record.Grade : null,
                    CourseType: c.CourseType,
                    IsPlanned: isPlanned,
                    RecommendedSemester: c.RecommendedSemester,
                    Type: c.CourseType switch
                    {
                        CourseType.DeptElective => "Dept. Elective",
                        CourseType.FreeElective => "Free Elective",
                        CourseType.UniversityReq => "University Req",
                        _ => c.CourseType.ToString()
                    },
                    Availability: c.SemesterAvailability switch
                    {
                        SemesterAvailability.FallSpring => "Fall / Spring",
                        _ => c.SemesterAvailability.ToString()
                    },
                    PassingGrade: c.PassingGradeThreshold,
                    DepartmentName: c.Department?.Name
                );
            }).ToList();

            var courseIdSet = courses.Select(c => c.Id).ToHashSet();

            var edges = courses
                .SelectMany(c => c.Prerequisites, (course, prereq) => new { course, prereq })
                .Where(x => courseIdSet.Contains(x.prereq.PrerequisiteId))
                .Select(x => new CourseEdgeDto(
                    SourceCourseId: x.prereq.PrerequisiteId,
                    TargetCourseId: x.course.Id,
                    SourceCode: x.prereq.Prerequisite!.Code,
                    TargetCode: x.course.Code
                ))
                .ToList();

            return new CourseGraphDto(
                Nodes: nodes,
                Edges: edges
            );
        }

        private async Task<HashSet<int>> GetPrimaryTermPlannedCourseIdsAsync(
            string studentId,
            int departmentId,
            IReadOnlyList<StudentCourse> studentCourses)
        {
            var department = await _db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == departmentId);

            var student = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);

            if (student is null)
                return [];

            var gradeScale = await _academicMetricsService.GetGradeScaleAsync(department?.UniversityId);
            var bestAttempts = _academicMetricsService.ResolveBestAttempts(studentCourses);
            var cgpa = _academicMetricsService.CalculateCgpa(bestAttempts, gradeScale);
            var creditLimit = _academicMetricsService.GetCreditLimits(student.CurrentStanding, cgpa);

            var terms = await _plannerService.GetPlanningTermsAsync(studentId, creditLimit);
            var primaryTerm = terms.FirstOrDefault(t => t.IsPrimary);
            if (primaryTerm is null)
                return [];

            var planned = await _plannerService.GetPlanAsync(
                studentId,
                primaryTerm.AcademicYear,
                primaryTerm.Semester);

            return planned.Select(pc => pc.CourseId).ToHashSet();
        }
    }
}



