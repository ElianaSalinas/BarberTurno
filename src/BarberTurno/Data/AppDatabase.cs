using BarberTurno.Models;
using Microsoft.Extensions.Logging;
using SQLite;

// Responsable: Módulo 3 · Base de datos SQLite
// Dueño de la única conexión SQLite de la app. La abre e inicializa una sola vez, en este orden:
// abrir → configurar → crear tablas → migrar → sembrar.
namespace BarberTurno.Data;

/// <summary>
/// Configuración de la base local. La ruta llega desde DataServiceRegistration
/// (FileSystem.AppDataDirectory); así AppDatabase no depende de MAUI y se puede probar.
/// </summary>
public sealed record DatabaseOptions(string RutaArchivo, bool SembrarDatosDePrueba)
{
    public const string NombreArchivoPorDefecto = "barberturno.db3";

    /// <summary>Lectura/escritura, crear si no existe y caché compartida (recomendado para sqlite-net en MAUI).</summary>
    public SQLiteOpenFlags Flags { get; init; } =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;
}

/// <summary>Administra la única conexión SQLite local, su esquema, migraciones y datos de desarrollo.</summary>
public sealed class AppDatabase : IAppDatabase, IAsyncDisposable
{
    // ---------- Esquema ----------

    /// <summary>Nombre del índice único parcial que impide dos citas activas a la misma hora.</summary>
    public const string IndiceHorarioUnico = "UX_Cita_BarberoId_Inicio_Activa";
    public const string ColumnaBarbero = nameof(Cita.BarberoId);
    public const string ColumnaInicio = nameof(Cita.Inicio);

    /// <summary>
    /// Estados que liberan la hora: una cita rechazada o cancelada no impide que otra persona
    /// reserve ese horario. Pendiente, Confirmada, Atendida y Ausente sí la ocupan.
    /// </summary>
    public static readonly IReadOnlyList<EstadoCita> EstadosQueLiberanHorario =
        new[] { EstadoCita.Rechazada, EstadoCita.Cancelada };

    /// <summary>Entidades del módulo 2 que se guardan como tablas.</summary>
    private static readonly Type[] Entidades =
        { typeof(Cliente), typeof(Barbero), typeof(Servicio), typeof(BarberoServicio), typeof(Cita) };

    /// <summary>
    /// ImplicitPK: la propiedad "Id" es la clave primaria aunque la entidad no tenga [PrimaryKey].
    /// ImplicitIndex: las propiedades que terminan en "Id" (BarberoId, ClienteId...) reciben índice.
    /// Si el módulo 2 añade atributos de sqlite-net, los atributos tienen prioridad.
    /// </summary>
    private const CreateFlags FlagsCreacion = CreateFlags.ImplicitPK | CreateFlags.ImplicitIndex;

    /// <summary>
    /// Migraciones en orden. La versión aplicada se guarda en PRAGMA user_version, dentro del propio
    /// archivo. Para cambiar el esquema en el futuro se agrega una entrada (2, ...) al final.
    /// </summary>
    private static readonly (int Version, Action<SQLiteConnection> Aplicar)[] Migraciones =
    {
        (1, CrearIndicesIniciales),
    };

    // ---------- Estado ----------

    private readonly DatabaseOptions _opciones;
    private readonly ILogger<AppDatabase> _logger;

    // Garantiza que, si dos repositorios piden la conexión a la vez, se inicialice una sola vez.
    private readonly SemaphoreSlim _candado = new(1, 1);
    private volatile SQLiteAsyncConnection? _conexion;

    /// <summary>Recibe la configuración de archivo y el registrador usados durante la inicialización.</summary>
    public AppDatabase(DatabaseOptions opciones, ILogger<AppDatabase> logger)
    {
        _opciones = opciones;
        _logger = logger;
    }

    // ---------- IAppDatabase ----------

    /// <summary>Fuerza la apertura e inicialización de la base local.</summary>
    public Task InitAsync() => GetConnectionAsync();

    /// <summary>Devuelve la conexión compartida, inicializándola una sola vez de forma segura.</summary>
    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        // Camino rápido: ya inicializada, sin esperar el candado.
        if (_conexion is { } lista)
            return lista;

        await _candado.WaitAsync().ConfigureAwait(false);
        try
        {
            // Otro hilo pudo inicializarla mientras esperábamos.
            if (_conexion is { } yaLista)
                return yaLista;

            var conexion = new SQLiteAsyncConnection(_opciones.RutaArchivo, _opciones.Flags, storeDateTimeAsTicks: true);
            try
            {
                await InicializarAsync(conexion).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Si falla, no se guarda la conexión: el próximo intento vuelve a probar.
                _logger.LogError(ex, "No se pudo inicializar la base local en {Ruta}", _opciones.RutaArchivo);
                await conexion.CloseAsync().ConfigureAwait(false);
                throw;
            }

            _conexion = conexion;
            return conexion;
        }
        finally
        {
            _candado.Release();
        }
    }

    /// <summary>Cierra la conexión local y libera el candado de inicialización.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_conexion is { } conexion)
        {
            _conexion = null;
            await conexion.CloseAsync().ConfigureAwait(false);
        }
        _candado.Dispose();
    }

    // ---------- Inicialización ----------

    private async Task InicializarAsync(SQLiteAsyncConnection conexion)
    {
        // WAL permite leer (pantallas) mientras se escribe (sincronización) sin bloquearse.
        // Este PRAGMA devuelve una fila, por eso se usa ExecuteScalarAsync.
        await conexion.ExecuteScalarAsync<string>("PRAGMA journal_mode = WAL;").ConfigureAwait(false);

        // Crea las tablas que falten y agrega columnas nuevas a las existentes. Es idempotente.
        await conexion.CreateTablesAsync(FlagsCreacion, Entidades).ConfigureAwait(false);

        var version = await AplicarMigracionesAsync(conexion).ConfigureAwait(false);

        if (_opciones.SembrarDatosDePrueba)
            await SembrarDatosDePruebaAsync(conexion).ConfigureAwait(false);

        _logger.LogInformation("Base local lista en {Ruta} (esquema v{Version}).", _opciones.RutaArchivo, version);
    }

    /// <summary>Aplica las migraciones pendientes, cada una en su propia transacción.</summary>
    private static async Task<int> AplicarMigracionesAsync(SQLiteAsyncConnection conexion)
    {
        var versionActual = await conexion.ExecuteScalarAsync<int>("PRAGMA user_version;").ConfigureAwait(false);

        foreach (var (version, aplicar) in Migraciones.Where(m => m.Version > versionActual).OrderBy(m => m.Version))
        {
            // Si la migración falla, la transacción se deshace y la versión no avanza.
            await conexion.RunInTransactionAsync(db =>
            {
                aplicar(db);
                db.Execute($"PRAGMA user_version = {version};"); // version es int: sin riesgo de inyección
            }).ConfigureAwait(false);
            versionActual = version;
        }

        return versionActual;
    }

    /// <summary>Migración 1: índices que sqlite-net no sabe crear con atributos.</summary>
    private static void CrearIndicesIniciales(SQLiteConnection db)
    {
        // Los nombres de tabla se leen del mapeo, por si el módulo 2 usa [Table("...")].
        var tablaCita = db.GetMapping<Cita>().TableName;
        var tablaBarberoServicio = db.GetMapping<BarberoServicio>().TableName;

        // Regla central: un barbero no puede tener dos citas ACTIVAS con la misma hora de inicio.
        // Es un índice parcial (WHERE): las citas rechazadas o canceladas no bloquean la hora.
        // sqlite-net guarda los enum como enteros, por eso se usan sus valores numéricos.
        var estadosLibres = string.Join(", ", EstadosQueLiberanHorario.Select(e => (int)e));
        db.Execute(
            $"CREATE UNIQUE INDEX IF NOT EXISTS \"{IndiceHorarioUnico}\" " +
            $"ON \"{tablaCita}\" (\"{ColumnaBarbero}\", \"{ColumnaInicio}\") " +
            $"WHERE \"{nameof(Cita.Estado)}\" NOT IN ({estadosLibres});");

        // Evita relaciones barbero-servicio duplicadas al sincronizar el catálogo.
        db.Execute(
            $"CREATE UNIQUE INDEX IF NOT EXISTS \"UX_BarberoServicio\" " +
            $"ON \"{tablaBarberoServicio}\" (\"{nameof(BarberoServicio.BarberoId)}\", \"{nameof(BarberoServicio.ServicioId)}\");");
    }

    /// <summary>
    /// Barberos y servicios de prueba para desarrollar sin la API. Solo se insertan si la base
    /// está vacía; la sincronización reemplaza este catálogo por el real.
    /// TODO(coordinar con el módulo 6): usar los mismos Id que la semilla de la API.
    /// </summary>
    private static async Task SembrarDatosDePruebaAsync(SQLiteAsyncConnection conexion)
    {
        if (await conexion.Table<Barbero>().CountAsync().ConfigureAwait(false) > 0)
            return;

        await conexion.RunInTransactionAsync(db =>
        {
            db.InsertAll(new[]
            {
                new Barbero { Id = 1, Nombre = "Carlos Martínez" },
                new Barbero { Id = 2, Nombre = "Luis Peña" },
                new Barbero { Id = 3, Nombre = "Miguel Rosario" },
            });
            db.InsertAll(new[]
            {
                new Servicio { Id = 1, Nombre = "Corte clásico", DuracionMin = 30, Precio = 400m },
                new Servicio { Id = 2, Nombre = "Corte y barba", DuracionMin = 45, Precio = 600m },
                new Servicio { Id = 3, Nombre = "Perfilado de barba", DuracionMin = 20, Precio = 250m },
                new Servicio { Id = 4, Nombre = "Corte infantil", DuracionMin = 25, Precio = 300m },
            });
            db.InsertAll(new[]
            {
                new BarberoServicio { BarberoId = 1, ServicioId = 1 },
                new BarberoServicio { BarberoId = 1, ServicioId = 2 },
                new BarberoServicio { BarberoId = 1, ServicioId = 3 },
                new BarberoServicio { BarberoId = 2, ServicioId = 1 },
                new BarberoServicio { BarberoId = 2, ServicioId = 4 },
                new BarberoServicio { BarberoId = 3, ServicioId = 2 },
                new BarberoServicio { BarberoId = 3, ServicioId = 3 },
                new BarberoServicio { BarberoId = 3, ServicioId = 4 },
            });
        }).ConfigureAwait(false);
    }
}
