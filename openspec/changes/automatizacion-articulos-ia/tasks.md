# Tasks

## 1. Dominio, estado persistente y base de pruebas

- [x] 1.1 Crear `Domine/Entities/AutomationRun.cs` y `AutomationSettings.cs` con sus campos (estado, disparador, temas, artículos, decisiones pendientes, errores; configuración y pausa) y validación mínima de invariantes; verificar con `dotnet build Domine/Domine.csproj`.
- [x] 1.2 Definir `Aplication/Interfaces/IAutomationStateRepository.cs` (GetSettingsAsync, SaveSettingsAsync, InsertRunAsync, GetRunsAsync, GetRunAsync); verificar con `dotnet build Aplication/Aplication.csproj`.
- [x] 1.3 Implementar `Infrastructure/Repositories/AutomationStateRepository.cs` sobre `MongoDbContext`; verificar con `dotnet build Infrastructure/Infrastructure.csproj`.
- [x] 1.4 Registrar la creación idempotente de las colecciones `automation_runs` y `automation_settings` en `MongoDbIndexer`; verificar que el arranque no falla con colecciones nuevas.
- [x] 1.5 Crear el proyecto de pruebas `tests/ArticleAutomation.Tests` (xUnit) referenciando `Domine`, `Aplication` e `Infrastructure`, agregarlo a `paginaWeb.slnx` y verificar que `dotnet test` ejecuta una prueba trivial.

## 2. Integraciones externas (Serper e IA)

- [x] 2.1 Crear `Infrastructure/Serper/SerperOptions.cs` y `SerperTopicDiscoveryService.cs` con `HttpClient` tipado hacia `api.serper.dev` (news/search, `gl`, `hl`, ventana temporal) mapeando la respuesta a `DiscoveredTopic`; verificar con una prueba unitaria usando un `HttpMessageHandler` falso que devuelve temas parseados.
- [x] 2.2 Crear `Infrastructure/Ai/AiOptions.cs` y `OpenAiCompatibleArticleGenerator.cs` con salida restringida por JSON Schema mapeada a `ContentBlockDto`; verificar con una prueba unitaria del mapeo de una respuesta de ejemplo.
- [x] 2.3 Añadir resiliencia (timeout, retry con backoff, circuit breaker) con Polly en ambos clientes; verificar con una prueba que un primer fallo reintenta y un fallo repetido abre el circuito sin lanzar hacia el orquestador.

## 3. Orquestador y coordinación

- [x] 3.1 Definir en `Aplication` las interfaces `ITopicDiscoveryService`, `IAiArticleGenerator` e `IAutomationService` con sus métodos; verificar con `dotnet build Aplication/Aplication.csproj`.
- [x] 3.2 Implementar `Aplication/Services/ArticleAutomationService.cs` (descubrir → detectar repeticiones y marcarlas pendientes de decisión → generar → sanitizar → publicar/borrador vía `IArticleService`) registrando el estado de cada corrida; verificar con pruebas usando fakes de Serper, IA y repositorios.
- [x] 3.3 Implementar la normalización de título y el hash para detectar repeticiones (acentos, capitalización, signos), marcando las temáticas repetidas como pendientes de decisión sin detener la corrida; verificar con pruebas unitarias de casos repetidos y no repetidos.
- [x] 3.4 Implementar `AutomationCoordinator` (singleton) con señal de ejecución manual y estado de pausa en memoria; verificar con una prueba de señalización y pausa.

## 4. Worker y registro de servicios

- [x] 4.1 Implementar `paginaWeb/Workers/ArticleAutomationWorker.cs` (`BackgroundService`) con `PeriodicTimer` más la señal del coordinador y un bucle `try/catch` a prueba de fallos; verificar con una prueba que una excepción simulada no detiene el bucle.
- [x] 4.2 Registrar en `Program.cs` el worker, el coordinador, los servicios, el repositorio y los clientes HTTP, y configurar `BackgroundServiceExceptionBehavior = Ignore`; verificar que la aplicación arranca correctamente.

## 5. Panel administrativo

- [x] 5.1 Crear los ViewModels en `Areas/Admin/Models` (panel, corrida, configuración) y el `AutomationController` con Index, Run, TogglePause, Historial, Detalle y Configuracion; verificar con `dotnet build paginaWeb/paginaWeb.csproj`.
- [x] 5.2 Crear las vistas `Areas/Admin/Views/Automation/` reutilizando el layout admin y los patrones `TempData`, `[ValidateAntiForgeryToken]` y AJAX del área; verificar la navegación manual en el navegador.
- [x] 5.3 Enlazar el menú administrativo y mostrar únicamente el indicador "configurado/ausente" para las claves de Serper e IA; verificar que las claves nunca se renderizan en la UI.
- [x] 5.4 Implementar el listado de temáticas pendientes de decisión en el panel con acciones Continuar/Omitir; verificar en el navegador que una decisión genera o descarta el artículo según corresponda.

## 6. Configuración y despliegue

- [x] 6.1 Añadir las secciones `Serper` y `Ai` a `appsettings.json` y al `.env.example` (claves, base URL, modelo) con binding de opciones; verificar que la app lee las opciones desde configuración o User Secrets.
- [x] 6.2 Documentar en `GUIA_COMANDOS.md` los comandos `user-secrets` para Serper e IA y el flujo de pausa/ejecución; verificar que los comandos documentados son correctos.

## 7. Integración y verificación final

- [x] 7.1 Ejecutar `dotnet build` y `dotnet test` sobre la solución completa; verificar cero errores y todas las pruebas en verde.
- [ ] 7.2 Ejecutar una corrida manual con Serper e IA reales y verificar: temas descubiertos, detección de repeticiones con decisión (continuar/omitir), artículos en borrador (y publicados con auto-publicar), historial y estado en el panel.
- [ ] 7.3 Provocar un fallo de Serper o de la IA y verificar que la web sigue disponible y la corrida queda marcada como fallida; reintentar y verificar que no se generan duplicados.
- [x] 7.4 Revisar con búsqueda global que no quedan claves API hardcodeadas ni expuestas en vistas o logs.

## 8. Claves, proveedor, programación y panel

- [x] 8.1 Añadir a `AutomationSettings` los campos de claves (`SerperApiKey`, `AiApiKey`), proveedor (`AiProvider`, `AiBaseUrl`, `AiModel`) y programación (`ScheduleEnabled`, `PublishHour`, `PublishMinute`); verificar con `dotnet build Domine/Domine.csproj`.
- [x] 8.2 Pasar las claves/proveedor por solicitud (`ApiKey` en `TopicDiscoveryRequest`; `ApiKey`/`BaseUrl`/`Model` en `TopicGenerationRequest`) y refactorizar los clientes para usarlas; eliminar `SerperOptions`/`AiOptions` y sus registros; verificar build y actualizar las pruebas de clientes.
- [x] 8.3 Añadir `ArticleStatus.Programado` y `Article.ScheduledAt`; mapearlos en `ArticleMapper`, `ArticleDto` y `ArticleResponseDto`; verificar build de `Domine`/`Aplication`.
- [x] 8.4 Añadir `IArticleRepository.GetScheduledToPublishAsync` y `IArticleService.PublishScheduledAsync`; verificar build.
- [x] 8.5 Integrar la programación en el orquestador (estado programado según `ScheduleEnabled`/`PublishHour`/`PublishMinute` o fecha por artículo) y exponer `PublishScheduledAsync` en `IAutomationService`; verificar con pruebas.
- [x] 8.6 Actualizar `ArticleAutomationWorker` a un ciclo por ticks que publique artículos programados vencidos y ejecute la generación según la frecuencia; verificar la prueba del worker.
- [x] 8.7 Crear `AiProviderPresets` y `LocaleOptions` (países/idiomas) en `Aplication`; verificar build.
- [x] 8.8 Actualizar `AutomationConfigViewModel` y la vista `Configuracion` con desplegables de proveedor/país/idioma, campos de claves enmascarados, campos de programación y gestión de categorías inline; verificar build y navegación.
- [x] 8.9 Actualizar `AutomationController` para guardar claves/proveedor/programación y crear categorías/subcategorías inline reutilizando `ICategoryService`; verificar build.
- [x] 8.10 Actualizar `appsettings.json`, `.env.example` y `docker-compose.yml` para eliminar las claves estáticas (ahora se gestionan desde el panel); verificar build.

## 9. Correcciones y mejoras de interfaz

- [x] 9.1 Manejar modelos de razonamiento: `MaxTokensPerArticle` por defecto en 8000 y mensaje claro cuando la IA devuelve contenido vacío o `finish_reason=length`.
- [x] 9.2 Mostrar las claves API como "✓ Configurada" (solo el check) con opción "Cambiar" que revela el campo.
- [x] 9.3 Mostrar el formulario de horario solo al activar "Programar publicación".
- [x] 9.4 Reutilizar el botón "+ Categoría" (estilo del botón de Categorías) para crear categorías desde la configuración.
- [x] 9.5 Añadir el botón "Validar y salir" que valida la configuración completa antes de salir.
- [x] 9.6 Añadir confirmación (aviso) y estado de carga a los botones "Ejecutar ahora", "Pausar" y "Continuar".
- [x] 9.7 Hacer la ejecución manual asíncrona (señal al worker) para no bloquear la petición HTTP.
- [x] 9.8 Añadir barra de estado del proceso (rojo en curso → verde finalizado) con polling al endpoint `/admin/automatizacion/estado`.
- [x] 9.9 Añadir parámetros configurables de Serper (tipo de búsqueda, antigüedad/tbs, resultados por consulta, búsqueda segura y autocorrección).
- [x] 9.10 Corregir "Omitir/Continuar": reemplazar el operador posicional no soportado (`PendingDecisions[-1]`) por carga y reemplazo del documento de corrida.
- [x] 9.11 Reemplazar el límite de tokens por longitud en palabras (desplegable: 500–3000) con estimación interna de tokens y longitud indicada en el prompt.
- [x] 9.12 Implementar reparto round-robin configurable entre temáticas base (checkbox en configuración) y alternar con el modo secuencial.
- [x] 9.13 Registrar y mostrar el consumo: créditos de Serper (campo `credits` de la respuesta) y tokens de IA (`usage`) por corrida, con panel de totales y última corrida.
- [x] 9.14 Permitir configurar la frecuencia en horas + minutos (además de activar/desactivar la generación).
- [x] 9.15 Añadir panel de saldo disponible: Serper (`GET /account`) y DeepSeek (`GET /user/balance`), con botón de actualización.
- [x] 9.16 Sistema global de toasts (`window.showToast`) en el layout admin, con los mensajes de TempData (éxito/error) como toasts.
- [x] 9.17 Corregir el botón "Actualizar" del saldo (sin caché + estado "Actualizando…").
- [x] 9.18 Dividir la configuración en secciones/cards independientes (Claves, Serper, IA, Publicación, General, Categorías).
- [x] 9.19 Mover el bloque de consumo/saldo a la parte superior, compacto y dinámico (uso cada 3 s; saldo cada 60 s y botón).
- [x] 9.20 Unificar la paleta con colores sólidos (base azul/amarillo + verde/rojo) en panel, botones, badges y toasts; sin pasteles ni degradados.
- [x] 9.21 Aumentar el timeout de la IA a 180 s (el de 30 s cortaba generaciones largas) y mostrar un mensaje claro ante `finish_reason=length`.
- [x] 9.22 Verificar generación con "Búsqueda web": 6 temas → 3 artículos, 0 errores, usando un modelo no razonador (`deepseek-chat`); el modelo de razonamiento agotaba el presupuesto en tokens de razonamiento.
- [x] 9.23 Añadir presupuesto de tokens configurable en la configuración ("Máx. tokens/artículo", 0 = automático según la longitud) y aviso sobre modelos de razonamiento.
- [x] 9.24 Mejorar los mensajes de error de IA con diagnóstico (finish_reason, tokens de salida y razonamiento) y sugerencia de solución.
- [x] 9.25 Reemplazar los diálogos nativos del navegador (`confirm`, `alert` y `prompt`) por un modal global con el estilo del proyecto (colores sólidos, tarjeta redondeada, iconos y botones del panel).
- [x] 9.26 Auditar y eliminar código muerto/repetido: exportación de diálogo sin uso, fallback nativo obsoleto, mapeo `Paused` sin uso, helper de presets sin uso, guardas inalcanzables y opciones JSON duplicadas.

## 10. Generación avanzada y calidad del artículo

- [x] 10.1 Añadir al dominio la configuración avanzada: plantilla, tono, bloques mín/máx, secciones requeridas, temperatura, intentos con corrección y tolerancia de palabras.
- [x] 10.2 Construir el prompt con plantilla, tono, rango de bloques, secciones requeridas y longitud objetivo, y aplicar la temperatura configurada.
- [x] 10.3 Crear el validador de artículos generados (extensión con tolerancia, bloques, jerarquía de encabezados, secciones requeridas, metadatos y etiquetas).
- [x] 10.4 Reintentar la generación hasta el número de intentos configurado enviando los errores de validación como indicaciones de corrección, acumulando tokens.
- [x] 10.5 Exponer la sección "Generación avanzada" en la configuración y mapear los campos de ida y vuelta.
- [x] 10.6 Actualizar las pruebas del orquestador y añadir pruebas del validador.
- [x] 10.7 Verificar una generación real con la estructura avanzada (10 bloques, lista y conclusión, extensión dentro de tolerancia).
- [x] 10.8 Añadir la plantilla "Informativa" (explicativa) a las opciones de generación y a su descripción de prompt.
- [x] 10.9 Calcular estándares automáticos por longitud (tokens, bloques y tolerancia) y aplicarlos salvo que el usuario desactive el ajuste.
- [x] 10.10 Mostrar la configuración avanzada al activar un control de alternancia, recordando su estado.
- [x] 10.11 Cuando el artículo no cumple los estándares tras los intentos, dejarlo pendiente de confirmación con los errores y permitir guardarlo de todos modos o descartarlo.

## Workflow follow-up

- Revisar los artefactos de OpenSpec con el usuario antes de aplicar.
- Archivar el cambio después de completar la implementación y la verificación.
