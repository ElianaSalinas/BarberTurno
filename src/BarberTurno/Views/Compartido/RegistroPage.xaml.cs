// Responsable: M5
// Code-behind mínimo: asigna el BindingContext y gestiona el foco entre campos
// (comportamiento visual puro, no lógica de negocio).
using BarberTurno.ViewModels;

namespace BarberTurno.Views;

public partial class RegistroPage : ContentPage
{
    private readonly RegistroViewModel _viewModel;

    public RegistroPage(RegistroViewModel viewModel)
    {
        InitializeComponent();
        _viewModel     = viewModel;
        BindingContext = _viewModel;

        // ── Cadena de foco entre campos (comportamiento visual, no lógica de negocio) ──
        // Cada vez que el usuario pulsa "Siguiente" en el teclado de Android,
        // el foco avanza al siguiente campo de forma natural.
        EntryNombre.Completed           += (s, e) => EntryTelefono.Focus();
        EntryTelefono.Completed         += (s, e) => EntryUsuario.Focus();
        EntryUsuario.Completed          += (s, e) => EntryPassword.Focus();
        EntryPassword.Completed         += (s, e) => EntryConfirmarPassword.Focus();
        // EntryConfirmarPassword tiene ReturnType="Done" y su ReturnCommand
        // enlazado a RegistrarCommand directamente en el XAML (es un ICommand válido).
    }
}
