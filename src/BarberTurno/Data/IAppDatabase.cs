// Responsable: M02
// Acceso a la base SQLite local.
namespace BarberTurno.Data;

public interface IAppDatabase
{
    /// <summary>Crea la base, las tablas y los índices (incluido el índice único de citas activas).</summary>
    Task InitAsync();
}

