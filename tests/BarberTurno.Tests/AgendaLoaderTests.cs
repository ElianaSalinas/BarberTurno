using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using Moq;
using Xunit;

namespace BarberTurno.Tests;

/// <summary>
/// Lógica compartida de la agenda del barbero (Mi agenda y Calendario):
/// identifica al barbero por BarberoId y muestra las horas locales sin desplazarlas.
/// </summary>
public class AgendaLoaderTests
{
    private static readonly DateOnly Dia = new(2026, 10, 30);
    private static readonly DateTime Ahora = new(2026, 10, 30, 9, 0, 0);

    private static (Mock<ICitaRepository> Citas, Mock<IClienteRepository> Clientes, Mock<IServicioRepository> Servicios) Repos()
    {
        var citas = new Mock<ICitaRepository>();
        var clientes = new Mock<IClienteRepository>();
        var servicios = new Mock<IServicioRepository>();

        clientes.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Cliente { Id = 10, Nombre = "Carlos Santana" });
        servicios.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Servicio { Id = 5, Nombre = "Corte Clásico", DuracionMin = 30, Precio = 350 });
        return (citas, clientes, servicios);
    }

    private static SesionUsuario Barbero(int usuarioId, int? barberoId) => new()
    {
        UsuarioId = usuarioId,
        BarberoId = barberoId,
        Nombre = "Juan Barber",
        Rol = RolUsuario.Barbero
    };

    private static Cita CitaA(DateTime inicio, EstadoCita estado = EstadoCita.Confirmada) => new()
    {
        Id = Guid.NewGuid(),
        BarberoId = 7,
        ClienteId = 10,
        ServicioId = 5,
        Inicio = inicio,
        Fin = inicio.AddMinutes(30),
        Estado = estado
    };

    [Fact]
    public async Task CargarAsync_ConsultaPorBarberoId_NuncaPorUsuarioId()
    {
        var (citas, clientes, servicios) = Repos();
        citas.Setup(r => r.GetAgendaBarberoAsync(7, Dia)).ReturnsAsync(new List<Cita> { CitaA(Dia.ToDateTime(new TimeOnly(10, 0))) });

        var resultado = await AgendaLoader.CargarAsync(Barbero(usuarioId: 2, barberoId: 7), Dia, Ahora,
            citas.Object, clientes.Object, servicios.Object);

        Assert.NotNull(resultado);
        Assert.Single(resultado!.Items);
        citas.Verify(r => r.GetAgendaBarberoAsync(7, Dia), Times.Once);
        citas.Verify(r => r.GetAgendaBarberoAsync(2, It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task CargarAsync_SinBarberoId_DevuelveNullYNoConsultaNingunId()
    {
        var (citas, clientes, servicios) = Repos();

        var resultado = await AgendaLoader.CargarAsync(Barbero(usuarioId: 2, barberoId: null), Dia, Ahora,
            citas.Object, clientes.Object, servicios.Object);

        Assert.Null(resultado);
        citas.Verify(r => r.GetAgendaBarberoAsync(It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Fact]
    public async Task CargarAsync_CitaALas15Locales_SeMuestraComo0300PM_SinDesplazarHoras()
    {
        var (citas, clientes, servicios) = Repos();
        // Cita.Inicio ya está en hora local; aunque llegue con Kind=Unspecified (como al leer de SQLite) no se reconvierte.
        var inicio = DateTime.SpecifyKind(Dia.ToDateTime(new TimeOnly(15, 0)), DateTimeKind.Unspecified);
        citas.Setup(r => r.GetAgendaBarberoAsync(7, Dia)).ReturnsAsync(new List<Cita> { CitaA(inicio) });

        var resultado = await AgendaLoader.CargarAsync(Barbero(2, 7), Dia, Ahora,
            citas.Object, clientes.Object, servicios.Object);

        var item = Assert.Single(resultado!.Items);
        Assert.Equal("03:00", item.Hora);
        Assert.Equal("PM", item.Periodo);
        Assert.Equal("03:00 PM", $"{item.Hora} {item.Periodo}");
        Assert.Equal("Carlos Santana", item.NombreCliente);
        Assert.Equal("Corte Clásico · 30 min", item.Servicio);
    }

    [Fact]
    public async Task CargarAsync_OmiteCanceladasYRechazadas_YSumaSoloIngresosActivos()
    {
        var (citas, clientes, servicios) = Repos();
        citas.Setup(r => r.GetAgendaBarberoAsync(7, Dia)).ReturnsAsync(new List<Cita>
        {
            CitaA(Dia.ToDateTime(new TimeOnly(10, 0)), EstadoCita.Confirmada),
            CitaA(Dia.ToDateTime(new TimeOnly(11, 0)), EstadoCita.Cancelada),
            CitaA(Dia.ToDateTime(new TimeOnly(12, 0)), EstadoCita.Rechazada),
            CitaA(Dia.ToDateTime(new TimeOnly(13, 0)), EstadoCita.Pendiente)
        });

        var resultado = await AgendaLoader.CargarAsync(Barbero(2, 7), Dia, Ahora,
            citas.Object, clientes.Object, servicios.Object);

        Assert.Equal(2, resultado!.Items.Count);
        Assert.Equal(700m, resultado.Ingresos);
        Assert.Equal("10:00", resultado.Proxima!.Hora); // la próxima es la primera cita futura de hoy
    }

    [Fact]
    public void ObtenerBarberoId_ClienteOSesionNula_DevuelveNull()
    {
        Assert.Null(AgendaLoader.ObtenerBarberoId(null));
        Assert.Null(AgendaLoader.ObtenerBarberoId(new SesionUsuario { UsuarioId = 3, BarberoId = 9, Rol = RolUsuario.Cliente }));
        Assert.Equal(7, AgendaLoader.ObtenerBarberoId(Barbero(2, 7)));
    }
}
