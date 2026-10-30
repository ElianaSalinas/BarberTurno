using BarberTurno.ViewModels;

// Responsable: M10 / M04
// Code-behind mínimo: solo asigna el BindingContext y propaga OnAppearing al ViewModel. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class AgendaDiaPage : ContentPage
{
    private readonly AgendaDiaViewModel _viewModel;

    public AgendaDiaPage(AgendaDiaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
