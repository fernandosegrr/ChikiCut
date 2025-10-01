using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChikiCut.web.Data.Entities
{
    [Table("role_template", Schema = "app")]
    public class RoleTemplate
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("permissions_json")]
        public string PermissionsJson { get; set; } = string.Empty;

        [Column("is_favorite")]
        public bool IsFavorite { get; set; }

        [Column("created_by")]
        public long? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Propiedades de navegación
        [ForeignKey("CreatedBy")]
        public virtual Usuario? Creator { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public int PermissionCount 
        { 
            get 
            {
                try
                {
                    if (string.IsNullOrEmpty(PermissionsJson)) return 0;
                    
                    var permissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(PermissionsJson);
                    if (permissions == null) return 0;
                    
                    int count = 0;
                    foreach (var module in permissions.Values)
                    {
                        foreach (var permission in module.Values)
                        {
                            if (permission) count++;
                        }
                    }
                    return count;
                }
                catch
                {
                    return 0;
                }
            }
        }

        [NotMapped]
        public string CreatedTimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                if (timeSpan.TotalDays >= 1)
                    return $"hace {(int)timeSpan.TotalDays} día(s)";
                if (timeSpan.TotalHours >= 1)
                    return $"hace {(int)timeSpan.TotalHours} hora(s)";
                return $"hace {(int)timeSpan.TotalMinutes} minuto(s)";
            }
        }
    }
}