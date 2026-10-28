using SQLite;

// Responsable: Módulo 3 · Base de datos SQLite
// Se lanza cuando el índice único rechaza una cita porque el barbero ya tiene otra cita activa
// a esa hora. Las capas superiores (BookingService, ViewModels) capturan esta excepción del
// dominio y nunca ven la SQLiteException original.
namespace BarberTurno.Data.Exceptions;

public sealed class SlotOcupadoException : Exception
{
    public int BarberoId { get; }
    public DateTime Inicio { get; }

    public SlotOcupadoException(int barberoId, DateTime inicio, Exception? innerException = null)
        : base($"El barbero {barberoId} ya tiene una cita activa que inicia el {inicio:yyyy-MM-dd HH:mm}.", innerException)
    {
        BarberoId = barberoId;
        Inicio = inicio;
    }

    /// <summary>
    /// Ejecuta una escritura de cita y traduce el choque de horario a SlotOcupadoException.
    /// Uso en CitaRepository (módulo 4):
    /// <code>await SlotOcupadoException.TraducirAsync(() => conexion.InsertAsync(cita), cita.BarberoId, cita.Inicio);</code>
    /// </summary>
    public static async Task<T> TraducirAsync<T>(Func<Task<T>> escritura, int barberoId, DateTime inicio)
    {
        try
        {
            return await escritura().ConfigureAwait(false);
        }
        catch (SQLiteException ex) when (EsHorarioOcupado(ex))
        {
            throw new SlotOcupadoException(barberoId, inicio, ex);
        }
    }

    /// <summary>
    /// True si el error viene del índice único (BarberoId, Inicio) de citas activas.
    /// Un Id de cita repetido también es un error UNIQUE, pero NO es "hora ocupada"
    /// (es la misma cita enviada dos veces), por eso se comprueban las columnas del mensaje.
    /// </summary>
    public static bool EsHorarioOcupado(SQLiteException ex) =>
        ex.Result == SQLite3.Result.Constraint
        && ex.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
        && ex.Message.Contains($".{AppDatabase.ColumnaBarbero}", StringComparison.Ordinal)
        && ex.Message.Contains($".{AppDatabase.ColumnaInicio}", StringComparison.Ordinal);
}
