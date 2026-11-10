# LabTrack

Aplicación académica de préstamo de equipos de laboratorio, basada en las cinco historias de usuario del documento suministrado. Implementada con **ASP.NET Core MVC, C#, Razor, Entity Framework Core y PostgreSQL**.

## Historias de usuario implementadas

| Historia | Función |
| --- | --- |
| HU-01 | Inicio de sesión por código y contraseña; error `Credenciales inválidas`; bloqueo de 15 minutos tras 3 fallos. |
| HU-02 | Catálogo con nombre, inventario, categoría y estado. Filtro exacto por Cómputo, Electrónica y Redes; estado vacío. |
| HU-03 | Solicitud con inicio y fin en horario de Perú; impide solaparse con una reserva aprobada del equipo y con otra solicitud propia. |
| HU-04 | Bandeja administrativa para aprobar o rechazar. El rechazo requiere justificación y la aprobación verifica conflictos nuevamente. |
| HU-05 | Préstamos por devolver, registro de devolución y penalización automática cuando se devuelve después de la hora límite. |

### Reglas importantes

- Los horarios se introducen en **America/Lima (UTC−5)** y se almacenan en UTC en PostgreSQL.
- El estado **Prestado** se calcula a partir de una reserva aprobada cuyo inicio ya llegó y que no tiene devolución. Las reservas futuras aprobadas no marcan el equipo como prestado *antes* de su horario; aun así impiden reservas que se crucen con ese intervalo.
- Un estudiante no puede tener simultáneamente solicitudes pendientes o aprobadas que se solapen.
- Una reserva rechazada no bloquea nuevos horarios. Una devolución elimina la condición de préstamo activo.
- Las operaciones de reservar, aprobar, rechazar y devolver usan transacciones **SERIALIZABLE** de PostgreSQL para evitar conflictos por concurrencia. Ante un conflicto transaccional se solicita reintentar.
- Los permisos se separan en roles `Estudiante` y `Administrador`. Todos los formularios POST incluyen protección antifalsificación (CSRF).

## Estructura MVC

```text
LabTrack/
├── Controllers/       Inicio de sesión, catálogo, reservas y administración
├── Models/            Entidades y estados
├── Data/              DbContext, relaciones y datos de desarrollo
├── Services/          Reglas transaccionales y conversión horaria
├── ViewModels/        Datos destinados a cada vista
├── Views/             Páginas Razor adaptables
├── wwwroot/           CSS y JavaScript sin dependencias externas
├── docs/              Plan de pruebas funcionales y de carga
├── compose.yaml       PostgreSQL para desarrollo local
└── Program.cs         Configuración de la aplicación
```

