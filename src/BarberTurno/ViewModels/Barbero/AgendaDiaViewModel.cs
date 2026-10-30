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

    private int _barberoId;
    private bool _cargando;

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
    }

    [RelayCommand]
    public async Task DiaAnteriorAsync()
    {
        FechaSeleccionada = FechaSeleccionada.AddDays(-1);
        ActualizarTextoFecha();
        await CargarAgendaAsync();
    }

    [RelayCommand]
    public async Task DiaSiguienteAsync()
    {
        FechaSeleccionada = FechaSeleccionada.AddDays(1);
        ActualizarTextoFecha();
        await CargarAgendaAsync();
    }

    [RelayCommand]
    public async Task HoyAsync()
    {
        if (FechaSeleccionada.Date == DateTime.Today) return;
        FechaSeleccionada = DateTime.Today;
        ActualizarTextoFecha();
        await CargarAgendaAsync();
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
            await CargarAgendaAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public async Task CargarAgendaAsync()
    {
        if (_cargando) return;
        _cargando = true;
        State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var sesion = _auth?.SesionActual;
            NombreBarbero = sesion?.Nombre ?? "Barbero";

            // Resolver el BarberoId local
            if (_barberoId == 0 && _barberoRepo != null)
            {
                var barberos = await _barberoRepo.GetAllAsync();
                var barbero = barberos.FirstOrDefault(b =>
                    !string.IsNullOrWhiteSpace(sesion?.Nombre) &&
                    b.Nombre.Equals(sesion.Nombre, StringComparison.OrdinalIgnoreCase))
                    ?? barberos.FirstOrDefault();

                if (barbero != null)
                    _barberoId = barbero.Id;
            }

            if (_citaRepo == null || _barberoId == 0)
            {
                ActualizarEstadoSinDatos();
                State = ViewState.ConDatos;
                return;
            }

            // Consultar citas del día seleccionado
            var dia = DateOnly.FromDateTime(FechaSeleccionada);
            var citas = await _citaRepo.GetAgendaBarberoAsync(_barberoId, dia);

            CitasDelDia.Clear();
            decimal totalIngresos = 0m;
            CitaAgendaItem? primeraProxima = null;
            var ahora = DateTime.Now;

            foreach (var cita in citas.OrderBy(c => c.Inicio))
            {
                var cliente = _clienteRepo != null ? await _clienteRepo.GetByIdAsync(cita.ClienteId) : null;
                var servicio = _servicioRepo != null ? await _servicioRepo.GetByIdAsync(cita.ServicioId) : null;

                var nombreCli = cliente?.Nombre ?? "Cliente";
                var nombreServ = servicio?.Nombre ?? "Servicio";
                var duracion = servicio?.DuracionMin ?? 30;
                var precio = servicio?.Precio ?? 0m;

                if (cita.Estado != EstadoCita.Cancelada && cita.Estado != EstadoCita.Rechazada)
                {
                    totalIngresos += precio;
                }

                // Generar iniciales del cliente
                var partes = nombreCli.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var iniciales = partes.Length >= 2
                    ? $"{partes[0][..1]}{partes[1][..1]}".ToUpperInvariant()
                    : (nombreCli.Length > 0 ? nombreCli[..1].ToUpperInvariant() : "CL");

                var item = new CitaAgendaItem
                {
                    Hora = cita.Inicio.ToString("hh:mm", CultureInfo.InvariantCulture),
                    Periodo = cita.Inicio.ToString("tt", CultureInfo.InvariantCulture).ToUpperInvariant(),
                    NombreCliente = nombreCli,
                    InicialesCliente = iniciales,
                    Servicio = $"{nombreServ} · {duracion} min",
                    Resumen = $"{nombreCli} · {nombreServ}"
                };

                CitasDelDia.Add(item);

                // Si es hoy y la cita aún no ha pasado, buscar la próxima cita más cercana
                if (FechaSeleccionada.Date == ahora.Date && cita.Inicio >= ahora && primeraProxima == null
                    && cita.Estado is EstadoCita.Confirmada or EstadoCita.Pendiente)
                {
                    primeraProxima = item;
                }
            }

            CantidadCitas = CitasDelDia.Count;
            EstaAgendaVacia = CantidadCitas == 0;
            IngresosDelDia = totalIngresos > 0 ? $"RD$ {totalIngresos:N0}" : "RD$ 0";
            MetaIngresos = "Meta: RD$ 4,000";

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
            Debug.WriteLine($"[AgendaDiaViewModel] Error al cargar agenda: {ex}");
            MensajeError = "No se pudieron cargar las citas de este día. Intenta de nuevo.";
            State = ViewState.Error;
        }
        finally
        {
            _cargando = false;
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
        MetaIngresos = "Meta: RD$ 4,000";
    }
}

public partial class CitaAgendaItem : ObservableObject
{
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