// Responsable: M02
// Convierte EstadoCita al color de estado correspondiente para la UI.
using BarberTurno.Models;
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un valor <see cref="EstadoCita"/> en el <see cref="Color"/> de estado
/// definido en la paleta de la app (AppTheme.xaml):
/// <list type="table">
///   <item><term>Confirmada</term><description>Verde (#27AE60)</description></item>
///   <item><term>Pendiente</term><description>Ámbar (#E67E22)</description></item>
///   <item><term>Rechazada</term><description>Rojo (#E74C3C)</description></item>
///   <item><term>Cancelada</term><description>Gris (#95A5A6)</description></item>
///   <item><term>Atendida</term><description>Verde oscuro (#1E8449)</description></item>
///   <item><term>Ausente</term><description>Gris oscuro (#717D7E)</description></item>
/// </list>
/// </summary>
public class EstadoCitaToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EstadoCita estado) return Colors.Gray;

        return estado switch
        {
            EstadoCita.Confirmada => Color.FromArgb("#27AE60"),
            EstadoCita.Pendiente  => Color.FromArgb("#E67E22"),
            EstadoCita.Rechazada  => Color.FromArgb("#E74C3C"),
            EstadoCita.Cancelada  => Color.FromArgb("#95A5A6"),
            EstadoCita.Atendida   => Color.FromArgb("#1E8449"),
            EstadoCita.Ausente    => Color.FromArgb("#717D7E"),
            _                     => Colors.Gray
        };
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("EstadoCitaToColorConverter no soporta conversión inversa.");
}
