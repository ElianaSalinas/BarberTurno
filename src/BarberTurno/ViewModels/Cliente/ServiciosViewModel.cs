using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Network;
using BarberTurno.Services.Sync;
// Responsable: M09
// Servicios del barbero elegido. Recibe barberoId por parámetros de navegación (IQueryAttributable).
namespace BarberTurno.ViewModels;

/// <summary>Muestra los servicios del barbero recibido y conduce a la selección de hora.</summary>
public partial class ServiciosViewModel : ClienteViewModelBase, IQueryAttributable
{
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly ISyncService _sync;
    private readonly INavigationService _navigation;
    private int _barberoId;

    public ServiciosViewModel(
        IBarberoRepository barberos,
        IServicioRepository servicios,
        ISyncService sync,
        INetworkService network,
        INavigationService navigation) : base(network)
    {
        _barberos = barberos;
        _servicios = servicios;
        _sync = sync;
        _navigation = navigation;
    }

    public ObservableCollection<ServicioItem> Servicios { get; } = new();

    [ObservableProperty]
    private string titulo = "Servicios";

    [ObservableProperty]
    private string subtitulo = "Elige el servicio que quieres reservar";

    /// <summary>Recibe el identificador del barbero enviado por la navegación y carga sus servicios.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavParams.BarberoId, out var valor)
            && int.TryParse(valor?.ToString(), out var barberoId))
        {
            _barberoId = barberoId;
            _ = CargarInternoAsync();
        }
        else if (_barberoId <= 0)
        {
            MensajeError = "No se indicó qué barbero elegiste. Vuelve atrás e inténtalo de nuevo.";
            State = ViewState.Error;
        }
    }

    [RelayCommand]
    private Task CargarAsync() => CargarInternoAsync();

    [RelayCommand]
    private async Task RefrescarAsync()
    {
        try
        {
            if (HayConexion)
                await _sync.SincronizarCatalogosAsync();

            await CargarInternoAsync(mostrarCargando: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    // Lee las entidades locales y las pasa a la siguiente pantalla de reserva.
    private async Task SeleccionarAsync(ServicioItem? servicio)
    {
        if (servicio is null)
            return;

        var barbero = await _barberos.GetByIdAsync(_barberoId);
        var servicioModel = await _servicios.GetByIdAsync(servicio.Id);

        var parametros = new Dictionary<string, object>
        {
            [NavParams.BarberoId] = _barberoId,
            [NavParams.ServicioId] = servicio.Id
        };

        if (barbero is not null)
            parametros[ParametrosNavegacion.Barbero] = barbero;
        if (servicioModel is not null)
            parametros[ParametrosNavegacion.Servicio] = servicioModel;

        await _navigation.GoToAsync(Routes.SeleccionHora, parametros);
    }

    private async Task CargarInternoAsync(bool mostrarCargando = true)
    {
        if (mostrarCargando)
            State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var barbero = await _barberos.GetByIdAsync(_barberoId);
            if (barbero is not null)
                Titulo = $"Servicios de {barbero.Nombre}";

            var lista = await _servicios.GetByBarberoAsync(_barberoId);

            if (HayConexion)
            {
                await _sync.SincronizarCatalogosAsync();
                lista = await _servicios.GetByBarberoAsync(_barberoId);
            }

            Servicios.Clear();
            foreach (var servicio in lista.OrderBy(s => s.Nombre))
                Servicios.Add(ServicioItem.Desde(servicio));

            State = Servicios.Count == 0 ? ViewState.Vacio : ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (Servicios.Count == 0)
            {
                MensajeError = "No pudimos cargar los servicios. Revisa tu conexión e intenta de nuevo.";
                State = ViewState.Error;
            }
        }
    }
}
