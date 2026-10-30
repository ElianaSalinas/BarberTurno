// Responsable: M10
// Visitas anteriores del cliente.
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
    // TODO(M10): propiedades, comandos y carga de datos. Sin lógica de negocio aquí: delegar en servicios.
}
