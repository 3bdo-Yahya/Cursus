namespace Cursus.Domain;

/// <summary>
/// Resolved authorization scope for an administrator principal.
/// University admins must be linked to exactly one university.
/// </summary>
public sealed record AdminScope(bool IsSuperAdmin, bool IsUniversityAdmin, int? UniversityId)
{
    public bool CanManageUniversities => IsSuperAdmin;

    public bool CanManageCatalog => IsUniversityAdmin && UniversityId.HasValue;

    public int RequireUniversityId()
    {
        if (!UniversityId.HasValue)
            throw new InvalidOperationException("Administrator is not linked to a university.");

        return UniversityId.Value;
    }
}
