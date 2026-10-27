using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
// Alias: MAUI también tiene un tipo llamado ViewState (Microsoft.Maui.Controls).
using ViewState = BarberTurno.Helpers.ViewState;

// Responsable: M01
// Base de todos los ViewModels (ObservableObject de CommunityToolkit.Mvvm). Aquí: State, Mensaje de error, IsBusy.
namespace BarberTurno.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private ViewState state = ViewState.ConDatos;

    [ObservableProperty]
    private string? mensajeError;

    [ObservableProperty]
    private string? titulo;

    public bool IsBusy => State == ViewState.Cargando;

    /// <summary>
    /// Patrón común de carga. La función devuelve true si hay datos para mostrar.
    /// Deja la pantalla en Cargando, luego en ConDatos / Vacio, o en Error si algo falla.
    /// </summary>
    protected async Task CargarAsync(Func<Task<bool>> cargar)
    {
        if (IsBusy) return;

        State = ViewState.Cargando;
        MensajeError = null;
        try
        {
            var hayDatos = await cargar();
            State = hayDatos ? ViewState.ConDatos : ViewState.Vacio;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{GetType().Name}] Error al cargar: {ex}");
            MensajeError = TraducirError(ex);
            State = ViewState.Error;
        }
    }

    /// <summary>
    /// Para acciones puntuales (guardar, reservar, cancelar) que no cambian la pantalla a Vacio.
    /// Devuelve true si la acción terminó bien; si falla, deja MensajeError y el estado anterior.
    /// </summary>
    protected async Task<bool> EjecutarAsync(Func<Task> accion)
    {
        if (IsBusy) return false;

        var estadoAnterior = State;
        State = ViewState.Cargando;
        MensajeError = null;
        try
        {
            await accion();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[{GetType().Name}] Error en acción: {ex}");
            MensajeError = TraducirError(ex);
            return false;
        }
        finally
        {
            State = estadoAnterior;
        }
    }

    /// <summary>
    /// Mensaje para el usuario según el tipo de error. Nunca mostrar ex.Message tal cual.
    /// Los ViewModels pueden sobrescribirlo para errores propios (p. ej. SlotOcupadoException).
    /// </summary>
    protected virtual string TraducirError(Exception ex) => ex switch
    {
        HttpRequestException => "No se pudo conectar con el servidor. Revisa tu conexión e inténtalo de nuevo.",
        TaskCanceledException => "El servidor tardó demasiado en responder. Inténtalo de nuevo.",
        _ => "Ocurrió un error inesperado. Inténtalo de nuevo."
    };
}
