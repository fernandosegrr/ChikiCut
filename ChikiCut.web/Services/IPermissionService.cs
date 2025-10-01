using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Services
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(long userId, string module, string action);
        Task<bool> HasRolePermissionAsync(long rolId, string module, string action);
        Dictionary<string, object> GetDefaultPermissions();
        Dictionary<string, object> GetRoleTemplate(string roleName);
        Task<Dictionary<string, object>> GetUserPermissionsAsync(long userId);
        bool ValidatePermissionsStructure(string permissionsJson);
        string GetPermissionsDisplayString(string permissionsJson);
        List<string> GetAvailableModules();
        List<string> GetAvailableActions();
    }
}