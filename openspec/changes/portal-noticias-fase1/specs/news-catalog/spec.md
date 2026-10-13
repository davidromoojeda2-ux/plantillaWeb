# Spec Delta: news-catalog

## Purpose
Gestiona la exploración, búsqueda textual, filtrado paginado y lectura detallada de artículos de noticias publicados, con soporte para imágenes WebP y metadatos SEO.

## ADDED Requirements

### Requirement: Visualización de portada y noticias destacadas
El sistema SHALL presentar en la página principal los artículos marcados como destacados y los artículos publicados más recientes.

#### Scenario: Visualización de la página de inicio
- **WHEN** un visitante accede a la portada del portal
- **THEN** el sistema despliega una sección con artículos destacados y un listado cronológico de las publicaciones más recientes en estado publicado

### Requirement: Lectura detallada del artículo con experiencia de lectura interactiva
El sistema SHALL mostrar la página completa de un artículo en `/articulos/{slug}` conforme al diseño de referencia (![Detalle de artículo](../../assets/frame%20articulo%20con%20barra%20de%20progreso%20en%20la%20parte%20superior%20y%20menu%20interactivo%20en%20el%20margen%20izquierdo.png)), incluyendo barra de progreso superior, tiempo estimado de lectura, índice lateral interactivo, imágenes WebP desde Cloudflare R2 y contador de vistas.

#### Scenario: Acceso a un artículo publicado
- **WHEN** el usuario navega a la URL de un artículo en estado publicado
- **THEN** el sistema presenta el contenido completo con cabecera editorial (breadcrumbs, tiempo de lectura, autor), imágenes WebP responsivas e incrementa el contador de lecturas

#### Scenario: Barra de progreso de lectura durante el scroll
- **WHEN** el usuario se desplaza verticalmente a lo largo del contenido del artículo
- **THEN** una barra visual en el borde superior de la pantalla se llena proporcionalmente al porcentaje de lectura completado

#### Scenario: Navegación mediante índice interactivo lateral
- **WHEN** el artículo cuenta con encabezados de sección y el usuario visualiza la pantalla en escritorio
- **THEN** se muestra una columna lateral adhesiva con el índice de secciones ("En esta página") que resalta el bloque activo y permite saltar al contenido al hacer clic

#### Scenario: Noticia en borrador o inexistente
- **WHEN** un visitante solicita un artículo no publicado o un slug inexistente
- **THEN** el sistema responde con una página de error HTTP 404

### Requirement: Listado y filtrado paginado de noticias
El sistema SHALL permitir listar artículos filtrados por categoría raíz o subcategoría con paginación de 9 elementos por página.

#### Scenario: Paginación en categoría
- **WHEN** un usuario consulta una categoría con más de 9 artículos publicados
- **THEN** el sistema entrega la primera página con 9 artículos y los controles de paginación correspondientes

### Requirement: Búsqueda textual de artículos
El sistema SHALL proveer una funcionalidad de búsqueda por coincidencia de términos en el título, resumen o cuerpo de artículos publicados.

#### Scenario: Búsqueda con resultados
- **WHEN** el usuario realiza una búsqueda con términos coincidentes
- **THEN** el sistema retorna la lista paginada de artículos publicados ordenados por relevancia o fecha

#### Scenario: Búsqueda sin coincidencias
- **WHEN** la búsqueda no arroja coincidencias con artículos publicados
- **THEN** el sistema muestra un mensaje claro indicando que no se encontraron resultados
