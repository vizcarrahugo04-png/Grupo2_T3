# Plan de pruebas de LabTrack

## Pruebas funcionales

| ID | HU | Escenario | Resultado esperado |
| --- | --- | --- | --- |
| T01 | 01 | Código y contraseña correctos | Abre el panel del rol correspondiente. |
| T02 | 01 | Contraseña incorrecta | Muestra literalmente `Credenciales inválidas`. |
| T03 | 01 | Tres contraseñas incorrectas seguidas | Bloquea por 15 minutos e informa una alerta de seguridad. |
| T04 | 01 | Clave correcta mientras está bloqueado | Continúa bloqueado hasta que expire el plazo. |
| T05 | 02 | Catálogo sin filtro | Muestra nombre, código, categoría y disponibilidad. |
| T06 | 02 | Filtrar por Cómputo, Electrónica y Redes | Solo devuelve la categoría exacta solicitada. |
| T07 | 02 | Categoría sin equipos | Muestra `No hay equipos disponibles en esta categoría`. |
| T08 | 03 | Solicitar equipo libre en horario futuro | Crea solicitud Pendiente. |
| T09 | 03 | Solicitar un horario que cruza una reserva Aprobada | Rechaza la transacción. |
| T10 | 03 | Estudiante solicita dos equipos en la misma franja | Rechaza la segunda solicitud. |
| T11 | 03 | Hora fin anterior o igual al inicio | Informa el error de validación. |
| T12 | 04 | Encargado aprueba una solicitud Pendiente | Pasa a Aprobada. |
| T13 | 04 | Encargado aprueba dos solicitudes de un equipo en el mismo horario | Solo una se aprueba; la otra muestra conflicto. |
| T14 | 04 | Encargado rechaza sin motivo | Impide el rechazo. |
| T15 | 04 | Encargado rechaza con motivo | Estado Rechazada, motivo visible al estudiante. |
| T16 | 05 | Marcar devolución antes del fin | Equipo disponible, estudiante no penalizado. |
| T17 | 05 | Marcar devolución después del fin | Equipo disponible, estudiante Penalizado. |
| T18 | Roles | Estudiante navega a `/Admin` | Acceso denegado. |
| T19 | Seguridad | POST sin token antifalsificación | HTTP 400. |

## Prueba de concurrencia crítica

1. Prepara dos estudiantes distintos y un mismo equipo sin préstamos activos.
2. Los dos estudiantes solicitan el mismo rango horario.
3. Con sesiones concurrentes, el encargado intenta aprobar ambas solicitudes casi al mismo tiempo.
4. **Esperado:** una reserva aprobada y la otra pendiente con error de conflicto, o una transacción con aviso para reintentar. Nunca dos aprobaciones superpuestas.
5. Repite con dos solicitudes simultáneas del mismo estudiante para equipos distintos: no pueden quedar dos solicitudes solapadas.

## Carga del catálogo con Apache JMeter

1. Configura **HTTP Request Defaults** con host y puerto de la aplicación.
2. Registra un inicio de sesión real de prueba (usa un *HTTP Cookie Manager* y captura el token CSRF generado por el formulario de acceso).
3. Crea un **Thread Group** con 50 usuarios concurrentes y una subida gradual (por ejemplo, 10 segundos). Cada usuario requiere sesión autenticada.
4. Añade solicitudes HTTP GET a `/Catalog`, `/Catalog?categoria=C%C3%B3mputo`, `/Catalog?categoria=Electr%C3%B3nica` y `/Catalog?categoria=Redes`.
5. Añade un **Summary Report** y registra: solicitudes por segundo, tiempos medios, percentil 95, errores HTTP y uso de recursos.
6. Repite con 100 y 200 usuarios, ajustando el *ramp-up*. Guarda los reportes JTL y las condiciones del equipo de pruebas.

**Nota:** las cifras de rendimiento dependen del hardware, conexiones PostgreSQL y recursos del servidor; no se fijó un umbral de aceptación porque la historia original no define uno. Para evaluar de manera reproducible, especifica un SLO antes de ejecutar la carga.
