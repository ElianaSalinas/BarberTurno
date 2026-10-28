// Responsable: M02
// Convierte un DateTime a texto amigable en español para mostrar en la UI.
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un <see cref="DateTime"/> a una cadena amigable en español.
/// Ejemplos de salida:
/// <list type="bullet">
///   <item>Hoy a las 3:30 PM</item>
///   <item>Mañana a las 10:00 AM</item>
///   <item>lun 27 oct, 9:00 AM</item>
/// </list>
/// <para>
/// La entrada puede estar en UTC o local; si está en UTC se convierte a hora local antes de formatear.
/// </para>
/// </summary>
public class FechaAmigableConverter : IValueConverter
{
    private static readonly CultureInfo Espanol = new("es-ES");

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime fecha) return "";

        // Asegurar hora local para mostrar
        var local = fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha;
        var hoy   = DateTime.Today;

        var hora = local.ToString("h:mm tt", Espanol);

        if (local.Date == hoy)
            return $"Hoy a las {hora}";

        if (local.Date == hoy.AddDays(1))
            return $"Mañana a las {hora}";

        // Formato: "lun 27 oct, 9:00 AM"
        var diaSemana = local.ToString("ddd", Espanol);
        var diaMes    = local.ToString("d MMM", Espanol);
        return $"{diaSemana} {diaMes}, {hora}";
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("FechaAmigableConverter no soporta conversión inversa.");
}
