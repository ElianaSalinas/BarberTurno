using System.Globalization;

namespace BarberTurno.Helpers;

/// <summary>Da formato consistente a datos mostrados en las pantallas del cliente.</summary>
public static class FormatoCliente
{
    private static readonly CultureInfo CulturaEs = new("es-ES");

    public static string Iniciales(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return "?";
        var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][0].ToString().ToUpperInvariant();
        return $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
    }

    public static string Duracion(int minutos) => $"{minutos} min";

    public static string Precio(decimal precio) => precio % 1 == 0 ? $"RD$ {precio:N0}" : $"RD$ {precio:N2}";

    public static string FechaLarga(DateTime fecha) =>
        fecha.ToString("dddd, d 'de' MMMM", CulturaEs);

    public static string RangoHora(DateTime inicio, DateTime fin) =>
        $"{inicio:HH:mm} - {fin:HH:mm}";

    public static string Hora(DateTime fecha) => fecha.ToString("HH:mm");
}

/// <summary>Define las claves de objetos que viajan entre pantallas mediante Shell.</summary>
public static class ParametrosNavegacion
{
    public const string Barbero = "barbero";
    public const string Servicio = "servicio";
    public const string Slot = "slot";
    public const string Origen = "origen";
}

/// <summary>Define las claves de identificadores pasados por navegación.</summary>
public static class NavParams
{
    public const string BarberoId = "barberoId";
    public const string ServicioId = "servicioId";
}

/// <summary>Identifica la pantalla desde la que se inició una reserva.</summary>
public static class OrigenReserva
{
    public const string Barberos = "barberos";
    public const string Servicios = "servicios";
    public const string MisCitas = "miscitas";
}

/// <summary>Reúne rutas Shell específicas del flujo de cliente.</summary>
public static class RutasCliente
{
    public const string MisCitas = "//cliente/miscitas";
    public static string Atras(int niveles) => string.Join("/", Enumerable.Repeat("..", niveles));
}

/// <summary>Centraliza los mensajes mostrados durante el flujo de reserva del cliente.</summary>
public static class MensajesCliente
{
    public const string FaltanDatosReserva = "Faltan datos para realizar la reserva.";
    public const string TituloHoraOcupada = "Hora no disponible";
    public const string HoraYaNoDisponible = "La hora seleccionada ya fue ocupada por otro cliente.";
    public const string ErrorReservaGenerico = "No pudimos completar tu reserva. Intenta de nuevo.";
    public const string HoraYaPaso = "La hora seleccionada ya pasó. Elige un horario futuro.";
    public const string TituloReservaConfirmada = "¡Reserva confirmada!";
    public const string TituloReservaPendiente = "Reserva guardada";
    public const string ReservaPendiente = "Tu cita quedó guardada en el dispositivo y se confirmará automáticamente al recuperar internet.";
    public const string SinConexionHorasAproximadas = "Sin conexión: mostrando horarios aproximados según el horario habitual.";
    public const string ErrorCargarHoras = "No pudimos cargar los horarios disponibles. Intenta de nuevo.";
    public const string EligeUnaHora = "Elige una hora para continuar con tu reserva.";
}
