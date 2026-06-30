using Cursus.Domain.Enums;

namespace Cursus.Domain.DTOs;

public class TermGpaDto
{
    public string AcademicYear { get; set; } = string.Empty;
    public SemesterType Semester { get; set; }
    public string SemLabel { get; set; } = string.Empty;
    public decimal SemesterGpa { get; set; }
    public decimal CumulativeGpa { get; set; }
}
