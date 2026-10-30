using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Presenta el historial de un cliente mediante HistorialClienteViewModel.</summary>
public partial class HistorialClientePage : ContentPage
{
    private readonly HistorialClienteViewModel _viewModel;

    /// <summary>Asigna el ViewModel proporcionado por el contenedor DI.</summary>
    public HistorialClientePage(HistorialClienteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Solicita cargar los datos necesarios al mostrar la página.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
