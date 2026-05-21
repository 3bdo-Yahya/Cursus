namespace Cursus.PL.Models.Options;

public sealed class IdentitySeedOptions
{
    public string AdminEmail { get; set; } = "admin@cursus.com";
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminUniversityName { get; set; } = "South Valley National University";
}
