using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class SeleccionHoraPage : ContentPage
{
    private readonly SeleccionHoraViewModel _viewModel;

    public SeleccionHoraPage(SeleccionHoraViewModel viewModel)
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
