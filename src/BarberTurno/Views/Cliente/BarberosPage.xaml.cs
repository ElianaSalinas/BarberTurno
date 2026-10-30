using BarberTurno.ViewModels;
// Responsable: M09
// Code-behind mínimo: asigna el BindingContext y avisa al ViewModel cuando la página aparece/desaparece.
// Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Lista barberos mediante BarberosViewModel y sus enlaces XAML.</summary>
public partial class BarberosPage : ContentPage
{
    private readonly BarberosViewModel _viewModel;

    /// <summary>Conecta la vista con el ViewModel suministrado por DI.</summary>
    public BarberosPage(BarberosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Inicia la carga de barberos al mostrar la página.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    /// <summary>Permite al ViewModel liberar suscripciones al ocultar la página.</summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
