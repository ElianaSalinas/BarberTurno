using BarberTurno.Helpers;

namespace BarberTurno.Controls;

/// <summary>Barra de marca reutilizable con acceso a los avisos de la aplicación.</summary>
public partial class BrandBar : ContentView
{
    /// <summary>Inicializa los controles definidos en BrandBar.xaml.</summary>
    public BrandBar() => InitializeComponent();

    /// <summary>Anima el icono y solicita al helper la visualización de avisos.</summary>
    private async void OnCampanaTapped(object? sender, TappedEventArgs e)
    {
        await CampanaBorder.ScaleToAsync(0.88, 70, Easing.CubicOut);
        await CampanaBorder.ScaleToAsync(1, 110, Easing.CubicOut);
        await AvisosHelper.MostrarAsync(Handler?.MauiContext?.Services);
    }
}
