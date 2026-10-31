using BarberTurno.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

// Responsable: M01
// Punto de entrada de la app. Mantiene la sesión en SecureStorage según criterio del Módulo 5.
namespace BarberTurno;

/// <summary>Inicializa el tema y coordina el ciclo de vida global de la aplicación.</summary>
public partial class App : Application
{
    /// <summary>Restaura el tema guardado antes de mostrar la primera ventana.</summary>
    public App()
    {
        InitializeComponent();

        // Recordar el tema elegido (claro por defecto).
        var oscuro = Preferences.Default.Get(PerfilViewModel.PrefTemaOscuro, false);
        UserAppTheme = oscuro ? AppTheme.Dark : AppTheme.Light;

        // La sesión NO se borra al abrir: LoginPage la restaura desde SecureStorage mientras el token siga vigente.
    }

    /// <summary>Crea la ventana con splash y conecta la sincronización al paso a segundo plano.</summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window();
        window.Page = new Views.AnimatedSplashPage(() => window.Page = new AppShell());

        // Ciclo de vida: al pasar a segundo plano (Stopped / OnStop) se intenta subir lo pendiente; al volver (Resumed / OnResume), se refrescan datos.
        window.Stopped += async (_, _) =>
        {
            try
            {
                // Usa el servicio singleton para enviar reservas que quedaron pendientes sin conexión.
                var sync = IPlatformApplication.Current?.Services.GetService<BarberTurno.Services.Sync.ISyncService>();
                if (sync is not null)
                    await sync.SincronizarPendientesAsync();
            }
            catch
            {
                // Sin conexión: quedará pendiente para el siguiente intento.
            }
        };

        window.Resumed += async (_, _) =>
        {
            try
            {
                var sync = IPlatformApplication.Current?.Services.GetService<BarberTurno.Services.Sync.ISyncService>();
                if (sync is not null)
                {
                    await sync.SincronizarPendientesAsync();
                    await sync.SincronizarCitasAsync();
                }
            }
            catch
            {
                // Sin conexión: se continúa visualizando la información local de SQLite.
            }
        };

        return window;
    }
}
