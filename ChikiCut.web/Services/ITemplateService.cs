using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Services
{
    public interface ITemplateService
    {
        Task<List<RoleTemplate>> GetAvailableTemplatesAsync();
        Task<List<RoleTemplate>> GetFavoriteTemplatesAsync();
        Task<List<Rol>> GetAvailableRolesAsync();
        Task<RoleTemplate?> GetTemplateAsync(long id);
        Task<string?> GetRolePermissionsAsync(long roleId);
        Task<RoleTemplate> SaveAsTemplateAsync(string name, string description, string permissionsJson, long? createdBy, bool isFavorite = false);
        Task<bool> ToggleFavoriteAsync(long templateId);
        Task<bool> DeleteTemplateAsync(long templateId);
        Task<List<RoleTemplate>> SearchTemplatesAsync(string searchTerm);
        Task<bool> UpdateTemplateAsync(long id, string name, string description, bool isFavorite);
    }
}