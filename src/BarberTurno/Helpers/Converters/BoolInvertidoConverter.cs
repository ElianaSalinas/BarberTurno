// Responsable: M02
// Invertidor de booleano. Útil para enlazar IsVisible al inverso de IsBusy, etc.
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un <see cref="bool"/> a su valor negado.
/// <para>
/// Uso típico: <c>IsVisible="{Binding IsBusy, Converter={StaticResource BoolInvertido}}"</c>
/// para ocultar un elemento cuando el ViewModel está cargando.
/// </para>
/// </summary>
public class BoolInvertidoConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}
