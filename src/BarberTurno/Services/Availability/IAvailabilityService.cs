using BarberTurno.Models;
// Responsable: M08
// Calcula horas libres según duración del servicio y citas existentes.
namespace BarberTurno.Services.Availability;

public interface IAvailabilityService
{
    /// <summary>Con conexión: pide al servidor (autoridad). Sin conexión: cálculo aproximado con datos locales.</summary>
    Task<IReadOnlyList<SlotDisponible>> GetHorasLibresAsync(int barberoId, int servicioId, DateOnly fecha);
}

