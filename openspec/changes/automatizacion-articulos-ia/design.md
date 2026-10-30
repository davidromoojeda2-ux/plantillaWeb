# Design

## Context

Ver `proposal.md` (motivación) y `specs/article-automation/spec.md` (contrato de comportamiento).

El proyecto es una solución .NET (ASP.NET Core MVC) con capas `Domine`, `Aplication`, `Infrastructure` y `paginaWeb`, persistencia MongoDB y despliegue en un único contenedor Docker sobre un VPS. Puntos que condicionan el diseño:

- La publicación ya existe y es reutilizable: `IArticleService.CreateAsync` aplica `ContentBodySanitizer`, valida invariantes, genera el slug (rechazando duplicados vía `SlugExistsAsync`) y calcula campos derivados (`SearchText`, `ReadingTimeMinutes`).
- El contenido es por bloques tipados (`ContentBlock` / `ContentBlockDto`); cualquier texto generado por IA debe pasar por el sanitizador antes de persistirse.
- No hay infraestructura de tareas en segundo plano ni cola: hay que introducirla sin añadir servicios externos (MongoDB Atlas es externo; el resto corre en un solo proceso/container).
- La seguridad usa cookies y `[Authorize]` en el área Admin; el panel de control debe reutilizar ese mecanismo.

## Goals / Non-Goals

**Goals:**

- Un worker embebido que no detenga la web ante fallos de Serper/IA/MongoDB.
- Un orquestador en `Aplication` reutilizable, desacoplado del host, que pueda extraerse a un worker independiente en el futuro.
- Integraciones externas (Serper, IA) detrás de interfaces, proveedor-agnósticas.
- Estado e historial persistentes en MongoDB (fuente de verdad para idempotencia y observabilidad).
- Un panel administrativo propio que controle y observe el pipeline.

**Non-Goals:**

- Escalar el worker de forma independiente o aislarlo en otro proceso/container (posible en el futuro, no ahora).
- Un motor de colas o un dashboard genérico de terceros (Hangfire): no aporta sobre un panel propio.
- Migrar o alterar el modelo de bloques, la sanitización o el flujo de publicación existentes.
- Generación automática de imágenes con IA (se deja fuera; opcional vía URL permitida).

## Decisions

### 1. Worker embebido (`BackgroundService`) dentro de `paginaWeb`

**Decisión**: registrar el motor como un `BackgroundService` en el `Program.cs` del proyecto web. El orquestador vive en `Aplication`, de modo que el host sea una cáscara delgada y extraíble.

**Razón**: un solo VPS con `docker-compose`; el coste operativo de un segundo proyecto/container no se justifica hoy. Mantener el orquestador en `Aplication` preserva la opción de extraerlo después sin reescribir lógica.

**Alternativas descartadas**:
- Worker Service externo (segundo proyecto/container): mismo resultado funcional, pero exige otra imagen Docker, otro `Program.cs` con DI duplicada y un mecanismo de control entre procesos. Se difiere.
- Hangfire con dashboard: añade un store y una UI ajena al diseño; el panel propio cubre el control requerido.

### 2. Abstracciones en `Aplication`, implementaciones en `Infrastructure`

**Decisión**: introducir en `Aplication`:

- `ITopicDiscoveryService` → `DiscoverAsync(TopicDiscoveryRequest, CancellationToken)`.
- `IAiArticleGenerator` → `GenerateAsync(TopicGenerationRequest, CancellationToken)`.
- `IAutomationStateRepository` → consulta/escritura de corridas y de configuración.
- `IAutomationService` (orquestador) → `RunOnceAsync`, `GetStatusAsync`, `GetRunsAsync`, `GetSettingsAsync`, `SaveSettingsAsync`.

En `Infrastructure`: `SerperTopicDiscoveryService` (HttpClient a `api.serper.dev`), `OpenAiCompatibleArticleGenerator` (salida JSON), `AutomationStateRepository` (MongoDB).

**Razón**: sigue la separación de capas existente y permite falsificar las integraciones en pruebas y cambiar de proveedor sin tocar el orquestador.

**Alternativas descartadas**: codificar las llamadas HTTP dentro del orquestador (acopla y hace imposible testear sin red).

### 3. IA proveedor-agnóstica con salida JSON estructurada

**Decisión**: `OpenAiCompatibleArticleGenerator` apunta a un `baseUrl` y `model` configurables (cubre OpenAI, DeepSeek, OpenRouter y endpoints compatibles). La respuesta se restringe con un JSON Schema que describe `{ Title, Summary, Blocks[], Tags, CategorySuggestion, MetaDescription }`, mapeado a `ContentBlockDto`. Anthropic/Gemini quedan como implementaciones alternativas del mismo contrato si se eligen.

**Razón**: la salida estructurada es obligatoria para alimentar `ContentBodySanitizer`; el cliente compatible con OpenAI da cobertura amplia con una sola implementación y deja el proveedor como configuración, no como decisión de código.

**Alternativas descartadas**: pedir HTML plano a la IA (se perdería la estructura de bloques y la seguridad); parsear texto libre (frágil).

### 4. Estado en MongoDB: `automation_runs` y `automation_settings`

**Decisión**: dos colecciones nuevas en la misma base (`portal_noticias`):

- `automation_runs`: una por corrida, con `Status` (EnProgreso/Completado/Fallido), disparador (Programada/Manual), temas descubiertos, artículos creados y errores.
- `automation_settings`: documento único con la configuración (auto-publicar, frecuencia, categorías habilitadas, temas base, país/idioma, límites) y el estado de pausa.

Entidades correspondientes en `Domine` (`AutomationRun`, `AutomationSettings`).

**Razón**: MongoDB ya es la fuente de verdad del portal; reutilizarlo evita un store nuevo y hace el estado visible al panel sin infraestructura adicional.

**Alternativas descartadas**: estado solo en memoria (se pierde al reiniciar y rompe el idempotencia); una cola externa (Redis/RabbitMQ) innecesaria para una sola réplica.

### 5. Deduplicación por hash normalizado + slug existente

**Decisión**: antes de generar, normalizar el título (minúsculas, sin acentos, sin signos) y calcular un hash; descartar si el hash ya figura en `automation_runs` o si existe un artículo con un slug equivalente (`IArticleRepository.SlugExistsAsync` / título normalizado). La comprobación de slug existente ya cubre colisiones al publicar.

**Razón**: evita contenido duplicado (riesgo SEO) y reintentos redundantes entre corridas.

**Alternativas descartadas**: comparar solo el slug (no cubre temas procesados pero no publicados); comparar el título sin normalizar (falla ante diferencias de capitalización/acentos).

### 6. Resiliencia: fail-safe + `BackgroundServiceExceptionBehavior.Ignore` + Polly

**Decisión**:

- El bucle de `ExecuteAsync` envuelve cada corrida en `try/catch` para que ninguna excepción escape.
- Registrar `HostOptions.BackgroundServiceExceptionBehavior = Ignore` como red de seguridad.
- Aplicar por etapa: timeout en `HttpClient` (Serper/IA), retry con backoff y circuit breaker (Polly), y límites (máx. artículos por corrida, máx. tokens).
- Un artículo que falle se salta y se registra; no aborta la corrida.

**Razón**: por defecto, una excepción no controlada en un `BackgroundService` detiene el host (`StopHost`); blindar el bucle es lo que garantiza que la web no caiga.

**Alternativas descartadas**: dejar que las excepciones se propaguen (riesgo de tumbar el sitio); reintentos infinitos (consumo de créditos).

### 7. Programación ligera: `PeriodicTimer` + señal en memoria (sin dependencia nueva)

**Decisión**: el worker usa `PeriodicTimer` con el intervalo de `automation_settings` y, además, un coordinador singleton (`AutomationCoordinator`) con una señal (`SemaphoreSlim`/`Channel`) para disparar corridas manuales desde el controlador y exponer el estado de pausa en memoria. La configuración durable se lee de MongoDB; la señal es solo para la ejecución inmediata dentro del proceso.

**Razón**: suficiente para una sola réplica y evita la dependencia y el cron de Quartz; el controlador y el worker comparten proceso, así que la coordinación en memoria es directa y sin latencia.

**Alternativas descartadas**: Quartz.NET (cron expresivo + trigger manual, útil si luego se necesita más granularidad, pero añade dependencia); Hangfire (descartado arriba).

### 8. Panel administrativo propio en `Areas/Admin`

**Decisión**: `AutomationController` (`[Area("Admin")] [Authorize] [Route("admin/automatizacion")]`) con acciones: `Index` (panel/estado), `Run` (POST ejecutar ahora), `TogglePause`, `Historial`, `Detalle`, `Configuracion` (GET/POST). ViewModels en `Areas/Admin/Models` y vistas en `Areas/Admin/Views/Automation/` que reutilizan el layout y los patrones (`TempData`, `[ValidateAntiForgeryToken]`, AJAX) del resto del área. La revisión de borradores reutiliza `ArticulosController` existente.

**Razón**: integra el control con la autenticación y el diseño ya existentes; las claves API no se muestran (solo indicador "configurado/ausente").

**Alternativas descartadas**: exponer un dashboard de Hangfire (UI ajena y sin acoplar a la auth de cookies).

## Risks / Trade-offs

- **[Riesgo] Contenido duplicado o alucinado por la IA.** → Mitigación: deduplicación por hash/slug, revisión en borrador por defecto y sanitización obligatoria.
- **[Riesgo] Costo descontrolado (tokens + créditos Serper).** → Mitigación: límites por corrida, circuit breaker y pausa automática ante fallos repetidos.
- **[Riesgo] El worker comparte CPU/memoria con las peticiones web.** → Mitigación: límites estrictos y timeouts; en caso de contención severa, extraer a worker externo (el orquestador ya está en `Aplication`).
- **[Riesgo] `BackgroundServiceExceptionBehavior.Ignore` enmascara errores si el catch falla.** → Mitigación: logging explícito y estado de corrida persistido; el `Ignore` es solo red de seguridad.
- **[Riesgo] El JSON de la IA no respeta el contrato de bloques.** → Mitigación: JSON Schema estricto y validación/sanitización del lado servidor antes de persistir.
- **[Riesgo] Claves API filtradas.** → Mitigación: solo en User Secrets/`.env`, nunca en la UI ni en el repo; el panel muestra únicamente "configurado/ausente".

## Migration Plan

1. Añadir entidades de estado (`AutomationRun`, `AutomationSettings`) y el repositorio; crear colecciones e índices al arrancar (idempotente, junto a `MongoDbIndexer`).
2. Implementar `SerperTopicDiscoveryService` y `OpenAiCompatibleArticleGenerator` con opciones y resiliencia.
3. Implementar `ArticleAutomationService` (orquestador) y `AutomationCoordinator`.
4. Registrar el `BackgroundService`, el coordinador y los servicios en `Program.cs`, con `BackgroundServiceExceptionBehavior.Ignore`.
5. Añadir `AutomationController`, ViewModels y vistas; enlazar el menú administrativo.
6. Añadir configuración (`Serper:ApiKey`, `Ai:ApiKey`, `Ai:BaseUrl`, `Ai:Model`) a `appsettings.json`/User Secrets y `.env.example`.
7. Validar con `dotnet build`/`dotnet test` y una corrida manual con Serper/IA reales; revertir desactivando la automatización (`Enabled = false`) sin tocar el resto.

## Open Questions

- **Proveedor IA concreto** (OpenAI vs. DeepSeek vs. otro): no cambia el diseño gracias a la interfaz agnóstica; se fija como configuración al aplicar.
- **Imagen de portada**: por defecto sin imagen automática; opcionalmente una URL de Unsplash (host ya permitido en `ContentSecurity:AllowedImageHosts`) según tema. Decidible al aplicar sin afectar specs ni tareas.
