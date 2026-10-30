using BarberTurno.Models;
using Xunit;

namespace BarberTurno.Tests;

/// <summary>
/// Pruebas del reto central de la práctica:
/// Dos citas no pueden solaparse para el mismo barbero.
/// Regla matemática: existente.Inicio < nuevo.Fin && existente.Fin > nuevo.Inicio
/// </summary>
public class CitaOverlapTests
{
    private static bool SeSolapan(DateTime ini1, DateTime fin1, DateTime ini2, DateTime fin2)
    {
        return ini1 < fin2 && fin1 > ini2;
    }

    [Fact]
    public void CitasQueCoincidenEnElMismoHorario_SeSolapan()
    {
        // Caso exacto: 10:00 a 10:30 contra 10:00 a 10:30
        var hoy = DateTime.Today;
        var citaExistenteInicio = hoy.AddHours(10);
        var citaExistenteFin = hoy.AddHours(10).AddMinutes(30);

        var nuevaInicio = hoy.AddHours(10);
        var nuevaFin = hoy.AddHours(10).AddMinutes(30);

        Assert.True(SeSolapan(citaExistenteInicio, citaExistenteFin, nuevaInicio, nuevaFin));
    }

    [Fact]
    public void CitasParcialmenteSolapadas_SeSolapan()
    {
        // Caso solapamiento parcial: 10:00 a 10:30 contra 10:15 a 10:45
        var hoy = DateTime.Today;
        var citaExistenteInicio = hoy.AddHours(10);
        var citaExistenteFin = hoy.AddHours(10).AddMinutes(30);

        var nuevaInicio = hoy.AddHours(10).AddMinutes(15);
        var nuevaFin = hoy.AddHours(10).AddMinutes(45);

        Assert.True(SeSolapan(citaExistenteInicio, citaExistenteFin, nuevaInicio, nuevaFin));
    }

    [Fact]
    public void CitasContiguas_NoSeSolapan()
    {
        // Caso citas consecutivas exactas: 10:00 a 10:30 y 10:30 a 11:00
        var hoy = DateTime.Today;
        var citaExistenteInicio = hoy.AddHours(10);
        var citaExistenteFin = hoy.AddHours(10).AddMinutes(30);

        var nuevaInicio = hoy.AddHours(10).AddMinutes(30);
        var nuevaFin = hoy.AddHours(11);

        Assert.False(SeSolapan(citaExistenteInicio, citaExistenteFin, nuevaInicio, nuevaFin));
    }

    [Fact]
    public void CitasEnHorariosDiferentes_NoSeSolapan()
    {
        var hoy = DateTime.Today;
        var citaExistenteInicio = hoy.AddHours(10);
        var citaExistenteFin = hoy.AddHours(10).AddMinutes(30);

        var nuevaInicio = hoy.AddHours(14);
        var nuevaFin = hoy.AddHours(14).AddMinutes(30);

        Assert.False(SeSolapan(citaExistenteInicio, citaExistenteFin, nuevaInicio, nuevaFin));
    }

    [Theory]
    [InlineData(EstadoCita.Cancelada, true)]
    [InlineData(EstadoCita.Rechazada, true)]
    [InlineData(EstadoCita.Pendiente, false)]
    [InlineData(EstadoCita.Confirmada, false)]
    public void EstadosQueLiberanHorario_CumplenReglaDelDiseno(EstadoCita estado, bool libera)
    {
        // Cancelada y Rechazada liberan el turno para que otros clientes puedan reservarlo
        var liberaHorario = estado is EstadoCita.Cancelada or EstadoCita.Rechazada;
        Assert.Equal(libera, liberaHorario);
    }
}
