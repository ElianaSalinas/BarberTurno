using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

// Responsable: M10.
// Cuando M02 y M06 terminen sus contratos, aquí se conectarán
// IAuthService, ICitaRepository, IClienteRepository e IServicioRepository.
namespace BarberTurno.ViewModels;

public partial class AgendaDiaViewModel : BaseViewModel
{
   
    // Nombre real del barbero. M06 lo proveerá desde SesionUsuario.Nombre.
    [ObservableProperty]
    public partial string NombreBarbero { get; set; } = string.Empty;

    
    [ObservableProperty]
    public partial string FechaResumen { get; set; } = string.Empty;

    /*
        Estado inicial real: si no se han cargado citas desde el repositorio,
        la cantidad es cero. No representa datos de demostración.
    */
    [ObservableProperty]
    public partial int CantidadCitas { get; set; }

    /*
        No asignamos "$0" ni una meta ficticia.
        Estas propiedades permanecen vacías hasta que se calculen con
        precios reales de Servicio entregados por M02.
    */
    [ObservableProperty]
    public partial string IngresosDelDia { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MetaIngresos { get; set; } = string.Empty;

 
    [ObservableProperty]
    public partial CitaAgendaItem ProximaCita { get; set; } = new();

    /*
    Estado que controla la visibilidad de la tarjeta “Próxima cita”.

    Usamos SetProperty, heredado de ObservableObject mediante BaseViewModel.
    SetProperty notifica a XAML cuando el valor cambia, por eso
    IsVisible="{Binding TieneProximaCita}" se actualiza automáticamente.

    No usamos partial aquí para evitar el error del generador.
*/
    private bool tieneProximaCita;

    public bool TieneProximaCita
    {
        get => tieneProximaCita;

        private set => SetProperty(ref tieneProximaCita, value);
    }

    /*
    Indica si la agenda no contiene citas.

    La usamos para mostrar un estado vacío diseñado por M10.
    No es un dato inventado: se calcula desde CitasDelDia.
*/
    private bool estaAgendaVacia;

    public bool EstaAgendaVacia
    {
        get => estaAgendaVacia;

        private set => SetProperty(ref estaAgendaVacia, value);
    }

    /*
        Lista enlazada a CollectionView.

        ObservableCollection se usa en vez de List porque avisa a la interfaz
        cuando se agregan o eliminan citas reales.
    */
    public ObservableCollection<CitaAgendaItem> CitasDelDia { get; } = new();

    public AgendaDiaViewModel()
    {
        // Se muestra la fecha real del dispositivo en español.
        FechaResumen = CrearResumenDeFecha(DateTime.Today);

        // No se agregan datos de demostración.
        ActualizarEstadoSinDatos();
    }

    /*
        Prepara la pantalla antes de que M02/M06 estén listos.

        Cuando carguemos repositorios reales, este método dejará de ser el
        estado principal y será sustituido por CargarAgendaAsync().
    */
    private void ActualizarEstadoSinDatos()
    {
        CantidadCitas = CitasDelDia.Count;

        // La próxima cita no existe mientras no haya datos reales.
        TieneProximaCita = false;
        ProximaCita = new CitaAgendaItem();

        // Estado real de la colección actual.
        EstaAgendaVacia = CitasDelDia.Count == 0;

        // No hay ingresos o metas reales todavía.
        IngresosDelDia = string.Empty;
        MetaIngresos = string.Empty;
    }

    /*
        Convierte la fecha del dispositivo a texto visible pariguayo.

        Ejemplo: “Hoy · jueves, 29 de octubre”.
    */
    private static string CrearResumenDeFecha(DateTime fecha)
    {
        CultureInfo cultura = CultureInfo.GetCultureInfo("es-DO");

        string fechaFormateada = fecha.ToString(
            "dddd, d 'de' MMMM",
            cultura);

        return $"Hoy · {fechaFormateada}";
    }
}

/*
    Modelo exclusivo de presentación para una fila de la agenda.

    No sustituye Models/Cita.cs de M02:
    - Cita conserva IDs, Inicio, Fin y Estado.
    - CitaAgendaItem contiene textos ya listos para dibujar en XAML.

    Más adelante, AgendaDiaViewModel convertirá:
    Cita + Cliente + Servicio -> CitaAgendaItem.
*/
public partial class CitaAgendaItem : ObservableObject
{
    // Hora visible de la cita, por ejemplo: “10:00”.
    [ObservableProperty]
    public partial string Hora { get; set; } = string.Empty;

    // Periodo visible, por ejemplo: “AM” o “PM”.
    [ObservableProperty]
    public partial string Periodo { get; set; } = string.Empty;

    // Iniciales calculadas desde Cliente.Nombre.
    [ObservableProperty]
    public partial string InicialesCliente { get; set; } = string.Empty;

    // Nombre real del cliente.
    [ObservableProperty]
    public partial string NombreCliente { get; set; } = string.Empty;

    // Servicio y duración reales.
    [ObservableProperty]
    public partial string Servicio { get; set; } = string.Empty;

    // Resumen de la próxima cita: cliente + servicio real.
    [ObservableProperty]
    public partial string Resumen { get; set; } = string.Empty;
}