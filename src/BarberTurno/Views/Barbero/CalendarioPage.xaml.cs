using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Vista de calendario conectada a CalendarioViewModel.</summary>
public partial class CalendarioPage : ContentPage
{
    private readonly CalendarioViewModel _viewModel;

    /// <summary>Asigna el ViewModel inyectado al BindingContext.</summary>
    public CalendarioPage(CalendarioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Notifica al ViewModel que la vista está visible.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
