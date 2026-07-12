using Cursus.Domain;
using Cursus.Domain.Constants;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace Cursus.BLL.Services;

public sealed class AdminScopeService : IAdminScopeService
{
    private readonly UserManager<AppUser> _userManager;

    public AdminScopeService(UserManager<AppUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<AdminScope> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new AdminScope(IsSuperAdmin: false, IsUniversityAdmin: false, UniversityId: null);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return new AdminScope(IsSuperAdmin: false, IsUniversityAdmin: false, UniversityId: null);

        var roles = await _userManager.GetRolesAsync(user);
        var isSuperAdmin = roles.Contains(Roles.SuperAdmin);
        var isUniversityAdmin = roles.Contains(Roles.Admin);

        return new AdminScope(isSuperAdmin, isUniversityAdmin, user.UniversityId);
    }
}
