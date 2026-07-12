using Cursus.DAL.Database;
using Cursus.Domain.Constants;
using Cursus.Domain.DTOs;
using Cursus.Domain.Entities;
using Cursus.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cursus.BLL.Services;

public sealed class UniversityAdminService : IUniversityAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UniversityAdminService(ApplicationDbContext context, UserManager<AppUser> userManager)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    public async Task<IReadOnlyList<UniversityAdminDto>> GetAdminsAsync(int? universityId = null)
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == Roles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId is null)
            return [];

        var query = _context.Users
            .Include(u => u.University)
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == adminRoleId))
            .AsNoTracking();

        if (universityId.HasValue)
            query = query.Where(u => u.UniversityId == universityId.Value);

        var users = await query
            .OrderBy(u => u.Email)
            .ToListAsync();

        return users
            .Select(u => new UniversityAdminDto(
                u.Id,
                u.Email ?? string.Empty,
                u.DisplayName,
                u.UniversityId ?? 0,
                u.University?.Name ?? string.Empty))
            .ToList();
    }

    public async Task<int> CountAsync()
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == Roles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId is null)
            return 0;

        return await _context.UserRoles.CountAsync(ur => ur.RoleId == adminRoleId);
    }

    public async Task<UniversityAdminCommandResult> CreateAdminAsync(
        CreateUniversityAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return UniversityAdminCommandResult.Failure("Email is required.", nameof(CreateUniversityAdminRequest.Email));

        if (request.UniversityId <= 0)
        {
            return UniversityAdminCommandResult.Failure(
                "Please select a university.",
                nameof(CreateUniversityAdminRequest.UniversityId));
        }

        var universityExists = await _context.Universities
            .AsNoTracking()
            .AnyAsync(u => u.Id == request.UniversityId, cancellationToken);

        if (!universityExists)
        {
            return UniversityAdminCommandResult.Failure(
                "Please select a valid university.",
                nameof(CreateUniversityAdminRequest.UniversityId));
        }

        var normalizedEmail = email.ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existing is not null)
        {
            return UniversityAdminCommandResult.Failure(
                "An account with this email address already exists.",
                nameof(CreateUniversityAdminRequest.Email));
        }

        var user = new AppUser
        {
            UserName = normalizedEmail,
            Email = email,
            EmailConfirmed = true,
            UniversityId = request.UniversityId
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return UniversityAdminCommandResult.Failures(createResult.Errors.Select(e => e.Description));

        try
        {
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Admin);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return UniversityAdminCommandResult.Failures(roleResult.Errors.Select(e => e.Description));
            }
        }
        catch (InvalidOperationException)
        {
            await _userManager.DeleteAsync(user);
            return UniversityAdminCommandResult.Failure(
                $"The role \u201c{Roles.Admin}\u201d is not configured.");
        }

        return UniversityAdminCommandResult.Success(user.DisplayName);
    }

    public async Task<UniversityAdminCommandResult> UpdateUniversityAsync(
        string userId,
        int universityId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return UniversityAdminCommandResult.Failure("Admin id is required.");

        if (universityId <= 0)
        {
            return UniversityAdminCommandResult.Failure(
                "Please select a university.",
                nameof(CreateUniversityAdminRequest.UniversityId));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return UniversityAdminCommandResult.Failure("Admin not found.");

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
            return UniversityAdminCommandResult.Failure("Only university admins can be reassigned.");

        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
            return UniversityAdminCommandResult.Failure("Cannot reassign a SuperAdmin account.");

        var universityExists = await _context.Universities
            .AsNoTracking()
            .AnyAsync(u => u.Id == universityId, cancellationToken);

        if (!universityExists)
        {
            return UniversityAdminCommandResult.Failure(
                "Please select a valid university.",
                nameof(CreateUniversityAdminRequest.UniversityId));
        }

        user.UniversityId = universityId;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return UniversityAdminCommandResult.Failures(updateResult.Errors.Select(e => e.Description));

        return UniversityAdminCommandResult.Success(user.DisplayName);
    }

    public async Task<UniversityAdminCommandResult> DeleteAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return UniversityAdminCommandResult.Failure("Admin id is required.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return UniversityAdminCommandResult.Failure("Admin not found.");

        if (!await _userManager.IsInRoleAsync(user, Roles.Admin))
            return UniversityAdminCommandResult.Failure("Only university admins can be deleted here.");

        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin))
            return UniversityAdminCommandResult.Failure("Cannot delete a SuperAdmin account.");

        var displayName = user.DisplayName;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return UniversityAdminCommandResult.Failures(result.Errors.Select(e => e.Description));

        return UniversityAdminCommandResult.Success(displayName);
    }
}
