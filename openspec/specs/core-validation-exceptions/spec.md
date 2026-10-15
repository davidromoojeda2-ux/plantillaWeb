# Core Validation Exceptions Specification

## Purpose

Define el comportamiento esperado para la validación de reglas de negocio e invariantes en el núcleo, la tipificación de excepciones, la adopción del patrón MVC con ViewModels y Tag Helpers en presentación y la eliminación de duplicidad de consultas entre controladores y servicios.

## Requirements

### Requirement: Validación de invariantes en entidades de dominio
Las entidades del dominio editorial (Artículos y Categorías) MUST validar sus invariantes de negocio antes de permitir cambios de estado o persistencia, impidiendo la existencia de entidades inconsistentes en memoria o base de datos.

#### Scenario: Intento de publicar un artículo sin contenido o título
- **WHEN** se solicita cambiar el estado de un artículo a Publicado sin contar con título o contenido válido
- **THEN** el sistema lanza una excepción de validación de dominio detallando los campos faltantes sin alterar la base de datos

#### Scenario: Intento de asignar una categoría como su propio padre
- **WHEN** se intenta asignar a una categoría un identificador padre igual a su propio identificador
- **THEN** el sistema rechaza la operación con una excepción de validación de dominio impidiendo ciclos jerárquicos

### Requirement: Detección de colisiones de slug sin consultas duplicadas
La verificación de unicidad de slug MUST realizarse exclusivamente dentro de la capa de servicios de aplicación durante la creación o actualización, eliminando consultas preventivas duplicadas desde los controladores.

#### Scenario: Creación de entidad con slug duplicado
- **WHEN** un usuario intenta registrar una categoría o artículo con un slug ya existente en el catálogo
- **THEN** el servicio ejecuta una única comprobación contra el repositorio y arroja una excepción tipada de conflicto de slug con el mensaje correspondiente

#### Scenario: Edición de entidad manteniendo su slug actual
- **WHEN** se actualiza un artículo o categoría conservando su slug propio
- **THEN** el servicio excluye el identificador de la entidad actual al validar colisión y permite la persistencia sin lanzar excepciones

### Requirement: Propagación y mapeo de excepciones tipadas en controladores
Los controladores administrativos MUST capturar únicamente excepciones tipadas de negocio y validación para asociarlas al ModelState del formulario, permitiendo que fallos inesperados de infraestructura alcancen el manejador global de errores.

#### Scenario: Manejo de error de validación de negocio en formulario administrativo
- **WHEN** una acción de controlador recibe una excepción de validación o conflicto de slug del servicio
- **THEN** el controlador agrega el error al campo correspondiente del ModelState y recarga la vista con los datos del formulario

#### Scenario: Ocurrencia de fallo de infraestructura en base de datos o almacenamiento
- **WHEN** la base de datos MongoDB o el servicio de almacenamiento remoto no responden durante una operación
- **THEN** la excepción de infraestructura no es capturada como error de validación de formulario y se propaga al manejador de errores de la aplicación

### Requirement: Separación del modelo de presentación (ViewModels) y orquestación del controlador
Los controladores de la capa de presentación MUST orquestar la comunicación entre los servicios de aplicación y las vistas Razor mediante ViewModels fuertemente tipados, prohibiendo el uso de colecciones en ViewBag para alimentar formularios.

#### Scenario: Carga de vista de formulario con ViewModel tipado
- **WHEN** un administrador accede a la creación o edición de un artículo o categoría
- **THEN** el controlador construye un ViewModel que encapsula los datos del recurso junto a las opciones de selección necesarias y lo entrega a la vista tipada

#### Scenario: Procesamiento de envío de formulario mediante ViewModel
- **WHEN** el formulario es enviado por el usuario
- **THEN** el controlador recibe el ViewModel, valida el estado de la presentación, mapea los datos hacia el DTO correspondiente para la capa de aplicación y delega la ejecución al servicio

### Requirement: Generación de enlaces internos mediante Tag Helpers
Todas las vistas Razor del sistema MUST generar sus enlaces internos hacia controladores y acciones mediante Tag Helpers de ASP.NET Core, prohibiendo el uso de URLs relativas hardcodeadas en texto plano en atributos de navegación.

#### Scenario: Navegación administrativa y operaciones de entidad mediante Tag Helpers
- **WHEN** se renderiza cualquier enlace o botón de acción hacia módulos del área administrativa
- **THEN** el enlace se genera utilizando los atributos asp-area, asp-controller, asp-action y asp-route correspondientes según la tabla de enrutamiento activa

#### Scenario: Navegación del portal público y enlaces de lectura mediante Tag Helpers
- **WHEN** se renderiza la barra de navegación, el pie de página o las tarjetas de artículos en el portal público
- **THEN** las URLs resultantes son calculadas dinámicamente por los Tag Helpers respetando el contexto y enrutamiento del sistema
