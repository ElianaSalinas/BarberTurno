using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Navigation;

namespace BarberTurno.ViewModels;

/// <summary>
/// Responsable: M10.
/// ViewModel de la pantalla de Historial de Visitas del Cliente.
/// 
/// ¿QUÉ HACE?
/// - Recibe el "clienteId" desde DetalleCitaPage mediante navegación Shell.
/// - Consulta las visitas pasadas del cliente en SQLite con ICitaRepository.GetHistorialClienteAsync.
/// - Cruza los datos con IServicioRepository para conocer qué se realizó en cada visita.
/// - Calcula el resumen del cliente: total de visitas y monto total invertido en la barbería.
/// - Si no tiene historial, presenta limpiamente el estado vacío (cero datos inventados).
/// </summary>
[QueryProperty(nameof(ClienteIdParametro), "clienteId")]
public partial class HistorialClienteViewModel : BaseViewModel
{
    // Dependencias inyectadas
    private readonly ICitaRepository _citaRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly INavigationService _navigationService;

    // ════════════════════════════════════════════════════════════════════════════
    // PROPIEDADES OBSERVABLES (Datos reales del cliente)
    // ════════════════════════════════════════════════════════════════════════════

    private string _clienteIdParametro = string.Empty;
    public string ClienteIdParametro
    {
        get => _clienteIdParametro;
        set
        {
            if (SetProperty(ref _clienteIdParametro, value) && int.TryParse(value, out int id))
            {
                _ = CargarHistorialAsync(id);
            }
        }
    }

    /// <summary>Nombre del cliente en consulta.</summary>
    [ObservableProperty]
    private string nombreCliente = string.Empty;

    /// <summary>Iniciales para el avatar.</summary>
    [ObservableProperty]
    private string inicialesCliente = string.Empty;

    /// <summary>Teléfono del cliente.</summary>
    [ObservableProperty]
    private string telefonoCliente = string.Empty;

    /// <summary>Total de citas históricas que ha tenido el cliente.</summary>
    [ObservableProperty]
    private int totalVisitas;

    /// <summary>Total acumulado gastado por el cliente en la barbería.</summary>
    [ObservableProperty]
    private string totalInvertido = "$0";

    /// <summary>Indica si el cliente no tiene citas pasadas.</summary>
    [ObservableProperty]
    private bool historialVacio = true;

    /// <summary>Colección observable de visitas para el listado.</summary>
    public ObservableCollection<VisitaHistorialItem> Visitas { get; } = new();

    // ════════════════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ════════════════════════════════════════════════════════════════════════════
    public HistorialClienteViewModel(
        ICitaRepository citaRepository,
        IClienteRepository clienteRepository,
        IServicioRepository servicioRepository,
        INavigationService navigationService)
    {
        _citaRepository = citaRepository;
        _clienteRepository = clienteRepository;
        _servicioRepository = servicioRepository;
        _navigationService = navigationService;

        Titulo = "Historial del Cliente";
    }

    // ════════════════════════════════════════════════════════════════════════════
    // CARGA REAL DESDE SQLITE
    // ════════════════════════════════════════════════════════════════════════════
    public async Task CargarHistorialAsync(int idCliente)
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Visitas.Clear();

            // 1. Obtener datos del cliente
            var cliente = await _clienteRepository.GetByIdAsync(idCliente);
            NombreCliente = cliente?.Nombre ?? $"Cliente #{idCliente}";
            InicialesCliente = ObtenerIniciales(NombreCliente);
            TelefonoCliente = !string.IsNullOrWhiteSpace(cliente?.Telefono)
                ? cliente.Telefono
                : "Sin teléfono registrado";

            // 2. Consultar citas pasadas del cliente en SQLite (ordenadas descendente)
            List<Cita> citas = await _citaRepository.GetHistorialClienteAsync(idCliente);

            if (citas is null || citas.Count == 0)
            {
                TotalVisitas = 0;
                TotalInvertido = "$0";
                HistorialVacio = true;
                return;
            }

            // 3. Traer catálogo de servicios para mapear nombres y precios
            List<Servicio> servicios = await _servicioRepository.GetAllAsync();
            var serviciosDict = servicios.ToDictionary(s => s.Id);

            decimal sumaInvertido = 0;
            CultureInfo cultura = CultureInfo.GetCultureInfo("es-DO");

            foreach (var cita in citas)
            {
                serviciosDict.TryGetValue(cita.ServicioId, out var serv);

                string nomServ = serv?.Nombre ?? "Servicio";
                decimal precio = serv?.Precio ?? 0;

                // Solo sumar ingresos de citas que fueron efectivamente atendidas o confirmadas
                if (cita.Estado == EstadoCita.Atendida || cita.Estado == EstadoCita.Confirmada)
                {
                    sumaInvertido += precio;
                }

                DateTime fechaLocal = cita.Inicio.ToLocalTime();
                string fechaFormateada = fechaLocal.ToString("d 'de' MMMM, yyyy", cultura);

                Visitas.Add(new VisitaHistorialItem
                {
                    Id = cita.Id,
                    Fecha = fechaFormateada,
                    Hora = fechaLocal.ToString("hh:mm tt"),
                    Servicio = nomServ,
                    Precio = precio.ToString("C0", cultura),
                    Estado = cita.Estado,
                    EstadoTexto = cita.Estado.ToString()
                });
            }

            TotalVisitas = Visitas.Count;
            TotalInvertido = sumaInvertido.ToString("C0", cultura);
            HistorialVacio = Visitas.Count == 0;
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
            HistorialVacio = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task VolverAsync()
    {
        await _navigationService.GoBackAsync();
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

/// <summary>
/// Modelo de presentación para cada fila de visita pasada en el historial.
/// </summary>
public partial class VisitaHistorialItem : ObservableObject
{
    [ObservableProperty]
    private Guid id;

    [ObservableProperty]
    private string fecha = string.Empty;

    [ObservableProperty]
    private string hora = string.Empty;

    [ObservableProperty]
    private string servicio = string.Empty;

    [ObservableProperty]
    private string precio = string.Empty;

    [ObservableProperty]
    private EstadoCita estado;

    [ObservableProperty]
    private string estadoTexto = string.Empty;
}