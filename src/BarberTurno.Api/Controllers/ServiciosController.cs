using BarberTurno.Api.Data;
using BarberTurno.Api.Domain;
using BarberTurno.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberTurno.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>Expone y administra el catálogo de servicios consumido por la aplicación.</summary>
public class ServiciosController : ControllerBase
{
    private readonly ApiDbContext _context;

    /// <summary>Recibe el contexto EF Core que persiste el catálogo.</summary>
    public ServiciosController(ApiDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    /// <summary>Devuelve los servicios disponibles en formato de transferencia.</summary>
    public async Task<ActionResult<List<ServicioDto>>> GetServicios()
    {
        var servicios = await _context.Servicios
            .Select(s => new ServicioDto
            {
                Id = s.Id,
                Nombre = s.Nombre,
                DuracionMin = s.DuracionMin,
                Precio = s.Precio,
                FotoUrl = s.FotoUrl
            })
            .ToListAsync();

        return Ok(servicios);
    }

    [HttpPost]
    /// <summary>Crea un servicio y devuelve el catálogo normalizado para la aplicación.</summary>
    public async Task<ActionResult<ServicioDto>> CrearServicio([FromBody] CrearServicioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre del servicio es obligatorio.");

        var nuevo = new ServicioEntity
        {
            Nombre = dto.Nombre,
            DuracionMin = dto.DuracionMin > 0 ? dto.DuracionMin : 30,
            Precio = dto.Precio,
            FotoUrl = dto.FotoUrl
        };

        _context.Servicios.Add(nuevo);
        await _context.SaveChangesAsync();

        return Ok(new ServicioDto
        {
            Id = nuevo.Id,
            Nombre = nuevo.Nombre,
            DuracionMin = nuevo.DuracionMin,
            Precio = nuevo.Precio,
            FotoUrl = nuevo.FotoUrl
        });
    }
}
