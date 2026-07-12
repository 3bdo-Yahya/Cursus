namespace Cursus.Domain.DTOs;

public sealed class CreateUniversityAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int UniversityId { get; set; }
}

public sealed record UniversityAdminDto(
    string Id,
    string Email,
    string DisplayName,
    int UniversityId,
    string UniversityName);

public sealed class UniversityAdminCommandResult
{
    public bool IsSuccess { get; }
    public string? DisplayName { get; }
    public string? Field { get; }
    public IReadOnlyList<string> Errors { get; }

    private UniversityAdminCommandResult(
        bool isSuccess,
        string? displayName,
        string? field,
        IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        DisplayName = displayName;
        Field = field;
        Errors = errors;
    }

    public static UniversityAdminCommandResult Success(string displayName) =>
        new(true, displayName, null, []);

    public static UniversityAdminCommandResult Failure(string error, string? field = null) =>
        new(false, null, field, [error]);

    public static UniversityAdminCommandResult Failures(IEnumerable<string> errors) =>
        new(false, null, null, errors.ToList());
}
