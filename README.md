# LabTrack

Aplicación académica de préstamo de equipos de laboratorio, basada en las cinco historias de usuario del documento suministrado. Implementada con **ASP.NET Core MVC, C#, Razor, Entity Framework Core y PostgreSQL**. Interfaz minimalista y adaptable a celular, tableta y escritorio.

## Requisitos

- SDK **.NET 10**.
- **PostgreSQL 17**; puedes usar Docker para levantar la base local.
- Visual Studio 2026, VS Code o cualquier editor de C#.

## Ejecución rápida

Desde la carpeta `LabTrack`:

1. Inicia PostgreSQL:

   ```bash
   docker compose up -d
   ```

2. Restaura y ejecuta:

   ```bash
   dotnet restore
   dotnet run --launch-profile https
   ```

3. Abre **https://localhost:7048**. Si tu equipo no confía en el certificado local, ejecuta `dotnet dev-certs https --trust`.

Al iniciar, la aplicación crea automáticamente las tablas si todavía no existen. PostgreSQL debe tener creada la base `labtrack` (Docker Compose lo hace automáticamente).

### Usuarios de demostración

Solo se generan en entorno `Development` y en una base vacía:

| Rol | Código | Contraseña |
| --- | --- | --- |
| Estudiante | `20260001` | `Demo123*` |
| Estudiante | `20260002` | `Demo123*` |
| Encargado | `LAB001` | `Demo123*` |

Se agregan seis equipos de ejemplo repartidos entre Cómputo, Electrónica y Redes. Las contraseñas se almacenan con hash de ASP.NET Core Identity, nunca en texto plano.

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

## Usar otra base PostgreSQL

En desarrollo, modifica `ConnectionStrings:PostgreSQL` de `appsettings.Development.json` o usa una variable de entorno:

```bash
ConnectionStrings__PostgreSQL="Host=SERVIDOR;Port=5432;Database=labtrack;Username=USUARIO;Password=CLAVE" dotnet run
```

No subas contraseñas reales a un repositorio. En producción configura la cadena por variable de entorno o gestor de secretos.

## Consideraciones antes de desplegar en producción

Esta es una **versión académica funcional / MVP**, no un sistema universitario listo para exposición pública. Para producción faltan la integración con el padrón real de alumnos, gestión de usuarios/contraseñas, auditoría, recuperación de acceso, políticas de penalización configurables, copias de seguridad, protección contra ataques distribuidos y migraciones de EF Core. El proyecto usa `EnsureCreated` para su primera base; si el esquema evoluciona, conviene migrarlo a un flujo de `dotnet ef migrations`.

## Pruebas

Consulta [docs/Casos-de-prueba.md](docs/Casos-de-prueba.md) para casos manuales HU-01 a HU-05 y una guía de carga del catálogo con JMeter.
