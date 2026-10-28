// Responsable: M02
// Entidad central Cita. Se persiste en SQLite; sincronización bidireccional con la API vía DtoMapper.
using SQLite;

namespace BarberTurno.Models;

/// <summary>
/// Reserva de un cliente con un barbero para un servicio en un horario concreto.
/// <para>
/// Fechas: <see cref="Inicio"/> y <see cref="Fin"/> se almacenan en UTC.
/// DtoMapper las convierte a hora local al leerlas y a UTC al enviarlas.
/// </para>
/// <para>
/// Índice único: M03 crea un índice UNIQUE(BarberoId, Inicio) en la base de datos
/// para evitar solapamientos a nivel local; el servidor aplica la misma restricción.
/// </para>
/// </summary>
[Table("citas")]
public class Cita
{
    /// <summary>
    /// Identificador único generado en el dispositivo (GUID).
    /// Sirve además como clave de idempotencia: reenviar la misma cita
    /// al servidor nunca crea un duplicado.
    /// </summary>
    [PrimaryKey]
    public Guid Id { get; set; }

    /// <summary>FK hacia la tabla de clientes. Indexado para consultas por cliente.</summary>
    [Indexed]
    public int ClienteId { get; set; }

    /// <summary>FK hacia la tabla de barberos. Indexado (junto con Inicio) para detección de solapamientos.</summary>
    [Indexed]
    public int BarberoId { get; set; }

    /// <summary>FK hacia la tabla de servicios.</summary>
    public int ServicioId { get; set; }

    /// <summary>Fecha y hora de inicio en UTC. Convertir a hora local solo al mostrar.</summary>
    [Indexed]
    public DateTime Inicio { get; set; }

    /// <summary>Fecha y hora de fin en UTC (= Inicio + DuracionMin del servicio).</summary>
    public DateTime Fin { get; set; }

    /// <summary>Estado actual de la cita dentro de su ciclo de vida.</summary>
    public EstadoCita Estado { get; set; }

    /// <summary>Motivo de rechazo proporcionado por el servidor (solo cuando Estado = Rechazada).</summary>
    public string? MotivoRechazo { get; set; }

    /// <summary>
    /// Indica que la cita fue creada sin conexión y aún no se ha enviado al servidor.
    /// M08 (SyncService) la lee para enviar las citas pendientes al reconectar.
    /// </summary>
    public bool PendienteSincronizar { get; set; }
}
