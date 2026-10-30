using System.Text;
using BarberTurno.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de datos (Soporta PostgreSQL para Supabase o SQLite local de respaldo)
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
        connectionString = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
}
else
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException("CRÍTICO: No se ha configurado la cadena de conexión de PostgreSQL/Supabase en Production.");
    }
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("[ADVERTENCIA API] No se configuró PostgreSQL/Supabase en user-secrets ni en variables de entorno. Usando SQLite local ('Data Source=barberturno.db').");
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

// 4. Autenticación con JWT Bearer
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
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Swagger con soporte para JWT
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
        Description = "Encabezado de autorización JWT. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Endpoint de diagnóstico para verificar conexión sin exponer contraseñas
app.MapGet("/api/diagnostico", async (ApiDbContext db) =>
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
        return Results.Ok(new
        {
            proveedor,
            host,
            database,
            tablasCreadas = false,
            mensaje = "Conexión a PostgreSQL/Supabase exitosa, pero las tablas aún no han sido creadas en el servidor.",
            detalle = ex.Message
        });
    }
});

app.MapControllers();

app.Run();
