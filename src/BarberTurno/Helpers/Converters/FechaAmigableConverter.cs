// Responsable: M02
// Convierte un DateTime a texto amigable en español (es-DO con respaldo a "es") en formato de 12 horas AM/PM.
using System.Globalization;

namespace BarberTurno.Helpers.Converters;

/// <summary>
/// Convierte un <see cref="DateTime"/> a una cadena amigable en español dominicano ("es-DO")
/// garantizando siempre el formato de 12 horas con AM/PM.
/// Si el dispositivo no dispone de la cultura "es-DO", usa "es" como respaldo sin lanzar excepción.
/// Ejemplos de salida:
/// <list type="bullet">
///   <item>Hoy a las 3:30 PM</item>
///   <item>Mañana a las 10:00 AM</item>
///   <item>lun 27 oct, 9:00 AM</item>
/// </list>
/// </summary>
public class FechaAmigableConverter : IValueConverter
{
    private static readonly CultureInfo CulturaEspanol = CrearCulturaSegura();

    private static CultureInfo CrearCulturaSegura()
    {
        CultureInfo cultura;
        try
        {
            cultura = new CultureInfo("es-DO");
        }
        catch (CultureNotFoundException)
        {
            try
            {
                // Respaldo a español neutro si el dispositivo no tiene es-DO
                cultura = new CultureInfo("es");
            }
            catch (CultureNotFoundException)
            {
                cultura = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            }
        }

        // Asegura que siempre muestre exactamente "AM" y "PM" en formato 12h
        cultura.DateTimeFormat.AMDesignator = "AM";
        cultura.DateTimeFormat.PMDesignator = "PM";
        return CultureInfo.ReadOnly(cultura);
    }

    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime fecha) return "";

        // Si por alguna razón viene en UTC, se pasa a hora local antes de mostrar
        var local = fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha;
        var hoy   = DateTime.Today;

        // "h:mm tt" fuerza formato de 12 horas (1–12) con designador AM/PM
        var hora = local.ToString("h:mm tt", CulturaEspanol);

        if (local.Date == hoy)
            return $"Hoy a las {hora}";

        if (local.Date == hoy.AddDays(1))
            return $"Mañana a las {hora}";

        // Formato: "lun 27 oct, 9:00 AM"
        var diaSemana = local.ToString("ddd", CulturaEspanol).TrimEnd('.');
        var diaMes    = local.ToString("d MMM", CulturaEspanol).TrimEnd('.');
        return $"{diaSemana} {diaMes}, {hora}";
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("FechaAmigableConverter no soporta conversión inversa.");
}
