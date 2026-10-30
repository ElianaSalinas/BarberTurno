using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class HistorialClientePage : ContentPage
{
    private readonly HistorialClienteViewModel _viewModel;

    public HistorialClientePage(HistorialClienteViewModel viewModel)
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
