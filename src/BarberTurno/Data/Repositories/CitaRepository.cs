using BarberTurno.Data.Exceptions;
using BarberTurno.Models;
using SQLite;

// Responsable: M04 (Repositorios)
// Implementa ICitaRepository sobre SQLite (sqlite-net-pcl).
//
// Protección contra citas solapadas en esta capa:
//  - El índice único filtrado de M03 (BarberoId, Inicio) rechaza dos citas que empiezan a la misma hora.
//    Aquí se traduce ese error a SlotOcupadoException con la ayuda que dejó M03.
//  - ExisteSolapamientoAsync detecta los solapamientos parciales (10:00-10:30 frente a 10:15-10:45).
//  - La autoridad final es el servidor (M06/M07).
namespace BarberTurno.Data.Repositories;

public sealed class CitaRepository : ICitaRepository
{
    private readonly IAppDatabase _db;

    public CitaRepository(IAppDatabase db) => _db = db;

    // Estados que NO ocupan hora. Se reutiliza la constante de M03 para que el repositorio y el
    // índice único usen exactamente la misma regla (una sola fuente de verdad).
    // Es List<T> (no arreglo) a propósito: en C# 14 el Contains de un arreglo puede enlazarse a la
    // versión de Span, que sqlite-net no sabe traducir a SQL; List<T>.Contains sí se traduce a IN (...).
    private static readonly List<EstadoCita> Liberan = AppDatabase.EstadosQueLiberanHorario.ToList();


    // ---------------- Consultas ----------------
    public async Task<Cita?> GetByIdAsync(Guid id)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<Cita>(id);
    }

    public async Task<List<Cita>> GetByClienteAsync(int clienteId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cita>()
            .Where(c => c.ClienteId == clienteId)
            .OrderBy(c => c.Inicio)
            .ToListAsync();
    }


    public async Task<List<Cita>> GetProximasClienteAsync(int clienteId, DateTime desde)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cita>()
            .Where(c => c.ClienteId == clienteId && c.Inicio >= desde)
            .OrderBy(c => c.Inicio)
            .ToListAsync();
    }


    public async Task<List<Cita>> GetAnterioresClienteAsync(int clienteId, DateTime desde)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cita>()
            .Where(c => c.ClienteId == clienteId && c.Inicio < desde)
            .OrderByDescending(c => c.Inicio)
            .ToListAsync();
    }


    public async Task<List<Cita>> GetAgendaBarberoAsync(int barberoId, DateOnly dia)
    {
        // sqlite-net no traduce DateOnly a SQL: se usa el rango [día 00:00, día siguiente 00:00).
        var desde = dia.ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);

        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cita>()
            .Where(c => c.BarberoId == barberoId && c.Inicio >= desde && c.Inicio < hasta)
            .OrderBy(c => c.Inicio)
            .ToListAsync();
    }

    public async Task<List<Cita>> GetHistorialClienteAsync(int clienteId, int? barberoId = null,
                                                           DateTime? antesDe = null, int skip = 0, int take = 50)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(skip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(take);

        var conn = await _db.GetConnectionAsync();
        var query = conn.Table<Cita>().Where(c => c.ClienteId == clienteId);

        // Los filtros opcionales se añaden solo si tienen valor (la consulta SQL queda más simple).
        if (barberoId is int b)
            query = query.Where(c => c.BarberoId == b);
        if (antesDe is DateTime limite)
            query = query.Where(c => c.Inicio < limite);

        return await query
            .OrderByDescending(c => c.Inicio)
            .Skip(skip)     // OFFSET
            .Take(take)     // LIMIT
            .ToListAsync();
    }

    public async Task<List<Cita>> GetPendientesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<Cita>()
            .Where(c => c.PendienteSincronizar == true)
            .OrderBy(c => c.Inicio)
            .ToListAsync();
    }

    public async Task<bool> ExisteSolapamientoAsync(int barberoId, DateTime inicio, DateTime fin, Guid? excluirId = null)
    {
        var conn = await _db.GetConnectionAsync();
        var query = conn.Table<Cita>().Where(c =>
            c.BarberoId == barberoId
            && !Liberan.Contains(c.Estado)          // solo las que ocupan hora
            && c.Inicio < fin && c.Fin > inicio);   // regla de solapamiento del plan

        if (excluirId is Guid excluir)
            query = query.Where(c => c.Id != excluir);

        return await query.FirstOrDefaultAsync() is not null;   // LIMIT 1
    }


    // ---------------- Escrituras ----------------
    public async Task InsertAsync(Cita cita)
    {
        ArgumentNullException.ThrowIfNull(cita);
        var conn = await _db.GetConnectionAsync();
        // Ayuda de M03: si el índice único rechaza la fila, convierte la SQLiteException en SlotOcupadoException.
        await SlotOcupadoException.TraducirAsync(() => conn.InsertAsync(cita), cita.BarberoId, cita.Inicio);
    }


    public Task<bool> ActualizarEstadoAsync(Guid id, EstadoCita estado, string? motivo = null) =>
        CambiarEstadoAsync(id, estado, motivo, marcarSincronizada: false);


    public Task<bool> MarcarSincronizadaAsync(Guid id, EstadoCita estado, string? motivo = null) =>
        CambiarEstadoAsync(id, estado, motivo, marcarSincronizada: true);


    public async Task UpsertRangeAsync(IEnumerable<Cita> citas)
    {
        // Primero las que liberan hora: así "mi cita pasó a Rechazada" se aplica antes que
        // "la del otro cliente quedó Confirmada" a esa misma hora, sin importar el orden del lote.
        var lista = (citas ?? []).OrderBy(c => Liberan.Contains(c.Estado) ? 0 : 1).ToList();
        if (lista.Count == 0)
            return;

        var conn = await _db.GetConnectionAsync();
        await conn.RunInTransactionAsync(db =>
        {
            foreach (var cita in lista)
            {
                try
                {
                    // UPDATE por Id; si no existía (0 filas), INSERT. Nunca INSERT OR REPLACE: borraría en
                    // silencio la cita local con otro Id que choque en (BarberoId, Inicio).
                    if (db.Update(cita) == 0)
                        db.Insert(cita);
                }
                catch (SQLiteException ex) when (SlotOcupadoException.EsHorarioOcupado(ex))
                {
                    // Lanzar dentro de la transacción la deshace completa: no se aplica ningún cambio.
                    throw new SlotOcupadoException(cita.BarberoId, cita.Inicio, ex);
                }
            }
        });
    }


    public async Task DeleteAllAsync()
    {
        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAllAsync<Cita>();
    }


    // ---------------- Privados ----------------
    /// <summary>
    /// UPDATE de solo las columnas necesarias, en una sentencia (atómico, sin leer la cita antes).
    /// Devuelve false si ninguna fila tiene ese Id.
    /// </summary>
    private async Task<bool> CambiarEstadoAsync(Guid id, EstadoCita estado, string? motivo, bool marcarSincronizada)
    {
        var conn = await _db.GetConnectionAsync();
        var mapa = await conn.GetMappingAsync<Cita>();

        var set = $"\"{nameof(Cita.Estado)}\" = ?, \"{nameof(Cita.MotivoRechazo)}\" = ?";
        if (marcarSincronizada)
            set += $", \"{nameof(Cita.PendienteSincronizar)}\" = 0";
        var sql = $"UPDATE \"{mapa.TableName}\" SET {set} WHERE \"{nameof(Cita.Id)}\" = ?";

        try
        {
            // sqlite-net guarda los enum como enteros: se pasa (int)estado. Los valores van como parámetros.
            return await conn.ExecuteAsync(sql, (int)estado, motivo, id) > 0;
        }
        catch (SQLiteException ex) when (SlotOcupadoException.EsHorarioOcupado(ex))
        {
            // Reactivar una cita sobre una hora ya ocupada: se informa con la hora de la cita afectada.
            var cita = await conn.FindAsync<Cita>(id);
            throw new SlotOcupadoException(cita?.BarberoId ?? 0, cita?.Inicio ?? default, ex);
        }
    }
}