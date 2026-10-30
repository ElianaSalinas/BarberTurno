using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

/// <summary>
/// Responsable: M10.
/// ViewModel de la Agenda del Barbero.
/// 
/// REGLAS ARQUITECTÓNICAS:
/// - Ningún dato hardcodeado: solo consume datos reales de SQLite e IAuthService.
/// - Si la base de datos está vacía, presenta de forma limpia el estado vacío.
/// - Calcula ingresos y turnos estrictamente sobre las citas existentes.
/// </summary>
public partial class AgendaDiaViewModel : BaseViewModel
{
    // Dependencias inyectadas mediante el contenedor de servicios
    private readonly IAuthService _authService;
    private readonly ICitaRepository _citaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly INavigationService _navigationService;

    // ════════════════════════════════════════════════════════════════════════════
    // PROPIEDADES OBSERVABLES (Datos reales)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>Fecha seleccionada actualmente para la consulta en SQLite.</summary>
    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    /// <summary>Nombre del barbero autenticado desde IAuthService.</summary>
    [ObservableProperty]
    private string nombreBarbero = string.Empty;

    /// <summary>Subtítulo dinámico: "Hoy tienes 0 citas · [Fecha de hoy]".</summary>
    [ObservableProperty]
    private string saludoSubtitulo = string.Empty;

    /// <summary>Total de citas reales registradas para la fecha seleccionada.</summary>
    [ObservableProperty]
    private int cantidadCitas;

    /// <summary>Cantidad de citas que aún están en estado Pendiente.</summary>
    [ObservableProperty]
    private string turnosPendientesTexto = "0 pendientes";

    /// <summary>Monto real acumulado de los servicios de las citas activas.</summary>
    [ObservableProperty]
    private string ingresosDelDia = "$0";

    /// <summary>Meta establecida de negocio.</summary>
    [ObservableProperty]
    private string metaIngresos = "Meta diaria";

    /// <summary>Cita más inmediata del día (si existe alguna).</summary>
    [ObservableProperty]
    private CitaAgendaItem proximaCita = new();

    /// <summary>Controla si se muestra o se oculta la tarjeta oscura Hero.</summary>
    [ObservableProperty]
    private bool tieneProximaCita;

    /// <summary>Controla la visibilidad del mensaje de agenda vacía.</summary>
    [ObservableProperty]
    private bool estaAgendaVacia = true;

    /// <summary>Colección observable enlazada a la interfaz.</summary>
    public ObservableCollection<CitaAgendaItem> CitasDelDia { get; } = new();

    // ════════════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ════════════════════════════════════════════════════════════════════════════
    public AgendaDiaViewModel(
        IAuthService authService,
        ICitaRepository citaRepository,
        IClienteRepository clienteRepository,
        IServicioRepository servicioRepository,
        INavigationService navigationService)
    {
        _authService = authService;
        _citaRepository = citaRepository;
        _clienteRepository = clienteRepository;
        _servicioRepository = servicioRepository;
        _navigationService = navigationService;

        Titulo = "Mi agenda";

        // Inicializar estado real sin datos
        EstablecerEstadoVacio();
    }

    /// <summary>
    /// Ciclo de vida: se dispara al mostrarse la pantalla.
    /// </summary>
    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = CargarAgendaAsync();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CARGA DE DATOS REALES DESDE SQLITE
    // ════════════════════════════════════════════════════════════════════════════
    [RelayCommand]
    public async Task CargarAgendaAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // 1. Obtener la sesión activa del barbero
            var sesion = _authService.SesionActual;
            NombreBarbero = !string.IsNullOrWhiteSpace(sesion?.Nombre) ? sesion.Nombre : "Barbero";
            int barberoId = sesion?.UsuarioId ?? 1;

            // 2. Consultar citas reales en SQLite para el barbero y día actual
            DateOnly diaConsulta = DateOnly.FromDateTime(FechaSeleccionada);
            List<Cita> citas = await _citaRepository.GetAgendaBarberoAsync(barberoId, diaConsulta);

            CitasDelDia.Clear();

            // 3. Si no hay citas, establecer el estado vacío real
            if (citas is null || citas.Count == 0)
            {
                EstablecerEstadoVacio();
                return;
            }

            // 4. Si hay citas, traer clientes y servicios relacionados
            var clienteIds = citas.Select(c => c.ClienteId).Distinct().ToList();
            List<Cliente> clientes = await _clienteRepository.GetByIdsAsync(clienteIds);
            var clientesDict = clientes.ToDictionary(c => c.Id);

            List<Servicio> servicios = await _servicioRepository.GetAllAsync();
            var serviciosDict = servicios.ToDictionary(s => s.Id);

            decimal totalIngresos = 0;
            int pendientes = 0;
            DateTime ahora = DateTime.Now;
            CitaAgendaItem? proxima = null;

            var citasOrdenadas = citas.OrderBy(c => c.Inicio).ToList();

            foreach (var cita in citasOrdenadas)
            {
                clientesDict.TryGetValue(cita.ClienteId, out var cliente);
                serviciosDict.TryGetValue(cita.ServicioId, out var servicio);

                string nombreCli = cliente?.Nombre ?? $"Cliente #{cita.ClienteId}";
                string nombreServ = servicio?.Nombre ?? "Servicio";
                int duracion = servicio?.DuracionMin ?? 30;
                decimal precio = servicio?.Precio ?? 0;

                if (cita.Estado == EstadoCita.Pendiente)
                    pendientes++;

                // Solo sumar ingresos de citas que no estén canceladas ni rechazadas
                if (cita.Estado != EstadoCita.Cancelada && cita.Estado != EstadoCita.Rechazada)
                    totalIngresos += precio;

                var item = new CitaAgendaItem
                {
                    Id = cita.Id,
                    Hora = cita.Inicio.ToLocalTime().ToString("h:mm"),
                    Periodo = cita.Inicio.ToLocalTime().ToString("tt", CultureInfo.InvariantCulture).ToUpper(),
                    NombreCliente = nombreCli,
                    InicialesCliente = ObtenerIniciales(nombreCli),
                    ServicioConDuracion = $"{nombreServ} · {duracion} min",
                    Servicio = nombreServ,
                    Precio = precio,
                    Estado = cita.Estado,
                    Resumen = $"{nombreCli} · {nombreServ}",
                    EsDestacada = false
                };

                // Identificar la próxima cita futura
                if (proxima is null && cita.Fin.ToLocalTime() >= ahora && cita.Estado != EstadoCita.Cancelada)
                {
                    proxima = item;
                    item.EsDestacada = true;
                }

                CitasDelDia.Add(item);
            }

            // 5. Asignar cálculos reales
            CantidadCitas = CitasDelDia.Count;
            TurnosPendientesTexto = $"{pendientes} pendientes";
            IngresosDelDia = totalIngresos.ToString("C0", CultureInfo.GetCultureInfo("es-DO"));
            EstaAgendaVacia = false;

            if (proxima is not null)
            {
                ProximaCita = proxima;
                TieneProximaCita = true;
            }
            else
            {
                TieneProximaCita = false;
            }

            ActualizarSubtituloFecha();
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
            EstablecerEstadoVacio();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Configura el estado inicial cuando la agenda no tiene citas en la base de datos.
    /// </summary>
    private void EstablecerEstadoVacio()
    {
        CantidadCitas = 0;
        TurnosPendientesTexto = "0 pendientes";
        IngresosDelDia = "$0";
        TieneProximaCita = false;
        EstaAgendaVacia = true;
        CitasDelDia.Clear();
        ActualizarSubtituloFecha();
    }

    private void ActualizarSubtituloFecha()
    {
        CultureInfo cultura = CultureInfo.GetCultureInfo("es-DO");
        string fechaTexto = FechaSeleccionada.ToString("dddd, d 'de' MMMM", cultura);
        fechaTexto = char.ToUpper(fechaTexto[0]) + fechaTexto[1..];
        SaludoSubtitulo = $"Hoy tienes {CantidadCitas} citas · {fechaTexto}";
    }

    [RelayCommand]
    public async Task VerDetalleCitaAsync(CitaAgendaItem? item)
    {
        if (item is null || item.Id == Guid.Empty) return;

        var parametros = new Dictionary<string, object>
        {
            { "citaId", item.Id }
        };

        await _navigationService.GoToAsync(Routes.DetalleCita, parametros);
    }

    private static string ObtenerIniciales(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "BT";
        var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1)
        {
            return partes[0].Length >= 2 ? partes[0][..2].ToUpperInvariant() : partes[0].ToUpperInvariant();
        }
        return $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
    }
}

public partial class CitaAgendaItem : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string hora = string.Empty;

    [ObservableProperty]
    private string periodo = string.Empty;

    [ObservableProperty]
    private string inicialesCliente = string.Empty;

    [ObservableProperty]
    private string nombreCliente = string.Empty;

    [ObservableProperty]
    private string servicioConDuracion = string.Empty;

    [ObservableProperty]
    private string servicio = string.Empty;

    [ObservableProperty]
    private decimal precio;

    [ObservableProperty]
    private string resumen = string.Empty;

    [ObservableProperty]
    private EstadoCita estado;

    [ObservableProperty]
    private bool esDestacada;
}