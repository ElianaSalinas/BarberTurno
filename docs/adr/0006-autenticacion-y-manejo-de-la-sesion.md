# ADR-0006: Autenticación y manejo de la sesión

- **Estado:** Propuesto
- **Fecha:** 2026-10-28
- **Responsable(s):** M5 (Autenticación, roles y sesión)

## Contexto

La aplicación *BarberTurno* necesita identificar al usuario que la utiliza y su rol (Cliente o Barbero). La sesión debe persistir al cerrar y volver a abrir la app, inclusive sin conexión a internet. El registro permite elegir el rol y la credencial (token) debe almacenarse de forma segura en Android mediante `SecureStorage`.

## Decisión

1. **Login y registro contra la API**
   -     - El servidor devuelve LoginResponseDto con el JWT firmado (Token, que lleva el rol dentro), ExpiraEnUtc, Rol, UsuarioId y Nombre..
2. **Persistencia de la sesión**
   - `SecureStorage` guarda **únicamente**:
     - `auth_token` (string)
     - `auth_expira` (ISO‑8601, UTC) – provisto por el servidor
     - `auth_sesion` (JSON con `UsuarioId`, `Nombre`, `Rol`).
   - **Nunca** se almacena la contraseña.
3. **Restaurar sesión**
   - `LoginViewModel.InicializarAsync()` (llamado desde `OnAppearing` de `LoginPage`) invoca `AuthService.RestaurarSesionAsync()`, que lee token, expiración y datos de sesión de `ISessionStorage`.
   - Si la expiración está a menos de 1 minuto del momento actual, se considera expirada y se solicita login. No se consulta al servidor.
4. **Logout**
   - `ISessionStorage.Clear()` elimina únicamente las claves creadas (`Remove`), dejando intacto cualquier otro dato del Keystore.
   - `SesionActual` se establece a `null`.
5. **Registro**
   - El usuario elige su rol mediante dos `RadioButton` enlazados a `EsCliente`/`EsBarbero`.
   - `AuthService.RegistrarAsync` envía los datos al servidor; la respuesta incluye el JWT con el **rol asignado por el servidor**.
   - La app navega según `SesionActual.Rol` (no según la elección del usuario).
6. **Navegación basada en rol**
   - En la app, el rol solo decide qué interfaz se muestra (`//cliente` o `//barbero`). La seguridad real la aplica el servidor: el rol viaja dentro del JWT firmado y el servidor lo valida en cada petición; si alguien manipula la app para ver pantallas de otro rol, el servidor responderá 401/403 y no entregará datos.
7. **Comunicación de resultados**
   - `ResultadoLogin` y `ResultadoRegistro` son enums que permiten traducir cada caso a mensajes claros en la UI.
8. **Validaciones en ViewModel**
   - Se valida formato de campos (todos obligatorios, nombre ≥3, teléfono 10 dígitos, usuario sin espacios ≥3, contraseña ≥6, coincidencia de contraseñas). Esto mejora la experiencia de usuario; la seguridad se mantiene porque el servidor también valida.
   - La contraseña se limpia de memoria (`Password = ""; ConfirmarPassword = "";`) en el bloque `finally` de cada intento.
9. **Inyección de dependencias**
   - `ISessionStorage` y `IAuthService` se registran como **singleton** porque representan un único punto de gestión de sesión para toda la aplicación.

## Alternativas consideradas

- **Preferences (sin cifrar)** – descartada porque no protege el token.
- **SQLite (archivo sin cifrar)** – descartada por complejidad y falta de cifrado por defecto.
- **Guardar la contraseña** para auto‑login – descartada por riesgo máximo de exposición.
- **No persistir sesión** (pedir login siempre) – descartada por mala experiencia, sobre todo offline.
- **Validar expiración con petición al servidor** al abrir – descartada porque no funciona sin internet.
- **Permitir registro solo de clientes** – descartada por decisión del equipo de permitir ambos roles.

## Consecuencias

### Ventajas
- La credencial está protegida en Android Keystore y nunca se guarda la contraseña.
- La sesión funciona sin internet gracias a la restauración offline.
- La interfaz cambia según el rol y la lógica de negocio es simple y testeable.
- Rutas absolutas evitan bucles de navegación y el botón Android «atrás» no regresa al login.

### Costos y riesgos
- **Robo del token**: permite acceso hasta su expiración. Mitigado con expiración razonable definida por el servidor y siempre uso de HTTPS.
- **Fallo de SecureStorage al reinstalar**: se captura la excepción y se limpia la sesión, forzando login. Además se pidió a M1 android:allowBackup="false" en AndroidManifest para evitar restaurar datos cifrados con llaves que ya no existen.
- **Cualquier usuario puede registrarse como Barbero**: el backend valida los roles permitidos; mejora futura: código de invitación o aprobación de administrador.
- **Revocación de token no implementada**: pendiente solicitar al módulo de red la gestión de respuestas 401/403 para redirigir al login.
- **Dependencias externas**: M1 debe registrar `INavigationService`, la ruta `registro`, los `ViewModel`/`Page` en DI, y proveer `IsBusy`/`MensajeError` en `BaseViewModel`. También se necesita el módulo DTO (`RegistroRequestDto`) y el módulo de red (método `RegistrarAsync`, manejo 401/SinConexion, adjuntar token). M1 debe quitar del BarberTurno.csproj los paquetes de servidor (JwtBearer, EntityFrameworkCore), que impiden compilar para Android.

## Pendientes
- Implementar refresh‑token (mejora futura).
- Gestión del 401 con el módulo de red (redirección al login).
- Código de invitación o aprobación para registro de Barberos.
- Descomentar `RegistrarAsync` en `AuthService` cuando el módulo de red lo entregue.
