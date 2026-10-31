using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Presenta los horarios disponibles para la reserva seleccionada.</summary>
public partial class SeleccionHoraPage : ContentPage
{
    private readonly SeleccionHoraViewModel _viewModel;

    /// <summary>Conecta SeleccionHoraViewModel con los enlaces de la página.</summary>
    public SeleccionHoraPage(SeleccionHoraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Recalcula la disponibilidad mediante el ViewModel al aparecer.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AlAparecerAsync();
    }
}
