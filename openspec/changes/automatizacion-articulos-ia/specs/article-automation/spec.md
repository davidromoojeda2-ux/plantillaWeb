# Spec Delta

## Purpose

Define el pipeline de automatización que descubre temáticas actuales mediante Serper, genera artículos estructurados con IA y los publica o deja en borrador, garantizando resiliencia y control administrativo.

## ADDED Requirements

### Requirement: Descubrimiento de temáticas mediante Serper

El sistema SHALL descubrir temáticas candidatas consultando Serper a partir de los temas base, el país y el idioma configurados dentro de una ventana temporal, y MUST devolver para cada temática un título y una referencia a la fuente.

#### Scenario: Descubrimiento exitoso de temáticas

- **WHEN** se ejecuta una corrida con temas base y configuración de país/idioma válidos
- **THEN** el sistema obtiene una lista de temáticas candidatas desde Serper

#### Scenario: Serper no disponible

- **WHEN** Serper no responde o devuelve un error durante una corrida
- **THEN** el sistema registra el fallo y no genera artículos en esa corrida

### Requirement: Deduplicación de temáticas ya cubiertas

El sistema MUST descartar las temáticas candidatas que ya fueron procesadas en corridas anteriores o que ya cuentan con un artículo equivalente publicado, comparando una forma normalizada del título.

#### Scenario: Tema ya publicado

- **WHEN** una temática candidata ya tiene un artículo publicado con un título equivalente
- **THEN** el sistema la omite y no genera un artículo duplicado

#### Scenario: Tema ya procesado sin publicar

- **WHEN** una temática ya fue procesada en una corrida anterior aunque no se publicó
- **THEN** el sistema no la regenera y evita reintentos redundantes

### Requirement: Generación de artículos estructurados con IA

El sistema SHALL generar, para cada temática aceptada, un artículo completo (título, resumen, bloques de contenido, etiquetas, metadatos SEO y categoría sugerida) mediante una IA con salida estructurada en el contrato de bloques del portal.

#### Scenario: Generación válida

- **WHEN** la IA responde con una estructura de artículo válida
- **THEN** el sistema obtiene un borrador de artículo con bloques y metadatos listos para validar

#### Scenario: Generación inválida o incompleta

- **WHEN** la IA responde con una estructura inválida o incompleta
- **THEN** el sistema rechaza el artículo y registra el error sin publicarlo

### Requirement: Sanitización y validación del contenido generado

El contenido generado por la IA MUST pasar por el sanitizador y el validador del portal antes de persistirse, y el contenido que no cumpla las reglas MUST rechazarse o quedar en borrador según la configuración.

#### Scenario: Contenido con HTML no permitido

- **WHEN** el contenido generado contiene HTML no permitido
- **THEN** el sistema lo sanitiza antes de persistirlo

#### Scenario: Contenido fuera de límites

- **WHEN** el contenido no cumple los límites de bloques o de longitud establecidos
- **THEN** el sistema rechaza el artículo con un error de validación

### Requirement: Publicación con control de estado

El sistema SHALL publicar o dejar en borrador cada artículo generado según la configuración de auto-publicación, y MUST reutilizar el flujo de publicación existente con slug único, campos derivados y validación de invariantes.

#### Scenario: Auto-publicación habilitada

- **WHEN** la auto-publicación está habilitada y el artículo generado es válido
- **THEN** el sistema lo publica y el artículo queda visible en el portal

#### Scenario: Auto-publicación deshabilitada

- **WHEN** la auto-publicación está deshabilitada
- **THEN** el sistema guarda el artículo en borrador para revisión administrativa

### Requirement: Resiliencia del worker ante fallos externos

Un fallo en Serper, la IA o la base de datos MUST NOT detener la aplicación web. La automatización SHALL aplicar timeouts, reintentos y circuit breaker, y MUST registrar el estado de cada corrida para reintentarla en la siguiente.

#### Scenario: Fallo externo durante una corrida

- **WHEN** un servicio externo falla durante una corrida
- **THEN** la aplicación web sigue disponible y la corrida se marca como fallida

#### Scenario: Reintento sin duplicados

- **WHEN** una corrida falla y se ejecuta una siguiente
- **THEN** el sistema reintenta las temáticas pendientes sin duplicar los artículos ya publicados

### Requirement: Control y observabilidad administrativa

Un administrador autenticado SHALL poder ejecutar una corrida manual, pausar o reanudar la automatización, consultar el historial de corridas y revisar los borradores generados desde el panel administrativo.

#### Scenario: Ejecución manual de una corrida

- **WHEN** un administrador solicita ejecutar una corrida manual
- **THEN** el sistema lanza el pipeline de inmediato y registra la corrida

#### Scenario: Pausa de la automatización

- **WHEN** un administrador pausa la automatización
- **THEN** el worker deja de ejecutar corridas programadas hasta que se reanude

#### Scenario: Revisión de un borrador generado

- **WHEN** un administrador revisa un borrador generado
- **THEN** puede publicarlo, editarlo o descartarlo mediante el flujo administrativo existente

### Requirement: Configuración del pipeline de automatización

El sistema SHALL permitir configurar la frecuencia, las categorías habilitadas, los temas base, el país/idioma y los límites de artículos por corrida, y MUST persistir esa configuración para las siguientes ejecuciones.

#### Scenario: Configuración válida guardada

- **WHEN** un administrador guarda una configuración válida
- **THEN** el sistema la aplica en la siguiente corrida

#### Scenario: Configuración sin categorías ni temas base

- **WHEN** la configuración no incluye categorías habilitadas o temas base
- **THEN** el sistema no genera artículos y lo indica en el estado de la automatización
