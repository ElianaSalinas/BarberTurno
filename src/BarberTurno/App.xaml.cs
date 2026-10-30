using BarberTurno.ViewModels;
using Microsoft.Maui.Storage;

// Responsable: M01
// Punto de entrada de la app.
namespace BarberTurno;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Iniciar siempre en tema claro
        UserAppTheme = AppTheme.Light;
        Preferences.Default.Set(PerfilViewModel.PrefTemaOscuro, false);

        // Limpiar sesión previa para garantizar que siempre muestre la pantalla de Login al compilar/iniciar
        try
        {
            SecureStorage.Default.Remove("auth_token");
            SecureStorage.Default.Remove("auth_expira");
            SecureStorage.Default.Remove("auth_sesion");
        }
        catch
        {
            // Seguro ante excepciones de plataforma en SecureStorage
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
        => new Window(new AppShell());
}
