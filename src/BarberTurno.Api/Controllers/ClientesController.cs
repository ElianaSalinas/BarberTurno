using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly ApiDbContext _context;

    public ClientesController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
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
