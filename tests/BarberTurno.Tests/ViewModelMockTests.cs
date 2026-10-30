using BarberTurno.Data.Repositories;
using AppViewState = BarberTurno.Helpers.ViewState;
using BarberTurno.Dtos;
using BarberTurno.Models;
using BarberTurno.Services.Api;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Network;
using BarberTurno.Services.Sync;
using BarberTurno.ViewModels;
using Moq;
using Xunit;

namespace BarberTurno.Tests;

/// <summary>
/// Cumple con el requisito del PDF:
/// "Pruebas unitarias de un ViewModel con un repositorio simulado (Moq)"
/// </summary>
public class ViewModelMockTests
{
    [Fact]
    public async Task BarberosViewModel_ConDatosEnRepositorio_PasaAEstadoConDatosYCargaLista()
    {
        // 1. Arrange: simular los repositorios y servicios con Moq
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNetwork = new Mock<INetworkService>();
        var mockNav = new Mock<INavigationService>();
        var mockDialog = new Mock<IDialogService>();

        mockNetwork.Setup(n => n.HayConexion).Returns(true);

        var barberosPrueba = new List<Barbero>
        {
            new() { Id = 1, Nombre = "Alejandro" },
            new() { Id = 2, Nombre = "Roberto" }
        };

        var serviciosPrueba = new List<Servicio>
        {
            new() { Id = 1, Nombre = "Corte Clásico", DuracionMin = 30, Precio = 500 }
        };

        mockBarberoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(barberosPrueba);
        mockServicioRepo.Setup(r => r.GetByBarberoAsync(It.IsAny<int>())).ReturnsAsync(serviciosPrueba);

        var viewModel = new BarberosViewModel(
            mockBarberoRepo.Object,
            mockServicioRepo.Object,
            mockSync.Object,
            mockNetwork.Object,
            mockNav.Object,
            mockDialog.Object
        );

        // 2. Act: ejecutar el comando de carga (como lo haría la pantalla al abrirse)
        await viewModel.CargarCommand.ExecuteAsync(null);

        // 3. Assert: verificar que el ViewModel manejó correctamente los datos y estados
        Assert.Equal(AppViewState.ConDatos, viewModel.State);
        Assert.Equal(2, viewModel.Barberos.Count);
        Assert.Equal("Alejandro", viewModel.Barberos[0].Nombre);
        Assert.Equal("Roberto", viewModel.Barberos[1].Nombre);

        // Verificar que el repositorio fue consultado exactamente una vez
        mockBarberoRepo.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task BarberosViewModel_SinDatosYSinConexion_PasaAEstadoErrorConMensajeClaro()
    {
        // 1. Arrange
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNetwork = new Mock<INetworkService>();
        var mockNav = new Mock<INavigationService>();
        var mockDialog = new Mock<IDialogService>();

        mockNetwork.Setup(n => n.HayConexion).Returns(false);
        mockBarberoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Barbero>());

        var viewModel = new BarberosViewModel(
            mockBarberoRepo.Object,
            mockServicioRepo.Object,
            mockSync.Object,
            mockNetwork.Object,
            mockNav.Object,
            mockDialog.Object
        );

        // 2. Act
        await viewModel.CargarCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.Equal(AppViewState.Error, viewModel.State);
        Assert.False(string.IsNullOrEmpty(viewModel.MensajeError));
        Assert.Empty(viewModel.Barberos);
    }

    [Fact]
    public async Task AgendaDiaViewModel_DiaSiguiente_CargaCitasDelDiaSiguienteCorrectamente()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockCitaRepo = new Mock<ICitaRepository>();
        var mockClienteRepo = new Mock<IClienteRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNav = new Mock<INavigationService>();

        var manana = DateTime.Today.AddDays(1);
        var diaManana = DateOnly.FromDateTime(manana);

        mockAuth.Setup(a => a.SesionActual).Returns(new SesionUsuario
        {
            UsuarioId = 2,
            Nombre = "Juan Pérez",
            Rol = RolUsuario.Barbero,
            BarberoId = 1
        });

        mockBarberoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Barbero>
        {
            new() { Id = 1, Nombre = "Juan Pérez" }
        });

        var citaManana = new Cita
        {
            Id = Guid.NewGuid(),
            BarberoId = 1,
            ClienteId = 10,
            ServicioId = 5,
            Inicio = manana.Date.AddHours(10), // 10:00 AM de mañana
            Fin = manana.Date.AddHours(10).AddMinutes(45),
            Estado = EstadoCita.Confirmada
        };

        mockCitaRepo.Setup(r => r.GetAgendaBarberoAsync(1, diaManana))
                    .ReturnsAsync(new List<Cita> { citaManana });

        mockClienteRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new Cliente
        {
            Id = 10,
            Nombre = "Carlos Santana",
            Telefono = "8095551234"
        });

        mockServicioRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(new Servicio
        {
            Id = 5,
            Nombre = "Corte y Barba",
            DuracionMin = 45,
            Precio = 800m
        });

        var viewModel = new AgendaDiaViewModel(
            mockAuth.Object,
            mockCitaRepo.Object,
            mockClienteRepo.Object,
            mockServicioRepo.Object,
            mockBarberoRepo.Object,
            mockSync.Object,
            mockNav.Object
        );

        // 2. Act: avanzar al día siguiente
        await viewModel.DiaSiguienteCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.Equal(manana.Date, viewModel.FechaSeleccionada.Date);
        Assert.Equal(1, viewModel.CantidadCitas);
        Assert.False(viewModel.EstaAgendaVacia);
        Assert.Equal(AppViewState.ConDatos, viewModel.State);
        Assert.Single(viewModel.CitasDelDia);
        Assert.Equal("Carlos Santana", viewModel.CitasDelDia[0].NombreCliente);
        Assert.Equal("CS", viewModel.CitasDelDia[0].InicialesCliente);
        Assert.Equal("Corte y Barba · 45 min", viewModel.CitasDelDia[0].Servicio);
        Assert.Equal("RD$ 800", viewModel.IngresosDelDia);
        Assert.True(viewModel.PuedeVolverAHoy);
    }

    [Fact]
    public async Task AgendaDiaViewModel_DiaSinCitas_PasaAEstadoVacioYCantidadCero()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockCitaRepo = new Mock<ICitaRepository>();
        var mockClienteRepo = new Mock<IClienteRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNav = new Mock<INavigationService>();

        mockAuth.Setup(a => a.SesionActual).Returns(new SesionUsuario
        {
            UsuarioId = 2,
            Nombre = "Juan Pérez",
            Rol = RolUsuario.Barbero,
            BarberoId = 1
        });

        mockBarberoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Barbero>
        {
            new() { Id = 1, Nombre = "Juan Pérez" }
        });

        mockCitaRepo.Setup(r => r.GetAgendaBarberoAsync(It.IsAny<int>(), It.IsAny<DateOnly>()))
                    .ReturnsAsync(new List<Cita>());

        var viewModel = new AgendaDiaViewModel(
            mockAuth.Object,
            mockCitaRepo.Object,
            mockClienteRepo.Object,
            mockServicioRepo.Object,
            mockBarberoRepo.Object,
            mockSync.Object,
            mockNav.Object
        );

        // 2. Act
        await viewModel.CargarAgendaAsync();

        // 3. Assert
        Assert.Equal(0, viewModel.CantidadCitas);
        Assert.True(viewModel.EstaAgendaVacia);
        Assert.Equal(AppViewState.Vacio, viewModel.State);
        Assert.Empty(viewModel.CitasDelDia);
        Assert.Equal("RD$ 0", viewModel.IngresosDelDia);
    }

    [Fact]
    public async Task PerfilViewModel_CrearServicio_PrecioInvalido_MuestraErrorYNoLlamaApi()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockNav = new Mock<INavigationService>();
        var mockDialogs = new Mock<IDialogService>();
        var mockApi = new Mock<IBarberTurnoApi>();
        var mockBarberos = new Mock<IBarberoRepository>();
        var mockServicios = new Mock<IServicioRepository>();
        var mockSync = new Mock<ISyncService>();

        var viewModel = new PerfilViewModel(
            mockAuth.Object,
            mockNav.Object,
            mockDialogs.Object,
            mockApi.Object,
            mockBarberos.Object,
            mockServicios.Object,
            mockSync.Object
        );

        viewModel.NuevoServicioNombre = "Corte Premium";
        viewModel.NuevoServicioPrecio = "cero"; // Invalido

        // 2. Act
        await viewModel.CrearNuevoServicioCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.True(viewModel.TieneErrorServicio);
        Assert.Equal("Ingresa un precio válido mayor a 0 (ej. 500).", viewModel.MensajeErrorServicio);
        mockApi.Verify(a => a.CrearServicioAsync(It.IsAny<CrearServicioDto>()), Times.Never);
    }

    [Fact]
    public async Task PerfilViewModel_CrearServicio_NombreVacio_MuestraErrorYNoLlamaApi()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockNav = new Mock<INavigationService>();
        var mockDialogs = new Mock<IDialogService>();
        var mockApi = new Mock<IBarberTurnoApi>();
        var mockBarberos = new Mock<IBarberoRepository>();
        var mockServicios = new Mock<IServicioRepository>();
        var mockSync = new Mock<ISyncService>();

        var viewModel = new PerfilViewModel(
            mockAuth.Object,
            mockNav.Object,
            mockDialogs.Object,
            mockApi.Object,
            mockBarberos.Object,
            mockServicios.Object,
            mockSync.Object
        );

        viewModel.NuevoServicioNombre = "   "; // Espacios en blanco
        viewModel.NuevoServicioPrecio = "500";

        // 2. Act
        await viewModel.CrearNuevoServicioCommand.ExecuteAsync(null);

        // 3. Assert
        Assert.True(viewModel.TieneErrorServicio);
        Assert.Equal("Ingresa el nombre del servicio.", viewModel.MensajeErrorServicio);
        mockApi.Verify(a => a.CrearServicioAsync(It.IsAny<CrearServicioDto>()), Times.Never);
    }

    [Fact]
    public async Task AgendaDiaViewModel_DosDiaSiguienteSeguidos_MuestraCitasDePasadoManana()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockCitaRepo = new Mock<ICitaRepository>();
        var mockClienteRepo = new Mock<IClienteRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNav = new Mock<INavigationService>();

        mockAuth.Setup(a => a.SesionActual).Returns(new SesionUsuario
        {
            UsuarioId = 2,
            Nombre = "Juan Pérez",
            Rol = RolUsuario.Barbero,
            BarberoId = 1
        });

        var manana = DateTime.Today.AddDays(1);
        var pasadoManana = DateTime.Today.AddDays(2);
        var diaManana = DateOnly.FromDateTime(manana);
        var diaPasadoManana = DateOnly.FromDateTime(pasadoManana);

        var citaPasadoManana = new Cita
        {
            Id = Guid.NewGuid(),
            BarberoId = 1,
            ClienteId = 20,
            ServicioId = 8,
            Inicio = pasadoManana.Date.AddHours(14),
            Fin = pasadoManana.Date.AddHours(14).AddMinutes(30),
            Estado = EstadoCita.Confirmada
        };

        // Simular que la primera llamada tarda más para provocar que termine después de la segunda
        mockCitaRepo.Setup(r => r.GetAgendaBarberoAsync(1, diaManana))
            .Returns(async () =>
            {
                await Task.Delay(40);
                return new List<Cita>();
            });

        mockCitaRepo.Setup(r => r.GetAgendaBarberoAsync(1, diaPasadoManana))
            .ReturnsAsync(new List<Cita> { citaPasadoManana });

        mockClienteRepo.Setup(r => r.GetByIdAsync(20)).ReturnsAsync(new Cliente
        {
            Id = 20,
            Nombre = "Roberto Gómez",
            Telefono = "8095559999"
        });

        mockServicioRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(new Servicio
        {
            Id = 8,
            Nombre = "Corte Clásico",
            DuracionMin = 30,
            Precio = 500m
        });

        var viewModel = new AgendaDiaViewModel(
            mockAuth.Object,
            mockCitaRepo.Object,
            mockClienteRepo.Object,
            mockServicioRepo.Object,
            mockBarberoRepo.Object,
            mockSync.Object,
            mockNav.Object
        );

        // 2. Act: dos DiaSiguiente seguidos rápidos
        var t1 = viewModel.DiaSiguienteCommand.ExecuteAsync(null);
        var t2 = viewModel.DiaSiguienteCommand.ExecuteAsync(null);
        await Task.WhenAll(t1, t2);

        // 3. Assert: debe mostrar la fecha y citas de pasado mañana
        Assert.Equal(pasadoManana.Date, viewModel.FechaSeleccionada.Date);
        Assert.Single(viewModel.CitasDelDia);
        Assert.Equal("Roberto Gómez", viewModel.CitasDelDia[0].NombreCliente);
        Assert.Equal("Corte Clásico · 30 min", viewModel.CitasDelDia[0].Servicio);
        Assert.Equal(1, viewModel.CantidadCitas);
        Assert.False(viewModel.EstaAgendaVacia);
        Assert.Equal(AppViewState.ConDatos, viewModel.State);
    }

    [Fact]
    public async Task AgendaDiaViewModel_SinBarberoIdEnSesion_MuestraEstadoError()
    {
        // 1. Arrange
        var mockAuth = new Mock<IAuthService>();
        var mockCitaRepo = new Mock<ICitaRepository>();
        var mockClienteRepo = new Mock<IClienteRepository>();
        var mockServicioRepo = new Mock<IServicioRepository>();
        var mockBarberoRepo = new Mock<IBarberoRepository>();
        var mockSync = new Mock<ISyncService>();
        var mockNav = new Mock<INavigationService>();

        mockAuth.Setup(a => a.SesionActual).Returns(new SesionUsuario
        {
            UsuarioId = 2,
            Nombre = "Usuario Sin Perfil",
            Rol = RolUsuario.Barbero,
            BarberoId = null
        });

        var viewModel = new AgendaDiaViewModel(
            mockAuth.Object,
            mockCitaRepo.Object,
            mockClienteRepo.Object,
            mockServicioRepo.Object,
            mockBarberoRepo.Object,
            mockSync.Object,
            mockNav.Object
        );

        // 2. Act
        await viewModel.CargarAgendaAsync();

        // 3. Assert
        Assert.Equal(AppViewState.Error, viewModel.State);
        Assert.Equal("No se encontró tu perfil de barbero.", viewModel.MensajeError);
        mockCitaRepo.Verify(r => r.GetAgendaBarberoAsync(It.IsAny<int>(), It.IsAny<DateOnly>()), Times.Never);
    }

    [Theory]
    [InlineData("1,500", 1500)]
    [InlineData("1.500", 1500)]
    [InlineData("1,500.50", 1500.50)]
    [InlineData("1.500,50", 1500.50)]
    [InlineData("500", 500)]
    [InlineData("500.25", 500.25)]
    [InlineData("500,25", 500.25)]
    public void PerfilViewModel_TryParsePrecio_FormatosValidos(string entrada, decimal esperado)
    {
        bool exito = PerfilViewModel.TryParsePrecio(entrada, out decimal precio);
        Assert.True(exito);
        Assert.Equal(esperado, precio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-10")]
    [InlineData("cero")]
    [InlineData("0")]
    public void PerfilViewModel_TryParsePrecio_FormatosInvalidos(string entrada)
    {
        bool exito = PerfilViewModel.TryParsePrecio(entrada, out _);
        Assert.False(exito);
    }
}
