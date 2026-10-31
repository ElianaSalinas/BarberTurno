// Responsable: M10 / M04
// Agenda diaria del barbero: citas programadas, métricas e historial por fecha con navegación de días.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

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
   
    // Nombre real del barbero. M06 lo proveerá desde SesionUsuario.Nombre.
    [ObservableProperty]
    public partial string NombreBarbero { get; set; } = string.Empty;

    
    [ObservableProperty]
    public partial string FechaResumen { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int CantidadCitas { get; set; }

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
    public Guid CitaId { get; set; }
    public int ClienteId { get; set; }

    // Hora visible de la cita, por ejemplo: “10:00”.
    [ObservableProperty]
    public partial string Hora { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Periodo { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string InicialesCliente { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NombreCliente { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Servicio { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Resumen { get; set; } = string.Empty;
}
