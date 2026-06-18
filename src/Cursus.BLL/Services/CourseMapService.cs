using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
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

        public async Task<CourseGraphDto> GetCourseGraphForStudentAsync(int studentId, int departmentId)
        {
            var coursesQuery = _courseRepository.GetAll()
                .Where(c => c.DepartmentId == departmentId && c.IsActive)
                .Include(c => c.Prerequisites)
                .ThenInclude(p => p.Prerequisite);


            var courses = await Task.FromResult(coursesQuery.ToList());

            var studentCoursesQuery = _studentCourseRepository.GetAll()
                .Where(sc => sc.StudentId == studentId.ToString());

            var studentCourses = await Task.FromResult(studentCoursesQuery.ToList());

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

            var edges = courses
                .SelectMany(c => c.Prerequisites, (course, prereq) =>
                {
                    return new CourseEdgeDto(
                        SourceCourseId: prereq.PrerequisiteId,
                        TargetCourseId: course.Id,
                        SourceCode: prereq.Prerequisite!.Code,
                        TargetCode: course.Code
                    );
                })
                .ToList();

            return new CourseGraphDto(
                Nodes: nodes,
                Edges: edges
            );
        }
    }
}