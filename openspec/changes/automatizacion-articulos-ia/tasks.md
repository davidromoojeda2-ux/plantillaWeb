# Tasks

## 1. Dominio, estado persistente y base de pruebas

- [ ] 1.1 Crear `Domine/Entities/AutomationRun.cs` y `AutomationSettings.cs` con sus campos (estado, disparador, temas, artículos, errores; configuración y pausa) y validación mínima de invariantes; verificar con `dotnet build Domine/Domine.csproj`.
- [ ] 1.2 Definir `Aplication/Interfaces/IAutomationStateRepository.cs` (GetSettingsAsync, SaveSettingsAsync, InsertRunAsync, GetRunsAsync, GetRunAsync); verificar con `dotnet build Aplication/Aplication.csproj`.
- [ ] 1.3 Implementar `Infrastructure/Repositories/AutomationStateRepository.cs` sobre `MongoDbContext`; verificar con `dotnet build Infrastructure/Infrastructure.csproj`.
- [ ] 1.4 Registrar la creación idempotente de las colecciones `automation_runs` y `automation_settings` en `MongoDbIndexer`; verificar que el arranque no falla con colecciones nuevas.
- [ ] 1.5 Crear el proyecto de pruebas `tests/ArticleAutomation.Tests` (xUnit) referenciando `Domine`, `Aplication` e `Infrastructure`, agregarlo a `paginaWeb.slnx` y verificar que `dotnet test` ejecuta una prueba trivial.

## 2. Integraciones externas (Serper e IA)

- [ ] 2.1 Crear `Infrastructure/Serper/SerperOptions.cs` y `SerperTopicDiscoveryService.cs` con `HttpClient` tipado hacia `api.serper.dev` (news/search, `gl`, `hl`, ventana temporal) mapeando la respuesta a `DiscoveredTopic`; verificar con una prueba unitaria usando un `HttpMessageHandler` falso que devuelve temas parseados.
- [ ] 2.2 Crear `Infrastructure/Ai/AiOptions.cs` y `OpenAiCompatibleArticleGenerator.cs` con salida restringida por JSON Schema mapeada a `ContentBlockDto`; verificar con una prueba unitaria del mapeo de una respuesta de ejemplo.
- [ ] 2.3 Añadir resiliencia (timeout, retry con backoff, circuit breaker) con Polly en ambos clientes; verificar con una prueba que un primer fallo reintenta y un fallo repetido abre el circuito sin lanzar hacia el orquestador.

## 3. Orquestador y coordinación

- [ ] 3.1 Definir en `Aplication` las interfaces `ITopicDiscoveryService`, `IAiArticleGenerator` e `IAutomationService` con sus métodos; verificar con `dotnet build Aplication/Aplication.csproj`.
- [ ] 3.2 Implementar `Aplication/Services/ArticleAutomationService.cs` (descubrir → deduplicar → generar → sanitizar → publicar/borrador vía `IArticleService`) registrando el estado de cada corrida; verificar con pruebas usando fakes de Serper, IA y repositorios.
- [ ] 3.3 Implementar la normalización de título y el hash de deduplicación (acentos, capitalización, signos) junto a la comprobación de slug existente; verificar con pruebas unitarias de casos duplicados y no duplicados.
- [ ] 3.4 Implementar `AutomationCoordinator` (singleton) con señal de ejecución manual y estado de pausa en memoria; verificar con una prueba de señalización y pausa.

## 4. Worker y registro de servicios

- [ ] 4.1 Implementar `paginaWeb/Workers/ArticleAutomationWorker.cs` (`BackgroundService`) con `PeriodicTimer` más la señal del coordinador y un bucle `try/catch` a prueba de fallos; verificar con una prueba que una excepción simulada no detiene el bucle.
- [ ] 4.2 Registrar en `Program.cs` el worker, el coordinador, los servicios, el repositorio y los clientes HTTP, y configurar `BackgroundServiceExceptionBehavior = Ignore`; verificar que la aplicación arranca correctamente.

## 5. Panel administrativo

- [ ] 5.1 Crear los ViewModels en `Areas/Admin/Models` (panel, corrida, configuración) y el `AutomationController` con Index, Run, TogglePause, Historial, Detalle y Configuracion; verificar con `dotnet build paginaWeb/paginaWeb.csproj`.
- [ ] 5.2 Crear las vistas `Areas/Admin/Views/Automation/` reutilizando el layout admin y los patrones `TempData`, `[ValidateAntiForgeryToken]` y AJAX del área; verificar la navegación manual en el navegador.
- [ ] 5.3 Enlazar el menú administrativo y mostrar únicamente el indicador "configurado/ausente" para las claves de Serper e IA; verificar que las claves nunca se renderizan en la UI.

## 6. Configuración y despliegue

- [ ] 6.1 Añadir las secciones `Serper` y `Ai` a `appsettings.json` y al `.env.example` (claves, base URL, modelo) con binding de opciones; verificar que la app lee las opciones desde configuración o User Secrets.
- [ ] 6.2 Documentar en `GUIA_COMANDOS.md` los comandos `user-secrets` para Serper e IA y el flujo de pausa/ejecución; verificar que los comandos documentados son correctos.

## 7. Integración y verificación final

- [ ] 7.1 Ejecutar `dotnet build` y `dotnet test` sobre la solución completa; verificar cero errores y todas las pruebas en verde.
- [ ] 7.2 Ejecutar una corrida manual con Serper e IA reales y verificar: temas descubiertos, deduplicación, artículos en borrador (y publicados con auto-publicar), historial y estado en el panel.
- [ ] 7.3 Provocar un fallo de Serper o de la IA y verificar que la web sigue disponible y la corrida queda marcada como fallida; reintentar y verificar que no se generan duplicados.
- [ ] 7.4 Revisar con búsqueda global que no quedan claves API hardcodeadas ni expuestas en vistas o logs.

## Workflow follow-up

- Revisar los artefactos de OpenSpec con el usuario antes de aplicar.
- Archivar el cambio después de completar la implementación y la verificación.
