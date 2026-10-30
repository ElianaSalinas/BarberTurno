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
/// OPTIMIZACIONES:
/// - Cero datos simulados.
/// - Carga asíncrona segura sin bloqueos de interfaz.
/// </summary>
public partial class AgendaDiaViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ICitaRepository _citaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    [ObservableProperty]
    private string nombreBarbero = string.Empty;

    [ObservableProperty]
    private string saludoSubtitulo = string.Empty;

    [ObservableProperty]
    private int cantidadCitas;

    [ObservableProperty]
    private string turnosPendientesTexto = "0 pendientes";

    [ObservableProperty]
    private string ingresosDelDia = "$0";

    [ObservableProperty]
    private string metaIngresos = "Meta diaria";

    [ObservableProperty]
    private CitaAgendaItem proximaCita = new();

    [ObservableProperty]
    private bool tieneProximaCita;

    [ObservableProperty]
    private bool estaAgendaVacia = true;

    public ObservableCollection<CitaAgendaItem> CitasDelDia { get; } = new();

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
        EstablecerEstadoVacio();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await CargarAgendaAsync();
        });
    }

    [RelayCommand]
    public async Task CargarAgendaAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var sesion = _authService.SesionActual;
            NombreBarbero = !string.IsNullOrWhiteSpace(sesion?.Nombre) ? sesion.Nombre : "Barbero";
            int barberoId = sesion?.UsuarioId ?? 1;

            DateOnly diaConsulta = DateOnly.FromDateTime(FechaSeleccionada);

            List<Cita> citas = await Task.Run(async () =>
            {
                return await _citaRepository.GetAgendaBarberoAsync(barberoId, diaConsulta);
            });

            CitasDelDia.Clear();

            if (citas is null || citas.Count == 0)
            {
                EstablecerEstadoVacio();
                return;
            }

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

                if (proxima is null && cita.Fin.ToLocalTime() >= ahora && cita.Estado != EstadoCita.Cancelada)
                {
                    proxima = item;
                    item.EsDestacada = true;
                }

                CitasDelDia.Add(item);
            }

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