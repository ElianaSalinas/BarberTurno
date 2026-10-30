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

    [HttpPut("{id}/foto")]
    public async Task<IActionResult> ActualizarFoto(int id, [FromBody] ActualizarFotoDto dto)
    {
        var barbero = await _context.Barberos.FindAsync(id);
        if (barbero == null) return NotFound();
        barbero.FotoUrl = dto.FotoUrl;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}/servicios")]
    public async Task<IActionResult> ActualizarServicios(int id, [FromBody] List<int> servicioIds)
    {
        var barbero = await _context.Barberos.Include(b => b.BarberoServicios).FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return NotFound();

        _context.BarberoServicios.RemoveRange(barbero.BarberoServicios);
        foreach (var sid in servicioIds)
        {
            _context.BarberoServicios.Add(new BarberTurno.Api.Domain.BarberoServicioEntity
            {
                BarberoId = id,
                ServicioId = sid
            });
        }
        await _context.SaveChangesAsync();
        return Ok();
    }
}
