using BarberTurno.Api.Data;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisponibilidadController : ControllerBase
{
    private readonly ApiDbContext _context;

    public DisponibilidadController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<SlotDto>>> GetDisponibilidad(
        [FromQuery] int barberoId,
        [FromQuery] int servicioId,
        [FromQuery] string fecha)
    {
        if (!DateTime.TryParse(fecha, out var fechaParsed))
        {
            return BadRequest(new { mensaje = "Formato de fecha inválido. Utilice formato AAAA-MM-DD." });
        }

        var servicio = await _context.Servicios.FindAsync(servicioId);
        if (servicio == null)
        {
            return NotFound(new { mensaje = "Servicio no encontrado." });
        }

        var barbero = await _context.Barberos.FindAsync(barberoId);
        if (barbero == null)
        {
            return NotFound(new { mensaje = "Barbero no encontrado." });
        }

        var diaInicio = new DateTime(fechaParsed.Year, fechaParsed.Month, fechaParsed.Day, 9, 0, 0, DateTimeKind.Utc);
        var diaFin = new DateTime(fechaParsed.Year, fechaParsed.Month, fechaParsed.Day, 18, 0, 0, DateTimeKind.Utc);

        // Citas activas de ese barbero en ese día
        var citasDelDia = await _context.Citas
            .Where(c => c.BarberoId == barberoId
                        && c.Estado != "Cancelada"
                        && c.Estado != "Rechazada"
                        && c.InicioUtc < diaFin
                        && c.FinUtc > diaInicio)
            .ToListAsync();

        var slotsLibres = new List<SlotDto>();
        var duracion = TimeSpan.FromMinutes(servicio.DuracionMin > 0 ? servicio.DuracionMin : 30);
        var paso = TimeSpan.FromMinutes(30);

        var actual = diaInicio;
        while (actual + duracion <= diaFin)
        {
            var finActual = actual + duracion;

            // Se descarta si se solapa con una cita existente:
            // existente.Inicio < nuevo.Fin && existente.Fin > nuevo.Inicio
            bool solapado = citasDelDia.Any(c => c.InicioUtc < finActual && c.FinUtc > actual);

            if (!solapado)
            {
                slotsLibres.Add(new SlotDto
                {
                    InicioUtc = actual,
                    FinUtc = finActual
                });
            }

            actual = actual.Add(paso);
        }

        return Ok(slotsLibres);
    }
}
