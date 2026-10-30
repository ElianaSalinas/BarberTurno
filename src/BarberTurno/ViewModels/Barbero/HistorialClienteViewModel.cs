using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

// Responsable: M10.

// Los datos reales llegarán después desde:
// - ICitaRepository.GetHistorialClienteAsync.
// - IClienteRepository.GetByIdAsync.
// - IAuthService.SesionActual.
namespace BarberTurno.ViewModels;

public class HistorialClienteViewModel : BaseViewModel
{
    /*
        Controla si la pantalla recibió un cliente real desde DetalleCitaPage.

        False: muestra “Selecciona un cliente”.
        True: muestra la cabecera del cliente y sus visitas anteriores.
    */
    private bool tieneClienteSeleccionado;

    public bool TieneClienteSeleccionado
    {
        get => tieneClienteSeleccionado;
        private set => SetProperty(ref tieneClienteSeleccionado, value);
    }

    /*
        Controla el estado “sin visitas”.

        Solo debe ser true cuando sí existe un cliente seleccionado,
        pero no hay citas anteriores registradas.
    */
    private bool estaHistorialVacio;

    public bool EstaHistorialVacio
    {
        get => estaHistorialVacio;
        private set => SetProperty(ref estaHistorialVacio, value);
    }

    // ID real del cliente. Se recibirá mediante navegación más adelante.
    private int? clienteId;

    public int? ClienteId
    {
        get => clienteId;
        private set => SetProperty(ref clienteId, value);
    }

    // Datos reales del cliente.
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

    private string telefonoCliente = string.Empty;

    public string TelefonoCliente
    {
        get => telefonoCliente;
        private set => SetProperty(ref telefonoCliente, value);
    }

    /*
        Total de visitas cargadas desde el repositorio.

        Cero es el estado inicial real de una colección aún no cargada;
        no representa un dato de demostración.
    */
    private int totalVisitas;

    public int TotalVisitas
    {
        get => totalVisitas;
        private set => SetProperty(ref totalVisitas, value);
    }

    /*
        Lista enlazada al CollectionView.

        ObservableCollection notifica a la vista al agregar, eliminar
        o reemplazar visitas reales.
    */
    public ObservableCollection<HistorialVisitaItem> Visitas { get; } = new();

    public HistorialClienteViewModel()
    {
        LimpiarHistorial();
    }

    /*
        Restablece la pantalla a un estado seguro.

        Más adelante, antes de buscar un cliente diferente, este método
        limpia los datos anteriores para evitar mostrar información vieja.
    */
    private void LimpiarHistorial()
    {
        ClienteId = null;
        NombreCliente = string.Empty;
        InicialesCliente = string.Empty;
        TelefonoCliente = string.Empty;
        TotalVisitas = 0;

        Visitas.Clear();

        TieneClienteSeleccionado = false;
        EstaHistorialVacio = false;
    }
}

/*
    Modelo de presentación para una visita anterior.

    No reemplaza Models/Cita.cs de M02. Esta clase solamente contiene
    texto listo para dibujar en la interfaz de HistorialClientePage.
*/
public class HistorialVisitaItem
{
    // Ejemplo futuro: “18 JUL”.
    public string FechaCorta { get; set; } = string.Empty;

    // Ejemplo futuro: “Jueves, 18 de julio”.
    public string FechaCompleta { get; set; } = string.Empty;

    // Hora formateada para presentación.
    public string Hora { get; set; } = string.Empty;

    // Servicio real asociado a la cita.
    public string Servicio { get; set; } = string.Empty;

    // Duración real obtenida desde Servicio.DuracionMin.
    public string Duracion { get; set; } = string.Empty;

    // Estado real: Atendida, Ausente, Cancelada, etc.
    public string Estado { get; set; } = string.Empty;

    // Precio real obtenido desde Servicio.Precio.
    public string Precio { get; set; } = string.Empty;
}