// Responsable: M01
// Base de todos los ViewModels (ObservableObject de CommunityToolkit.Mvvm).
using CommunityToolkit.Mvvm.ComponentModel;
using ViewStateHelper = BarberTurno.Helpers.ViewState;

namespace BarberTurno.ViewModels;

/// <summary>Base compartida para el estado visual y mensajes de todas las pantallas MVVM.</summary>
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

    /// <summary>Permite a la vista iniciar cargas al mostrarse.</summary>
    public virtual void OnAppearing() { }

    /// <summary>Permite a la vista liberar suscripciones al ocultarse.</summary>
    public virtual void OnDisappearing() { }
}
