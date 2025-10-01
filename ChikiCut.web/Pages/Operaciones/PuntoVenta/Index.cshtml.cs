using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Linq;
using ChikiCut.web.Data;
using ChikiCut.web.Data.Entities;

namespace ChikiCut.web.Pages.Operaciones.PuntoVenta
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _db;
        public IndexModel(AppDbContext db)
        {
            _db = db;
        }

        public List<Cliente> Clientes { get; set; } = new();
        public List<(string Nombre, decimal Precio)> Productos { get; set; } = new()
        {
            ("Gel para cabello", 80), ("Shampoo infantil", 120), ("Peine de colores", 50), ("Spray desenredante", 90), ("Cera modeladora", 110),
            ("Diadema unicornio", 60), ("Moño fantasía", 45), ("Tijeras infantiles", 150), ("Set brochas", 130), ("Toalla estampada", 70),
            ("Cepillo anti-tirones", 95), ("Mascarilla capilar", 140), ("Brillantina", 55), ("Kit peinado", 160), ("Gorro de baño", 35)
        };
        public List<(string Nombre, decimal Precio)> Servicios { get; set; } = new()
        {
            ("Corte niño", 150), ("Corte niña", 170), ("Peinado especial", 200), ("Trenza francesa", 120), ("Trenza boxeadora", 130),
            ("Peinado con glitter", 180), ("Manicure infantil", 100), ("Pedicure infantil", 110), ("Mascarilla facial kids", 90), ("Maquillaje fantasía", 160)
        };
        public List<string> MetodosPago { get; set; } = new() { "Efectivo", "Tarjeta", "Transferencia" };
        public string SucursalActual { get; set; } = "Sucursal";
        public string ClienteActual { get; set; } = "Cliente de mostrador";
        public string UsuarioNombre { get; set; } = "";
        public List<Sucursal> SucursalesUsuario { get; set; } = new();
        public long? SucursalSeleccionadaId { get; set; }

        public void OnGet()
        {
            // Usuario autenticado
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                UsuarioNombre = User.FindFirstValue(ClaimTypes.Name) ?? "";
                if (string.IsNullOrEmpty(UsuarioNombre))
                    UsuarioNombre = HttpContext.Session.GetString("UserName") ?? "";
            }

            // Obtener el id de usuario desde claims o sesión
            long? usuarioId = null;
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(claimId) && long.TryParse(claimId, out var id))
                usuarioId = id;
            else if (HttpContext.Session.GetString("UserId") is string sid && long.TryParse(sid, out var sidLong))
                usuarioId = sidLong;

            // Obtener sucursales asignadas activas
            if (usuarioId.HasValue)
            {
                SucursalesUsuario = _db.UsuarioSucursales
                    .Where(us => us.UsuarioId == usuarioId && us.IsActive && us.Sucursal.IsActive)
                    .Select(us => us.Sucursal)
                    .OrderBy(s => s.Name)
                    .ToList();
            }

            // Sucursal seleccionada en sesión
            SucursalSeleccionadaId = HttpContext.Session.GetString("SucursalId") is string sucIdStr && long.TryParse(sucIdStr, out var sucId) ? sucId : (long?)null;
            if (SucursalSeleccionadaId.HasValue)
            {
                var suc = SucursalesUsuario.FirstOrDefault(s => s.Id == SucursalSeleccionadaId.Value);
                if (suc != null)
                    SucursalActual = suc.Name;
            }
            else if (SucursalesUsuario.Count == 1)
            {
                // Si solo tiene una sucursal, seleccionarla automáticamente
                var suc = SucursalesUsuario.First();
                SucursalSeleccionadaId = suc.Id;
                SucursalActual = suc.Name;
                HttpContext.Session.SetString("SucursalId", suc.Id.ToString());
            }
            // Si no hay sucursal seleccionada, SucursalActual queda como "Sucursal" y se debe mostrar el modal en la vista

            // Obtener clientes reales de la base de datos
            Clientes = _db.Clientes.Where(c => c.IsActive).OrderBy(c => c.NombreCompleto).ToList();
        }

        public IActionResult OnPostSetSucursal([FromBody] SetSucursalRequest req)
        {
            if (req == null || req.SucursalId <= 0)
                return BadRequest();
            // Validar que la sucursal pertenece al usuario
            long? usuarioId = null;
            var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(claimId) && long.TryParse(claimId, out var id))
                usuarioId = id;
            else if (HttpContext.Session.GetString("UserId") is string sid && long.TryParse(sid, out var sidLong))
                usuarioId = sidLong;
            if (!usuarioId.HasValue)
                return Unauthorized();
            var sucursal = _db.UsuarioSucursales.FirstOrDefault(us => us.UsuarioId == usuarioId && us.SucursalId == req.SucursalId && us.IsActive && us.Sucursal.IsActive);
            if (sucursal == null)
                return Forbid();
            HttpContext.Session.SetString("SucursalId", req.SucursalId.ToString());
            return new JsonResult(new { ok = true });
        }
        public class SetSucursalRequest { public long SucursalId { get; set; } }

        public IActionResult OnPostAgregarCliente([FromBody] NuevoClienteRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Nombre))
                return BadRequest();
            var existe = _db.Clientes.Any(c => c.NombreCompleto.ToLower() == req.Nombre.Trim().ToLower());
            if (existe)
                return new JsonResult(new { ok = false, error = "Ya existe un cliente con ese nombre" });
            var cliente = new Cliente
            {
                NombreCompleto = req.Nombre.Trim(),
                Telefono = string.IsNullOrWhiteSpace(req.Telefono) ? null : req.Telefono.Trim(),
                Email = string.IsNullOrWhiteSpace(req.Email) ? string.Empty : req.Email.Trim(),
                FechaNacimiento = string.IsNullOrWhiteSpace(req.FechaNacimiento) ? null : DateOnly.TryParse(req.FechaNacimiento, out var fn) ? fn : null,
                Genero = string.IsNullOrWhiteSpace(req.Genero) ? null : req.Genero.Trim(),
                Direccion = string.IsNullOrWhiteSpace(req.Direccion) ? null : req.Direccion.Trim(),
                Ciudad = string.IsNullOrWhiteSpace(req.Ciudad) ? null : req.Ciudad.Trim(),
                Estado = string.IsNullOrWhiteSpace(req.Estado) ? null : req.Estado.Trim(),
                CodigoPostal = string.IsNullOrWhiteSpace(req.CodigoPostal) ? null : req.CodigoPostal.Trim(),
                Pais = string.IsNullOrWhiteSpace(req.Pais) ? "México" : req.Pais.Trim(),
                Notas = string.IsNullOrWhiteSpace(req.Notas) ? null : req.Notas.Trim(),
                IsActive = true
            };
            _db.Clientes.Add(cliente);
            _db.SaveChanges();
            return new JsonResult(new { ok = true, id = cliente.Id });
        }
        public class NuevoClienteRequest
        {
            public string Nombre { get; set; } = "";
            public string? Telefono { get; set; }
            public string? Email { get; set; }
            public string? FechaNacimiento { get; set; }
            public string? Genero { get; set; }
            public string? Direccion { get; set; }
            public string? Ciudad { get; set; }
            public string? Estado { get; set; }
            public string? CodigoPostal { get; set; }
            public string? Pais { get; set; }
            public string? Notas { get; set; }
        }
    }
}
