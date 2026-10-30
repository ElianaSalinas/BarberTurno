using BarberTurno.ViewModels;
// Responsable: M09
// Code-behind mínimo: asigna el BindingContext y avisa al ViewModel cuando la página aparece/desaparece.
// Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Muestra y actualiza las citas del cliente mediante MisCitasViewModel.</summary>
public partial class MisCitasPage : ContentPage
{
    private readonly MisCitasViewModel _viewModel;

    /// <summary>Asigna el ViewModel al BindingContext de la página.</summary>
    public MisCitasPage(MisCitasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Solicita el refresco de citas al volver a la vista.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    /// <summary>Notifica la salida para detener observadores del ViewModel.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
