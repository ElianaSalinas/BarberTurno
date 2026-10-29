// Responsable: M02
// Convierte EstadoCita al color de estado definido en AppTheme.xaml (única fuente de verdad).
using BarberTurno.Models;
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un valor <see cref="EstadoCita"/> en el <see cref="Color"/> de estado
/// leyendo directamente los recursos de <c>AppTheme.xaml</c> según el tema claro u oscuro activo.
/// Si no encuentra el recurso en el diccionario de la aplicación, devuelve <see cref="Colors.Gray"/> como respaldo.
/// </summary>
public class EstadoCitaToColorConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        EstadoCita estado;
        if (value is EstadoCita e)
            estado = e;
        else if (value is string s && Enum.TryParse<EstadoCita>(s, true, out var parsed))
            estado = parsed;
        else
            return Colors.Gray;

        var claveBase = estado switch
        {
            EstadoCita.Confirmada => "EstadoConfirmada",
            EstadoCita.Pendiente  => "EstadoPendiente",
            EstadoCita.Rechazada  => "EstadoRechazada",
            EstadoCita.Cancelada  => "EstadoCancelada",
            EstadoCita.Atendida   => "EstadoAtendida",
            EstadoCita.Ausente    => "EstadoAusente",
            _                     => ""
        };

        if (string.IsNullOrEmpty(claveBase))
            return Colors.Gray;

        var recursos = Application.Current?.Resources;
        if (recursos is null)
            return Colors.Gray;

        // 1. Intentar obtener la variante específica del tema activo (Claro / Oscuro) para mejor contraste
        var esOscuro  = Application.Current?.RequestedTheme == AppTheme.Dark;
        var claveTema = esOscuro ? $"{claveBase}Oscuro" : $"{claveBase}Claro";

        if (recursos.TryGetValue(claveTema, out var recursoTema) && recursoTema is Color colorTema)
            return colorTema;

        // 2. Si no existe variante por tema, buscar la clave base en AppTheme.xaml
        if (recursos.TryGetValue(claveBase, out var recursoBase) && recursoBase is Color colorBase)
            return colorBase;

        // 3. Respaldo seguro si el diccionario aún no fue registrado
        return Colors.Gray;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("EstadoCitaToColorConverter no soporta conversión inversa.");
}

/// <summary>
/// Convierte un valor <see cref="EstadoCita"/> o cadena en un texto legible para la UI.
/// </summary>
public class EstadoCitaToTextoConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        EstadoCita estado;
        if (value is EstadoCita e)
            estado = e;
        else if (value is string s && Enum.TryParse<EstadoCita>(s, true, out var parsed))
            estado = parsed;
        else
            return value?.ToString() ?? string.Empty;

        return estado switch
        {
            EstadoCita.Pendiente  => "Pendiente",
            EstadoCita.Confirmada => "Confirmada",
            EstadoCita.Rechazada  => "Rechazada",
            EstadoCita.Cancelada  => "Cancelada",
            EstadoCita.Atendida   => "Atendida",
            EstadoCita.Ausente    => "Ausente",
            _                     => estado.ToString()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("EstadoCitaToTextoConverter no soporta conversión inversa.");
}
