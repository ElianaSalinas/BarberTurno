// Responsable: M10 / M04
// Agenda diaria del barbero: citas programadas, métricas e historial por fecha con navegación de días.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;
using BarberTurno.Services.Sync;

namespace BarberTurno.ViewModels;

public partial class AgendaDiaViewModel : BaseViewModel
{
    private static readonly CultureInfo CulturaEs = CultureInfo.GetCultureInfo("es-DO");

    private readonly IAuthService? _auth;
    private readonly ICitaRepository? _citaRepo;
    private readonly IClienteRepository? _clienteRepo;
    private readonly IServicioRepository? _servicioRepo;
    private readonly IBarberoRepository? _barberoRepo;
    private readonly ISyncService? _sync;
    private readonly INavigationService? _nav;

    private int _versionCarga;

    [ObservableProperty]
    private string nombreBarbero = string.Empty;

    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    [ObservableProperty]
    private string fechaResumen = string.Empty;

    [ObservableProperty]
    private int cantidadCitas;

    [ObservableProperty]
    private string ingresosDelDia = string.Empty;

    [ObservableProperty]
    private string metaIngresos = string.Empty;

    [ObservableProperty]
    private bool tieneMetaIngresos;

    [ObservableProperty]
    private CitaAgendaItem proximaCita = new();

    [ObservableProperty]
    private bool tieneProximaCita;

    [ObservableProperty]
    private bool estaAgendaVacia;

    [ObservableProperty]
    private bool esDiaHoy = true;

    [ObservableProperty]
    private bool puedeVolverAHoy;

    [ObservableProperty]
    private bool isRefreshing;

    public ObservableCollection<CitaAgendaItem> CitasDelDia { get; } = new();

    public AgendaDiaViewModel(
        IAuthService auth,
        ICitaRepository citaRepo,
        IClienteRepository clienteRepo,
        IServicioRepository servicioRepo,
        IBarberoRepository barberoRepo,
        ISyncService sync,
        INavigationService nav)
    {
        _auth = auth;
        _citaRepo = citaRepo;
        _clienteRepo = clienteRepo;
        _servicioRepo = servicioRepo;
        _barberoRepo = barberoRepo;
        _sync = sync;
        _nav = nav;

        ActualizarTextoFecha();
    }

    /// <summary>Constructor sin parámetros para compatibilidad con vistas y diseño.</summary>
    public AgendaDiaViewModel()
    {
        ActualizarTextoFecha();
        ActualizarEstadoSinDatos();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = CargarAgendaAsync();
        _ = SincronizarEnSegundoPlanoAsync();
    }

    private async Task SincronizarEnSegundoPlanoAsync()
    {
        if (_sync == null) return;
        try
        {
            await _sync.SincronizarCitasAsync();
            await CargarAgendaAsync(mostrarCargando: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AgendaDiaViewModel] Error en sincronización de fondo: {ex.Message}");
        }
    }

    [RelayCommand]
    private Task ReintentarAsync() => CargarAgendaAsync();

    // Tocar una cita abre su detalle con el historial del cliente.
    [RelayCommand]
    private async Task AbrirCitaAsync(CitaAgendaItem? item)
    {
        if (item is null || item.CitaId == Guid.Empty || _nav is null)
            return;

        await _nav.GoToAsync(Routes.DetalleCita, new Dictionary<string, object> { ["citaId"] = item.CitaId });
    }

    [RelayCommand]
    public async Task DiaAnteriorAsync()
    {
        FechaSeleccionada = FechaSeleccionada.AddDays(-1);
        ActualizarTextoFecha();
        await CargarAgendaAsync();
        _ = SincronizarEnSegundoPlanoAsync();
    }

    [RelayCommand]
    public async Task DiaSiguienteAsync()
    {
        FechaSeleccionada = FechaSeleccionada.AddDays(1);
        ActualizarTextoFecha();
        await CargarAgendaAsync();
        _ = SincronizarEnSegundoPlanoAsync();
    }

    [RelayCommand]
    public async Task HoyAsync()
    {
        if (FechaSeleccionada.Date == DateTime.Today) return;
        FechaSeleccionada = DateTime.Today;
        ActualizarTextoFecha();
        await CargarAgendaAsync();
        _ = SincronizarEnSegundoPlanoAsync();
    }

    [RelayCommand]
    public Task CargarCommandAsync() => CargarAgendaAsync();

    [RelayCommand]
    public async Task RefrescarAsync()
    {
        try
        {
            if (_sync != null)
            {
                await _sync.SincronizarCitasAsync();
            }
            await CargarAgendaAsync(mostrarCargando: false);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task CargarAgendaAsync(bool mostrarCargando = true)
    {
        int versionActual = Interlocked.Increment(ref _versionCarga);

        if (mostrarCargando)
        {
            State = ViewState.Cargando;
            MensajeError = null;
        }

        try
        {
            var sesion = _auth?.SesionActual;
            NombreBarbero = sesion?.Nombre ?? "Barbero";

            // Obtener el BarberoId real directamente de la sesión activa en cada carga (no se cachea en campo privado).
            int? barberoId = AgendaLoader.ObtenerBarberoId(sesion);
            if (barberoId is null)
            {
                if (versionActual != _versionCarga) return;
                ActualizarEstadoSinDatos();
                MensajeError = "No se encontró tu perfil de barbero.";
                State = ViewState.Error;
                return;
            }

            if (_citaRepo == null || _clienteRepo == null || _servicioRepo == null)
            {
                if (versionActual != _versionCarga) return;
                ActualizarEstadoSinDatos();
                State = ViewState.ConDatos;
                return;
            }

            // Lógica compartida con Calendario: BarberoId de la sesión, horas locales y filtro de estados.
            var resultado = await AgendaLoader.CargarAsync(
                sesion, DateOnly.FromDateTime(FechaSeleccionada), DateTime.Now,
                _citaRepo, _clienteRepo, _servicioRepo);

            // Si se inició una carga posterior mientras esperábamos la consulta, descartar este resultado desfasado
            if (versionActual != _versionCarga) return;

            var items = resultado?.Items ?? new List<CitaAgendaItem>();
            var totalIngresos = resultado?.Ingresos ?? 0m;
            var primeraProxima = resultado?.Proxima;

            // Comprobación de versión antes de volcar cambios en colecciones y propiedades observables
            if (versionActual != _versionCarga) return;

            CitasDelDia.Clear();
            foreach (var item in items)
            {
                CitasDelDia.Add(item);
            }

            CantidadCitas = CitasDelDia.Count;
            EstaAgendaVacia = CantidadCitas == 0;
            IngresosDelDia = totalIngresos > 0 ? $"RD$ {totalIngresos:N0}" : "RD$ 0";
            MetaIngresos = string.Empty;
            TieneMetaIngresos = false;

            if (primeraProxima != null)
            {
                ProximaCita = primeraProxima;
                TieneProximaCita = true;
            }
            else
            {
                TieneProximaCita = false;
                ProximaCita = new CitaAgendaItem();
            }

            State = EstaAgendaVacia ? ViewState.Vacio : ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            if (versionActual != _versionCarga) return;
            Debug.WriteLine($"[AgendaDiaViewModel] Error al cargar agenda: {ex}");
            MensajeError = "No se pudieron cargar las citas de este día. Intenta de nuevo.";
            State = ViewState.Error;
        }
    }

    private void ActualizarTextoFecha()
    {
        var hoy = DateTime.Today;
        var diff = (FechaSeleccionada.Date - hoy).Days;
        EsDiaHoy = diff == 0;
        PuedeVolverAHoy = !EsDiaHoy;

        string prefijo = diff switch
        {
            0 => "Hoy",
            1 => "Mañana",
            -1 => "Ayer",
            _ => char.ToUpper(FechaSeleccionada.ToString("dddd", CulturaEs)[0]) + FechaSeleccionada.ToString("dddd", CulturaEs)[1..]
        };

        var fechaFormato = FechaSeleccionada.ToString("d 'de' MMMM", CulturaEs);
        FechaResumen = $"{prefijo} · {fechaFormato}";
    }

    private void ActualizarEstadoSinDatos()
    {
        CantidadCitas = 0;
        TieneProximaCita = false;
        ProximaCita = new CitaAgendaItem();
        EstaAgendaVacia = true;
        IngresosDelDia = "RD$ 0";
        MetaIngresos = string.Empty;
        TieneMetaIngresos = false;
    }
}

public partial class CitaAgendaItem : ObservableObject
{
    /// <summary>Id de la cita (para abrir su detalle e historial).</summary>
    public Guid CitaId { get; set; }

    public int ClienteId { get; set; }

    [ObservableProperty]
    private string hora = string.Empty;

    [ObservableProperty]
    private string periodo = string.Empty;

    [ObservableProperty]
    private string inicialesCliente = string.Empty;

    [ObservableProperty]
    private string nombreCliente = string.Empty;

    [ObservableProperty]
    private string servicio = string.Empty;

    [ObservableProperty]
    private string resumen = string.Empty;
}