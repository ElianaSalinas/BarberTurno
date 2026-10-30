using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Expone el catálogo de barberos y permite actualizar su perfil profesional.</summary>
public class BarberosController : ControllerBase
{
    private readonly ApiDbContext _context;

    /// <summary>Recibe el contexto EF Core que almacena barberos y sus servicios.</summary>
    public BarberosController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    /// <summary>Devuelve cada barbero con los servicios que tiene asociados.</summary>
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
    /// <summary>Guarda la URL de foto para el barbero indicado.</summary>
    public async Task<IActionResult> ActualizarFoto(int id, [FromBody] ActualizarFotoDto dto)
    {
        var barbero = await _context.Barberos.FindAsync(id);
        if (barbero == null) return NotFound();
        barbero.FotoUrl = dto.FotoUrl;
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}/servicios")]
    /// <summary>Reemplaza los servicios asociados al barbero indicado.</summary>
    public async Task<IActionResult> ActualizarServicios(int id, [FromBody] List<int> servicioIds)
    {
        var barbero = await _context.Barberos.Include(b => b.BarberoServicios).FirstOrDefaultAsync(b => b.Id == id);
        if (barbero == null) return NotFound();

        // La relación se reconstruye para que refleje exactamente la selección recibida.
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
