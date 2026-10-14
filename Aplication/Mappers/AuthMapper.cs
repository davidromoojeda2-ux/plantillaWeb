using Aplication.DTOs;
using Domine.Entities;

namespace Aplication.Mappers;

public static class AuthMapper
{
    public static AdminUserDto ToDto(AdminUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        DisplayName = user.DisplayName,
        IsActive = user.IsActive
    };
}
