// Responsable: M5
// Code-behind mínimo: asigna el BindingContext y llama a InicializarAsync diferido con Dispatcher.
// Sin lógica de negocio.
using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Gestiona el inicio de sesión visual mediante LoginViewModel.</summary>
public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    /// <summary>Asigna el ViewModel y configura el avance de foco entre campos.</summary>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel     = viewModel;
        BindingContext = _viewModel;

        EntryUsuario.Completed += (s, e) => EntryPassword.Focus();
    }

    /// <summary>Inicia la restauración de sesión cuando la vista ya está lista.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Diferir con Dispatcher para que Shell en Android termine de inicializar la ventana antes de navegar automáticamente
        Dispatcher.Dispatch(async () =>
        {
            await _viewModel.InicializarAsync();
        });
    }
}
