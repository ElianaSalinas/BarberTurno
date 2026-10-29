using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Implementa IClienteRepository sobre SQLite (sqlite-net-pcl).
namespace BarberTurno.Data.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    // SQLite limita cuántos parámetros (?) admite una consulta; se consulta por bloques para no rozar ese límite
    private const int TamanoBloque = 500;


    private readonly IAppDatabase _db;


    public ClienteRepository(IAppDatabase db) => _db = db;


    public async Task<Cliente?> GetByIdAsync(int id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Cliente>(id);
    }


    public async Task<List<Cliente>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var unicos = (ids ?? []).Distinct().ToList();
        if (unicos.Count == 0)
            return [];

        var conn = await _db.GetConnectionAsync();
        var resultado = new List<Cliente>(unicos.Count);
        // List<int> (no arreglo): ver la nota de C# 14 sobre Contains en CitaRepository.
        foreach (var bloque in unicos.Chunk(TamanoBloque).Select(b => b.ToList()))
        {
            // sqlite-net traduce Contains a "WHERE Id IN (?, ?, ...)": una consulta por bloque, no una por cliente.
            resultado.AddRange(await conn.Table<Cliente>().Where(c => bloque.Contains(c.Id)).ToListAsync());
        }
        return resultado.OrderBy(c => c.Nombre).ToList();
    }


    public async Task<List<Cliente>> GetAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cliente>().OrderBy(c => c.Nombre).ToListAsync();
    }


    public async Task UpsertAsync(Cliente cliente)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        var conn = await _db.GetConnectionAsync();
        // Seguro: Cliente no tiene más índice único que su Id.
        await conn.InsertOrReplaceAsync(cliente);
    }


    public async Task UpsertRangeAsync(IEnumerable<Cliente> clientes)
    {
        var lista = (clientes ?? []).ToList();
        if (lista.Count == 0)
            return;

        var conn = await _db.GetConnectionAsync();
        // Una transacción: una sola escritura a disco en lugar de una por cliente (mucho más rápido).
        await conn.RunInTransactionAsync(db =>
        {
            foreach (var c in lista)
                db.InsertOrReplace(c);
        });
    }


    public async Task DeleteAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAllAsync<Cliente>();
    }
}