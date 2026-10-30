using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class ConfirmarReservaPage : ContentPage
{
    private readonly ConfirmarReservaViewModel _viewModel;

    public ConfirmarReservaPage(ConfirmarReservaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AlAparecerAsync();
    }
}
