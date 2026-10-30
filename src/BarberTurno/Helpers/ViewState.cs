// Responsable: M01
// Estados de pantalla comunes para StateView y BaseViewModel.
global using ViewState = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Helpers;

public enum ViewState
{
    Cargando,
    Vacio,
    Error,
    ConDatos
}
