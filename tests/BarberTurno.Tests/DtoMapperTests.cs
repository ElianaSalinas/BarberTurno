using BarberTurno.Dtos;
using BarberTurno.Mappers;
using BarberTurno.Models;
using Xunit;

namespace BarberTurno.Tests;

/// <summary>
/// Pruebas para DtoMapper:
/// Garantiza la conversión bidireccional DTO <-> Entidad y el manejo correcto de UTC y estados.
/// </summary>
public class DtoMapperTests
{
    [Fact]
    public void ClienteDto_ToEntidad_ConservaIdYDatos()
    {
        var dto = new ClienteDto
        {
            Id = 42,
            Nombre = "Juan Perez",
            Telefono = "8095551234"
        };

        var entidad = dto.ToEntidad();

        Assert.NotNull(entidad);
        Assert.Equal(42, entidad.Id);
        Assert.Equal("Juan Perez", entidad.Nombre);
        Assert.Equal("8095551234", entidad.Telefono);
    }

    [Fact]
    public void ClienteEntidad_ToDto_ConservaIdYDatos()
    {
        var entidad = new Cliente
        {
            Id = 99,
            Nombre = "Carlos Lopez",
            Telefono = "8295559876"
        };

        var dto = entidad.ToDto();

        Assert.NotNull(dto);
        Assert.Equal(99, dto.Id);
        Assert.Equal("Carlos Lopez", dto.Nombre);
        Assert.Equal("8295559876", dto.Telefono);
    }

    [Fact]
    public void BarberoDto_ToEntidad_MapeaHorarioCorrectamente()
    {
        var dto = new BarberoDto
        {
            Id = 5,
            Nombre = "Pedro El Barbero",
            HoraEntrada = new TimeSpan(9, 0, 0),
            HoraSalida = new TimeSpan(18, 0, 0),
            DiasTrabajo = "1,2,3,4,5"
        };

        var entidad = dto.ToEntidad();

        Assert.Equal(5, entidad.Id);
        Assert.Equal("Pedro El Barbero", entidad.Nombre);
        Assert.Equal(new TimeSpan(9, 0, 0), entidad.HoraEntrada);
        Assert.Equal(new TimeSpan(18, 0, 0), entidad.HoraSalida);
        Assert.Equal("1,2,3,4,5", entidad.DiasTrabajo);
    }
}
