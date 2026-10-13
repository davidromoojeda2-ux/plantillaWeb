using Aplication.Interfaces;
using Aplication.Services;
using Domine.Entities;

namespace Infrastructure.Auth;

/// <summary>
/// Implementación de IAuthService usando BCrypt para verificación de contraseñas.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IAdminUserRepository _userRepo;

    public AuthService(IAdminUserRepository userRepo) => _userRepo = userRepo;

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _userRepo.GetByUsernameAsync(username);
        if (user is null) return false;

        var valid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (valid) await _userRepo.UpdateLastLoginAsync(user.Id);
        return valid;
    }

    public Task<AdminUser?> GetUserAsync(string username) =>
        _userRepo.GetByUsernameAsync(username);
}
