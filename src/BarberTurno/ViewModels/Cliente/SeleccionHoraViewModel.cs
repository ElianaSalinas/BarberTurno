using System.Collections.ObjectModel;
using System.Diagnostics;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Availability;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Responsable: M09 (pantalla, según el plan de 10 módulos). Las horas las calcula IAvailabilityService.
// Fecha + horas libres según duración. La autoridad de las horas libres es el servidor (ADR-0008).
namespace BarberTurno.ViewModels;

public partial class SeleccionHoraViewModel : BaseViewModel, IQueryAttributable
{
    /// <summary>Hasta cuántos días adelante puede reservar el cliente.</summary>
    public const int DiasMaximosAdelante = 30;

    private readonly IAvailabilityService _disponibilidad;
    private readonly INetworkService _red;
    private readonly INavigationService _navegacion;
    private string _origen = OrigenReserva.Barberos;
    private int _versionCarga;   // descarta respuestas viejas si el usuario cambia de fecha rápido

    public SeleccionHoraViewModel(
        IAvailabilityService disponibilidad,
        INetworkService red,
        INavigationService navegacion)
    {
        _disponibilidad = disponibilidad;
        _red = red;
        _navegacion = navegacion;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private Barbero? barbero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resumen))]
    private Servicio? servicio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FechaTexto))]
    private DateTime fecha = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayHoraSeleccionada))]
    [NotifyCanExecuteChangedFor(nameof(ContinuarCommand))]
    private SlotItem? horaSeleccionada;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayAviso))]
    private string? aviso;

    public bool HayAviso => !string.IsNullOrWhiteSpace(Aviso);
    public bool HayHoraSeleccionada => HoraSeleccionada is not null;

    public DateTime FechaMinima => DateTime.Today;
    public DateTime FechaMaxima => DateTime.Today.AddDays(DiasMaximosAdelante);
    public string FechaTexto => FormatoCliente.FechaLarga(Fecha);

    /// <summary>"Corte clásico · 30 min con Juan Pérez"</summary>
    public string Resumen => Barbero is null || Servicio is null
        ? ""
        : $"{Servicio.Nombre} · {FormatoCliente.Duracion(Servicio.DuracionMin)} con {Barbero.Nombre}";

    public ObservableCollection<SlotItem> Horas { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ParametrosNavegacion.Barbero, out var b) && b is Barbero barberoRecibido)
            Barbero = barberoRecibido;
        if (query.TryGetValue(ParametrosNavegacion.Servicio, out var s) && s is Servicio servicioRecibido)
            Servicio = servicioRecibido;
        if (query.TryGetValue(ParametrosNavegacion.Origen, out var o) && o is string origen)
            _origen = origen;
    }

    /// <summary>
    /// Se recarga SIEMPRE al aparecer: si el cliente vuelve desde Confirmar porque la hora se ocupó,
    /// debe ver la lista actualizada del servidor.
    /// </summary>
    public Task AlAparecerAsync() => CargarHorasAsync();

    partial void OnFechaChanged(DateTime value)
    {
        // Cambiar de fecha recarga las horas (el DatePicker ya limita el rango).
        if (Barbero is not null && Servicio is not null)
            _ = CargarHorasAsync();
    }

    [RelayCommand]
    private async Task CargarHorasAsync()
    {
        if (Barbero is null || Servicio is null)
        {
            MensajeError = MensajesCliente.FaltanDatosReserva;
            State = ViewState.Error;
            return;
        }

        var version = ++_versionCarga;
        HoraSeleccionada = null;
        MensajeError = null;
        Aviso = _red.HayConexion ? null : MensajesCliente.SinConexionHorasAproximadas;
        State = ViewState.Cargando;
        IsBusy = true;

        try
        {
            var slots = await _disponibilidad.GetHorasLibresAsync(Barbero.Id, Servicio.Id, DateOnly.FromDateTime(Fecha));
            if (version != _versionCarga)
                return; // llegó una carga más reciente

            var ahora = DateTime.Now;
            Horas.Clear();
            foreach (var slot in slots.Where(x => x.Inicio > ahora).OrderBy(x => x.Inicio))
                Horas.Add(new SlotItem(slot));

            State = Horas.Count > 0 ? ViewState.ConDatos : ViewState.Vacio;
        }
        catch (Exception ex)
        {
            if (version != _versionCarga)
                return;
            Debug.WriteLine($"[SeleccionHora] {ex}");
            Horas.Clear();
            MensajeError = MensajesCliente.ErrorCargarHoras;
            State = ViewState.Error;
        }
        finally
        {
            if (version == _versionCarga)
                IsBusy = false;
        }
    }

    [RelayCommand]
    private void DiaAnterior()
    {
        if (Fecha.Date > FechaMinima)
            Fecha = Fecha.AddDays(-1);
    }

    [RelayCommand]
    private void DiaSiguiente()
    {
        if (Fecha.Date < FechaMaxima)
            Fecha = Fecha.AddDays(1);
    }

    [RelayCommand(CanExecute = nameof(HayHoraSeleccionada))]
    private async Task ContinuarAsync()
    {
        if (Barbero is null || Servicio is null || HoraSeleccionada is null)
        {
            MensajeError = MensajesCliente.EligeUnaHora;
            return;
        }

        await _navegacion.GoToAsync(Routes.ConfirmarReserva, new Dictionary<string, object>
        {
            [ParametrosNavegacion.Barbero] = Barbero,
            [ParametrosNavegacion.Servicio] = Servicio,
            [ParametrosNavegacion.Slot] = HoraSeleccionada.Slot,
            [ParametrosNavegacion.Origen] = _origen
        });
    }
}

/// <summary>Franja horaria con su texto listo para mostrar.</summary>
public sealed class SlotItem
{
    public SlotItem(SlotDisponible slot) => Slot = slot;

    public SlotDisponible Slot { get; }
    public string HoraTexto => FormatoCliente.Hora(Slot.Inicio);
    public string FinTexto => $"hasta {FormatoCliente.Hora(Slot.Fin)}";
}
