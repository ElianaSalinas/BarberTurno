using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Entrega a los barberos el directorio de clientes para sus pantallas de atención.</summary>
public class ClientesController : ControllerBase
{
    private readonly ApiDbContext _context;

    /// <summary>Recibe el contexto que contiene los usuarios cliente.</summary>
    public ClientesController(ApiDbContext context)
    {
        _context = context;
    }

    // Solo los barberos pueden consultar el directorio de clientes para ver su historial y citas
    [Authorize(Roles = "Barbero")]
    [HttpGet]
    /// <summary>Devuelve los datos básicos de todos los usuarios con rol Cliente.</summary>
    public async Task<ActionResult<List<ClienteDto>>> GetClientes()
    {
        var clientes = await _context.Usuarios
            .Where(u => u.Rol == "Cliente")
            .Select(u => new ClienteDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Telefono = u.Telefono
            })
            .ToListAsync();

        return Ok(clientes);
    }
}
