namespace Cursus.PL.Models.Options;

public sealed class IdentitySeedOptions
{
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminUniversityName { get; set; } = "South Valley National University";

    /// <summary>Optional platform super-admin. Skipped when email or password is empty.</summary>
    public string SuperAdminEmail { get; set; } = string.Empty;
    public string SuperAdminPassword { get; set; } = string.Empty;
}

