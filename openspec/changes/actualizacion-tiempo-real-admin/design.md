# Design

## Context

El panel administrativo es una aplicación ASP.NET Core MVC (`net10.0`) con un solo contenedor y MongoDB Atlas. Hoy no usa Blazor ni SignalR. La única actualización automática es el polling de `Automation/Index` cada 3 segundos, que solo refresca la barra de estado y el consumo; los listados de artículos, categorías, comentarios y decisiones pendientes se renderizan una vez y quedan desactualizados.

El estado de automatización se consulta con cuatro operaciones Mongo por ciclo (`GetSettingsAsync`, `GetPendingDecisionsAsync`, `GetRunsAsync` y `GetUsageTotalsAsync`). `GetPendingDecisionsAsync` llega a cargar hasta 100 corridas completas con `DraftJson`, y la agregación de consumo recorre todo el historial. El editor de artículos (`content-blocks-editor.js`, ~80 KB) y los formularios administrativos funcionan y no forman parte de este cambio.

Véase `proposal.md` para la motivación y `specs/admin-real-time-updates/spec.md` para el contrato de comportamiento.

## Goals / Non-Goals

**Goals:**

- Actualizar automáticamente los bloques de automatización, artículos, categorías y comentarios del Admin.
- Introducir componentes reutilizables para esos bloques sin reescribir todo el panel.
- Eliminar el polling pesado y las consultas Mongo periódicas por pestaña.
- Mantener una degradación funcional cuando el canal en vivo no está disponible.
- Preservar el sitio público, el editor, los formularios y las URLs existentes.

**Non-Goals:**

- Migrar el sitio público, el editor de artículos o los formularios a Blazor.
- Cambiar el esquema de MongoDB o el modelo de dominio.
- Añadir webhooks de negocio o integraciones externas nuevas.
- Garantizar tiempo real entre múltiples réplicas en esta primera etapa.
- Reemplazar el worker de automatización o el pipeline de IA.

## Decisions

### 1. Islas Blazor Server dentro del MVC actual

**Decisión:** añadir Blazor Server de forma incremental, hospedando componentes interactivos dentro de las vistas MVC del Admin. No se crea una aplicación Blazor separada ni se migran las rutas.

**Razón:** permite tiempo real y componentización reutilizando controladores, servicios, autenticación y estilos existentes. El sitio público y el editor quedan intactos.

**Alternativas consideradas:**

- **Migración completa del Admin a Blazor:** descartada por el alto riesgo de reescribir formularios, filtros, paginación y el editor, sin beneficio directo para el problema.
- **Blazor WebAssembly:** descartada por el tamaño de descarga, la duplicación de lógica y la necesidad de exponer servicios al cliente.
- **SignalR + JavaScript sin Blazor:** válida y más barata si solo se quisiera push, pero no componentiza la UI ni unifica el modelo de suscripción.
- **Solo polling:** válida como primera fase, pero mantiene el costo y no resuelve la actualización de todos los listados sin más JS.

**Integración técnica:**

- `AddServerSideBlazor()` y `MapBlazorHub().RequireAuthorization()` en `Program.cs`, porque el MVC existente hospeda las islas y no se necesita el enrutador de una Blazor Web App.
- `_Imports.razor` para los componentes; no se requiere `App.razor` al no usar enrutamiento Blazor.
- Los componentes se renderizan desde las vistas MVC con el tag helper `component` y `render-mode="Server"`.
- Script `~/_framework/blazor.server.js` en `_AdminLayout.cshtml`.
- Para widgets vivos se usa `render-mode="Server"`; la suscripción se activa en `OnAfterRender` para no duplicarla.
- Tailwind debe incluir la carpeta de componentes en `Styles/tailwind.css` (`@source "../Components"`).

### 2. Contador de versiones por módulo

**Decisión:** un servicio singleton mantiene una versión monotónica por módulo (`automation`, `articles`, `categories`, `comments`). Cada escritura exitosa incrementa solo la versión del módulo afectado.

**Razón:** permite invalidación selectiva, sirve tanto para las islas Blazor como para el polling de respaldo y evita consultar datos completos para saber si algo cambió.

**Alternativas consideradas:**

- **Change streams de MongoDB:** útiles para múltiples réplicas, pero añaden una conexión persistente y complejidad operativa; no son necesarios con una sola instancia.
- **Comparar `UpdatedAt` en cada consulta:** obliga a consultar Mongo y no detecta borrados con facilidad.

La versión se incrementa **después** de que la escritura haya tenido éxito, nunca antes. Un fallo de persistencia no debe notificar a la UI.

### 3. Bus de eventos en proceso, acotado y con coalescencia

**Decisión:** publicar eventos de cambio en un `Channel<T>` acotado con un único consumidor. El bus agrupa eventos por módulo/entidad en una ventana corta (250–500 ms) y descarta eventos antiguos si la cola se llena, emitiendo una señal de reconciliación.

**Razón:** evita tormentas de renders durante una corrida de automatización, no bloquea las escrituras de negocio y mantiene un consumo de memoria predecible.

**Alternativas consideradas:**

- **Llamar directamente a SignalR desde cada servicio:** acopla la capa de aplicación a la UI y multiplica mensajes.
- **Un evento por artículo o por token:** genera fan-out y renders innecesarios.
- **Cola sin límite:** riesgo de crecimiento de memoria si un cliente o la UI se atrasa.

Contrato propuesto:

- `CmsChangeEvent` con `Module`, `EntityId` opcional, `Operation`, `Version` y un payload mínimo o solo la indicación de invalidación.
- `ICmsChangePublisher` para publicar; `ICmsChangeStream` para suscribirse con `CancellationToken`.
- Los eventos nunca incluyen claves de API, saldos, configuración completa ni `DraftJson`.

### 4. Publicación desde los servicios de aplicación

**Decisión:** publicar cambios en el punto donde la escritura ya fue exitosa:

- `ArticleService`: crear, actualizar, eliminar, publicar programados y cambios de destacado.
- `CategoryService`: crear, actualizar y eliminar.
- `CommentService`: envío público, aprobación, rechazo y eliminación.
- `ArticleAutomationService`: inicio de corrida, artículo generado, decisión pendiente creada/resuelta, fin, detención y cambios de configuración.
- `ArticleAutomationWorker`: publicación de artículos programados.

**Razón:** garantiza que la UI se entere de cambios hechos por cualquier vía (manual, programada, otro administrador) y no solo de los que pasan por un controlador.

**Alternativa considerada:** publicar desde los controladores. Se descarta porque el worker y los flujos internos también modifican datos.

### 5. Estado vivo y caché de automatización

**Decisión:** mantener un `AutomationLiveState` singleton con el snapshot de la corrida en curso (inicio, disparador, artículos generados, decisiones pendientes y último evento) y una caché del estado consolidado para la carga inicial. La UI se alimenta del snapshot durante la corrida; Mongo se consulta al cargar, al reconectar y al cerrar la corrida.

**Razón:** el documento `AutomationRun` se persiste al inicio y al final; no es necesario reemplazarlo por cada token ni por cada artículo. La UI necesita progreso, no persistencia por evento.

**Alternativas consideradas:**

- **Persistir la corrida en cada artículo:** aumenta escrituras y carga sin mejorar la experiencia.
- **Seguir consultando Mongo cada 3 s:** mantiene el costo actual y los documentos grandes.

Además, el estado consolidado debe usar proyecciones/conteos para pendientes y consumo, no `GetPendingDecisionsAsync()` completo en cada carga.

### 6. Islas y límites de componentes

**Decisión:** crear componentes con responsabilidades acotadas:

- `AutomationStatusPanel`: estado, última corrida, consumo y conexión.
- `PendingDecisionsList`: decisiones pendientes y acciones de continuar/omitir.
- `ArticleList`: listado con filtros, búsqueda, paginación, portada y toggle de destacado.
- `CategoryTree`: jerarquía, contadores y acciones de edición/eliminación.
- `CommentModerationList`: filtros por estado, acciones y contador global.

**Razón:** cada isla puede suscribirse solo a su módulo y recibir datos iniciales del controlador, evitando consultas duplicadas y acoplamiento entre listados.

**Alternativas consideradas:**

- **Un único componente “Admin”:** concentra estado, dificulta el aislamiento y provoca renders innecesarios.
- **Componentes por fila:** multiplican suscripciones; se prefiere suscripción por lista y actualización local de la fila afectada.

Los formularios y el editor siguen siendo MVC/JS. El listado de artículos puede reutilizar el marcado y las clases Tailwind actuales, pero la lógica de filtros/paginación pasa al componente.

### 7. Actualización de componentes y reconciliación

**Decisión:** cada componente recibe la página inicial desde el controlador, se suscribe al módulo correspondiente y, al recibir un evento:

- actualiza la fila/entidad si está presente;
- refresca la página actual si el evento afecta al conjunto visible;
- o muestra un indicador de “cambios disponibles” si el elemento no coincide con el filtro/página.

Las notificaciones se aplican con `InvokeAsync(StateHasChanged)` y coalescencia. Las suscripciones usan `IAsyncDisposable`/`CancellationToken` y se cancelan al navegar.

**Razón:** evita recargar toda la página y mantiene el contexto del usuario (filtros, búsqueda, scroll).

### 8. Degradación y polling de respaldo

**Decisión:** si el circuito no está disponible, el panel muestra el estado de desconexión y usa un polling ligero contra un endpoint de versiones en memoria. Al reconectar, reconcilia el estado y vuelve al modo en vivo.

**Razón:** cumple el requisito de no presentar datos viejos como actuales y mantiene la funcionalidad en redes o proxies que bloqueen WebSocket.

**Implementación prevista:** un script pequeño en el layout consulta versiones con baja frecuencia cuando corresponde y dispara una recarga o refresco de la isla. El polling de respaldo nunca consulta Mongo si la versión no cambió.

### 9. Seguridad

**Decisión:** el endpoint de circuitos y el canal de actualización exigen autenticación. La autorización se valida en el circuito y en los servicios que entregan datos. Los eventos son genéricos; los componentes consultan solo lo que necesitan.

**Razón:** el panel contiene claves de API, saldos y configuración de automatización. Nada de eso debe viajar por un evento ni llegar a un cliente no autorizado.

**Consideraciones:**

- Revalidar la cookie de administrador en circuitos de larga duración o forzar el cierre al expirar.
- Mantener la política `[Authorize]` del área Admin.
- No incluir secretos en logs, eventos ni parámetros de componentes.
- Desuscribir componentes al salir para no dejar circuitos consumiendo recursos.

### 10. Escalado

**Decisión:** diseñar la publicación detrás de una interfaz y asumir una sola instancia en esta etapa. Si se añaden réplicas, se incorpora un backplane de SignalR (por ejemplo Redis) o change streams de Mongo.

**Razón:** el despliegue actual usa un solo contenedor. Añadir Redis ahora sería infraestructura innecesaria.

**Alternativa considerada:** backplane desde el inicio. Se pospone hasta que exista una necesidad real de varias réplicas.

## Risks / Trade-offs

- **[Riesgo] Complejidad de integrar Blazor con MVC y autenticación por cookies.** → Mitigación: implementar una isla piloto (Automatización), validar `_blazor` con `[Authorize]`, revalidación de sesión y proxy antes de extender.
- **[Riesgo] Tormenta de eventos durante una corrida.** → Mitigación: cola acotada, coalescencia, un solo dispatcher y eventos por fase/artículo, nunca por token.
- **[Riesgo] Consultas Mongo por evento.** → Mitigación: versiones, caché, proyecciones y actualización local; el evento no debe disparar consultas completas.
- **[Riesgo] Documentos grandes con `DraftJson`.** → Mitigación: no incluirlos en el estado consolidado; cargar el detalle solo al abrir una decisión.
- **[Riesgo] Fugas de suscripción o circuitos retenidos.** → Mitigación: `IAsyncDisposable`, cancelación, límites de retención y pruebas de navegación repetida.
- **[Riesgo] Proxy sin WebSocket.** → Mitigación: permitir `Upgrade` y timeouts; el polling de respaldo mantiene la funcionalidad.
- **[Riesgo] Multi-instancia sin backplane.** → Mitigación: documentar la limitación y dejar la interfaz preparada para Redis/change streams.
- **[Riesgo] El listado de artículos tiene filtros, paginación, imágenes y toggle de Hero.** → Mitigación: migrarlo como una isla completa con datos iniciales del controlador, no como filas sueltas; mantener el marcado existente.
- **[Riesgo] Tailwind no genera clases de los `.razor`.** → Mitigación: añadir la carpeta de componentes a `@source` y verificar el CSS compilado.
- **[Riesgo] Regresiones en el sitio público o el editor.** → Mitigación: no tocar esas rutas; pruebas de humo y verificación de que no se carga Blazor fuera del Admin.

## Migration Plan

1. **Fase 0 — Base:** añadir contratos de versiones/eventos, bus acotado y endpoint de versiones; probar que no hay cambios de comportamiento visibles.
2. **Fase 1 — Piloto:** integrar Blazor Server y migrar `AutomationStatusPanel` y `PendingDecisionsList`; retirar el polling de 3 s de Automatización.
3. **Fase 2 — Automatización:** añadir estado vivo, caché, proyecciones y eventos de corrida/decisión; validar corridas manuales, programadas y detenidas.
4. **Fase 3 — Comentarios:** migrar la moderación y el contador global; validar envío público, aprobación, rechazo y eliminación.
5. **Fase 4 — Artículos:** migrar el listado completo con filtros, búsqueda, paginación, portada y destacados.
6. **Fase 5 — Categorías:** migrar el árbol/listado y sus acciones.
7. **Fase 6 — Endurecimiento:** reconexión, polling de respaldo, pruebas de carga, autorización, límites y documentación de escalado.

**Rollback:** cada isla debe poder volver a la vista MVC anterior sin cambiar datos ni rutas. La publicación de eventos y el contador de versiones pueden quedar activos sin consumidores; no alteran la persistencia.

## Open Questions

- **Frecuencia exacta del polling de respaldo:** se puede ajustar durante la implementación según la latencia aceptable; el requisito solo exige que no consulte Mongo si no cambió la versión.
- **Comportamiento visual cuando un cambio no coincide con el filtro actual:** el contrato permite un indicador de “cambios disponibles”; el texto y la posición exactos pueden definirse en la implementación sin cambiar el comportamiento del spec.
- **Momento para introducir un backplane multi-instancia:** queda fuera de esta etapa; se decide cuando el despliegue pase de una réplica.
