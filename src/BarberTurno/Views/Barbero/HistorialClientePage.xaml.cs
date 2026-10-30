using BarberTurno.ViewModels;

// Responsable: M10.
// Code-behind mínimo: solo conecta la vista con su ViewModel.
namespace BarberTurno.Views;

public partial class HistorialClientePage : ContentPage
{
    public HistorialClientePage(HistorialClienteViewModel viewModel)
    {
        InitializeComponent();

        // Permite que el XAML use los bindings del ViewModel.
        BindingContext = viewModel;
    }
}