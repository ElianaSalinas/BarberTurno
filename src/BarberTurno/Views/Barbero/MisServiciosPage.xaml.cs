using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class MisServiciosPage : ContentPage
{
    private readonly MisServiciosViewModel _viewModel;

    public MisServiciosPage(MisServiciosViewModel viewModel)
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
