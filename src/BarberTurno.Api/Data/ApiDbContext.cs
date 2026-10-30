using Microsoft.EntityFrameworkCore;
using BarberTurno.Api.Domain;

namespace BarberTurno.Api.Data;

/// <summary>Contexto EF Core que conecta las entidades de la API con su base de datos.</summary>
public class ApiDbContext : DbContext
{
    /// <summary>Configura el contexto con las opciones registradas en Program.</summary>
    public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
    {
    }

    public DbSet<UsuarioEntity> Usuarios => Set<UsuarioEntity>();
    public DbSet<BarberoEntity> Barberos => Set<BarberoEntity>();
    public DbSet<ServicioEntity> Servicios => Set<ServicioEntity>();
    public DbSet<BarberoServicioEntity> BarberoServicios => Set<BarberoServicioEntity>();
    public DbSet<CitaEntity> Citas => Set<CitaEntity>();
    public DbSet<CitaBloqueEntity> CitaBloques => Set<CitaBloqueEntity>();

    /// <summary>Define relaciones, índices de concurrencia y datos iniciales del dominio.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Clave compuesta BarberoServicio
        modelBuilder.Entity<BarberoServicioEntity>()
            .HasKey(bs => new { bs.BarberoId, bs.ServicioId });

        modelBuilder.Entity<BarberoServicioEntity>()
            .HasOne(bs => bs.Barbero)
            .WithMany(b => b.BarberoServicios)
            .HasForeignKey(bs => bs.BarberoId);

        modelBuilder.Entity<BarberoServicioEntity>()
            .HasOne(bs => bs.Servicio)
            .WithMany(s => s.BarberoServicios)
            .HasForeignKey(bs => bs.ServicioId);

        // Índice único en Usuario
        modelBuilder.Entity<UsuarioEntity>()
            .HasIndex(u => u.Usuario)
            .IsUnique();

        // Índice único en Cita para evitar solapes exactos: (BarberoId, InicioUtc)
        modelBuilder.Entity<CitaEntity>()
            .HasIndex(c => new { c.BarberoId, c.InicioUtc });

        // Índice único en CitaBloque para control de concurrencia a nivel de bloque
        modelBuilder.Entity<CitaBloqueEntity>()
            .HasIndex(cb => new { cb.BarberoId, cb.BloqueInicioUtc })
            .IsUnique();

        // Semilla de datos (Seed Data)
        // Hash de BCrypt para "123456"
        // Hash fijo de BCrypt para "123456" que coincide exactamente con el sembrado en Supabase
        const string passwordHash = "$2a$11$z9N8IviCtl4FShxUiMhUyedH2UXHd30R66OlqflT5lO1748MXpEWi";

        modelBuilder.Entity<UsuarioEntity>().HasData(
            new UsuarioEntity
            {
                Id = 1,
                Usuario = "cliente1",
                PasswordHash = passwordHash,
                Nombre = "Carlos Pérez",
                Rol = "Cliente",
                Telefono = "8095550101"
            },
            new UsuarioEntity
            {
                Id = 2,
                Usuario = "barbero1",
                PasswordHash = passwordHash,
                Nombre = "Juan Barber",
                Rol = "Barbero",
                Telefono = "8095550202"
            },
            new UsuarioEntity
            {
                Id = 3,
                Usuario = "barbero2",
                PasswordHash = passwordHash,
                Nombre = "Pedro Estilos",
                Rol = "Barbero",
                Telefono = "8095550303"
            }
        );

        modelBuilder.Entity<BarberoEntity>().HasData(
            new BarberoEntity
            {
                Id = 1,
                UsuarioId = 2,
                Nombre = "Juan Barber",
                FotoUrl = "https://images.unsplash.com/photo-1503454537195-1dcabb73ffb9"
            },
            new BarberoEntity
            {
                Id = 2,
                UsuarioId = 3,
                Nombre = "Pedro Estilos",
                FotoUrl = "https://images.unsplash.com/photo-1583863788434-e58a36330cf0"
            }
        );

        modelBuilder.Entity<ServicioEntity>().HasData(
            new ServicioEntity
            {
                Id = 1,
                Nombre = "Corte Clásico",
                DuracionMin = 30,
                Precio = 350m
            },
            new ServicioEntity
            {
                Id = 2,
                Nombre = "Corte y Barba",
                DuracionMin = 45,
                Precio = 500m
            },
            new ServicioEntity
            {
                Id = 3,
                Nombre = "Perfilado de Barba",
                DuracionMin = 20,
                Precio = 200m
            },
            new ServicioEntity
            {
                Id = 4,
                Nombre = "Tratamiento Facial y Capilar",
                DuracionMin = 60,
                Precio = 750m
            }
        );

        modelBuilder.Entity<BarberoServicioEntity>().HasData(
            new BarberoServicioEntity { BarberoId = 1, ServicioId = 1 },
            new BarberoServicioEntity { BarberoId = 1, ServicioId = 2 },
            new BarberoServicioEntity { BarberoId = 1, ServicioId = 3 },
            new BarberoServicioEntity { BarberoId = 2, ServicioId = 1 },
            new BarberoServicioEntity { BarberoId = 2, ServicioId = 2 },
            new BarberoServicioEntity { BarberoId = 2, ServicioId = 4 }
        );
    }
}
