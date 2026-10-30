using Android.App;
using Android.Content.PM;
using Android.OS;

namespace BarberTurno;

/// <summary>Actividad Android principal que inicia MAUI y adapta la barra del sistema.</summary>
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    /// <summary>Inicia la actividad y configura la apariencia visual propia de Android.</summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Barra de estado clara (mismo color que el fondo de la app) con iconos oscuros.
        if (Window is { } ventana)
        {
#pragma warning disable CA1422
            ventana.SetStatusBarColor(Android.Graphics.Color.ParseColor("#F8F7F3"));
#pragma warning restore CA1422
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
                ventana.InsetsController?.SetSystemBarsAppearance(
                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars,
                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
        }
    }
}
