using BarberTurno.Helpers;
using BarberTurno.Views;

// Responsable: M01
// Registro de rutas de páginas apiladas (solo navegación, sin lógica de negocio).
namespace BarberTurno;

/// <summary>Registra las rutas Shell que conectan las pantallas del flujo de reservas.</summary>
public partial class AppShell : Shell
{
    /// <summary>Inicializa el menú definido en XAML y las rutas de navegación no visibles.</summary>
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("registro", typeof(RegistroPage));
        Routing.RegisterRoute(Routes.Servicios, typeof(ServiciosPage));
        Routing.RegisterRoute(Routes.SeleccionHora, typeof(SeleccionHoraPage));
        Routing.RegisterRoute(Routes.ConfirmarReserva, typeof(ConfirmarReservaPage));
        Routing.RegisterRoute(Routes.HistorialCliente, typeof(HistorialClientePage));
        Routing.RegisterRoute(Routes.DetalleCita, typeof(DetalleCitaPage));
    }
}
