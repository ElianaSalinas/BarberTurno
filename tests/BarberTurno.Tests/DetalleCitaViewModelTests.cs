using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.ViewModels;
using Moq;
using Xunit;
using AppViewState = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Tests;

/// <summary>El barbero ve, para cada cita, el historial (visitas anteriores) del cliente.</summary>
public class DetalleCitaViewModelTests
{
    private static async Task EsperarAsync(Func<bool> condicion)
    {
        for (var i = 0; i < 200 && !condicion(); i++)
            await Task.Delay(10);
    }

    [Fact]
    public async Task AlRecibirLaCita_CargaClienteYHistorialDeVisitasAnteriores_SinCanceladas()
    {
        var citaId = Guid.NewGuid();
        var inicio = new DateTime(2026, 10, 31, 10, 0, 0);

        var citas = new Mock<ICitaRepository>();
        citas.Setup(r => r.GetByIdAsync(citaId)).ReturnsAsync(new Cita
        {
            Id = citaId, ClienteId = 10, BarberoId = 7, ServicioId = 5, Inicio = inicio, Estado = EstadoCita.Confirmada
        });
        citas.Setup(r => r.GetHistorialClienteAsync(10, 7, inicio, 0, It.IsAny<int>())).ReturnsAsync(new List<Cita>
        {
            new() { Id = Guid.NewGuid(), ClienteId = 10, ServicioId = 5, Inicio = inicio.AddDays(-10), Estado = EstadoCita.Atendida },
            new() { Id = Guid.NewGuid(), ClienteId = 10, ServicioId = 5, Inicio = inicio.AddDays(-20), Estado = EstadoCita.Cancelada },
            new() { Id = Guid.NewGuid(), ClienteId = 10, ServicioId = 6, Inicio = inicio.AddDays(-30), Estado = EstadoCita.Confirmada }
        });

        var clientes = new Mock<IClienteRepository>();
        clientes.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Cliente { Id = 10, Nombre = "Carlos Santana", Telefono = "8095551234" });
        var servicios = new Mock<IServicioRepository>();
        servicios.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Servicio { Id = 5, Nombre = "Corte Clásico", DuracionMin = 30, Precio = 350 });
        servicios.Setup(r => r.GetByIdAsync(6)).ReturnsAsync(new Servicio { Id = 6, Nombre = "Perfilado", DuracionMin = 20, Precio = 200 });
        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.SesionActual).Returns(new SesionUsuario { UsuarioId = 2, BarberoId = 7, Rol = RolUsuario.Barbero, Nombre = "Juan" });

        var vm = new DetalleCitaViewModel(citas.Object, clientes.Object, servicios.Object, auth.Object);

        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["citaId"] = citaId });
        await EsperarAsync(() => vm.State == AppViewState.ConDatos);

        Assert.Equal(AppViewState.ConDatos, vm.State);
        Assert.True(vm.TieneCitaSeleccionada);
        Assert.Equal("Carlos Santana", vm.NombreCliente);
        Assert.Equal("CS", vm.InicialesCliente);
        Assert.Equal("Corte Clásico", vm.Servicio);
        Assert.Equal("10:00 AM", vm.HoraCita);
        Assert.Equal(2, vm.Historial.Count);                       // la cancelada no cuenta como visita
        Assert.Equal("Corte Clásico", vm.Historial[0].Servicio);
        Assert.True(vm.HayHistorial);
        Assert.False(vm.SinHistorial);
    }

    [Fact]
    public async Task ClienteSinVisitasPrevias_MarcaSinHistorial()
    {
        var citaId = Guid.NewGuid();
        var inicio = new DateTime(2026, 10, 31, 9, 0, 0);
        var citas = new Mock<ICitaRepository>();
        citas.Setup(r => r.GetByIdAsync(citaId)).ReturnsAsync(new Cita { Id = citaId, ClienteId = 10, ServicioId = 5, Inicio = inicio, Estado = EstadoCita.Confirmada });
        citas.Setup(r => r.GetHistorialClienteAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<int>()))
             .ReturnsAsync(new List<Cita>());
        var clientes = new Mock<IClienteRepository>();
        clientes.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Cliente { Id = 10, Nombre = "Luis" });
        var servicios = new Mock<IServicioRepository>();
        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.SesionActual).Returns(new SesionUsuario { BarberoId = 7, Rol = RolUsuario.Barbero });

        var vm = new DetalleCitaViewModel(citas.Object, clientes.Object, servicios.Object, auth.Object);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["citaId"] = citaId.ToString() });
        await EsperarAsync(() => vm.State == AppViewState.ConDatos);

        Assert.Empty(vm.Historial);
        Assert.True(vm.SinHistorial);
        Assert.False(vm.HayHistorial);
    }

    [Fact]
    public async Task CitaInexistente_PasaAEstadoError()
    {
        var citas = new Mock<ICitaRepository>();
        citas.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cita?)null);
        var vm = new DetalleCitaViewModel(citas.Object, new Mock<IClienteRepository>().Object,
            new Mock<IServicioRepository>().Object, new Mock<IAuthService>().Object);

        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["citaId"] = Guid.NewGuid() });
        await EsperarAsync(() => vm.State == AppViewState.Error);

        Assert.Equal(AppViewState.Error, vm.State);
        Assert.False(vm.TieneCitaSeleccionada);
        Assert.False(string.IsNullOrWhiteSpace(vm.MensajeError));
    }
}
