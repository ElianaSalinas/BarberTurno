using BarberTurno.ViewModels;
// Responsable: M10
// Code-behind mínimo: solo asigna el BindingContext. Sin lógica de negocio.
namespace BarberTurno.Views;

public partial class AgendaDiaPage : ContentPage
{
    public AgendaDiaPage(AgendaDiaViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
