// Responsable: M02 / M06
// Code-behind de StateView: propiedades enlazables y lógica de visibilidad por estado.
using System.Windows.Input;
using BarberTurno.Helpers;
using Microsoft.Maui.Controls;

// Alias para evitar ambigüedad con Microsoft.Maui.Controls.ViewState
using EstadoPantalla = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Controls;

/// <summary>
/// Control reutilizable que muestra uno de cuatro estados de pantalla:
/// Cargando, Vacío, Error (con reintento) y Con datos.
/// Soporta propiedades en español (Estado, TextoVacio, TextoError, ComandoReintentar, Contenido)
/// y en inglés (State, EmptyText, ErrorText, RetryCommand, Content).
/// </summary>
[ContentProperty(nameof(Contenido))]
public partial class StateView : ContentView
{
    // ── Propiedades enlazables (Español) ────────────────────────────────────────

    public static readonly BindableProperty EstadoProperty =
        BindableProperty.Create(
            propertyName:    nameof(Estado),
            returnType:      typeof(EstadoPantalla),
            declaringType:   typeof(StateView),
            defaultValue:    EstadoPantalla.Cargando,
            propertyChanged: OnEstadoCambiado);

    public static readonly BindableProperty TextoVacioProperty =
        BindableProperty.Create(
            propertyName:    nameof(TextoVacio),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "No hay datos disponibles.",
            propertyChanged: OnTextoVacioCambiado);

    public static readonly BindableProperty TextoErrorProperty =
        BindableProperty.Create(
            propertyName:    nameof(TextoError),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "Ocurrió un error inesperado.",
            propertyChanged: OnTextoErrorCambiado);

    public static readonly BindableProperty ComandoReintentarProperty =
        BindableProperty.Create(
            propertyName:    nameof(ComandoReintentar),
            returnType:      typeof(ICommand),
            declaringType:   typeof(StateView),
            defaultValue:    null,
            propertyChanged: OnComandoReintentarCambiado);

    public static readonly BindableProperty ContenidoProperty =
        BindableProperty.Create(
            propertyName:    nameof(Contenido),
            returnType:      typeof(View),
            declaringType:   typeof(StateView),
            defaultValue:    null,
            propertyChanged: OnContenidoCambiado);

    // ── Propiedades enlazables (Inglés / Alias) ────────────────────────────────

    public static readonly BindableProperty StateProperty =
        BindableProperty.Create(
            propertyName:    nameof(State),
            returnType:      typeof(EstadoPantalla),
            declaringType:   typeof(StateView),
            defaultValue:    EstadoPantalla.Cargando,
            propertyChanged: OnEstadoCambiado);

    public static readonly BindableProperty EmptyTextProperty =
        BindableProperty.Create(
            propertyName:    nameof(EmptyText),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "No hay datos disponibles.",
            propertyChanged: OnTextoVacioCambiado);

    public static readonly BindableProperty ErrorTextProperty =
        BindableProperty.Create(
            propertyName:    nameof(ErrorText),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "Ocurrió un error inesperado.",
            propertyChanged: OnTextoErrorCambiado);

    public static readonly BindableProperty RetryCommandProperty =
        BindableProperty.Create(
            propertyName:    nameof(RetryCommand),
            returnType:      typeof(ICommand),
            declaringType:   typeof(StateView),
            defaultValue:    null,
            propertyChanged: OnComandoReintentarCambiado);

    // ── Propiedades CLR ───────────────────────────────────────────────────────

    public EstadoPantalla Estado
    {
        get => (EstadoPantalla)GetValue(EstadoProperty);
        set => SetValue(EstadoProperty, value);
    }

    public EstadoPantalla State
    {
        get => (EstadoPantalla)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public string TextoVacio
    {
        get => (string)GetValue(TextoVacioProperty);
        set => SetValue(TextoVacioProperty, value);
    }

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public string TextoError
    {
        get => (string)GetValue(TextoErrorProperty);
        set => SetValue(TextoErrorProperty, value);
    }

    public string ErrorText
    {
        get => (string)GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public ICommand? ComandoReintentar
    {
        get => (ICommand?)GetValue(ComandoReintentarProperty);
        set => SetValue(ComandoReintentarProperty, value);
    }

    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    public View? Contenido
    {
        get => (View?)GetValue(ContenidoProperty);
        set => SetValue(ContenidoProperty, value);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public StateView()
    {
        InitializeComponent();
        AplicarEstado(EstadoPantalla.Cargando);
    }

    // ── Callbacks de cambio de propiedades ────────────────────────────────────

    private static void OnEstadoCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv && newVal is EstadoPantalla estado)
            sv.AplicarEstado(estado);
    }

    private static void OnTextoVacioCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv && sv.LblVacio != null)
            sv.LblVacio.Text = newVal as string ?? "";
    }

    private static void OnTextoErrorCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv && sv.LblError != null)
            sv.LblError.Text = newVal as string ?? "";
    }

    private static void OnComandoReintentarCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv && sv.BtnReintentar != null)
            sv.BtnReintentar.Command = newVal as ICommand;
    }

    private static void OnContenidoCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv && sv.ConDatosLayout != null)
        {
            // Protección contra ciclos recursivos si la plantilla interna es asignada a Contenido
            if (ReferenceEquals(newVal, sv.Content) || ReferenceEquals(newVal, sv.ConDatosLayout))
                return;

            sv.ConDatosLayout.Content = newVal as View;
        }
    }

    // ── Lógica de estados ─────────────────────────────────────────────────────

    private void AplicarEstado(EstadoPantalla estado)
    {
        if (CargandoLayout == null || VacioLayout == null || ErrorLayout == null || ConDatosLayout == null)
            return;

        CargandoLayout.IsVisible = estado == EstadoPantalla.Cargando;
        VacioLayout.IsVisible    = estado == EstadoPantalla.Vacio;
        ErrorLayout.IsVisible    = estado == EstadoPantalla.Error;
        ConDatosLayout.IsVisible = estado == EstadoPantalla.ConDatos;
    }
}
