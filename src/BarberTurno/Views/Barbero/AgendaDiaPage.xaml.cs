using BarberTurno.ViewModels;

// Responsable: M10 / M04
// Code-behind mínimo: solo asigna el BindingContext y propaga OnAppearing al ViewModel. Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Presenta la agenda diaria del barbero enlazada a AgendaDiaViewModel.</summary>
public partial class AgendaDiaPage : ContentPage
{
    private readonly AgendaDiaViewModel _viewModel;

    /// <summary>Asigna el ViewModel inyectado al contexto de la vista.</summary>
    public AgendaDiaPage(AgendaDiaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Solicita al ViewModel actualizar la agenda al aparecer.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    /// <summary>Navega al calendario del barbero desde el gesto de la vista.</summary>
    private async void OnVerCalendarioTapped(object? sender, TappedEventArgs e)
        => await Shell.Current.GoToAsync("//barbero/calendario");

    /// <summary>Abre los avisos reutilizando el helper de presentación.</summary>
    private async void OnCampanaTapped(object? sender, TappedEventArgs e)
        => await BarberTurno.Helpers.AvisosHelper.MostrarAsync(Handler?.MauiContext?.Services);
}
