# Spec Delta

## Purpose

Mantiene actualizados los bloques de datos del panel administrativo sin recarga manual, con invalidación selectiva por módulo, reconexión segura y degradación controlada, sin polling pesado ni webhooks de negocio abiertos.

## ADDED Requirements

### Requirement: Actualización del estado de automatización

El panel administrativo SHALL reflejar el inicio, progreso y fin de una corrida de automatización, manual o programada, sin que el usuario recargue la página.

#### Scenario: Corrida manual

- **WHEN** un administrador inicia una corrida manual
- **THEN** el estado pasa a "en curso" en menos de 2 segundos
- **AND** al finalizar muestra el resultado, el disparador y los artículos generados en menos de 5 segundos

#### Scenario: Corrida programada

- **WHEN** el worker inicia o termina una corrida programada mientras el panel está abierto
- **THEN** el estado visible se actualiza igual que en una corrida manual
- **AND** no es necesario refrescar el navegador

#### Scenario: Corrida detenida

- **WHEN** un administrador solicita detener la corrida
- **THEN** el estado refleja la detención y deja de mostrarse como "en curso" en menos de 5 segundos

### Requirement: Actualización de decisiones pendientes

Las decisiones pendientes de una corrida SHALL aparecer y desaparecer del panel sin recarga manual.

#### Scenario: Nueva decisión pendiente

- **WHEN** la automatización genera una decisión pendiente por duplicado o por validación fallida
- **THEN** la decisión aparece en la lista en menos de 5 segundos
- **AND** el contador de pendientes se incrementa

#### Scenario: Decisión resuelta

- **WHEN** un administrador continúa u omite una decisión
- **THEN** la decisión desaparece de la lista de pendientes en menos de 5 segundos
- **AND** el contador se decrementa

### Requirement: Actualización del listado de artículos

El listado de artículos del panel SHALL reflejar altas, cambios de estado, publicación programada y cambios de destacado producidos por la automatización o por otro administrador, sin recarga manual.

#### Scenario: Artículo creado por automatización

- **WHEN** una corrida crea un artículo que coincide con el filtro y la página actuales
- **THEN** el artículo aparece en el listado en menos de 5 segundos
- **AND** si no coincide con el filtro, se muestra un indicador de que hay cambios disponibles

#### Scenario: Publicación programada

- **WHEN** el worker publica un artículo cuya fecha programada venció
- **THEN** su estado cambia a "Publicado" en el listado en menos de 5 segundos

#### Scenario: Destacado modificado por otro administrador

- **WHEN** otro administrador fija o quita un artículo del Hero
- **THEN** el listado y los contadores de destacados se actualizan en menos de 5 segundos

### Requirement: Actualización del listado de categorías

El listado jerárquico de categorías SHALL reflejar altas, ediciones y bajas sin recarga manual.

#### Scenario: Categoría creada o editada

- **WHEN** se crea o edita una categoría principal o subcategoría
- **THEN** el árbol y los contadores se actualizan en menos de 5 segundos

#### Scenario: Categoría eliminada

- **WHEN** se elimina una categoría principal o subcategoría
- **THEN** la categoría y sus dependencias visuales desaparecen del listado en menos de 5 segundos

### Requirement: Actualización de la moderación de comentarios

La pantalla de moderación SHALL reflejar comentarios nuevos y cambios de estado sin recarga manual.

#### Scenario: Comentario pendiente nuevo

- **WHEN** un visitante envía un comentario que queda pendiente
- **THEN** el comentario aparece en el filtro "Pendientes" en menos de 5 segundos
- **AND** el indicador global de pendientes se incrementa

#### Scenario: Comentario aprobado o rechazado

- **WHEN** un administrador aprueba, rechaza o elimina un comentario
- **THEN** la lista y el indicador global se actualizan en menos de 5 segundos

### Requirement: Invalidación selectiva y eficiencia

Cada módulo SHALL exponer una versión de cambios propia, y una modificación en un módulo MUST NOT obligar a recargar los demás.

#### Scenario: Cambio en un solo módulo

- **WHEN** se modifica un artículo
- **THEN** solo se actualiza el bloque de artículos
- **AND** los bloques de categorías y comentarios no vuelven a consultar sus datos

#### Scenario: Panel inactivo

- **WHEN** no hay cambios ni corridas en curso
- **THEN** el panel no realiza consultas periódicas de datos a MongoDB
- **AND** como máximo consulta una versión ligera en memoria

### Requirement: Reconexión y degradación controlada

Si el canal de actualización en vivo se interrumpe, el panel SHALL informar el estado de desconexión y mantener una degradación funcional sin presentar datos viejos como actuales.

#### Scenario: Pérdida de conexión

- **WHEN** se pierde la conexión en vivo
- **THEN** el panel muestra que la actualización automática está suspendida
- **AND** activa un polling ligero de respaldo

#### Scenario: Reconexión

- **WHEN** se recupera la conexión
- **THEN** el panel reconcilia los datos y vuelve al modo en vivo
- **AND** no duplica eventos ni suscripciones

### Requirement: Seguridad y aislamiento de datos

Solo administradores autenticados SHALL recibir actualizaciones del panel, y los eventos MUST NOT transportar secretos ni configuración sensible.

#### Scenario: Acceso no autenticado

- **WHEN** un cliente no autenticado intenta abrir el canal de actualización
- **THEN** la conexión es rechazada

#### Scenario: Datos sensibles

- **WHEN** se publica un evento de automatización
- **THEN** no se envían claves de API, saldos de proveedores ni la configuración completa
- **AND** el componente solicita solo los datos autorizados que necesita

#### Scenario: Navegación fuera del panel

- **WHEN** el administrador navega fuera de una página con actualización en vivo
- **THEN** la suscripción se cancela y no queda consumiendo recursos

### Requirement: Límites de carga y backpressure

El mecanismo de actualización SHALL acotar la cola de eventos, coalescer ráfagas y evitar que un cliente lento bloquee a los demás o a las escrituras.

#### Scenario: Ráfaga de cambios

- **WHEN** la automatización genera varios artículos en pocos segundos
- **THEN** los eventos se agrupan y la interfaz converge al estado final
- **AND** las escrituras de negocio no se bloquean por la UI

#### Scenario: Cliente lento

- **WHEN** un cliente no consume eventos a tiempo
- **THEN** los demás clientes siguen recibiendo actualizaciones
- **AND** el cliente atrasado recibe una señal de reconciliación en lugar de acumular eventos sin límite

### Requirement: Sin webhooks de negocio abiertos

El tiempo real MUST NOT requerir endpoints de webhook entrantes ni mantener conexiones HTTP abiertas hacia proveedores externos.

#### Scenario: Corrida de automatización

- **WHEN** la automatización consulta Serper o un proveedor de IA
- **THEN** usa llamadas HTTP de solicitud/respuesta de vida corta
- **AND** no deja un webhook ni una conexión abierta esperando la respuesta

### Requirement: Compatibilidad del sitio público y del editor

El cambio MUST NOT alterar las URLs, formularios, validaciones, SEO ni el editor de artículos del sitio público.

#### Scenario: Navegación pública

- **WHEN** un visitante navega por el portal público
- **THEN** las páginas siguen sirviéndose por el flujo MVC existente
- **AND** no se carga el canal de actualización del panel administrativo

#### Scenario: Edición de artículos

- **WHEN** un administrador crea o edita un artículo
- **THEN** utiliza el editor y formulario existentes
- **AND** el comportamiento de guardado y validación no cambia
