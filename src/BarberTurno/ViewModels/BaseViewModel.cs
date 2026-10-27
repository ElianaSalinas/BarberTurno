// Responsable: M01
// Base de todos los ViewModels (ObservableObject de CommunityToolkit.Mvvm). Aquí: State, Mensaje de error, IsBusy.
namespace BarberTurno.ViewModels;

public abstract partial class BaseViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    // TODO(M01): ViewState State, string? MensajeError, patrón común de carga con try/catch.
}
