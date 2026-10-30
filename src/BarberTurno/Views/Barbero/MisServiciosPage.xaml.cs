using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Administra los servicios del barbero con MisServiciosViewModel.</summary>
public partial class MisServiciosPage : ContentPage
{
    private readonly MisServiciosViewModel _viewModel;

    /// <summary>Conecta el ViewModel inyectado al enlace XAML.</summary>
    public MisServiciosPage(MisServiciosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Activa la carga de servicios al entrar en la vista.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
