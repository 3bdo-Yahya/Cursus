namespace Cursus.Domain.DTOs;

/// <summary>
/// Explicit success/failure for student create/delete (expected failures, not exceptions).
/// </summary>
public sealed class StudentCommandResult
{
    public bool IsSuccess { get; }
    public string? DisplayName { get; }
    public string? Field { get; }
    public IReadOnlyList<string> Errors { get; }

    private StudentCommandResult(
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

    public static StudentCommandResult Success(string displayName) =>
        new(true, displayName, null, []);

    public static StudentCommandResult Failure(string error, string? field = null) =>
        new(false, null, field, [error]);

    public static StudentCommandResult Failures(IEnumerable<string> errors) =>
        new(false, null, null, errors.ToList());
}
