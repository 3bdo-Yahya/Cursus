using Cursus.Domain.DTOs;

namespace Cursus.Domain.Interfaces.Services;

/// <summary>
/// Computes a full graduation audit for a student, breaking completion
/// down by the four <see cref="Cursus.Domain.Enums.CourseType"/> categories.
/// </summary>
public interface IProgressService
{
    /// <summary>
    /// Returns a <see cref="GraduationAuditDto"/> for the given student,
    /// or <c>null</c> when the student cannot be found or has no department assigned.
    /// </summary>
    Task<GraduationAuditDto?> GetGraduationAuditAsync(string studentId);
}
