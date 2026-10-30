using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using BarberTurno.Data.Repositories;
using BarberTurno.Helpers;
using BarberTurno.Models;
using BarberTurno.Services.Auth;
using EstadoDeCita = BarberTurno.Models.EstadoCita;

// Responsable: M10.
// Detalle de una cita del barbero con el historial del cliente (sus visitas anteriores).
// Recibe el id de la cita por navegación (IQueryAttributable, clave "citaId") y lee todo de los repositorios locales,
// por lo que funciona sin conexión.
namespace BarberTurno.ViewModels;

/// <summary>Una visita anterior del cliente, lista para mostrar.</summary>
public sealed class VisitaClienteItem
{
    public string Fecha { get; init; } = string.Empty;
    public string Servicio { get; init; } = string.Empty;
    public string Precio { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
}

public partial class DetalleCitaViewModel : BaseViewModel, IQueryAttributable
{
    private static readonly CultureInfo CulturaEs = CultureInfo.GetCultureInfo("es");
    private const int MaximoVisitas = 20;

    private readonly ICitaRepository _citas;
    private readonly IClienteRepository _clientes;
    private readonly IServicioRepository _servicios;
    private readonly IAuthService _auth;

    public DetalleCitaViewModel(
        ICitaRepository citas,
        IClienteRepository clientes,
        IServicioRepository servicios,
        IAuthService auth)
    {
        _citas = citas;
        _clientes = clientes;
        _servicios = servicios;
        _auth = auth;
        State = ViewState.Vacio; // hasta recibir una cita real: "Selecciona una cita"
    }

    // Indica si la pantalla recibió una cita real desde la agenda o el calendario.
    [ObservableProperty]
    private bool tieneCitaSeleccionada;

    [ObservableProperty]
    private Guid? citaId;

    [ObservableProperty]
    private string nombreCliente = string.Empty;

    [ObservableProperty]
    private string inicialesCliente = string.Empty;

    [ObservableProperty]
    private string telefonoCliente = string.Empty;

    [ObservableProperty]
    private string fechaCita = string.Empty;

    [ObservableProperty]
    private string horaCita = string.Empty;

    [ObservableProperty]
    private string servicio = string.Empty;

    [ObservableProperty]
    private string duracion = string.Empty;

    [ObservableProperty]
    private string precioServicio = string.Empty;

    [ObservableProperty]
    private string estadoCita = string.Empty;

    [ObservableProperty]
    private bool hayHistorial;

    [ObservableProperty]
    private bool sinHistorial;

    public ObservableCollection<VisitaClienteItem> Historial { get; } = new();

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("citaId", out var valor))
            return;

        var id = valor switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => Guid.Empty
        };

        if (id != Guid.Empty)
            _ = CargarAsync(id);
    }

    private async Task CargarAsync(Guid id)
    {
        State = ViewState.Cargando;
        MensajeError = null;

        try
        {
            var cita = await _citas.GetByIdAsync(id);
            if (cita is null)
            {
                LimpiarDetalle();
                MensajeError = "No se encontró la cita. Vuelve a la agenda e inténtalo de nuevo.";
                State = ViewState.Error;
                return;
            }

            var cliente = await _clientes.GetByIdAsync(cita.ClienteId);
            var serv = await _servicios.GetByIdAsync(cita.ServicioId);

            var nombre = string.IsNullOrWhiteSpace(cliente?.Nombre) ? "Cliente" : cliente!.Nombre.Trim();
            var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            CitaId = cita.Id;
            NombreCliente = nombre;
            InicialesCliente = (partes.Length >= 2 ? $"{partes[0][0]}{partes[1][0]}" : nombre[..1]).ToUpperInvariant();
            TelefonoCliente = cliente?.Telefono ?? string.Empty;
            // Cita.Inicio ya está en hora local.
            FechaCita = cita.Inicio.ToString("dddd d 'de' MMMM", CulturaEs);
            HoraCita = cita.Inicio.ToString("hh:mm tt", CultureInfo.InvariantCulture);
            Servicio = serv?.Nombre ?? "Servicio";
            Duracion = serv is null ? string.Empty : $"{serv.DuracionMin} min";
            PrecioServicio = serv is null ? string.Empty : $"${serv.Precio:N0}";
            EstadoCita = TextoEstado(cita.Estado);
            TieneCitaSeleccionada = true;

            await CargarHistorialAsync(cita);
            State = ViewState.ConDatos;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[DetalleCitaViewModel] Error al cargar la cita: {ex}");
            MensajeError = "No se pudo cargar el detalle de la cita.";
            State = ViewState.Error;
        }
    }

    // Visitas anteriores del mismo cliente (con este barbero), de la más reciente a la más antigua.
    private async Task CargarHistorialAsync(Cita cita)
    {
        Historial.Clear();

        var previas = await _citas.GetHistorialClienteAsync(
            cita.ClienteId, _auth.SesionActual?.BarberoId, antesDe: cita.Inicio, skip: 0, take: MaximoVisitas);

        var catalogo = new Dictionary<int, Servicio?>();
        foreach (var previa in previas.Where(c => c.Id != cita.Id
                     && c.Estado is not (EstadoDeCita.Cancelada or EstadoDeCita.Rechazada)))
        {
            if (!catalogo.TryGetValue(previa.ServicioId, out var serv))
                catalogo[previa.ServicioId] = serv = await _servicios.GetByIdAsync(previa.ServicioId);

            Historial.Add(new VisitaClienteItem
            {
                Fecha = previa.Inicio.ToString("ddd d MMM yyyy · hh:mm tt", CulturaEs),
                Servicio = serv?.Nombre ?? "Servicio",
                Precio = serv is null ? string.Empty : $"${serv.Precio:N0}",
                Estado = TextoEstado(previa.Estado)
            });
        }

        HayHistorial = Historial.Count > 0;
        SinHistorial = Historial.Count == 0;
    }

    private void LimpiarDetalle()
    {
        CitaId = null;
        NombreCliente = string.Empty;
        InicialesCliente = string.Empty;
        TelefonoCliente = string.Empty;
        FechaCita = string.Empty;
        HoraCita = string.Empty;
        Servicio = string.Empty;
        Duracion = string.Empty;
        PrecioServicio = string.Empty;
        EstadoCita = string.Empty;
        TieneCitaSeleccionada = false;
        Historial.Clear();
        HayHistorial = false;
        SinHistorial = false;
    }

    private static string TextoEstado(EstadoDeCita estado) => estado switch
    {
        EstadoDeCita.Pendiente => "Pendiente de confirmación",
        EstadoDeCita.Confirmada => "Confirmada",
        EstadoDeCita.Rechazada => "Rechazada",
        EstadoDeCita.Cancelada => "Cancelada",
        EstadoDeCita.Atendida => "Atendida",
        EstadoDeCita.Ausente => "Ausente",
        _ => estado.ToString()
    };
}
