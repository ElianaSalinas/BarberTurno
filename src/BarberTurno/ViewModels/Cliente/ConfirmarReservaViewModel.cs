using System.Diagnostics;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Availability;
using BarberTurno.Services.Booking;
using BarberTurno.Services.Dialogs;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

// Responsable: M09 (pantalla, según el plan de 10 módulos). La reserva la hace IBookingService.
// Resumen, validación y confirmación. Valida antes de enviar (campos obligatorios y que la hora siga libre)
// y traduce el ResultadoReserva a mensajes claros. La decisión final siempre es del servidor (409 = hora ocupada).
namespace BarberTurno.ViewModels;

/// <summary>Valida y envía la reserva final mediante el servicio de reservas.</summary>
public partial class ConfirmarReservaViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IBookingService _reservas;
    private readonly IAvailabilityService _disponibilidad;
    private readonly INetworkService _red;
    private readonly IDialogService _dialogos;
    private readonly INavigationService _navegacion;
    private string _origen = OrigenReserva.Barberos;

    public ConfirmarReservaViewModel(
        IBookingService reservas,
        IAvailabilityService disponibilidad,
        INetworkService red,
        IDialogService dialogos,
        INavigationService navegacion)
    {
        _reservas = reservas;
        _disponibilidad = disponibilidad;
        _red = red;
        _dialogos = dialogos;
        _navegacion = navegacion;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BarberoNombre), nameof(BarberoIniciales))]
    private Barbero? barbero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ServicioNombre), nameof(DuracionTexto), nameof(PrecioTexto))]
    private Servicio? servicio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FechaTexto), nameof(HoraTexto))]
    private SlotDisponible? slot;

    /// <summary>Texto del indicador mientras se envía ("Comprobando la hora...", "Reservando...").</summary>
    [ObservableProperty]
    private string? progreso;

    public string BarberoNombre => Barbero?.Nombre ?? "";
    public string BarberoIniciales => FormatoCliente.Iniciales(Barbero?.Nombre ?? "");
    public string ServicioNombre => Servicio?.Nombre ?? "";
    public string DuracionTexto => Servicio is null ? "" : FormatoCliente.Duracion(Servicio.DuracionMin);
    public string PrecioTexto => Servicio is null ? "" : FormatoCliente.Precio(Servicio.Precio);
    public string FechaTexto => Slot is null ? "" : FormatoCliente.FechaLarga(Slot.Inicio);
    public string HoraTexto => Slot is null ? "" : FormatoCliente.RangoHora(Slot.Inicio, Slot.Fin);

    /// <summary>Aviso visible si se va a reservar sin conexión (la cita quedará Pendiente).</summary>
    public bool SinConexion => !_red.HayConexion;

    /// <summary>Recibe los datos de la reserva seleccionados en las pantallas anteriores.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(ParametrosNavegacion.Barbero, out var b) && b is Barbero barberoRecibido)
            Barbero = barberoRecibido;
        if (query.TryGetValue(ParametrosNavegacion.Servicio, out var s) && s is Servicio servicioRecibido)
            Servicio = servicioRecibido;
        if (query.TryGetValue(ParametrosNavegacion.Slot, out var h) && h is SlotDisponible slotRecibido)
            Slot = slotRecibido;
        if (query.TryGetValue(ParametrosNavegacion.Origen, out var o) && o is string origen)
            _origen = origen;
    }

    /// <summary>Comprueba que la navegación entregó todos los datos necesarios para reservar.</summary>
    public Task AlAparecerAsync()
    {
        MensajeError = null;
        OnPropertyChanged(nameof(SinConexion));

        if (Barbero is null || Servicio is null || Slot is null)
        {
            MensajeError = MensajesCliente.FaltanDatosReserva;
            State = ViewState.Error;
        }
        else
        {
            State = ViewState.ConDatos;
        }
        return Task.CompletedTask;
    }

    /// <summary>Botón Reintentar del estado de error: no hay nada que recargar aquí, se vuelve a elegir hora.</summary>
    [RelayCommand]
    private Task VolverAsync() => _navegacion.GoBackAsync();

    [RelayCommand]
    // Revalida la franja y delega la creación al servicio, cuya API tiene la decisión final.
    private async Task ConfirmarAsync()
    {
        if (IsBusy)
            return;

        MensajeError = null;

        // 1) Validación local: campos obligatorios y hora en el futuro.
        var errorValidacion = Validar();
        if (errorValidacion is not null)
        {
            MensajeError = errorValidacion;
            return;
        }

        IsBusy = true;
        try
        {
            // 2) ¿La hora sigue libre? Con conexión se pregunta al servidor (autoridad) justo antes de enviar.
            if (_red.HayConexion)
            {
                Progreso = "Comprobando que la hora siga libre...";
                if (!await HoraSigueLibreAsync())
                {
                    await _dialogos.ShowAlertAsync(MensajesCliente.TituloHoraOcupada, MensajesCliente.HoraYaNoDisponible);
                    await _navegacion.GoBackAsync();   // SeleccionHora recarga la lista al aparecer
                    return;
                }
            }

            // 3) Reservar. El servidor decide; el resultado se traduce a un mensaje claro.
            Progreso = "Reservando...";
            var resultado = await _reservas.ReservarAsync(new ReservaSolicitud
            {
                BarberoId = Barbero!.Id,
                ServicioId = Servicio!.Id,
                Inicio = Slot!.Inicio
            });

            await ProcesarResultadoAsync(resultado);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ConfirmarReserva] {ex}");
            MensajeError = MensajesCliente.ErrorReservaGenerico;
        }
        finally
        {
            Progreso = null;
            IsBusy = false;
        }
    }

    /// <summary>Devuelve el mensaje de error o null si todo está bien.</summary>
    internal string? Validar()
    {
        if (Barbero is null || Servicio is null || Slot is null)
            return MensajesCliente.FaltanDatosReserva;
        if (Slot.Inicio <= DateTime.Now)
            return MensajesCliente.HoraYaPaso;
        return null;
    }

    private async Task<bool> HoraSigueLibreAsync()
    {
        try
        {
            var libres = await _disponibilidad.GetHorasLibresAsync(
                Barbero!.Id, Servicio!.Id, DateOnly.FromDateTime(Slot!.Inicio));
            return libres.Any(h => h.Inicio == Slot.Inicio);
        }
        catch (Exception ex)
        {
            // Si la comprobación previa falla, se intenta igual: el servidor rechaza con 409 si ya está ocupada.
            Debug.WriteLine($"[ConfirmarReserva] No se pudo revalidar la hora: {ex.Message}");
            return true;
        }
    }

    private async Task ProcesarResultadoAsync(ResultadoReserva resultado)
    {
        switch (resultado.Tipo)
        {
            case ResultadoReservaTipo.Confirmada:
                await _dialogos.ShowAlertAsync(MensajesCliente.TituloReservaConfirmada,
                    $"Te esperamos el {FechaTexto.ToLowerInvariant()} a las {FormatoCliente.Hora(Slot!.Inicio)} con {BarberoNombre}.");
                await IrAMisCitasAsync();
                break;

            case ResultadoReservaTipo.PendienteSinConexion:
                await _dialogos.ShowAlertAsync(MensajesCliente.TituloReservaPendiente, MensajesCliente.ReservaPendiente);
                await IrAMisCitasAsync();
                break;

            case ResultadoReservaTipo.HoraOcupada:
                await _dialogos.ShowAlertAsync(MensajesCliente.TituloHoraOcupada,
                    resultado.Mensaje ?? MensajesCliente.HoraYaNoDisponible);
                await _navegacion.GoBackAsync();
                break;

            case ResultadoReservaTipo.Invalida:
            case ResultadoReservaTipo.Error:
            default:
                MensajeError = string.IsNullOrWhiteSpace(resultado.Mensaje)
                    ? MensajesCliente.ErrorReservaGenerico
                    : resultado.Mensaje;
                break;
        }
    }

    /// <summary>Cierra las páginas apiladas del flujo y muestra la pestaña "Mis citas".</summary>
    private async Task IrAMisCitasAsync()
    {
        if (_origen == OrigenReserva.MisCitas)
        {
            // MisCitas → SeleccionHora → Confirmar: dos niveles atrás y ya estamos en Mis citas.
            await _navegacion.GoToAsync(RutasCliente.Atras(2));
        }
        else
        {
            // Barberos → Servicios → SeleccionHora → Confirmar: se limpia la pila de Barberos y se cambia de pestaña.
            await _navegacion.GoToAsync(RutasCliente.Atras(3));
            await _navegacion.GoToAsync(RutasCliente.MisCitas);
        }
    }
}
