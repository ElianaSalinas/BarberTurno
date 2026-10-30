using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Network;
using BarberTurno.Services.Sync;
// Responsable: M09
// Lista de barberos y sus servicios (cuatro estados). Sin lógica de negocio: lee repositorios y delega.
namespace BarberTurno.ViewModels;

public partial class BarberosViewModel : ClienteViewModelBase
{
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly ISyncService _sync;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;

    public BarberosViewModel(
        IBarberoRepository barberos,
        IServicioRepository servicios,
        ISyncService sync,
        INetworkService network,
        INavigationService navigation,
        IDialogService dialogs) : base(network)
    {
        _barberos = barberos;
        _servicios = servicios;
        _sync = sync;
        _navigation = navigation;
        _dialogs = dialogs;
    }

    public ObservableCollection<BarberoItem> Barberos { get; } = new();

    public override void OnAppearing()
    {
        base.OnAppearing();
        // Recarga silenciosa si ya hay datos (evita parpadeo del indicador al volver a la pestaña).
        _ = CargarInternoAsync(mostrarCargando: Barberos.Count == 0);
    }

    /// <summary>Carga completa con indicador. También es el botón Reintentar/Actualizar de StateView.</summary>
    [RelayCommand]
    private Task CargarAsync() => CargarInternoAsync(mostrarCargando: true);

    /// <summary>Gesto de arrastre: sincroniza con el servidor y vuelve a leer los datos locales.</summary>
    [RelayCommand]
    private async Task RefrescarAsync()
    {
        try
        {
            if (!HayConexion)
            {
                await _dialogs.ShowAlertAsync("Sin conexión",
                    "No hay conexión a internet. Se muestran los datos guardados en tu teléfono.");
                return;
            }

            await _sync.SincronizarCatalogosAsync();
            await CargarInternoAsync(mostrarCargando: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("No se pudo actualizar",
                "No pudimos actualizar la lista. Intenta de nuevo en unos minutos.");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private Task SeleccionarAsync(BarberoItem? barbero)
    {
        if (barbero is null)
            return Task.CompletedTask;

        return _navigation.GoToAsync(Routes.Servicios, new Dictionary<string, object>
        {
            [NavParams.BarberoId] = barbero.Id
        });
    }

    private async Task CargarInternoAsync(bool mostrarCargando)
    {
        if (mostrarCargando)
            State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var items = await LeerBarberosAsync();

            // Primera vez (SQLite vacío): pedir el catálogo al servidor si hay red.
            if (items.Count == 0 && HayConexion)
            {
                await _sync.SincronizarCatalogosAsync();
                items = await LeerBarberosAsync();
            }

            if (items.Count == 0 && !HayConexion)
            {
                MensajeError = "Sin conexión y sin datos guardados. Conéctate a internet para ver los barberos.";
                State = ViewState.Error;
                return;
            }

            Barberos.Clear();
            foreach (var item in items)
                Barberos.Add(item);

            State = Barberos.Count == 0 ? ViewState.Vacio : ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            // Si ya había datos en pantalla se conservan; si no, se muestra el error con Reintentar.
            if (Barberos.Count == 0)
            {
                MensajeError = "No pudimos cargar los barberos. Revisa tu conexión e intenta de nuevo.";
                State = ViewState.Error;
            }
        }
    }

    private async Task<List<BarberoItem>> LeerBarberosAsync()
    {
        var lista = await _barberos.GetAllAsync();
        var items = new List<BarberoItem>();

        foreach (var barbero in lista.OrderBy(b => b.Nombre))
        {
            var servicios = await _servicios.GetByBarberoAsync(barbero.Id);
            var resumen = string.Join(" · ", servicios.Select(s => s.Nombre));
            items.Add(new BarberoItem(barbero.Id, barbero.Nombre, barbero.FotoUrl,
                string.IsNullOrEmpty(resumen) ? "Sin servicios registrados" : resumen));
        }

        return items;
    }
}
