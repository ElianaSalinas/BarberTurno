using BarberTurno.ViewModels;
// Responsable: M09
// Code-behind mínimo: asigna el BindingContext y avisa al ViewModel cuando la página aparece/desaparece.
// Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class BarberosPage : ContentPage
{
    private readonly BarberosViewModel _viewModel;

    public BarberosPage(BarberosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
