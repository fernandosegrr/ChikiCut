using Microsoft.AspNetCore.Mvc;
using ChikiCut.web.Data.Entities;
using ChikiCut.web.Services;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChikiCut.web.Api
{
    [Route("api/clientes")]
    [ApiController]
    public class ClientesApiController : ControllerBase
    {
        private readonly IClienteService _clienteService;
        public ClientesApiController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 2)
                return Ok(new List<object>());
            var clientes = await _clienteService.GetAllAsync();
            var coincidencias = clientes
                .Where(c => c.NombreCompleto != null && c.NombreCompleto.Contains(nombre, System.StringComparison.OrdinalIgnoreCase))
                .Select(c => new { nombreCompleto = c.NombreCompleto, email = c.Email })
                .Take(10)
                .ToList();
            return Ok(coincidencias);
        }
    }
}
