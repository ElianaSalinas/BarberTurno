// Responsable: M01
// Base de todos los ViewModels (ObservableObject de CommunityToolkit.Mvvm).
using CommunityToolkit.Mvvm.ComponentModel;
using ViewStateHelper = BarberTurno.Helpers.ViewState;

namespace BarberTurno.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private ViewStateHelper state = ViewStateHelper.Cargando;

    [ObservableProperty]
    private string? mensajeError;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string titulo = string.Empty;

    public virtual void OnAppearing() { }
    public virtual void OnDisappearing() { }
}
