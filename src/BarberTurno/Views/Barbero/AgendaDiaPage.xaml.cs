using BarberTurno.ViewModels;

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
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Se invoca cada vez que la página vuelve a estar visible en pantalla
    /// (por ejemplo al volver desde DetalleCitaPage o tras desbloquear el teléfono).
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
