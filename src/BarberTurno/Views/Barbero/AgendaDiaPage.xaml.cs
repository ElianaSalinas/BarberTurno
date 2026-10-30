using BarberTurno.ViewModels;
// Responsable: M10
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>
/// Code-behind de la página AgendaDiaPage.
/// 
/// REGLA DE ORO DE LA ARQUITECTURA MVVM:
/// - "Ninguna lógica de negocio en el code-behind".
/// - El code-behind únicamente conecta el ViewModel inyectado en el constructor
///   y propaga eventos de ciclo de vida nativos de MAUI (OnAppearing).
/// </summary>
public partial class AgendaDiaPage : ContentPage
{
    private readonly AgendaDiaViewModel _viewModel;

    public AgendaDiaPage(AgendaDiaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
