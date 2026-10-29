using BarberTurno.Models;

// Responsable: M04 (Repositorios)
// Contrato del repositorio de barberos.

// Regla de diseño: en SQLite solo existen los barberos VINCULADOS al usuario de la sesión.
// El vínculo cliente-barbero (por código del barbero) vive en el servidor; GET /barberos ya devuelve
// solo los vinculados. Por eso "barbero en la base local" == "barbero vinculado".
// Este repositorio no valida códigos ni decide nada: solo guarda y consulta.
namespace BarberTurno.Data.Repositories;

public interface IBarberoRepository
{
    // Barberos vinculados guardados localmente, ordenados por nombre
    Task<List<Barbero>> GetAllAsync();


    // Barbero por Id, o <c>null</c> si no está en la base local
    Task<Barbero?> GetByIdAsync(int id);


    // Indica si hay al menos un barbero guardado. Lo usa el arranque/login del cliente para decidir, si muestra la pantalla "Introduce el código de tu barbero" o entra directo al Shell
    Task<bool> ExisteAlgunoAsync();


    // Inserta o actualiza UN barbero (p. ej. justo después de vincular un código) junto con los servicios que ofrece. Sustituye sus relaciones anteriores; no toca a los demás barberos
    Task UpsertAsync(Barbero barbero, IEnumerable<BarberoServicio> relaciones);


    // Reemplaza todo el catálogo local por lo recibido de la API (sincronización: el servidor manda). Es atómico: o se aplica completo o no cambia nada
    Task ReplaceAllAsync(IEnumerable<Barbero> barberos, IEnumerable<BarberoServicio> relaciones);


    // Elimina un barbero y sus relaciones con servicios (desvincular). Sus citas NO se borran, para conservar el historial
    Task DeleteAsync(int id);


    // Borra todos los barberos y relaciones (cierre de sesión)
    Task DeleteAllAsync();
}