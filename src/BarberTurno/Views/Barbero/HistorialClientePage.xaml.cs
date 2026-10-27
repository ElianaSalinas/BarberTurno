using BarberTurno.ViewModels;
// Responsable: M10
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class HistorialClientePage : ContentPage
{
    public HistorialClientePage(HistorialClienteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
