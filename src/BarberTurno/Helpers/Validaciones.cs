// Responsable: M5
// Métodos reutilizables de validación para entradas de usuario (MVVM).
using System.Globalization;
using System.Text.RegularExpressions;

namespace BarberTurno.Helpers;

public static partial class Validaciones
{
    // Solo letras (incluidas tildes á, é, í, ó, ú, ü y ñ/Ñ) y espacios.
    [GeneratedRegex(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]*$")]
    private static partial Regex SoloLetrasRegex();

    // Solo números, espacios y guiones para teléfono.
    [GeneratedRegex(@"^[0-9\s-]*$")]
    private static partial Regex TelefonoCaracteresRegex();

    // Exactamente 10 dígitos numéricos.
    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex DiezDigitosRegex();

    // Usuario estricto (sin espacios): letras, números, punto, guion y guion bajo.
    [GeneratedRegex(@"^[a-zA-Z0-9._-]*$")]
    private static partial Regex UsuarioEstrictoRegex();

    // Usuario o correo (para Login, sin espacios): permite además arroba '@'.
    [GeneratedRegex(@"^[a-zA-Z0-9._@\-]*$")]
    private static partial Regex UsuarioOEmailRegex();

    // Precio en tiempo real: números y como máximo un punto decimal.
    [GeneratedRegex(@"^\d*(\.\d*)?$")]
    private static partial Regex PrecioCaracteresRegex();

    // Entero positivo (para duración en minutos).
    [GeneratedRegex(@"^\d*$")]
    private static partial Regex SoloDigitosRegex();

    // Nombre de servicio: letras, números, espacios y signos (+, -, /, ., paréntesis).
    [GeneratedRegex(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑüÜ\s+\-/.()]*$")]
    private static partial Regex NombreServicioCaracteresRegex();

    public static bool EsSoloLetras(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        return SoloLetrasRegex().IsMatch(texto);
    }

    public static bool EsTelefonoCaracteresValidos(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        return TelefonoCaracteresRegex().IsMatch(texto);
    }

    public static string LimpiarTelefono(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return string.Empty;
        return Regex.Replace(texto, @"[\s-]", "");
    }

    public static bool EsTelefonoValido(string? texto)
    {
        string limpio = LimpiarTelefono(texto);
        return DiezDigitosRegex().IsMatch(limpio);
    }

    public static bool EsUsuarioCaracteresValidos(string? texto, bool permiteArroba = false)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        if (texto.Contains(' ')) return false;
        return permiteArroba
            ? UsuarioOEmailRegex().IsMatch(texto)
            : UsuarioEstrictoRegex().IsMatch(texto);
    }

    public static bool EsPrecioCaracteresValidos(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        return PrecioCaracteresRegex().IsMatch(texto);
    }

    public static bool EsPrecioValido(string? texto, out decimal precio)
    {
        precio = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        if (!decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out precio))
        {
            // Intentar también con coma si la cultura del dispositivo la usa
            if (!decimal.TryParse(texto.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out precio))
                return false;
        }
        return precio > 0;
    }

    public static bool EsEnteroCaracteresValidos(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        return SoloDigitosRegex().IsMatch(texto);
    }

    public static bool EsDuracionValida(string? texto, out int duracion)
    {
        duracion = 0;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        if (!int.TryParse(texto.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out duracion))
            return false;
        return duracion >= 5 && duracion <= 480;
    }

    public static bool EsNombreServicioCaracteresValidos(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return true;
        return NombreServicioCaracteresRegex().IsMatch(texto);
    }
}
