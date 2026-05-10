using Cursus.BLL;

namespace Cursus.PL.Models;

public class StudentListIndexViewModel
{
    public IReadOnlyList<StudentListItemDto> Students { get; init; } = [];

    public string? SearchTerm { get; init; }

    public int? SelectedDepartmentId { get; init; }

    public int CurrentPage { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}
