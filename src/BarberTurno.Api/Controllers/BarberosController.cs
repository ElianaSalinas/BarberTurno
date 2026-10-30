using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BarberosController : ControllerBase
{
    private readonly ApiDbContext _context;

    public BarberosController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<BarberoDto>>> GetBarberos()
    {
        var barberos = await _context.Barberos
            .Include(b => b.BarberoServicios)
            .Select(b => new BarberoDto
            {
                Id = b.Id,
                Nombre = b.Nombre,
                FotoUrl = b.FotoUrl,
                ServicioIds = b.BarberoServicios.Select(bs => bs.ServicioId).ToList()
            })
            .ToListAsync();

        return Ok(barberos);
    }
}
