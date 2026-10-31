using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Booking;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Network;
using BarberTurno.Services.Sync;
// Responsable: M09
// Próximas y anteriores; estado de cada cita (pendiente / confirmada / rechazada) y cancelación (opcional).
namespace BarberTurno.ViewModels;

/// <summary>Lista, filtra y cancela las citas del cliente usando la caché y sincronización local.</summary>
public partial class MisCitasViewModel : ClienteViewModelBase
{
    private readonly ICitaRepository _citas;
    private readonly IBarberoRepository _barberos;
    private readonly IServicioRepository _servicios;
    private readonly IAuthService _auth;
    private readonly ISyncService _sync;
    private readonly IBookingService _booking;
    private readonly IDialogService _dialogs;

    private List<CitaItem> _todas = new();

    public MisCitasViewModel(
        ICitaRepository citas,
        IBarberoRepository barberos,
        IServicioRepository servicios,
        IAuthService auth,
        ISyncService sync,
        IBookingService booking,
        IDialogService dialogs,
        INetworkService network) : base(network)
    {
        _citas = citas;
        _barberos = barberos;
        _servicios = servicios;
        _auth = auth;
        _sync = sync;
        _booking = booking;
        _dialogs = dialogs;
    }

    /// <summary>Citas de la pestaña seleccionada (Próximas o Anteriores).</summary>
    public ObservableCollection<CitaItem> Citas { get; } = new();

    [ObservableProperty]
    private bool mostrandoProximas = true;

    public string TextoVacio => MostrandoProximas
        ? "No tienes citas próximas. Elige un barbero en la pestaña Barberos para reservar."
        : "Todavía no tienes citas anteriores.";

    partial void OnMostrandoProximasChanged(bool value)
    {
        OnPropertyChanged(nameof(TextoVacio));
        AplicarFiltro();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _sync.ReservaRechazada -= OnReservaRechazada;
        _sync.ReservaRechazada += OnReservaRechazada;
        _ = CargarInternoAsync(mostrarCargando: _todas.Count == 0);
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        _sync.ReservaRechazada -= OnReservaRechazada;
    }

    [RelayCommand]
    private Task CargarAsync() => CargarInternoAsync(mostrarCargando: true);

    [RelayCommand]
    private void VerProximas() => MostrandoProximas = true;

    [RelayCommand]
    private void VerAnteriores() => MostrandoProximas = false;

    /// <summary>Arrastrar para actualizar: envía pendientes, baja el estado actual y recarga.</summary>
    [RelayCommand]
    private async Task RefrescarAsync()
    {
        try
        {
            if (!HayConexion)
            {
                await _dialogs.ShowAlertAsync("Sin conexión",
                    "No hay conexión a internet. Se muestran las citas guardadas en tu teléfono.");
                return;
            }

            await _sync.SincronizarPendientesAsync();
            await _sync.SincronizarCatalogosAsync();
            await CargarInternoAsync(mostrarCargando: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("No se pudo actualizar",
                "No pudimos actualizar tus citas. Intenta de nuevo en unos minutos.");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task CancelarAsync(CitaItem? cita)
    {
        if (cita is null || !cita.PuedeCancelar)
            return;

        var confirmar = await _dialogs.ConfirmAsync(
            "Cancelar cita",
            $"¿Quieres cancelar tu cita de {cita.ServicioNombre} con {cita.BarberoNombre} el {cita.FechaTexto}?",
            "Sí, cancelar",
            "No");

        if (!confirmar)
            return;

        try
        {
            var cancelada = await _booking.CancelarAsync(cita.Id);
            if (!cancelada)
            {
                await _dialogs.ShowAlertAsync("No se pudo cancelar",
                    "No pudimos cancelar la cita. Inténtalo de nuevo cuando tengas conexión.");
            }

            await CargarInternoAsync(mostrarCargando: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            await _dialogs.ShowAlertAsync("No se pudo cancelar",
                "Ocurrió un problema al cancelar la cita. Intenta de nuevo.");
        }
    }

    private void OnReservaRechazada(object? sender, Guid citaId)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await CargarInternoAsync(mostrarCargando: false);
            await _dialogs.ShowAlertAsync("Reserva rechazada",
                "Esa hora ya no estaba disponible. Elige otra hora desde la pestaña Barberos.");
        });
    }

    private async Task CargarInternoAsync(bool mostrarCargando)
    {
        if (mostrarCargando)
            State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var sesion = _auth.SesionActual;
            if (sesion is null)
            {
                MensajeError = "Inicia sesión para ver tus citas.";
                State = ViewState.Error;
                return;
            }

            var citas = await _citas.GetByClienteAsync(sesion.UsuarioId);
            var barberos = (await _barberos.GetAllAsync()).GroupBy(b => b.Id).ToDictionary(g => g.Key, g => g.First());
            var servicios = (await _servicios.GetAllAsync()).GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First());

            _todas = citas
                .Select(c => new CitaItem(
                    c,
                    barberos.TryGetValue(c.BarberoId, out var b) ? b.Nombre : "Barbero",
                    servicios.TryGetValue(c.ServicioId, out var s) ? s.Nombre : "Servicio"))
                .ToList();

            AplicarFiltro();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            if (_todas.Count == 0)
            {
                MensajeError = "No pudimos cargar tus citas. Revisa tu conexión e intenta de nuevo.";
                State = ViewState.Error;
            }
        }
    }

    private void AplicarFiltro()
    {
        var ahora = DateTime.Now;

        IEnumerable<CitaItem> seleccion = MostrandoProximas
            ? _todas.Where(c => c.EsProxima(ahora)).OrderBy(c => c.Inicio)
            : _todas.Where(c => !c.EsProxima(ahora)).OrderByDescending(c => c.Inicio);

        Citas.Clear();
        foreach (var cita in seleccion)
            Citas.Add(cita);

        State = Citas.Count == 0 ? ViewState.Vacio : ViewState.ConDatos;
    }
}
