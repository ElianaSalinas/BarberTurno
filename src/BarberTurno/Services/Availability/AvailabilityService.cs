// Responsable: M07
// Implementa IAvailabilityService.
using BarberTurno.Data.Repositories;
using BarberTurno.Models;
using BarberTurno.Services.Api;

namespace BarberTurno.Services.Availability;

public class AvailabilityService : IAvailabilityService
{

    private readonly IBarberoRepository _barberoRepository;
    private readonly IServicioRepository _servicioRepository;
    private readonly ICitaRepository _citaRepository;
    private readonly IBarberTurnoApi _api;

    public AvailabilityService(IBarberoRepository barberoRepository,IServicioRepository servicioRepository,ICitaRepository citaRepository,IBarberTurnoApi api)
    {
        _barberoRepository = barberoRepository;
        _servicioRepository = servicioRepository;
        _citaRepository = citaRepository;
        _api = api;
    }

    //Calculo simpole para saber si una cita nueva se solapa con una ya puesta.
    //
     
    private static bool TieneSolapamiento(DateTime inicioExistente, DateTime finExistente, DateTime inicioNuevo, DateTime finNuevo) 
    {
        return inicioNuevo < finExistente &&
               finNuevo > inicioExistente;

    }

    private  List<SlotDisponible> GenerarFranjas(DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, int duracionMins, int intervaloMins) 
    { 
        if(horaInicio >= horaFin)
        {
            throw new ArgumentException("La hora de inicio no puede ser mayor que la hora de fin.");
        }
        if (duracionMins <= 0 || intervaloMins <= 0)
        {
            throw new ArgumentException("La duración y el intervalo deben ser valores positivos.");
        }

        List<SlotDisponible> franjasDisponibles = new List<SlotDisponible>();

        DateTime inicio = fecha.ToDateTime(horaInicio);
        DateTime cierre = fecha.ToDateTime(horaFin);

        var finalFranja = inicio.AddMinutes(duracionMins);

        while (finalFranja <= cierre)
        {
            franjasDisponibles.Add(new SlotDisponible
            {
                Inicio = inicio,
                Fin = finalFranja
            });
            inicio = inicio.AddMinutes(intervaloMins);
            finalFranja = inicio.AddMinutes(duracionMins);
        }
        return franjasDisponibles;
    }

    private static List<SlotDisponible> FiltrarFranjasOcupadas(List<SlotDisponible> franjas, List<Cita> citasExistentes, int barberoID) 
    {
        List<SlotDisponible> franjasD = new List<SlotDisponible>();

        foreach (var franja in franjas)
        { 
            bool ocupada = false;

            foreach (var cita in citasExistentes)
            { if ((cita.Estado == EstadoCita.Confirmada || cita.Estado == EstadoCita.Pendiente || cita.Estado == EstadoCita.Atendida || cita.Estado == EstadoCita.Ausente) && cita.BarberoId == barberoID && TieneSolapamiento(cita.Inicio, cita.Fin, franja.Inicio, franja.Fin))
                {
                    ocupada = true;
                    break;
                }
            }
            if (!ocupada)
            {
                franjasD.Add(franja);
            }
        }
        return franjasD;
    }


    private async Task<IReadOnlyList<SlotDisponible>> ObtenerHorasLibresLocalesAsync(
    int barberoId,
    int servicioId,
    DateOnly fecha)
    {
        // Obtener barbero
        var barbero = await _barberoRepository.GetByIdAsync(barberoId);

        if (barbero is null)
            throw new InvalidOperationException("El barbero no existe.");

        // Obtener servicio
        var servicio = await _servicioRepository.GetByIdAsync(servicioId);

        if (servicio is null)
            throw new InvalidOperationException("El servicio no existe.");

        // Comprobar que el barbero ofrece el servicio
        var serviciosBarbero = await _servicioRepository.GetByBarberoAsync(barberoId);

        if (!serviciosBarbero.Any(s => s.Id == servicioId))
            throw new InvalidOperationException(
                "El barbero no ofrece el servicio seleccionado.");

        // Obtener citas locales
        var citasExistentes = await _citaRepository.GetAgendaBarberoAsync(
            barberoId,
            fecha);

        // TODO: Sustituir por el horario laboral real del barbero.
        TimeOnly horaApertura = new(9, 0);
        TimeOnly horaCierre = new(18, 0);
        int intervaloMinutos = 30;

        // Generar horarios candidatos
        var franjas = GenerarFranjas(
            fecha,
            horaApertura,
            horaCierre,
            servicio.DuracionMin,
            intervaloMinutos);

        // Eliminar horarios ocupados
        var franjasLibres = FiltrarFranjasOcupadas(
            franjas,
            citasExistentes,
            barberoId);

        return franjasLibres;
    }


    // 2. OBTENER HORARIOS DISPONIBLES DESDE LA API

    private async Task<IReadOnlyList<SlotDisponible>?> ObtenerHorasLibresRemotasAsync(
        int barberoId,
        int servicioId,
        DateOnly fecha)
    {
        // Consultar disponibilidad oficial
        var resultado = await _api.GetDisponibilidadAsync(
            barberoId,
            servicioId,
            fecha);

        // Sin conexión: permitir utilizar SQLite
        if (resultado.SinConexion)
            return null;

        // Error del servidor
        if (!resultado.Exito)
        {
            throw new InvalidOperationException(
                $"Error al consultar disponibilidad. HTTP: {resultado.CodigoHttp}. {resultado.Error}");
        }

        if (resultado.Datos is null)
        {
            throw new InvalidOperationException(
                "La API respondió correctamente, pero no devolvió datos.");
        }

        // TODO(M05): Sustituir esta conversión por DtoMapper.
        // Zona horaria provisional de la barbería.
        var zonaBarberia = TimeZoneInfo.FindSystemTimeZoneById(
            "America/Santo_Domingo");

        var franjasRemotas = resultado.Datos
            .Select(slot => new SlotDisponible
            {
                Inicio = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(slot.InicioUtc, DateTimeKind.Utc),
                    zonaBarberia),

                Fin = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(slot.FinUtc, DateTimeKind.Utc),
                    zonaBarberia)
            })
            .ToList();

        // Consultar solicitudes locales aún no confirmadas
        var citasLocales = await _citaRepository.GetAgendaBarberoAsync(
            barberoId,
            fecha);

        var citasPendientes = citasLocales
            .Where(c => c.Estado == EstadoCita.Pendiente)
            .ToList();

        // No ofrecer horarios solicitados localmente
        var franjasLibres = FiltrarFranjasOcupadas(
            franjasRemotas,
            citasPendientes,
            barberoId);

        return franjasLibres;
    }


    // 3. MÉTODO PRINCIPAL DE IAvailabilityService

    public async Task<IReadOnlyList<SlotDisponible>> GetHorasLibresAsync(
        int barberoId,
        int servicioId,
        DateOnly fecha)
    {
        // Validar identificadores
        if (barberoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(barberoId));

        if (servicioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(servicioId));

        // Primero intentar consultar la disponibilidad oficial
        var franjasRemotas = await ObtenerHorasLibresRemotasAsync(
            barberoId,
            servicioId,
            fecha);

        // Si la API respondió correctamente, utilizar sus resultados
        // Una lista vacía también es un resultado válido.
        if (franjasRemotas is not null)
            return franjasRemotas;

        // Si no hubo conexión, calcular disponibilidad local
        var franjasLocales = await ObtenerHorasLibresLocalesAsync(
            barberoId,
            servicioId,
            fecha);

        return franjasLocales;
    }


}
