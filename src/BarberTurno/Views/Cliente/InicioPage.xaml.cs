using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Vista inicial del cliente; enlaza InicioViewModel con InicioPage.xaml.</summary>
public partial class InicioPage : ContentPage
{
    private readonly InicioViewModel _viewModel;

    /// <summary>Recibe el ViewModel registrado en DI y lo asigna al enlace XAML.</summary>
    public InicioPage(InicioViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Avisa al ViewModel para cargar el resumen al aparecer la vista.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
