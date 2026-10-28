using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Implementa IBarberoRepository sobre SQLite (sqlite-net-pcl).
// La conexión la entrega IAppDatabase (M03), ya inicializada: tablas, índices y migraciones listos.
namespace BarberTurno.Data.Repositories;

public sealed class BarberoRepository : IBarberoRepository
{
    private readonly IAppDatabase _db;

    public BarberoRepository(IAppDatabase db) => _db = db;

    public async Task<List<Barbero>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Barbero>().OrderBy(b => b.Nombre).ToListAsync();
    }


    public async Task<Barbero?> GetByIdAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        // FindAsync busca por clave primaria y devuelve null si no existe (GetAsync lanzaría excepción).
        return await conn.FindAsync<Barbero>(id);
    }


    public async Task<bool> ExisteAlgunoAsync()
    {
        var conn = await _db.GetConnectionAsync();
        // LIMIT 1: basta con encontrar una fila; no se cuentan ni se cargan todas.
        return await conn.Table<Barbero>().FirstOrDefaultAsync() is not null;
    }


    public async Task UpsertAsync(Barbero barbero, IEnumerable<BarberoServicio> relaciones)
    {
        ArgumentNullException.ThrowIfNull(barbero);
        var nuevas = NuevasRelaciones(relaciones).Where(r => r.BarberoId == barbero.Id).ToList();

        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            // Seguro aquí: Barbero no tiene más índice único que su Id, así que REPLACE solo
            // sustituye la fila del mismo barbero (no ocurre el problema que sí tendría Cita).
            db.InsertOrReplace(barbero);
            db.Table<BarberoServicio>().Delete(r => r.BarberoId == barbero.Id);
            db.InsertAll(nuevas, runInTransaction: false);
        });
    }


    public async Task ReplaceAllAsync(IEnumerable<Barbero> barberos, IEnumerable<BarberoServicio> relaciones)
    {
        // Si el servidor repite un Id, se queda la última versión (evita violar la clave primaria).
        var lista = (barberos ?? []).GroupBy(b => b.Id).Select(g => g.Last()).ToList();
        var rels = NuevasRelaciones(relaciones).ToList();

        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            db.DeleteAll<BarberoServicio>();
            db.DeleteAll<Barbero>();
            db.InsertAll(lista, runInTransaction: false);
            db.InsertAll(rels, runInTransaction: false);
        });
    }


    public async Task DeleteAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            db.Table<BarberoServicio>().Delete(r => r.BarberoId == id);
            db.Delete<Barbero>(id);
            // Las citas de este barbero NO se borran: forman parte del historial.
        });
    }


    public async Task DeleteAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            db.DeleteAll<BarberoServicio>();
            db.DeleteAll<Barbero>();
        });
    }


    /// <summary>
    /// Copia las relaciones sin su Id (lo asigna SQLite con AutoIncrement) y quita pares repetidos,
    /// que el índice único (BarberoId, ServicioId) de M02/M03 rechazaría. No modifica los objetos recibidos.
    /// </summary>
    private static IEnumerable<BarberoServicio> NuevasRelaciones(IEnumerable<BarberoServicio>? relaciones) =>
        (relaciones ?? [])
            .Select(r => (r.BarberoId, r.ServicioId))
            .Distinct()
            .Select(p => new BarberoServicio { BarberoId = p.BarberoId, ServicioId = p.ServicioId });
}