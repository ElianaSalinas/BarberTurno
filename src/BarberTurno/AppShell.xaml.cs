using BarberTurno.Helpers;
using BarberTurno.Views;

// Responsable: M01
// Registro de rutas de páginas apiladas (solo navegación, sin lógica de negocio).
namespace BarberTurno;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Routes.Servicios, typeof(ServiciosPage));
        Routing.RegisterRoute(Routes.SeleccionHora, typeof(SeleccionHoraPage));
        Routing.RegisterRoute(Routes.ConfirmarReserva, typeof(ConfirmarReservaPage));
        Routing.RegisterRoute(Routes.HistorialCliente, typeof(HistorialClientePage));
        Routing.RegisterRoute(Routes.DetalleCita, typeof(DetalleCitaPage));
    }
}
