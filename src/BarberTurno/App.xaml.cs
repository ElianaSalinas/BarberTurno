using BarberTurno.ViewModels;
using Microsoft.Maui.Storage;

// Responsable: M01
// Punto de entrada de la app. Mantiene la sesión en SecureStorage según criterio del Módulo 5.
namespace BarberTurno;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Iniciar siempre en tema claro
        UserAppTheme = AppTheme.Light;
        Preferences.Default.Set(PerfilViewModel.PrefTemaOscuro, false);
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}
