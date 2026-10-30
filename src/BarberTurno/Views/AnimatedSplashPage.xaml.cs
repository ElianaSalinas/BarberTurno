namespace BarberTurno.Views;

// Continúa el splash nativo (mismo fondo #1B1B17 y mismo icono) para que no haya salto visual.
/// <summary>Muestra la animación inicial y continúa hacia AppShell al finalizar.</summary>
public partial class AnimatedSplashPage : ContentPage
{
    private readonly Action _alTerminar;

    /// <summary>Recibe la acción que instala la pantalla principal después del splash.</summary>
    public AnimatedSplashPage(Action alTerminar)
    {
        InitializeComponent();
        _alTerminar = alTerminar;
        Loaded += async (_, _) => await RunAsync();
    }

    /// <summary>Ejecuta la animación visual sin bloquear el arranque de la aplicación.</summary>
    private async Task RunAsync()
    {
        try
        {
            await Task.WhenAll(
                IconImage.FadeToAsync(1, 450, Easing.CubicOut),
                IconImage.ScaleToAsync(1, 450, Easing.CubicOut));
            await TitleLabel.FadeToAsync(1, 350, Easing.CubicOut);
            await Task.Delay(450);
        }
        catch
        {
            // Si la animación falla, igual seguimos a la app.
        }

        MainThread.BeginInvokeOnMainThread(_alTerminar);
    }
}
