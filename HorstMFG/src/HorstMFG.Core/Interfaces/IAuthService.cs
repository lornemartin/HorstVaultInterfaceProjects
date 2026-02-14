using HorstMFG.Core.Entities;

namespace HorstMFG.Core.Interfaces;

public interface IAuthService
{
    Task<ApplicationUser?> ValidateCredentialsAsync(string userName, string password);
    Task<ApplicationUser?> GetUserByIdAsync(int userId);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
