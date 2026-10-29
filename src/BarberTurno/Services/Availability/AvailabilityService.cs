// Responsable: M08
// Implementa IAvailabilityService.
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Availability;

public class AvailabilityService : IAvailabilityService
{
    private readonly IBarberTurnoApi _api;

    public AvailabilityService(IBarberTurnoApi api)
    {
        _api = api;
    }

    public async Task<IReadOnlyList<SlotDisponible>> GetHorasLibresAsync(int barberoId, int servicioId, DateOnly fecha)
    {
        var result = await _api.GetDisponibilidadAsync(barberoId, servicioId, fecha);
        if (result.Exito && result.Datos != null)
        {
            return result.Datos.Select(s => new SlotDisponible
            {
                Inicio = s.InicioUtc.ToLocalTime(),
                Fin = s.FinUtc.ToLocalTime()
            }).ToList();
        }

        // Si no hay respuesta del servidor, generar horarios regulares (9:00 a 18:00)
        var slots = new List<SlotDisponible>();
        var actual = fecha.ToDateTime(new TimeOnly(9, 0));
        var finDia = fecha.ToDateTime(new TimeOnly(18, 0));
        while (actual.AddMinutes(30) <= finDia)
        {
            slots.Add(new SlotDisponible
            {
                Inicio = actual,
                Fin = actual.AddMinutes(30)
            });
            actual = actual.AddMinutes(30);
        }

        return slots;
    }
}
