// Responsable: M08
// Implementa IAvailabilityService: consulta la API (autoridad) y si falla calcula franjas reales con el horario del barbero y citas de SQLite.
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Availability;

/// <summary>Obtiene horarios libres desde la API y calcula un respaldo local con SQLite si no hay conexión.</summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly IBarberTurnoApi _api;
    private readonly IBarberoRepository _barberoRepo;
    private readonly IServicioRepository _servicioRepo;
    private readonly ICitaRepository _citaRepo;

    /// <summary>Recibe los clientes remoto y locales que participan en el cálculo de disponibilidad.</summary>
    public AvailabilityService(
        IBarberTurnoApi api,
        IBarberoRepository barberoRepo,
        IServicioRepository servicioRepo,
        ICitaRepository citaRepo)
    {
        _api = api;
        _barberoRepo = barberoRepo;
        _servicioRepo = servicioRepo;
        _citaRepo = citaRepo;
    }

    /// <summary>Devuelve los bloques disponibles para el barbero, servicio y fecha solicitados.</summary>
    public async Task<IReadOnlyList<SlotDisponible>> GetHorasLibresAsync(int barberoId, int servicioId, DateOnly fecha)
    {
        // 1. Intentar consultar disponibilidad al servidor (autoridad)
        try
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
        }
        catch
        {
            // Fallback a cálculo local
        }

        // 2. Si la API falla, calcular con los datos locales del barbero y sus citas para evitar franjas ficticias
        var barbero = await _barberoRepo.GetByIdAsync(barberoId);
        var servicio = await _servicioRepo.GetByIdAsync(servicioId);
        if (barbero is null || servicio is null)
            return Array.Empty<SlotDisponible>();

        // Verificar si el barbero trabaja el día de la semana solicitado (0 = Domingo, 1 = Lunes, ..., 6 = Sábado)
        var diaSemana = (int)fecha.DayOfWeek;
        var diasLaborables = (barbero.DiasTrabajo ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => int.TryParse(d, out var n) ? n : -1)
            .ToHashSet();

        if (diasLaborables.Count > 0 && !diasLaborables.Contains(diaSemana))
            return Array.Empty<SlotDisponible>();

        var duracion = TimeSpan.FromMinutes(servicio.DuracionMin > 0 ? servicio.DuracionMin : 30);
        var horaEntrada = barbero.HoraEntrada > TimeSpan.Zero ? barbero.HoraEntrada : new TimeSpan(9, 0, 0);
        var horaSalida = barbero.HoraSalida > horaEntrada ? barbero.HoraSalida : new TimeSpan(18, 0, 0);

        var slots = new List<SlotDisponible>();
        var actual = fecha.ToDateTime(TimeOnly.FromTimeSpan(horaEntrada));
        var limite = fecha.ToDateTime(TimeOnly.FromTimeSpan(horaSalida));

        while (actual + duracion <= limite)
        {
            var fin = actual + duracion;

            // Verificar si hay cita local que solape este horario
            var ocupado = await _citaRepo.ExisteSolapamientoAsync(barberoId, actual, fin);
            if (!ocupado)
            {
                slots.Add(new SlotDisponible
                {
                    Inicio = actual,
                    Fin = fin
                });
            }

            actual += duracion;
        }

        return slots;
    }
}
