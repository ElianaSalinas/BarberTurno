// Responsable: M01
// Estados de pantalla comunes para StateView y BaseViewModel.
global using ViewState = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Helpers;

/// <summary>Define el estado visual común que muestran los controles y ViewModels.</summary>
public enum ViewState
{
    Cargando,
    Vacio,
    Error,
    ConDatos
}
