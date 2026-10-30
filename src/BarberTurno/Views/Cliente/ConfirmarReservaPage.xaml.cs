using BarberTurno.ViewModels;
// Responsable: M08
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

/// <summary>Confirma una reserva usando ConfirmarReservaViewModel.</summary>
public partial class ConfirmarReservaPage : ContentPage
{
    private readonly ConfirmarReservaViewModel _viewModel;

    /// <summary>Asigna el ViewModel inyectado al contexto visual.</summary>
    public ConfirmarReservaPage(ConfirmarReservaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <summary>Permite al ViewModel validar y preparar el resumen de reserva.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AlAparecerAsync();
    }
}
