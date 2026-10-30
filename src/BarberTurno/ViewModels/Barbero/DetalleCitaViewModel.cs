using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Navigation;

// Responsable: M10.
// Estado de presentación de la pantalla DetalleCitaPage.
// Conectado con SQLite (ICitaRepository, IClienteRepository, IServicioRepository).
namespace BarberTurno.ViewModels;

[QueryProperty(nameof(CitaIdParametro), "citaId")]
public partial class DetalleCitaViewModel : BaseViewModel
{
    // Dependencias inyectadas de repositorios y navegación
    private readonly ICitaRepository _citaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly INavigationService _navigationService;

    // Cita en memoria
    private Cita? _citaActual;

    // ════════════════════════════════════════════════════════════════════════════
    // PROPIEDADES OBSERVABLES
    // ════════════════════════════════════════════════════════════════════════════

    private string _citaIdParametro = string.Empty;
    public string CitaIdParametro
    {
        get => _citaIdParametro;
        set
        {
            if (SetProperty(ref _citaIdParametro, value) && Guid.TryParse(value, out Guid id))
            {
                _ = CargarCitaPorIdAsync(id);
            }
        }
    }

    private bool tieneCitaSeleccionada;
    public bool TieneCitaSeleccionada
    {
        get => tieneCitaSeleccionada;
        private set => SetProperty(ref tieneCitaSeleccionada, value);
    }

    private Guid? citaId;
    public Guid? CitaId
    {
        get => citaId;
        private set => SetProperty(ref citaId, value);
    }

    private int clienteId;
    public int ClienteId
    {
        get => clienteId;
        private set => SetProperty(ref clienteId, value);
    }

    private string nombreCliente = string.Empty;
    public string NombreCliente
    {
        get => nombreCliente;
        private set => SetProperty(ref nombreCliente, value);
    }

    private string inicialesCliente = string.Empty;
    public string InicialesCliente
    {
        get => inicialesCliente;
        private set => SetProperty(ref inicialesCliente, value);
    }

    private string fechaCita = string.Empty;
    public string FechaCita
    {
        get => fechaCita;
        private set => SetProperty(ref fechaCita, value);
    }

    private string horaCita = string.Empty;
    public string HoraCita
    {
        get => horaCita;
        private set => SetProperty(ref horaCita, value);
    }

    private string servicio = string.Empty;
    public string Servicio
    {
        get => servicio;
        private set => SetProperty(ref servicio, value);
    }

    private string duracion = string.Empty;
    public string Duracion
    {
        get => duracion;
        private set => SetProperty(ref duracion, value);
    }

    private string precioServicio = string.Empty;
    public string PrecioServicio
    {
        get => precioServicio;
        private set => SetProperty(ref precioServicio, value);
    }

    private string estadoCita = string.Empty;
    public string EstadoCita
    {
        get => estadoCita;
        private set => SetProperty(ref estadoCita, value);
    }

    [ObservableProperty]
    private EstadoCita estadoCitaEnum;

    private string telefonoCliente = string.Empty;
    public string TelefonoCliente
    {
        get => telefonoCliente;
        private set => SetProperty(ref telefonoCliente, value);
    }

    [ObservableProperty]
    private bool puedeModificarEstado;

    // ════════════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ════════════════════════════════════════════════════════════════════════════
    public DetalleCitaViewModel(
        ICitaRepository citaRepository,
        IClienteRepository clienteRepository,
        IServicioRepository servicioRepository,
        INavigationService navigationService)
    {
        _citaRepository = citaRepository;
        _clienteRepository = clienteRepository;
        _servicioRepository = servicioRepository;
        _navigationService = navigationService;

        Titulo = "Detalle de Cita";
        LimpiarDetalle();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CARGA REAL DESDE SQLITE
    // ════════════════════════════════════════════════════════════════════════════
    public async Task CargarCitaPorIdAsync(Guid id)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            _citaActual = await _citaRepository.GetByIdAsync(id);
            if (_citaActual is null)
            {
                LimpiarDetalle();
                return;
            }

            var cliente = await _clienteRepository.GetByIdAsync(_citaActual.ClienteId);
            var serv = await _servicioRepository.GetByIdAsync(_citaActual.ServicioId);

            CitaId = _citaActual.Id;
            ClienteId = _citaActual.ClienteId;
            NombreCliente = cliente?.Nombre ?? $"Cliente #{_citaActual.ClienteId}";
            InicialesCliente = ObtenerIniciales(NombreCliente);
            TelefonoCliente = !string.IsNullOrWhiteSpace(cliente?.Telefono)
                ? cliente.Telefono
                : "Sin teléfono registrado";

            DateTime inicioLocal = _citaActual.Inicio.ToLocalTime();
            DateTime finLocal = _citaActual.Fin.ToLocalTime();

            CultureInfo cultura = CultureInfo.GetCultureInfo("es-DO");
            string formatoFecha = inicioLocal.ToString("dddd, d 'de' MMMM", cultura);
            FechaCita = char.ToUpper(formatoFecha[0]) + formatoFecha[1..];
            HoraCita = $"{inicioLocal:hh:mm tt} - {finLocal:hh:mm tt}";

            Servicio = serv?.Nombre ?? "Servicio general";
            int duracionMin = serv?.DuracionMin ?? (int)(finLocal - inicioLocal).TotalMinutes;
            Duracion = $"{duracionMin} min";

            decimal precio = serv?.Precio ?? 0;
            PrecioServicio = precio.ToString("C0", cultura);

            EstadoCitaEnum = _citaActual.Estado;
            EstadoCita = _citaActual.Estado.ToString();

            PuedeModificarEstado = _citaActual.Estado != Models.EstadoCita.Cancelada &&
                                   _citaActual.Estado != Models.EstadoCita.Rechazada;

            TieneCitaSeleccionada = true;
        }
        catch (Exception ex)
        {
            await MostrarAlertaAsync("Error", $"No se pudo consultar la cita: {ex.Message}");
            LimpiarDetalle();
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ACCIONES DEL BARBERO
    // ════════════════════════════════════════════════════════════════════════════

    [RelayCommand]
    public async Task MarcarAtendidaAsync()
    {
        if (_citaActual is null) return;

        bool confirmar = await MostrarConfirmacionAsync(
            "Confirmar",
            $"¿Deseas marcar la cita de {NombreCliente} como Atendida?");

        if (!confirmar) return;

        try
        {
            bool actualizado = await _citaRepository.ActualizarEstadoAsync(_citaActual.Id, Models.EstadoCita.Atendida);
            if (actualizado)
            {
                EstadoCitaEnum = Models.EstadoCita.Atendida;
                EstadoCita = Models.EstadoCita.Atendida.ToString();
                await MostrarAlertaAsync("Completado", "Cita marcada como Atendida.");
            }
        }
        catch (Exception ex)
        {
            await MostrarAlertaAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    public async Task MarcarAusenteAsync()
    {
        if (_citaActual is null) return;

        bool confirmar = await MostrarConfirmacionAsync(
            "Cliente Ausente",
            $"¿Confirmas que {NombreCliente} no asistió al turno?");

        if (!confirmar) return;

        try
        {
            bool actualizado = await _citaRepository.ActualizarEstadoAsync(_citaActual.Id, Models.EstadoCita.Ausente);
            if (actualizado)
            {
                EstadoCitaEnum = Models.EstadoCita.Ausente;
                EstadoCita = Models.EstadoCita.Ausente.ToString();
                await MostrarAlertaAsync("Actualizado", "La cita quedó marcada como Ausente.");
            }
        }
        catch (Exception ex)
        {
            await MostrarAlertaAsync("Error", ex.Message);
        }
    }

    [RelayCommand]
    public async Task VerHistorialClienteAsync()
    {
        if (ClienteId <= 0) return;

        var parametros = new Dictionary<string, object>
        {
            { "clienteId", ClienteId }
        };

        await _navigationService.GoToAsync(Routes.HistorialCliente, parametros);
    }

    [RelayCommand]
    public async Task VolverAsync()
    {
        await _navigationService.GoBackAsync();
    }

    private void LimpiarDetalle()
    {
        _citaActual = null;
        CitaId = null;
        ClienteId = 0;
        NombreCliente = string.Empty;
        InicialesCliente = string.Empty;
        FechaCita = string.Empty;
        HoraCita = string.Empty;
        Servicio = string.Empty;
        Duracion = string.Empty;
        PrecioServicio = string.Empty;
        EstadoCita = string.Empty;
        EstadoCitaEnum = Models.EstadoCita.Pendiente;
        TelefonoCliente = string.Empty;
        PuedeModificarEstado = false;
        TieneCitaSeleccionada = false;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // HELPERS NATIVOS DE DIÁLOGO (Garantizan cero errores CS1061)
    // ════════════════════════════════════════════════════════════════════════════
    private static async Task MostrarAlertaAsync(string titulo, string mensaje)
    {
        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page is { } page)
        {
            await page.DisplayAlertAsync(titulo, mensaje, "Aceptar");
        }
    }

    private static async Task<bool> MostrarConfirmacionAsync(string titulo, string mensaje)
    {
        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page is { } page)
        {
            return await page.DisplayAlertAsync(titulo, mensaje, "Aceptar", "Cancelar");
        }
        return false;
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