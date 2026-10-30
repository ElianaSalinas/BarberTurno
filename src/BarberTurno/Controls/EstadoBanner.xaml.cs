using System.Windows.Input;
using BarberTurno.Helpers;
using EstadoPantalla = BarberTurno.Helpers.ViewState;

namespace BarberTurno.Controls;

/// <summary>Muestra el estado Cargando o Error de una pantalla (ver EstadoBanner.xaml). Solo presentación.</summary>
public partial class EstadoBanner : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(EstadoPantalla), typeof(EstadoBanner), EstadoPantalla.ConDatos,
        propertyChanged: (b, _, _) => ((EstadoBanner)b).Actualizar());

    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(
        nameof(ErrorText), typeof(string), typeof(EstadoBanner), null,
        propertyChanged: (b, _, _) => ((EstadoBanner)b).Actualizar());

    public static readonly BindableProperty RetryCommandProperty = BindableProperty.Create(
        nameof(RetryCommand), typeof(ICommand), typeof(EstadoBanner), null,
        propertyChanged: (b, _, _) => ((EstadoBanner)b).Actualizar());

    public static readonly BindableProperty LoadingTextProperty = BindableProperty.Create(
        nameof(LoadingText), typeof(string), typeof(EstadoBanner), "Cargando…",
        propertyChanged: (b, _, n) => ((EstadoBanner)b).TextoCarga.Text = n as string ?? "Cargando…");

    public EstadoBanner() => InitializeComponent();

    public EstadoPantalla State { get => (EstadoPantalla)GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public string? ErrorText { get => (string?)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }
    public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }

    private void Actualizar()
    {
        var cargando = State == EstadoPantalla.Cargando;
        var error = State == EstadoPantalla.Error;

        PanelCarga.IsVisible = cargando;
        PanelError.IsVisible = error;
        Raiz.IsVisible = cargando || error;

        TextoError.Text = string.IsNullOrWhiteSpace(ErrorText) ? "Ocurrió un problema al cargar los datos." : ErrorText;
        BotonReintentar.Command = RetryCommand;
        BotonReintentar.IsVisible = RetryCommand is not null;
    }
}
