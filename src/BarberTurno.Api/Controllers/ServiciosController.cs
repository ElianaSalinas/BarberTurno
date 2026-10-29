using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly ApiDbContext _context;

    public ServiciosController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServicioDto>>> GetServicios()
    {
        var servicios = await _context.Servicios
            .Select(s => new ServicioDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                DuracionMin = s.DuracionMin,
                Precio = s.Precio
            })
            .ToListAsync();

        return Ok(servicios);
    }
}
