# Spec Delta: article-reading-experience

## Purpose
Provee capacidades enriquecidas para la lectura de artículos en el portal público, incluyendo distribución en redes sociales, elementos tipográficos editoriales estructurados, información biográfica del redactor y sugerencias de contenido relacionado.

## ADDED Requirements

### Requirement: Barra interactiva para compartir en redes sociales
El sistema SHALL mostrar una barra interactiva de botones para compartir el artículo inmediatamente debajo de la imagen de portada, ofreciendo acceso directo a redes sociales y copia al portapapeles.

#### Scenario: Visualización de botones de compartir
- **WHEN** un usuario accede a la vista de detalle de un artículo publicado
- **THEN** el sistema despliega una barra estilizada debajo de la portada con botones para Twitter/X, Facebook, LinkedIn, WhatsApp y un botón para copiar el enlace directo

#### Scenario: Apertura de ventana para compartir
- **WHEN** el usuario hace clic en el botón de una red social (Twitter/X, Facebook, LinkedIn o WhatsApp)
- **THEN** el sistema abre el diálogo oficial de la red social correspondiente con la URL del artículo y su título precargados

#### Scenario: Copia de enlace al portapapeles
- **WHEN** el usuario hace clic en el botón de copiar enlace
- **THEN** el sistema copia la URL canónica actual al portapapeles del dispositivo y muestra un mensaje de confirmación visual temporal tipo toast

### Requirement: Bloques visuales enriquecidos para citas y llamadas de atención
El sistema SHALL estilizar dentro del cuerpo del artículo (`#articleBody`) elementos de citas destacadas (`blockquote`) y contenedores de llamadas de atención (`callout`/`alert`) con variantes semánticas legibles y diferenciadas.

#### Scenario: Renderizado de citas destacadas con atribución
- **WHEN** el cuerpo del artículo contiene un elemento `blockquote` (con o sin etiqueta `cite`)
- **THEN** el sistema lo renderiza con tipografía serif prominente, comillas o borde lateral distintivo en color de acento y tipografía cursiva estilizada

#### Scenario: Renderizado de bloques de llamada de atención (callouts)
- **WHEN** el cuerpo del artículo incluye contenedores con clases o atributos de aviso (`info`, `warning`, `tip`, `success`)
- **THEN** el sistema aplica fondo coloreado atenuado, borde izquierdo acentuado, ícono temático SVG y tipografía sans de alto contraste

### Requirement: Ficha extendida del autor al pie del artículo
El sistema SHALL desplegar al final del cuerpo del artículo y antes de los comentarios una sección dedicada a la información y trayectoria del autor o redactor.

#### Scenario: Visualización completa de perfil de autor
- **WHEN** el artículo cuenta con información de autor, biografía y enlaces sociales
- **THEN** el sistema muestra una tarjeta destacada con el avatar o iniciales del autor, su nombre completo, biografía/descripción breve, rol editorial y accesos directos a redes o contacto

#### Scenario: Autor con datos básicos sin biografía
- **WHEN** el artículo solo tiene registrado el nombre del autor sin biografía extendida
- **THEN** el sistema renderiza la tarjeta de autor con sus iniciales, nombre y rol predeterminado de redacción, sin mostrar bloques vacíos o rotos

### Requirement: Sección de artículos recomendados y afines
El sistema SHALL consultar y desplegar una grilla de hasta 3 artículos recomendados al pie de la página, priorizando aquellos pertenecientes a la misma categoría y excluyendo el artículo que se encuentra en lectura.

#### Scenario: Consulta de artículos recomendados por categoría
- **WHEN** un usuario llega a la parte inferior de un artículo que pertenece a una categoría con otras publicaciones activas
- **THEN** el sistema muestra hasta 3 tarjetas con imagen de portada, título, tiempo de lectura y enlace hacia dichos artículos afines

#### Scenario: Fallback cuando la categoría tiene menos de tres artículos adicionales
- **WHEN** la categoría del artículo no cuenta con 3 artículos adicionales publicados
- **THEN** el sistema complementa la lista de recomendaciones con los artículos publicados más recientes del portal para completar la grilla

#### Scenario: Exclusión del artículo actual
- **WHEN** se generan los artículos recomendados
- **THEN** el artículo actualmente visualizado nunca es incluido en las recomendaciones
