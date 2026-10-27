using BarberTurno.ViewModels;
// Responsable: M06
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
