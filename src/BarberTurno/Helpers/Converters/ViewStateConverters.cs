// Responsable: M02
// Convertidores de ViewState a visibilidad. Usados por StateView y otras pantallas.
using BarberTurno.Helpers;
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un <see cref="ViewState"/> en <c>bool</c> (visibilidad).
/// <para>
/// Uso en XAML: <c>IsVisible="{Binding Estado, Converter={StaticResource ViewStateToVisibility}, ConverterParameter=ConDatos}"</c>
/// El parámetro es el nombre del estado que hace visible el elemento.
/// </para>
/// </summary>
public class ViewStateToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ViewState estado) return false;
        if (parameter is not string nombreEstado)  return false;

        return Enum.TryParse<ViewState>(nombreEstado, ignoreCase: true, out var estadoEsperado)
               && estado == estadoEsperado;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("ViewStateToVisibilityConverter no soporta conversión inversa.");
}
