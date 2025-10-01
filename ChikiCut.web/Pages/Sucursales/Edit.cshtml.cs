using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChikiCut.Web.Pages.Sucursales;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    public EditModel(AppDbContext db) => _db = db;

    [BindProperty]
    public Sucursal Sucursal { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var sucursal = await _db.Sucursals.FindAsync(id);
        if (sucursal == null)
        {
            return NotFound();
        }
        
        // Asignar la sucursal encontrada a la propiedad BindProperty
        Sucursal = sucursal;
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Remover validaciones de campos que se manejan automáticamente
        ModelState.Remove("Sucursal.CreatedAt");
        ModelState.Remove("Sucursal.UpdatedAt");

        // Asegurar que los campos requeridos por la base de datos tengan valores
        if (string.IsNullOrEmpty(Sucursal.Country))
            Sucursal.Country = "MX";
        if (string.IsNullOrEmpty(Sucursal.OpeningHours))
            Sucursal.OpeningHours = "{\"mon\":[],\"tue\":[],\"wed\":[],\"thu\":[],\"fri\":[],\"sat\":[],\"sun\":[]}";
        if (string.IsNullOrEmpty(Sucursal.AddressLine1))
            ModelState.AddModelError("Sucursal.AddressLine1", "La dirección es obligatoria.");

        if (!ModelState.IsValid)
        {
            // Depuración: Mostrar errores de validación
            foreach (var modelError in ModelState.Where(x => x.Value?.Errors?.Count > 0))
            {
                foreach (var error in modelError.Value!.Errors)
                {
                    Console.WriteLine($"Error en {modelError.Key}: {error.ErrorMessage}");
                }
            }
            return Page();
        }

        try
        {
            var entity = await _db.Sucursals.FindAsync(Sucursal.Id);
            if (entity == null) 
            {
                return NotFound();
            }

            // Mapear campos editables
            entity.Code = Sucursal.Code;
            entity.Name = Sucursal.Name;
            entity.RazonSocial = Sucursal.RazonSocial;
            entity.Rfc = Sucursal.Rfc;
            entity.Phone = Sucursal.Phone;
            entity.Email = Sucursal.Email;
            entity.AddressLine1 = Sucursal.AddressLine1;
            entity.AddressLine2 = Sucursal.AddressLine2;
            entity.City = Sucursal.City;
            entity.State = Sucursal.State;
            entity.PostalCode = Sucursal.PostalCode;
            entity.IsActive = Sucursal.IsActive;

            // Actualizar horarios
            entity.OpeningHours = Sucursal.OpeningHours;

            // Mantener valores que no se deben cambiar y asegurar campos requeridos
            if (string.IsNullOrEmpty(entity.Country))
                entity.Country = "MX";

            // Campo de sistema
            entity.UpdatedAt = DateTime.UtcNow;

            _db.Update(entity);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error al guardar los cambios: {ex.Message}");
            return Page();
        }
    }
}
