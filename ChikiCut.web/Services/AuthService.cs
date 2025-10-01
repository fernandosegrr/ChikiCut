using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ChikiCut.web.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuthService> _logger;
        private const string SALT = "ChikiCut_Salt_2025";

        public AuthService(AppDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private DateTime ToUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            var user = await GetUserByEmailAsync(email);
            
            if (user == null || !user.PuedeAcceder)
                return false;

            var valid = await VerifyAndUpgradePasswordAsync(user, password);

            if (!valid)
            {
                // Incrementar intentos fallidos
                user.IntentosFallidos++;
                
                // Bloquear después de 3 intentos fallidos
                if (user.IntentosFallidos >= 3)
                {
                    user.BloqueadoHasta = ToUtc(DateTime.UtcNow.AddMinutes(15));
                }
                
                user.UpdatedAt = ToUtc(DateTime.UtcNow);
                await _context.SaveChangesAsync();
                _logger.LogWarning("Login fallido para {Email}. Intentos: {Attempts}", email, user.IntentosFallidos);
                return false;
            }

            // Reset intentos fallidos en login exitoso
            user.IntentosFallidos = 0;
            user.BloqueadoHasta = null;
            await UpdateLastAccessAsync(user.Id);

            _logger.LogInformation("Login exitoso para {Email}", email);
            return true;
        }

        private async Task<bool> VerifyAndUpgradePasswordAsync(Usuario user, string password)
        {
            var hash = user.PasswordHash ?? string.Empty;

            try
            {
                // Verificación directa para hash temporal
                if (hash == "ChikiCut2025!_hash")
                {
                    return password == "ChikiCut2025!";
                }

                if (string.IsNullOrEmpty(hash))
                    return false;

                // Si es BCrypt
                if (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }

                // Si llega aquí, asumimos hash legacy (SHA256)
                var legacyHash = HashPassword(password);
                if (legacyHash == hash)
                {
                    // Re-hash con BCrypt y almacenar
                    try
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                        user.UpdatedAt = ToUtc(DateTime.UtcNow);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Password legacy migrado a BCrypt para usuario {UserId}", user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al rehashear contraseña para usuario {UserId}", user.Id);
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando contraseña para usuario {UserId}", user.Id);
                return false;
            }
        }

        public async Task<Usuario?> GetUserByEmailAsync(string email)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> GetUserByIdAsync(long userId)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.Sucursal)
                .Include(u => u.Empleado)
                    .ThenInclude(e => e.PuestoNavegacion)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<bool> CreateUserAsync(Usuario usuario, string password)
        {
            // Usar BCrypt para nuevas contraseñas
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            usuario.CodigoUsuario = await GenerateUserCodeAsync();
            usuario.CreatedAt = DateTime.UtcNow;
            
            _context.Usuarios.Add(usuario);
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(long userId, string currentPassword, string newPassword)
        {
            var user = await _context.Usuarios.FindAsync(userId);
            
            if (user == null || !await VerifyAndUpgradePasswordAsync(user, currentPassword))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = ToUtc(DateTime.UtcNow);
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(long userId, string newPassword)
        {
            var user = await _context.Usuarios.FindAsync(userId);
            
            if (user == null)
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = ToUtc(DateTime.UtcNow);
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateLastAccessAsync(long userId)
        {
            var user = await _context.Usuarios.FindAsync(userId);
            if (user != null)
            {
                user.UltimoAcceso = ToUtc(DateTime.UtcNow);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> LockUserAsync(long userId, TimeSpan lockDuration)
        {
            var user = await _context.Usuarios.FindAsync(userId);
            if (user != null)
            {
                user.BloqueadoHasta = ToUtc(DateTime.UtcNow.Add(lockDuration));
                user.UpdatedAt = ToUtc(DateTime.UtcNow);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UnlockUserAsync(long userId)
        {
            var user = await _context.Usuarios.FindAsync(userId);
            if (user != null)
            {
                user.BloqueadoHasta = null;
                user.IntentosFallidos = 0;
                user.UpdatedAt = ToUtc(DateTime.UtcNow);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + SALT));
            return Convert.ToBase64String(hashedBytes);
        }

        public async Task<string> GenerateUserCodeAsync()
        {
            var maxCode = await _context.Usuarios
                .Where(u => u.CodigoUsuario.StartsWith("USR"))
                .Select(u => u.CodigoUsuario)
                .OrderByDescending(c => c)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (!string.IsNullOrEmpty(maxCode) && maxCode.Length >= 6)
            {
                var numberStr = maxCode.Substring(3);
                if (int.TryParse(numberStr, out var number))
                {
                    nextNumber = number + 1;
                }
            }
            return $"USR{nextNumber:D3}";
        }

        public async Task<bool> IsEmailAvailableAsync(string email, long? excludeUserId = null)
        {
            var query = _context.Usuarios.Where(u => u.Email == email);
            
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public bool VerifyPassword(string password, string hash)
        {
            try
            {
                if (hash == "ChikiCut2025!_hash")
                    return password == "ChikiCut2025!";

                if (string.IsNullOrEmpty(hash))
                    return false;

                if (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }

                var passwordHash = HashPassword(password);
                return passwordHash == hash;
            }
            catch
            {
                return false;
            }
        }
    }
}