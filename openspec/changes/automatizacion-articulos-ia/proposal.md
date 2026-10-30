# Proposal

## Why

El portal depende hoy de redacción manual: cada artículo se crea a mano en el panel administrativo. Esto no escala y deja el sitio sin contenido fresco de forma recurrente. Se necesita una automatización que descubra temáticas actuales mediante Serper, genere borradores con IA y los publique (o los deje en revisión) reutilizando el modelo de contenido por bloques y el flujo de publicación ya existentes, garantizando además que un fallo del servicio de automatización nunca afecte la disponibilidad del sitio.

## What Changes

- Nuevo pipeline de automatización como **worker embebido** (`BackgroundService`) en la aplicación web: descubre temáticas con Serper, genera artículos con IA y publica a través del `IArticleService` existente.
- Nuevas abstracciones en `Aplication`: `ITopicDiscoveryService`, `IAiArticleGenerator` e `IAutomationService` (orquestador). Implementaciones en `Infrastructure`: cliente HTTP de Serper, generador IA y repositorio de estado.
- Deduplicación y estado persistidos en MongoDB mediante nuevas colecciones (`automation_runs` y `automation_settings`), que además actúan como fuente de verdad para el idempotencia y el historial.
- Panel de control administrativo "Automatización" en el área Admin: ejecutar ahora, pausar/reanudar, historial de corridas, revisión de borradores generados y configuración del pipeline.
- Blindaje del worker frente a fallos externos (Serper/IA/MongoDB) con captura de excepciones, reintentos, timeouts, circuit breaker y límites, de modo que un error nunca detenga el host.
- Configuración del pipeline: auto-publicar vs. dejar en borrador, frecuencia, categorías habilitadas, temas base, país/idioma y límites (artículos por corrida, tokens).

## Capabilities

### New Capabilities

- `article-automation`: pipeline de generación y publicación automática de artículos a partir de temáticas descubiertas con Serper y generadas con IA, con deduplicación, validación, control de estado de publicación, resiliencia ante fallos y panel administrativo de control.

### Modified Capabilities

<!-- Ninguna. La validación, sanitización y publicación de artículos ya están cubiertas por las capacidades existentes; este cambio añade un nuevo mecanismo que las reutiliza sin alterar su comportamiento. -->

## Impact

- **Domine**: entidades de estado de la automatización (`AutomationRun`, `AutomationSettings`).
- **Aplication**: interfaces nuevas (`ITopicDiscoveryService`, `IAiArticleGenerator`, `IAutomationService`, `IAutomationStateRepository`) y servicio orquestador.
- **Infrastructure**: cliente HTTP de Serper, cliente de IA con salida JSON estructurada y repositorio de estado en MongoDB.
- **paginaWeb**: `BackgroundService` (motor), `AutomationController` y vistas administrativas, ViewModels, registro de servicios y configuración en `appsettings.json`.
- **Configuración/Despliegue**: variables de entorno para claves de Serper e IA (User Secrets en desarrollo, `.env` en producción). Sin cambios en Docker: el worker corre en el mismo contenedor.
- **Dependencias**: `HttpClient` con resiliencia (retry/timeout/circuit breaker, vía Polly o manual); sin servicios externos adicionales.
- **Seguridad**: las claves API no se exponen en la UI; el panel requiere autenticación administrativa existente.
