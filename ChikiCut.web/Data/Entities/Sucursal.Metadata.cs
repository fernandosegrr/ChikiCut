using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChikiCut.web.Data.Entities;

// Esta clase no toca la entidad generada; solo agrega metadatos/validaciones
[ModelMetadataType(typeof(SucursalMetadata))]
public partial class Sucursal { }

public class SucursalMetadata
{
    [Display(Name = "Código")]
    [Required(ErrorMessage = "El {0} es obligatorio.")]
    [StringLength(32, ErrorMessage = "El {0} debe tener máximo {1} caracteres.")]
    public string Code { get; set; } = null!;

    [Display(Name = "Nombre comercial")]
    [Required(ErrorMessage = "El {0} es obligatorio.")]
    [StringLength(120)]
    public string Name { get; set; } = null!;

    [Display(Name = "Razón social")]
    [Required(ErrorMessage = "La {0} es obligatoria.")]
    [StringLength(160)]
    public string RazonSocial { get; set; } = null!;

    [Display(Name = "RFC")]
    [Required(ErrorMessage = "El {0} es obligatorio.")]
    [StringLength(13, MinimumLength = 12, ErrorMessage = "El {0} debe tener entre {2} y {1} caracteres.")]
    // Opcional: valida formato general (personas morales/físicas)
    //[RegularExpression(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{2,3}$", ErrorMessage = "El {0} no es válido.")]
    public string Rfc { get; set; } = null!;

    [Display(Name = "Teléfono")]
    [Required(ErrorMessage = "El {0} es obligatorio.")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Display(Name = "Correo electrónico")]
    [EmailAddress(ErrorMessage = "El {0} no es válido.")]
    [StringLength(120)]
    public string? Email { get; set; }

    [Display(Name = "Dirección (línea 1)")]
    [Required]
    [StringLength(160)]
    public string AddressLine1 { get; set; } = null!;

    [Display(Name = "Dirección (línea 2)")]
    [StringLength(160)]
    public string? AddressLine2 { get; set; }

    [Display(Name = "Ciudad")]
    [Required, StringLength(80)]
    public string City { get; set; } = null!;

    [Display(Name = "Estado")]
    [Required, StringLength(80)]
    public string State { get; set; } = null!;

    [Display(Name = "Código postal")]
    [Required, StringLength(12)]
    public string PostalCode { get; set; } = null!;

    [Display(Name = "País")]
    [StringLength(2)]
    public string Country { get; set; } = null!;

    [Display(Name = "Horarios")]
    public string OpeningHours { get; set; } = null!;

    [Display(Name = "Activo")]
    public bool IsActive { get; set; }

    [Display(Name = "Creado")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = false)]
    public DateTimeOffset CreatedAt { get; set; }

    [Display(Name = "Actualizado")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = false)]
    public DateTimeOffset? UpdatedAt { get; set; }
}
