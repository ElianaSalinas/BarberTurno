using BarberTurno.Data.Repositories;
using AppViewState = BarberTurno.Helpers.ViewState;
using BarberTurno.Models;
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
}
