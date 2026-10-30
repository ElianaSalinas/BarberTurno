using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class CatalogoPage : ContentPage
{
    private readonly CatalogoViewModel _viewModel;

    public CatalogoPage(CatalogoViewModel viewModel)
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
