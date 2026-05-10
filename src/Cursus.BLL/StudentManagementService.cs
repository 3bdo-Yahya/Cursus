using Cursus.DAL.Database;
using Cursus.Domain.Entities;
using Cursus.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL;

public class StudentManagementService : IStudentManagementService
{
    private readonly ApplicationDbContext _context;
    private const string StudentRoleName = "Student";

    public StudentManagementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StudentListItemDto>> GetAllStudentsAsync(int? departmentId)
    {
        var studentRoleId = await GetStudentRoleIdAsync();
        if (studentRoleId is null)
        {
            return [];
        }

        var studentsQuery = GetStudentQuery(studentRoleId);

        if (departmentId.HasValue && departmentId > 0)
        {
            studentsQuery = studentsQuery.Where(user => user.DepartmentId == departmentId);
        }

        var students = await studentsQuery
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Email)
            .ToListAsync();

        return students
            .Select(user => new StudentListItemDto
            {
                StudentId = user.Id,
                FullName = BuildDisplayName(user),
                Email = user.Email ?? string.Empty,
                DepartmentId = user.DepartmentId,
                DepartmentName = user.Department != null ? user.Department.Name : null,
                AcademicYear = user.AcademicYear,
                CurrentSemester = user.CurrentSemester,
                CurrentStanding = user.CurrentStanding
            })
            .ToList();
    }

    public async Task<StudentDetailDto?> GetStudentDetailAsync(string studentId)
    {
        var studentRoleId = await GetStudentRoleIdAsync();
        if (studentRoleId is null)
        {
            return null;
        }

        var student = await GetStudentQuery(studentRoleId)
            .Include(user => user.StudentCourses)
                .ThenInclude(studentCourse => studentCourse.Course)
            .FirstOrDefaultAsync(user => user.Id == studentId);

        if (student is null)
        {
            return null;
        }

        var courseRecords = student.StudentCourses
            .OrderByDescending(courseRecord => courseRecord.AcademicYear)
            .ThenBy(courseRecord => courseRecord.Semester)
            .ThenBy(courseRecord => courseRecord.Course != null ? courseRecord.Course.Code : string.Empty)
            .Select(courseRecord => new StudentCourseRecordDto
            {
                RecordId = courseRecord.Id,
                CourseId = courseRecord.CourseId,
                CourseCode = courseRecord.Course?.Code ?? string.Empty,
                CourseName = courseRecord.Course?.Name ?? string.Empty,
                CreditHours = courseRecord.Course?.CreditHours ?? 0,
                Grade = courseRecord.Grade,
                Status = courseRecord.Status,
                Semester = courseRecord.Semester,
                AcademicYear = courseRecord.AcademicYear
            })
            .ToList();

        return new StudentDetailDto
        {
            StudentId = student.Id,
            FullName = BuildDisplayName(student),
            Email = student.Email ?? string.Empty,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.Name,
            AcademicYear = student.AcademicYear,
            CurrentSemester = student.CurrentSemester,
            CurrentStanding = student.CurrentStanding,
            CourseRecords = courseRecords
        };
    }

    public async Task<StudentCourseMutationResult> AddCourseRecordAsync(string studentId, int courseId, string? grade, StudentCourseStatus status, SemesterType semester, string academicYear)
    {
        var normalizedAcademicYear = academicYear.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAcademicYear))
        {
            return Failure(StudentCourseMutationError.InvalidAcademicYear, "Academic year is required.");
        }

        var studentRoleId = await GetStudentRoleIdAsync();
        if (studentRoleId is null)
        {
            return Failure(StudentCourseMutationError.StudentNotFound, "Student not found.");
        }

        var student = await GetStudentQuery(studentRoleId).FirstOrDefaultAsync(user => user.Id == studentId);
        if (student is null)
        {
            return Failure(StudentCourseMutationError.StudentNotFound, "Student not found.");
        }

        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(courseItem => courseItem.Id == courseId);
        if (course is null)
        {
            return Failure(StudentCourseMutationError.CourseNotFound, "Course not found.");
        }

        if (student.DepartmentId is null || course.DepartmentId != student.DepartmentId.Value)
        {
            return Failure(StudentCourseMutationError.CourseNotInStudentDepartment, "Course must belong to the student's department.");
        }

        var normalizedGrade = GradeScaleCatalog.Normalize(grade);
        if (!string.IsNullOrWhiteSpace(grade) && normalizedGrade is null)
        {
            return Failure(StudentCourseMutationError.InvalidGrade, "Invalid grade selected.");
        }

        var duplicateExists = await _context.StudentCourses.AnyAsync(studentCourse =>
            studentCourse.StudentId == studentId &&
            studentCourse.CourseId == courseId &&
            studentCourse.Semester == semester &&
            studentCourse.AcademicYear == normalizedAcademicYear);

        if (duplicateExists)
        {
            return Failure(StudentCourseMutationError.DuplicateRecord, "This course record already exists for the selected semester and academic year.");
        }

        var resolvedStatus = normalizedGrade is null
            ? StudentCourseStatus.InProgress
            : GradeScaleCatalog.DetermineStatus(normalizedGrade, course.PassingGradeThreshold);

        var newRecord = new StudentCourse
        {
            StudentId = studentId,
            CourseId = courseId,
            Grade = normalizedGrade,
            Status = resolvedStatus,
            Semester = semester,
            AcademicYear = normalizedAcademicYear
        };

        _context.StudentCourses.Add(newRecord);

        try
        {
            await _context.SaveChangesAsync();
            return new StudentCourseMutationResult
            {
                Succeeded = true,
                StudentId = studentId,
                RecordId = newRecord.Id,
                Message = "Course record added successfully."
            };
        }
        catch (DbUpdateException)
        {
            return Failure(StudentCourseMutationError.DuplicateRecord, "This course record already exists for the selected semester and academic year.", studentId);
        }
    }

    public async Task<StudentCourseMutationResult> UpdateCourseRecordAsync(int recordId, string? grade, StudentCourseStatus status)
    {
        var record = await _context.StudentCourses.FirstOrDefaultAsync(studentCourse => studentCourse.Id == recordId);

        if (record is null)
        {
            return Failure(StudentCourseMutationError.RecordNotFound, "Course record not found.");
        }

        var normalizedGrade = GradeScaleCatalog.Normalize(grade);
        if (!string.IsNullOrWhiteSpace(grade) && normalizedGrade is null)
        {
            return Failure(StudentCourseMutationError.InvalidGrade, "Invalid grade selected.", record.StudentId, record.Id);
        }

        record.Grade = normalizedGrade;
        record.Status = status;

        try
        {
            await _context.SaveChangesAsync();
            return new StudentCourseMutationResult
            {
                Succeeded = true,
                StudentId = record.StudentId,
                RecordId = record.Id,
                Message = "Course record updated successfully."
            };
        }
        catch (DbUpdateException)
        {
            return Failure(StudentCourseMutationError.RecordNotFound, "Unable to update the course record.", record.StudentId, record.Id);
        }
    }

    public async Task<StudentCourseMutationResult> DeleteCourseRecordAsync(int recordId)
    {
        var record = await _context.StudentCourses.FirstOrDefaultAsync(studentCourse => studentCourse.Id == recordId);

        if (record is null)
        {
            return Failure(StudentCourseMutationError.RecordNotFound, "Course record not found.");
        }

        var studentId = record.StudentId;
        _context.StudentCourses.Remove(record);

        await _context.SaveChangesAsync();

        return new StudentCourseMutationResult
        {
            Succeeded = true,
            StudentId = studentId,
            RecordId = recordId,
            Message = "Course record deleted successfully."
        };
    }

    private IQueryable<AppUser> GetStudentQuery(string studentRoleId)
    {
        return _context.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .Where(user => _context.UserRoles.Any(userRole =>
                userRole.UserId == user.Id &&
                userRole.RoleId == studentRoleId));
    }

    private async Task<string?> GetStudentRoleIdAsync()
    {
        return await _context.Roles
            .Where(role => role.Name == StudentRoleName)
            .Select(role => role.Id)
            .FirstOrDefaultAsync();
    }

    private static string BuildDisplayName(AppUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.UserName) && user.UserName.Contains('@'))
        {
            return user.UserName.Split('@')[0];
        }

        return user.UserName ?? user.Email ?? user.Id;
    }

    private static StudentCourseMutationResult Failure(StudentCourseMutationError error, string message, string? studentId = null, int? recordId = null)
    {
        return new StudentCourseMutationResult
        {
            Succeeded = false,
            Error = error,
            Message = message,
            StudentId = studentId,
            RecordId = recordId
        };
    }
}
