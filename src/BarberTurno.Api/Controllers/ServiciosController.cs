using BarberTurno.Api.Data;
using BarberTurno.Api.Domain;
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

    [HttpPost]
    public async Task<ActionResult<ServicioDto>> CrearServicio([FromBody] CrearServicioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("El nombre del servicio es obligatorio.");

        var nuevo = new ServicioEntity
        {
            Nombre = dto.Nombre,
            DuracionMin = dto.DuracionMin > 0 ? dto.DuracionMin : 30,
            Precio = dto.Precio
        };

        _context.Servicios.Add(nuevo);
        await _context.SaveChangesAsync();

        return Ok(new ServicioDto
        {
            Id = nuevo.Id,
            Nombre = nuevo.Nombre,
            DuracionMin = nuevo.DuracionMin,
            Precio = nuevo.Precio
        });
    }
}
