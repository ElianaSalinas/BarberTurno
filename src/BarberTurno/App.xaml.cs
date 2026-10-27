// Responsable: M01
// Punto de entrada de la app. TODO(M01/M06): al iniciar, restaurar sesión y enviar al perfil correcto.
namespace BarberTurno;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}

