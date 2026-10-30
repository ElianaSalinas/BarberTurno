# Material de diseño

Flujos de usuario y bocetos de las pantallas principales de BarberTurno (cliente y barbero).
La paleta es carbón `#1B1B17`, crema `#F8F7F3` y dorado `#D6B362`, con tema claro y oscuro (ver ADR-0011).

## Bocetos de pantallas

| Perfil | Archivo | Pantallas |
|---|---|---|
| Cliente | [`bocetos-cliente.svg`](bocetos-cliente.svg) · [`.png`](bocetos-cliente.png) | Inicio de sesión, Inicio, Elegir barbero, Elegir servicio (con foto del resultado), Elegir hora, Confirmar reserva, Mis turnos |
| Barbero | [`bocetos-barbero.svg`](bocetos-barbero.svg) · [`.png`](bocetos-barbero.png) | Mi agenda, Calendario, Detalle de cita con historial del cliente, Mis servicios, Perfil |

![Bocetos del cliente](bocetos-cliente.png)

![Bocetos del barbero](bocetos-barbero.png)

## Flujo del cliente: reservar un turno

```mermaid
flowchart TD
    A([Abrir la app]) --> B{¿Sesión guardada y vigente?}
    B -- No --> C[Inicio de sesión / Registro]
    B -- Sí --> D
    C --> D[Inicio del cliente]
    D --> E[Elegir barbero]
    E --> F[Elegir servicio]
    F --> G[Elegir día y hora libre]
    G --> H[Confirmar reserva]
    H --> V{¿Datos válidos y hora futura?}
    V -- No --> H
    V -- Sí --> R{¿Hay conexión?}
    R -- Sí --> S[El servidor valida la hora]
    S -- Libre --> OK[Cita Confirmada + recordatorio 60 min antes]
    S -- Ocupada 409 --> OC[Aviso: hora ocupada, elegir otra]
    OC --> G
    R -- No --> P[Cita Pendiente guardada en el teléfono + recordatorio]
    P --> N[Vuelve la red: se envía al servidor]
    N -- Aceptada --> OK
    N -- Hora ya ocupada --> RJ[Cita Rechazada + aviso al cliente + se cancela el recordatorio]
    OK --> M[Mis turnos]
    RJ --> M
```

## Flujo del barbero: agenda e historial del cliente

```mermaid
flowchart TD
    A([Iniciar sesión como barbero]) --> B[Mi agenda de hoy, ordenada por hora]
    B --> C{Acción}
    C -- Cambiar de día --> B
    C -- Ver calendario --> D[Calendario: elegir fecha y ver sus turnos]
    C -- Tocar un turno --> E[Detalle de la cita]
    D -- Tocar un turno --> E
    E --> F[Historial del cliente: visitas anteriores con este barbero]
    C -- Servicios --> G[Mis servicios con foto]
    G --> H[Perfil: activar servicios o crear uno nuevo con foto]
    C -- Campana --> I[Avisos: turnos de hoy]
```

## Secuencia: dos clientes reservan la misma hora casi a la vez

```mermaid
sequenceDiagram
    participant A as Cliente A
    participant B as Cliente B
    participant API as API (POST /api/citas)
    participant DB as PostgreSQL
    A->>API: Reservar Juan 15:00
    B->>API: Reservar Juan 15:00
    API->>DB: Transacción A: comprobar solape → libre
    API->>DB: Transacción B: comprobar solape → libre
    API->>DB: A inserta cita + bloques de 5 min
    API->>DB: B inserta cita + bloques de 5 min
    DB-->>API: A: confirmado
    DB-->>API: B: violación del índice único (BarberoId, BloqueInicioUtc)
    API-->>A: 201 Confirmada
    API-->>B: 409 Hora ocupada
```

Detalle de la decisión en [ADR-0008](../adr/0008-como-se-evita-que-dos-citas-queden-a-la-misma.md).

## Flujo sin conexión

```mermaid
flowchart LR
    A[Sin red] --> B[Consultar datos guardados en SQLite]
    A --> C[Reservar: queda Pendiente]
    D[Vuelve la red] --> E[SyncService: catálogos y citas]
    E --> F[SincronizarPendientes]
    F -- 201 --> G[Confirmada]
    F -- 409 --> H[Rechazada + aviso]
```

Detalle en [ADR-0004](../adr/0004-datos-y-sincronizacion-fuente-de-verdad-y-mod.md).
