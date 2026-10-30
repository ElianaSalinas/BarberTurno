using BarberTurno.Helpers;

namespace BarberTurno.Controls;

public partial class BrandBar : ContentView
{
    public BrandBar() => InitializeComponent();

    private async void OnCampanaTapped(object? sender, TappedEventArgs e)
    {
        await CampanaBorder.ScaleToAsync(0.88, 70, Easing.CubicOut);
        await CampanaBorder.ScaleToAsync(1, 110, Easing.CubicOut);
        await AvisosHelper.MostrarAsync(Handler?.MauiContext?.Services);
    }
}
