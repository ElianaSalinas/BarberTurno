using BarberTurno.ViewModels;

// Responsable: M10.
// Code-behind mínimo: recibe el ViewModel desde inyección de dependencias
// y lo asigna como contexto de Binding.
namespace BarberTurno.Views;

public partial class DetalleCitaPage : ContentPage
{
    public DetalleCitaPage(DetalleCitaViewModel viewModel)
    {
        InitializeComponent();

        // Conecta DetalleCitaPage.xaml con DetalleCitaViewModel.cs.
        BindingContext = viewModel;
    }
}