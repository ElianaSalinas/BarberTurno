using BarberTurno.ViewModels;

namespace BarberTurno.Views;

/// <summary>Vista del catálogo de servicios conectado a CatalogoViewModel.</summary>
public partial class CatalogoPage : ContentPage
{
    private readonly CatalogoViewModel _viewModel;

    /// <summary>Asigna el ViewModel inyectado como contexto de enlace.</summary>
    public CatalogoPage(CatalogoViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Activa la carga del catálogo al entrar en la página.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }
}
