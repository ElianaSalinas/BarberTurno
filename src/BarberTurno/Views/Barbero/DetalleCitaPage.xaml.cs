using BarberTurno.ViewModels;
// Responsable: M10
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class DetalleCitaPage : ContentPage
{
    public DetalleCitaPage(DetalleCitaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
