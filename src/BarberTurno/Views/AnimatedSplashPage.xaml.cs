namespace BarberTurno.Views;

public partial class AnimatedSplashPage : ContentPage
{
    public AnimatedSplashPage()
    {
        InitializeComponent();
        this.Loaded += async (_, _) => await RunSplashAnimationAsync();
    }

    private async Task RunSplashAnimationAsync()
    {
        // Fade in + scale up con rebote
        await Task.WhenAll(
            IconImage.FadeToAsync(1, 600, Easing.CubicOut),
            IconImage.ScaleToAsync(1.2, 600, Easing.CubicOut)
        );

        // Rebote suave
        await IconImage.ScaleToAsync(1.0, 300, Easing.BounceOut);

        // Rotación elegante
        await IconImage.RotateToAsync(360, 800, Easing.CubicInOut);
        IconImage.Rotation = 0;

        // Pausa breve
        await Task.Delay(400);

        // Fade out
        await IconImage.FadeToAsync(0, 400, Easing.CubicIn);

        // Navegar a la página principal
        if (Application.Current != null)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
