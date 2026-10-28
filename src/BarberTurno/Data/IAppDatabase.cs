using SQLite;

// Responsable: Módulo 3 · Base de datos SQLite
// Contrato de acceso a la base SQLite local. Los repositorios (módulo 4) dependen de esta
// interfaz, nunca de AppDatabase directamente (principio de inversión de dependencias).
namespace BarberTurno.Data;

public interface IAppDatabase
{
    /// <summary>
    /// Crea la base, las tablas y los índices (incluido el índice único de citas activas),
    /// aplica las migraciones pendientes y la semilla. Es idempotente: llamarlo varias veces
    /// no repite trabajo.
    /// </summary>
    Task InitAsync();

    /// <summary>
    /// Devuelve la conexión compartida, ya inicializada. Si la base aún no se ha inicializado,
    /// la inicializa primero, así que ningún repositorio necesita llamar a <see cref="InitAsync"/>.
    /// </summary>
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
