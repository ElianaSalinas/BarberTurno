using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class ConfirmarReservaPage : ContentPage
{
    public ConfirmarReservaPage(ConfirmarReservaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
