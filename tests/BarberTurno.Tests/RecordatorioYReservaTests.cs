using BarberTurno.Data.Repositories;
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Booking;
using BarberTurno.Services.Notifications;
using Moq;
using Xunit;

namespace BarberTurno.Tests;

/// <summary>Recordatorio local de la cita y reglas de la reserva sin conexión.</summary>
public class RecordatorioYReservaTests
{
    // ── ReminderService: cuándo suena el aviso ───────────────────────────────

    [Fact]
    public void CalcularMomentoAviso_CitaEnDosHoras_AvisaUnaHoraAntes()
    {
        var ahora = new DateTime(2026, 10, 30, 12, 0, 0);
        var inicio = ahora.AddHours(2);

        var aviso = ReminderService.CalcularMomentoAviso(inicio, ahora);

        Assert.Equal(inicio - ReminderService.Anticipacion, aviso);
    }

    [Fact]
    public void CalcularMomentoAviso_CitaEnMediaHora_AvisaEnSegundos()
    {
        var ahora = new DateTime(2026, 10, 30, 12, 0, 0);

        var aviso = ReminderService.CalcularMomentoAviso(ahora.AddMinutes(30), ahora);

        Assert.Equal(ahora.AddSeconds(10), aviso);
    }

    [Fact]
    public void CalcularMomentoAviso_CitaYaEmpezada_NoAvisa()
    {
        var ahora = new DateTime(2026, 10, 30, 12, 0, 0);

        Assert.Null(ReminderService.CalcularMomentoAviso(ahora.AddMinutes(-5), ahora));
    }

    [Fact]
    public void IdNotificacion_EsEstableYNoNegativo_ParaElMismoGuid()
    {
        var id = Guid.NewGuid();

        var a = ReminderService.IdNotificacion(id);
        var b = ReminderService.IdNotificacion(id);

        Assert.Equal(a, b);
        Assert.True(a >= 0);
    }

    // ── BookingService: reserva sin conexión y recordatorios ─────────────────

    private static BookingService CrearServicio(
        Mock<IBarberTurnoApi> api, Mock<ICitaRepository> citas, Mock<IReminderService> recordatorios)
    {
        var servicios = new Mock<IServicioRepository>();
        servicios.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Servicio { Id = 5, Nombre = "Corte y barba", DuracionMin = 45, Precio = 500 });

        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.SesionActual).Returns(new SesionUsuario { UsuarioId = 12, Nombre = "Carlos", Rol = RolUsuario.Cliente });

        return new BookingService(api.Object, citas.Object, servicios.Object, auth.Object, recordatorios.Object);
    }

    [Fact]
    public async Task ReservarAsync_SinConexion_GuardaPendienteConClienteDeLaSesionYDuracionRealYProgramaAviso()
    {
        var api = new Mock<IBarberTurnoApi>();
        api.Setup(a => a.CrearCitaAsync(It.IsAny<CrearCitaRequestDto>()))
           .ReturnsAsync(new ApiResult<CitaDto> { Exito = false, SinConexion = true });
        var citas = new Mock<ICitaRepository>();
        var recordatorios = new Mock<IReminderService>();
        var servicio = CrearServicio(api, citas, recordatorios);

        var inicio = DateTime.Now.AddDays(1).Date.AddHours(15);
        var resultado = await servicio.ReservarAsync(new ReservaSolicitud { BarberoId = 3, ServicioId = 5, Inicio = inicio });

        Assert.Equal(ResultadoReservaTipo.PendienteSinConexion, resultado.Tipo);
        var cita = Assert.IsType<Cita>(resultado.Cita);
        Assert.Equal(12, cita.ClienteId);                      // antes quedaba en 0
        Assert.Equal(inicio.AddMinutes(45), cita.Fin);         // antes se fijaba en 30 min
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
        Assert.True(cita.PendienteSincronizar);
        recordatorios.Verify(r => r.ProgramarAsync(It.Is<Cita>(c => c.Id == cita.Id)), Times.Once);
    }

    [Fact]
    public async Task ReservarAsync_HoraOcupada409_NoProgramaRecordatorio()
    {
        var api = new Mock<IBarberTurnoApi>();
        api.Setup(a => a.CrearCitaAsync(It.IsAny<CrearCitaRequestDto>()))
           .ReturnsAsync(new ApiResult<CitaDto> { Exito = false, CodigoHttp = 409, Error = "ocupada" });
        var citas = new Mock<ICitaRepository>();
        var recordatorios = new Mock<IReminderService>();
        var servicio = CrearServicio(api, citas, recordatorios);

        var resultado = await servicio.ReservarAsync(new ReservaSolicitud
        {
            BarberoId = 3, ServicioId = 5, Inicio = DateTime.Now.AddDays(1)
        });

        Assert.Equal(ResultadoReservaTipo.HoraOcupada, resultado.Tipo);
        recordatorios.Verify(r => r.ProgramarAsync(It.IsAny<Cita>()), Times.Never);
    }

    [Fact]
    public async Task CancelarAsync_CancelaTambienElRecordatorio()
    {
        var citaId = Guid.NewGuid();
        var api = new Mock<IBarberTurnoApi>();
        api.Setup(a => a.CancelarCitaAsync(citaId)).ReturnsAsync(new ApiResult<CitaDto> { Exito = true });
        var citas = new Mock<ICitaRepository>();
        citas.Setup(r => r.GetByIdAsync(citaId)).ReturnsAsync(new Cita { Id = citaId, Estado = EstadoCita.Confirmada });
        var recordatorios = new Mock<IReminderService>();
        var servicio = CrearServicio(api, citas, recordatorios);

        var ok = await servicio.CancelarAsync(citaId);

        Assert.True(ok);
        recordatorios.Verify(r => r.CancelarAsync(citaId), Times.Once);
    }
}
