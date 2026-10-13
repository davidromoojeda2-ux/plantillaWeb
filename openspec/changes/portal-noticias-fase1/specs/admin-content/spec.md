# Spec Delta: admin-content

## Purpose
Permite a los administradores autenticados gestionar de forma integral el ciclo de vida de artículos, categorías y la moderación de comentarios desde el panel de control.

## ADDED Requirements

### Requirement: Gestión del ciclo de vida de artículos
El sistema SHALL proveer operaciones de creación, edición, listado paginado con filtros y eliminación para los artículos de noticias.

#### Scenario: Publicación y cambio de estado de artículo
- **WHEN** un administrador guarda un artículo marcándolo como publicado
- **THEN** el sistema registra la fecha de publicación y hace accesible el artículo en el portal público

#### Scenario: Guardado en borrador
- **WHEN** un administrador guarda un artículo con estado borrador
- **THEN** el sistema persiste los cambios sin exponer el artículo a los visitantes públicos

### Requirement: Generación automática y edición de slug
El sistema SHALL generar automáticamente un slug amigable y único a partir del título del artículo, permitiendo su modificación manual.

#### Scenario: Generación de slug no duplicado
- **WHEN** se crea un artículo con un título que generaría un slug ya existente
- **THEN** el sistema añade un sufijo diferenciador único al slug antes de persistir

### Requirement: Gestión de categorías y subcategorías
El sistema SHALL permitir la creación, ordenamiento, edición y eliminación de categorías y subcategorías que estructuran el menú de navegación.

#### Scenario: Modificación del orden o visibilidad
- **WHEN** un administrador actualiza el orden o la visibilidad de una categoría
- **THEN** el sistema persiste los cambios e invalida la caché en memoria del menú de navegación

### Requirement: Moderación administrativa de comentarios
El sistema SHALL permitir a los administradores listar los comentarios pendientes y aplicar acciones de aprobación, rechazo o eliminación.

#### Scenario: Aprobación de comentario
- **WHEN** el administrador pulsa la acción de aprobar sobre un comentario pendiente
- **THEN** el sistema cambia su estado a aprobado permitiendo su despliegue público en el artículo

### Requirement: Edición de contenido por bloques estilo Gutenberg
El sistema SHALL proveer un editor de contenido basado en bloques en el panel de administración que emule la experiencia de redacción de Gutenberg (Block Editor).

#### Scenario: Barra flotante contextual al seleccionar texto
- **WHEN** el redactor selecciona un fragmento de texto dentro del área de edición
- **THEN** el editor despliega una barra flotante (Bubble menu) con opciones de formato para negrita, cursiva, enlaces y encabezados

#### Scenario: Menú de inserción de bloques mediante comando slash
- **WHEN** el usuario escribe una barra inclinada `/` en una línea vacía o al inicio de un bloque
- **THEN** se despliega el menú contextual de inserción (block inserter) permitiendo seleccionar o transformar el bloque

#### Scenario: Reordenamiento visual mediante agarrador de bloques
- **WHEN** el usuario interactúa con el agarrador de seis puntos (⋮⋮) ubicado al margen de un bloque
- **THEN** el sistema permite arrastrar y reubicar el bloque visualmente en cualquier posición vertical del artículo

#### Scenario: Transformación en tiempo real mediante atajos Markdown
- **WHEN** el usuario escribe sintaxis Markdown al inicio de línea como `#`, `-` o `>`
- **THEN** el editor transforma automáticamente el bloque al elemento correspondiente sin interrumpir la escritura
