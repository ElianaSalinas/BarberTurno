// Responsable: M03 (configuración, auth, DI). M04 registra reservas/disponibilidad.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// TODO(M03): DbContext, JWT, CORS, Swagger, hash de contraseñas.
// TODO(M04): IDisponibilidadService, IReservaService.

var app = builder.Build();

app.UseHttpsRedirection();
// TODO(M03): app.UseAuthentication(); app.UseAuthorization();
app.MapControllers();

app.Run();
