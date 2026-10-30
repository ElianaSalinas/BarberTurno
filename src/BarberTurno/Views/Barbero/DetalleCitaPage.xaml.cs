using BarberTurno.ViewModels;

// Responsable: M10.
// Code-behind mínimo: recibe el ViewModel desde inyección de dependencias
// y lo asigna como contexto de Binding.
namespace BarberTurno.Views;

/// <summary>Muestra el detalle de una cita con DetalleCitaViewModel.</summary>
public partial class DetalleCitaPage : ContentPage
{
    /// <summary>Conecta el ViewModel inyectado con los controles de DetalleCitaPage.xaml.</summary>
    public DetalleCitaPage(DetalleCitaViewModel viewModel)
    {
        InitializeComponent();

        // Conecta DetalleCitaPage.xaml con DetalleCitaViewModel.cs.
        BindingContext = viewModel;
    }
}
