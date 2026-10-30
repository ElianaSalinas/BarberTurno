using BarberTurno.Models;

namespace BarberTurno.Data.Repositories;

/// <summary>Ofrece nombres de compatibilidad para operaciones frecuentes de los repositorios locales.</summary>
public static class RepositoryExtensions
{
    public static Task SaveItemAsync(this IBarberoRepository repo, Barbero barbero) =>
        repo.UpsertAsync(barbero, Enumerable.Empty<BarberoServicio>());

    public static Task SaveItemAsync(this IServicioRepository repo, Servicio servicio) =>
        repo.UpsertAsync(servicio);

    public static Task SaveItemAsync(this ICitaRepository repo, Cita cita) =>
        repo.UpsertRangeAsync(new[] { cita });

    public static Task<List<Cita>> GetPendientesDeSincronizarAsync(this ICitaRepository repo) =>
        repo.GetPendientesAsync();

    public static Task InsertOrUpdateAsync(this ICitaRepository repo, Cita cita) =>
        repo.UpsertRangeAsync(new[] { cita });
}
