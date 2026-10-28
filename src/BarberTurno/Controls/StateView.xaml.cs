// Responsable: M02
// Code-behind de StateView: propiedades enlazables y lógica de visibilidad por estado.
// No hay lógica de negocio aquí; solo gestión de qué layout se muestra según el estado.
using System.Windows.Input;
using BarberTurno.Helpers;

// Alias para evitar ambigüedad con Microsoft.Maui.Controls.ViewState
using EstadoPantalla = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Controls;

/// <summary>
/// Control reutilizable que muestra uno de cuatro estados de pantalla.
/// Los ViewModels de M09 y M10 enlazan la propiedad <see cref="Estado"/>
/// para controlar qué se muestra sin código en el code-behind de las páginas.
/// </summary>
public partial class StateView : ContentView
{
    // ── Propiedades enlazables ────────────────────────────────────────────────

    /// <summary>Estado actual de la pantalla. Cambiarlo actualiza automáticamente la vista.</summary>
    public static readonly BindableProperty EstadoProperty =
        BindableProperty.Create(
            propertyName:    nameof(Estado),
            returnType:      typeof(EstadoPantalla),
            declaringType:   typeof(StateView),
            defaultValue:    EstadoPantalla.Cargando,
            propertyChanged: OnEstadoCambiado);

    /// <summary>Mensaje que se muestra cuando el estado es <see cref="EstadoPantalla.Vacio"/>.</summary>
    public static readonly BindableProperty TextoVacioProperty =
        BindableProperty.Create(
            propertyName:    nameof(TextoVacio),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "No hay datos disponibles.",
            propertyChanged: OnTextoVacioCambiado);

    /// <summary>Mensaje que se muestra cuando el estado es <see cref="EstadoPantalla.Error"/>.</summary>
    public static readonly BindableProperty TextoErrorProperty =
        BindableProperty.Create(
            propertyName:    nameof(TextoError),
            returnType:      typeof(string),
            declaringType:   typeof(StateView),
            defaultValue:    "Ocurrió un error inesperado.",
            propertyChanged: OnTextoErrorCambiado);

    /// <summary>Comando enlazado al botón "Reintentar" en el estado Error.</summary>
    public static readonly BindableProperty ComandoReintentarProperty =
        BindableProperty.Create(
            propertyName:    nameof(ComandoReintentar),
            returnType:      typeof(ICommand),
            declaringType:   typeof(StateView),
            defaultValue:    null,
            propertyChanged: OnComandoReintentarCambiado);

    /// <summary>Vista que se presenta cuando el estado es <see cref="EstadoPantalla.ConDatos"/>.</summary>
    public static readonly BindableProperty ContenidoProperty =
        BindableProperty.Create(
            propertyName:    nameof(Contenido),
            returnType:      typeof(View),
            declaringType:   typeof(StateView),
            defaultValue:    null,
            propertyChanged: OnContenidoCambiado);

    // ── Propiedades CLR ───────────────────────────────────────────────────────

    /// <inheritdoc cref="EstadoProperty"/>
    public EstadoPantalla Estado
    {
        get => (EstadoPantalla)GetValue(EstadoProperty);
        set => SetValue(EstadoProperty, value);
    }

    /// <inheritdoc cref="TextoVacioProperty"/>
    public string TextoVacio
    {
        get => (string)GetValue(TextoVacioProperty);
        set => SetValue(TextoVacioProperty, value);
    }

    /// <inheritdoc cref="TextoErrorProperty"/>
    public string TextoError
    {
        get => (string)GetValue(TextoErrorProperty);
        set => SetValue(TextoErrorProperty, value);
    }

    /// <inheritdoc cref="ComandoReintentarProperty"/>
    public ICommand? ComandoReintentar
    {
        get => (ICommand?)GetValue(ComandoReintentarProperty);
        set => SetValue(ComandoReintentarProperty, value);
    }

    /// <inheritdoc cref="ContenidoProperty"/>
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
        if (bindable is StateView sv)
            sv.AplicarEstado((EstadoPantalla)newVal);
    }

    private static void OnTextoVacioCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv)
            sv.LblVacio.Text = newVal as string ?? "";
    }

    private static void OnTextoErrorCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv)
            sv.LblError.Text = newVal as string ?? "";
    }

    private static void OnComandoReintentarCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv)
            sv.BtnReintentar.Command = newVal as ICommand;
    }

    private static void OnContenidoCambiado(BindableObject bindable, object oldVal, object newVal)
    {
        if (bindable is StateView sv)
            sv.ConDatosLayout.Content = newVal as View;
    }

    // ── Lógica de estados ─────────────────────────────────────────────────────

    /// <summary>
    /// Muestra solo el layout correspondiente al estado indicado y oculta los demás.
    /// Esta es la única responsabilidad de este code-behind.
    /// </summary>
    private void AplicarEstado(EstadoPantalla estado)
    {
        CargandoLayout.IsVisible = estado == EstadoPantalla.Cargando;
        VacioLayout.IsVisible    = estado == EstadoPantalla.Vacio;
        ErrorLayout.IsVisible    = estado == EstadoPantalla.Error;
        ConDatosLayout.IsVisible = estado == EstadoPantalla.ConDatos;
    }
}
