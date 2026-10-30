// Responsable: M5
// Code-behind mínimo: asigna el BindingContext y llama a InicializarAsync diferido con Dispatcher.
// Sin lógica de negocio.
using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel     = viewModel;
        BindingContext = _viewModel;

        EntryUsuario.Completed += (s, e) => EntryPassword.Focus();
    }

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
