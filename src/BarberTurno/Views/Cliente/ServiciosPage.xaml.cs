using BarberTurno.ViewModels;
// Responsable: M09
// Code-behind mínimo: asigna el BindingContext y avisa al ViewModel cuando la página aparece/desaparece.
// Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Muestra los servicios del barbero elegido a través de ServiciosViewModel.</summary>
public partial class ServiciosPage : ContentPage
{
    private readonly ServiciosViewModel _viewModel;

    /// <summary>Asigna el ViewModel de servicios al contexto de enlace.</summary>
    public ServiciosPage(ServiciosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Solicita al ViewModel cargar los servicios al entrar.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    /// <summary>Notifica la salida para detener trabajo asociado a la vista.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
