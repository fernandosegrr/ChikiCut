using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Services
{
    public interface IAuthService
    {
        Task<bool> ValidateUserAsync(string email, string password);
        Task<Usuario?> GetUserByEmailAsync(string email);
        Task<Usuario?> GetUserByIdAsync(long userId);
        Task<bool> CreateUserAsync(Usuario usuario, string password);
        Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword);
        Task UpdateLastAccessAsync(long userId);
        Task<bool> LockUserAsync(long userId, TimeSpan lockDuration);
        Task<bool> UnlockUserAsync(long userId);
        Task<bool> ResetPasswordAsync(long userId, string newPassword);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
        Task<string> GenerateUserCodeAsync();
        Task<bool> IsEmailAvailableAsync(string email, long? excludeUserId = null);
    }
}