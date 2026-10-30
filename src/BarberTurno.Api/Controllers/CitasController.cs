using System.Security.Claims;
using BarberTurno.Api.Data;
using BarberTurno.Api.Domain;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitasController : ControllerBase
{
    private readonly ApiDbContext _context;

    public CitasController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<CitaDto>>> GetMisCitas()
    {
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var userIdStr = User.FindFirstValue("UsuarioId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdStr, out var usuarioId))
        {
            return Unauthorized();
        }

        IQueryable<CitaEntity> query = _context.Citas;

        if (rol.Equals("Barbero", StringComparison.OrdinalIgnoreCase))
        {
            var barbero = await _context.Barberos.FirstOrDefaultAsync(b => b.UsuarioId == usuarioId);
            if (barbero != null)
            {
                query = query.Where(c => c.BarberoId == barbero.Id);
            }
            else
            {
                return Ok(new List<CitaDto>());
            }
        }
        else
        {
            query = query.Where(c => c.ClienteId == usuarioId);
        }

        var citas = await query
            .OrderByDescending(c => c.InicioUtc)
            .Select(c => new CitaDto
            {
                Id = c.Id,
                ClienteId = c.ClienteId,
                BarberoId = c.BarberoId,
                ServicioId = c.ServicioId,
                InicioUtc = c.InicioUtc,
                FinUtc = c.FinUtc,
                Estado = c.Estado,
                MotivoRechazo = c.MotivoRechazo
            })
            .ToListAsync();

        return Ok(citas);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CitaDto>> CrearCita([FromBody] CrearCitaRequestDto request)
    {
        if (request == null || request.Id == Guid.Empty || request.BarberoId <= 0 || request.ServicioId <= 0)
        {
            return BadRequest(new { mensaje = "Datos de la reserva inválidos." });
        }

        var userIdStr = User.FindFirstValue("UsuarioId") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var clienteId))
        {
            return Unauthorized();
        }

        // Idempotencia: si ya existe una cita con ese Id, se devuelve 200 OK con la cita
        var citaExistente = await _context.Citas.FindAsync(request.Id);
        if (citaExistente != null)
        {
            return Ok(new CitaDto
            {
                Id = citaExistente.Id,
                ClienteId = citaExistente.ClienteId,
                BarberoId = citaExistente.BarberoId,
                ServicioId = citaExistente.ServicioId,
                InicioUtc = citaExistente.InicioUtc,
                FinUtc = citaExistente.FinUtc,
                Estado = citaExistente.Estado,
                MotivoRechazo = citaExistente.MotivoRechazo
            });
        }

        var servicio = await _context.Servicios.FindAsync(request.ServicioId);
        if (servicio == null)
        {
            return BadRequest(new { mensaje = "Servicio no encontrado." });
        }

        var duracionMin = servicio.DuracionMin > 0 ? servicio.DuracionMin : 30;
        var finUtc = request.InicioUtc.AddMinutes(duracionMin);

        // Transacción para garantizar consistencia y evitar solapes (ADR-0008)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Comprobar solape: existente.Inicio < nuevo.Fin && existente.Fin > nuevo.Inicio
            bool ocupado = await _context.Citas.AnyAsync(c =>
                c.BarberoId == request.BarberoId &&
                c.Estado != "Cancelada" &&
                c.Estado != "Rechazada" &&
                c.InicioUtc < finUtc &&
                c.FinUtc > request.InicioUtc);

            if (ocupado)
            {
                return StatusCode(StatusCodes.Status409Conflict, new { mensaje = "La hora seleccionada ya se encuentra ocupada por otra reserva." });
            }

            var nuevaCita = new CitaEntity
            {
                Id = request.Id,
                ClienteId = clienteId,
                BarberoId = request.BarberoId,
                ServicioId = request.ServicioId,
                InicioUtc = request.InicioUtc,
                FinUtc = finUtc,
                Estado = "Confirmada"
            };

            _context.Citas.Add(nuevaCita);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var dto = new CitaDto
            {
                Id = nuevaCita.Id,
                ClienteId = nuevaCita.ClienteId,
                BarberoId = nuevaCita.BarberoId,
                ServicioId = nuevaCita.ServicioId,
                InicioUtc = nuevaCita.InicioUtc,
                FinUtc = nuevaCita.FinUtc,
                Estado = nuevaCita.Estado
            };

            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { mensaje = "Error al procesar la reserva.", detalle = ex.Message });
        }
    }

    [HttpPost("{id}/cancelar")]
    [Authorize]
    public async Task<ActionResult<CitaDto>> CancelarCita(Guid id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
        {
            return NotFound(new { mensaje = "Cita no encontrada." });
        }

        cita.Estado = "Cancelada";
        await _context.SaveChangesAsync();

        return Ok(new CitaDto
        {
            Id = cita.Id,
            ClienteId = cita.ClienteId,
            BarberoId = cita.BarberoId,
            ServicioId = cita.ServicioId,
            InicioUtc = cita.InicioUtc,
            FinUtc = cita.FinUtc,
            Estado = cita.Estado,
            MotivoRechazo = cita.MotivoRechazo
        });
    }
}
