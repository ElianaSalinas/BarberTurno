using CommunityToolkit.Mvvm.ComponentModel;

// Responsable: M10.
// Estado de presentación de la pantalla DetalleCitaPage.
//
// No consulta repositorios todavía porque M02, M06 y M08 son responsables
// de implementar y registrar los servicios necesarios.
namespace BarberTurno.ViewModels;

public class DetalleCitaViewModel : BaseViewModel
{
    /*
        Indica si la pantalla recibió una cita real desde Agenda.

        False:
        - Se muestra el estado “Selecciona una cita”.
        - Se ocultan las tarjetas de detalle.

        True:
        - Se muestran cliente, fecha, hora, servicio y estado.
    */
    private bool tieneCitaSeleccionada;

    public bool TieneCitaSeleccionada
    {
        _citas = citas;
        _clientes = clientes;
        _servicios = servicios;
        _auth = auth;
        State = ViewState.Vacio; // hasta recibir una cita real: "Selecciona una cita"
    }

    /*
        ID real de la cita.

        Más adelante M01 enviará este valor mediante navegación y M02 permitirá
        buscarla con ICitaRepository.GetByIdAsync(CitaId).
    */
    private Guid? citaId;

    public Guid? CitaId
    {
        get => citaId;
        private set => SetProperty(ref citaId, value);
    }

    // Nombre real obtenido desde Cliente.Nombre.
    private string nombreCliente = string.Empty;

    public string NombreCliente
    {
        get => nombreCliente;
        private set => SetProperty(ref nombreCliente, value);
    }

    // Iniciales derivadas del nombre real del cliente.
    private string inicialesCliente = string.Empty;

    public string InicialesCliente
    {
        get => inicialesCliente;
        private set => SetProperty(ref inicialesCliente, value);
    }

    // Fecha formateada para la interfaz.
    private string fechaCita = string.Empty;

    public string FechaCita
    {
        get => fechaCita;
        private set => SetProperty(ref fechaCita, value);
    }

    // Hora formateada para la interfaz.
    private string horaCita = string.Empty;

    public string HoraCita
    {
        get => horaCita;
        private set => SetProperty(ref horaCita, value);
    }

    // Nombre real del servicio.
    private string servicio = string.Empty;

    public string Servicio
    {
        get => servicio;
        private set => SetProperty(ref servicio, value);
    }

    // Duración real, obtenida desde Servicio.DuracionMin.
    private string duracion = string.Empty;

    public string Duracion
    {
        get => duracion;
        private set => SetProperty(ref duracion, value);
    }

    // Precio real, obtenido desde Servicio.Precio.
    private string precioServicio = string.Empty;

    public string PrecioServicio
    {
        get => precioServicio;
        private set => SetProperty(ref precioServicio, value);
    }

    /*
        Estado de dominio de la cita:
        PendienteConfirmacion, Confirmada, Rechazada, Cancelada,
        Atendida o Ausente.

        M08 debe definir las transiciones válidas entre estados.
    */
    private string estadoCita = string.Empty;

    public string EstadoCita
    {
        get => estadoCita;
        private set => SetProperty(ref estadoCita, value);
    }

    // Teléfono real del cliente, si M02 lo tiene disponible.
    private string telefonoCliente = string.Empty;

    public string TelefonoCliente
    {
        get => telefonoCliente;
        private set => SetProperty(ref telefonoCliente, value);
    }

    /*
        Estado inicial seguro.

        No se cargan datos de demostración. La pantalla mostrará su estado vacío
        hasta que Agenda entregue un identificador de cita real.
    */
    public DetalleCitaViewModel()
    {
        LimpiarDetalle();
    }

    /*
        Limpia información previa antes de abrir otra cita.

        Cuando M01 defina la navegación y M02 entregue los repositorios,
        este método será llamado antes de cargar la nueva cita.
    */
    private void LimpiarDetalle()
    {
        _citaActual = null;
        CitaId = null;
        ClienteId = 0;
        NombreCliente = string.Empty;
        InicialesCliente = string.Empty;
        TelefonoCliente = string.Empty;
        FechaCita = string.Empty;
        HoraCita = string.Empty;
        Servicio = string.Empty;
        Duracion = string.Empty;
        PrecioServicio = string.Empty;
        EstadoCita = string.Empty;
        TelefonoCliente = string.Empty;
        TieneCitaSeleccionada = false;
        Historial.Clear();
        HayHistorial = false;
        SinHistorial = false;
    }
}