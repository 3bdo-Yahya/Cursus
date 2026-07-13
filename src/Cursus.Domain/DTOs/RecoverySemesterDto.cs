namespace Cursus.Domain.DTOs;

public sealed record RecoveryCourseDto(
    string Code,
    string Name,
    int CreditHours,
    bool IsRetake,
    bool IsNewlyUnlocked);

public sealed record RecoverySemesterDto(
    string Label,
    IEnumerable<RecoveryCourseDto> Courses,
    bool IsRetakeTerm = false);
