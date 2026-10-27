using BarberTurno.ViewModels;
// Responsable: M09
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class PerfilPage : ContentPage
{
    public PerfilPage(PerfilViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
