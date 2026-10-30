// Responsable: M5
// Code-behind mínimo: asigna el BindingContext y llama a InicializarAsync.
// Sin lógica de negocio.
using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class LoginPage : ContentPage
{
    // Guardamos el ViewModel en un campo para poder llamar a InicializarAsync
    // desde OnAppearing sin hacer casting de BindingContext cada vez.
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel     = viewModel;
        BindingContext = _viewModel;

        // Comportamiento solo visual: mueve el foco al campo de contraseña cuando
        // el usuario pulsa "Siguiente" en el teclado de Android.
        // No es lógica de negocio: es interacción de teclado específica de la vista.
        EntryUsuario.Completed += (s, e) => EntryPassword.Focus();
    }

    // OnAppearing: se ejecuta cada vez que la página se vuelve visible.
    // Es el punto de entrada correcto para restaurar sesión en MAUI/Shell:
    // el Shell garantiza que la página existe antes de llamar a este método.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InicializarAsync();
    }
}
