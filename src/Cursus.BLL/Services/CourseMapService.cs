using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Cursus.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services
{
    public class CourseMapService : ICourseMapService
    {
        private readonly IGenericRepository<Course> _courseRepository;
        private readonly IGenericRepository<StudentCourse> _studentCourseRepository;

        public CourseMapService(
            IGenericRepository<Course> courseRepository,
            IGenericRepository<StudentCourse> studentCourseRepository)
        {
            _courseRepository = courseRepository;
            _studentCourseRepository = studentCourseRepository;
        }

        public async Task<CourseGraphDto> GetCourseGraphForStudentAsync(string? studentId, int departmentId)
        {
            // Load courses for the department or general university requirements that are active
            var courses = await _courseRepository.GetAll()
                .Where(c => (c.DepartmentId == departmentId || c.CourseType == CourseType.UniversityReq) && c.IsActive)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.Prerequisite)
                .AsNoTracking()
                .ToListAsync();

            // Load student's courses asynchronously
            var studentCourses = await _studentCourseRepository.GetAll()
                .Where(sc => sc.StudentId == studentId)
                .AsNoTracking()
                .ToListAsync();

            var studentCourseMap = studentCourses
                .ToDictionary(sc => sc.CourseId, sc => (sc.Status, sc.Grade));

            var nodes = courses.Select(c =>
            {
                var hasStudentRecord = studentCourseMap.TryGetValue(c.Id, out var record);
                return new CourseNodeDto(
                    Id: c.Id,
                    Code: c.Code,
                    Name: c.Name,
                    CreditHours: c.CreditHours,
                    Status: hasStudentRecord ? record.Status : null,
                    Grade: hasStudentRecord ? record.Grade : null
                );
            }).ToList();

            var courseIdSet = courses.Select(c => c.Id).ToHashSet();

            // Create edges only if both source and target course exist in the loaded nodes set
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
    }
}