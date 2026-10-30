// Responsable: M04 - Seguridad, conexión a base de datos y configuración API
using System.Text;
using BarberTurno.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros hosts) indican el puerto en la variable PORT.
var puerto = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(puerto))
    builder.WebHost.UseUrls($"http://0.0.0.0:{puerto}");


// 1. Base de datos (PostgreSQL en Supabase mediante User Secrets / variables de entorno)
var rawConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(rawConn))
{
    rawConn = builder.Configuration["DATABASE_URL"];
}

string connectionString;
bool esPostgres = false;

if (!string.IsNullOrWhiteSpace(rawConn) &&
    (rawConn.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
     rawConn.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
     rawConn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
     rawConn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)))
{
    esPostgres = true;
    connectionString = rawConn;
    if (rawConn.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        rawConn.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var uri = new Uri(rawConn);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "postgres";
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require";
    }
}
else
{
    var allowLocalSqlite = builder.Configuration.GetValue<bool>("Database:UseLocalSqlite", false);
    if (!allowLocalSqlite)
    {
        // Falla rápida explicativa para evitar que un desarrollador trabaje accidentalmente contra SQLite local
        throw new InvalidOperationException(
            "CRÍTICO: No se ha configurado la cadena de conexión a PostgreSQL/Supabase.\n" +
            "Para desarrollo, configure los User Secrets ejecutando en la carpeta src/BarberTurno.Api:\n" +
            "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require\"\n" +
            "o defina la variable de entorno DATABASE_URL.\n" +
            "Si desea usar SQLite local explícitamente para pruebas aisladas, configure '\"Database\": { \"UseLocalSqlite\": true }' en appsettings.Development.json.");
    }

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[ADVERTENCIA API] Modo explícito de SQLite activado ('Data Source=barberturno.db').");
    Console.ResetColor();
    connectionString = "Data Source=barberturno.db";
}

builder.Services.AddDbContext<ApiDbContext>(options =>
{
    if (esPostgres)
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// 2. Controladores
builder.Services.AddControllers();

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 4. Autenticación y Autorización con JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"] ?? "BarberTurnoClaveSuperSecretaDeSeguridad126!JwtKey2026";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BarberTurnoApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BarberTurnoApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 5. Swagger con soporte estándar HTTP Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BarberTurno API",
        Version = "v1",
        Description = "API REST de BarberTurno con soporte para PostgreSQL (Supabase) y SQLite."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT obtenido en /api/auth/login",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Inicialización de la base de datos (con protección para no alterar Supabase sin confirmación)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    if (!esPostgres)
    {
        db.Database.EnsureCreated();
    }
    else if (builder.Configuration.GetValue<bool>("Database:AutoEnsureCreated", false))
    {
        db.Database.EnsureCreated();
    }
}

// Log seguro del proveedor de base de datos conectado
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    var conn = db.Database.GetDbConnection();
    var provNombre = esPostgres ? "PostgreSQL (Supabase)" : "SQLite Local";
    app.Logger.LogInformation("Base de datos configurada: Proveedor={Proveedor}, DataSource={DataSource}, Database={Database}", provNombre, conn.DataSource, conn.Database);
}
catch (Exception ex)
{
    app.Logger.LogWarning("No se pudo obtener la información de conexión: {Error}", ex.Message);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BarberTurno API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz
});

app.UseCors("AllowAll");

if (!app.Environment.IsDevelopment() && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT")))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoint de diagnóstico seguro solo en desarrollo (nunca en producción)
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/diagnostico", async (ApiDbContext db, ILogger<Program> logger) =>
    {
        var conn = db.Database.GetDbConnection();
        var proveedor = db.Database.ProviderName ?? "Desconocido";
        var host = conn.DataSource;
        var database = conn.Database;

        try
        {
            var totalUsuarios = await db.Usuarios.CountAsync();
            var totalBarberos = await db.Barberos.CountAsync();
            var totalServicios = await db.Servicios.CountAsync();
            var totalCitas = await db.Citas.CountAsync();

            return Results.Ok(new
            {
                proveedor,
                host,
                database,
                tablasCreadas = true,
                conteos = new
                {
                    usuarios = totalUsuarios,
                    barberos = totalBarberos,
                    servicios = totalServicios,
                    citas = totalCitas
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al consultar estado de la base de datos.");
            return Results.Problem(
                title: "Error al consultar base de datos",
                detail: "No se pudo consultar el conteo de tablas. Verifique los logs del servidor.",
                statusCode: 500);
        }
    });
}

app.MapControllers();

app.Run();
