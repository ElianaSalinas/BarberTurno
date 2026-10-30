using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Implementa IServicioRepository sobre SQLite (sqlite-net-pcl).
namespace BarberTurno.Data.Repositories;

public sealed class ServicioRepository : IServicioRepository
{
    private readonly IAppDatabase _db;


    public ServicioRepository(IAppDatabase db) => _db = db;


    public async Task<List<Servicio>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Servicio>().OrderBy(s => s.Nombre).ToListAsync();
    }


    public async Task<List<Servicio>> GetByBarberoAsync(int barberoId)
    {
        var conn = await _db.GetConnectionAsync();
        // JOIN con la tabla puente. Los nombres de tabla se leen del mapeo ([Table] de M02),
        // así la consulta no se rompe si M02 los cambia
        var servicios = (await conn.GetMappingAsync<Servicio>()).TableName;
        var puente = (await conn.GetMappingAsync<BarberoServicio>()).TableName;

        var sql = $"SELECT s.* FROM \"{servicios}\" s " +
                  $"INNER JOIN \"{puente}\" bs ON bs.\"{nameof(BarberoServicio.ServicioId)}\" = s.\"{nameof(Servicio.Id)}\" " +
                  $"WHERE bs.\"{nameof(BarberoServicio.BarberoId)}\" = ? " +
                  $"ORDER BY s.\"{nameof(Servicio.Nombre)}\"";
        // El valor va como parámetro (?), nunca concatenado: sin riesgo de inyección SQL
        return await conn.QueryAsync<Servicio>(sql, barberoId);
    }


    public async Task<Servicio?> GetByIdAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Servicio>(id);
    }


    public async Task UpsertAsync(Servicio servicio)
    {
        ArgumentNullException.ThrowIfNull(servicio);
        var conn = await _db.GetConnectionAsync();
        // Seguro: Servicio no tiene más índice único que su Id
        await conn.InsertOrReplaceAsync(servicio);
    }


    public async Task ReplaceAllAsync(IEnumerable<Servicio> servicios)
    {
        var lista = (servicios ?? []).GroupBy(s => s.Id).Select(g => g.Last()).ToList();
        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            // No se tocan las relaciones BarberoServicio: SyncService (M08) las reescribe a continuación
            // con IBarberoRepository.ReplaceAllAsync. Borrarlas aquí dejaría a los barberos sin servicios
            // entre un paso y otro.
            db.DeleteAll<Servicio>();
            db.InsertAll(lista, runInTransaction: false);
        });
    }


    public async Task DeleteAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            // Aquí sí se borran sus relaciones: sin un paso posterior que las arregle quedarían huérfanas
            db.Table<BarberoServicio>().Delete(r => r.ServicioId == id);
            db.Delete<Servicio>(id);
        });
    }


    public async Task DeleteAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            // Igual que DeleteAsync: sin servicios, sus relaciones con barberos no tienen sentido (quedarían huérfanas)
            db.DeleteAll<BarberoServicio>();
            db.DeleteAll<Servicio>();
        });
    }
}