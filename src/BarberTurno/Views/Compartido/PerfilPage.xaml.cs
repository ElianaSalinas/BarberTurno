using BarberTurno.ViewModels;

// Responsable: M09
// Code-behind mínimo: asigna el BindingContext y avisa al ViewModel cuando la página aparece/desaparece.
// Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Muestra el perfil compartido y lo conecta con PerfilViewModel.</summary>
public partial class PerfilPage : ContentPage
{
    private readonly PerfilViewModel _viewModel;

    /// <summary>Asigna el ViewModel recibido desde DI al BindingContext.</summary>
    public PerfilPage(PerfilViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Activa la carga y suscripciones del perfil al aparecer.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    /// <summary>Detiene las suscripciones del ViewModel al abandonar la página.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Avisar al ViewModel para desactivar la escucha de eventos de controles reciclados
        _viewModel.OnDisappearing();
    }
}
