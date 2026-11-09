# Proposal

## Why

El panel administrativo muestra datos desactualizados después de cada corrida de automatización, tanto manual como programada: la barra de estado se refresca por polling, pero los listados de artículos, categorías, comentarios y decisiones pendientes permanecen con la información de la carga inicial. El polling actual además consulta MongoDB cada 3 segundos por pestaña abierta y trae documentos completos con borradores, lo que no escala y no resuelve la desincronización de los listados.

## What Changes

- Introducir un contador de versión por módulo (`automation`, `articles`, `categories`, `comments`) en memoria, incrementado únicamente después de escrituras exitosas.
- Publicar eventos de cambio de dominio desde los servicios de aplicación y desde el worker de automatización, sin acoplar la lógica de negocio a la UI.
- Añadir un bus de notificaciones en proceso con cola acotada, coalescencia por entidad y un único despachador, para evitar tormentas de eventos y bloqueos.
- Convertir en islas Blazor Server interactivas los bloques de listado/estado del Admin:
  - estado y decisiones pendientes de automatización;
  - listado de artículos;
  - árbol/listado de categorías;
  - moderación de comentarios.
- Mantener ASP.NET Core MVC para el sitio público, el editor de artículos, los formularios de creación/edición y las pantallas sin necesidad de tiempo real.
- Añadir polling ligero de respaldo y reconexión para cuando el circuito Blazor no esté disponible, sin volver a consultar MongoDB en cada ciclo.
- Cachear el estado consolidado de automatización y usar proyecciones/conteos para evitar cargar corridas completas con `DraftJson` durante la actualización.
- Garantizar que el circuito y los eventos estén autenticados, que no se difundan claves de API, saldos ni configuración sensible, y que cada componente se desuscriba al navegar.
- **Sin webhooks de negocio nuevos**: el tiempo real usa el circuito SignalR interno de Blazor Server. Las llamadas a Serper/IA siguen siendo HTTP puntuales y no quedan abiertas.

## Capabilities

### New Capabilities

- `admin-real-time-updates`: actualización automática, segura y sin polling pesado de los bloques de datos del panel administrativo, con invalidación por módulo, reconciliación al reconectar y degradación controlada a polling ligero.

### Modified Capabilities

<!-- Ninguna: no existe una capability principal de automatización administrativa ni de tiempo real en openspec/specs/; este cambio introduce una nueva. -->

## Impact

- **Código afectado**:
  - `paginaWeb/Program.cs`: registro de componentes Blazor Server, circuitos, autorización y servicios de notificación.
  - `paginaWeb/Areas/Admin/Views`: reemplazo de bloques de listado/estado por componentes interactivos; el resto de vistas se conserva.
  - `paginaWeb/Areas/Admin/Controllers`: carga inicial de datos para las islas y endpoints de respaldo.
  - `paginaWeb/Components` (nuevo): componentes de listado/estado, layout mínimo de Blazor y suscripciones.
  - `Aplication/Services`: publicación de eventos después de escrituras en artículos, categorías, comentarios y automatización.
  - `Aplication`/`Infrastructure`: caché de estado y consultas proyectadas para no traer documentos completos.
  - `paginaWeb/Styles/tailwind.css`: inclusión de la carpeta de componentes en el escaneo de Tailwind.
- **Dependencias**: componentes Blazor Server incluidos en ASP.NET Core; no se requiere un proveedor externo ni Redis mientras exista una sola instancia. Si se escala a varias réplicas, se necesitará backplane o change streams.
- **Infraestructura**: el proxy reverso debe permitir WebSocket/`Upgrade` para el circuito; el polling de respaldo funciona sin esa configuración.
- **Persistencia**: sin cambios de esquema en MongoDB. Se evita persistir progreso por token o por bloque.
- **Compatibilidad**: las URLs, formularios, validaciones y comportamiento del sitio público se mantienen. El editor de artículos no se migra.
- **Pruebas**: build, tests existentes y nuevas pruebas de versiones, coalescencia, autorización, reconexión y ausencia de polling pesado.
