using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class SeleccionHoraPage : ContentPage
{
    public SeleccionHoraPage(SeleccionHoraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
