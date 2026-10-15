# Proposal: Mejora Integral del Cuerpo del Artículo

## Why
La vista de lectura actual de artículos (`/articulos/{slug}`) presenta el contenido en texto continuo sin elementos interactivos ni componentes que faciliten la viralización, la lectura guiada o el descubrimiento de nuevos contenidos. Para mejorar la retención de usuarios, enriquecer la jerarquía visual del contenido editorial y permitir compartir noticias rápidamente, se requiere incorporar botones de interacción social, bloques enriquecidos (citas y llamadas de atención), ficha de autor extendida y un bloque de artículos recomendados.

## What Changes
- **Botones para compartir en redes sociales**: Barra interactiva ubicada inmediatamente debajo de la imagen de portada con botones para Twitter/X, Facebook, LinkedIn, WhatsApp y opción de copiar enlace al portapapeles con confirmación visual tipo toast.
- **Bloques enriquecidos para el cuerpo del artículo**:
  - **Bloque de Citas Destacadas (Blockquote)**: Estilo editorial con tipografía tipográfica serif, comillas decorativas, barra lateral de acento y atribución opcional del autor de la cita.
  - **Llamadas de Atención (Callouts / Alerts)**: Bloques estilizados para destacar información clave en el cuerpo del texto con variantes semánticas (Información/Azul, Consejo/Ámbar, Advertencia/Rojo, Éxito/Verde), acompañados de íconos SVG y bordes sutiles.
- **Ficha Extendida del Autor**: Sección visualmente destacada al pie del artículo con avatar, nombre del autor, biografía/descripción breve, rol editorial y enlaces a perfiles sociales o correo de contacto.
- **Sección de Artículos Recomendados**: Grilla responsiva de hasta 3 artículos recomendados o relacionados (provenientes de la misma categoría o publicaciones destacadas recientes, excluyendo la noticia en curso) con imagen de portada, título, tiempo de lectura y enlace directo.
- **Extensión del Modelo y DTOs**: Actualización de la entidad `Article` y los DTOs para soportar información biográfica del autor (o vincular datos del redactor/usuario) sin romper compatibilidad con artículos existentes.

## Capabilities

### New Capabilities
- `article-reading-experience`: Experiencia integral de lectura enriquecida que comprende la botonera para compartir en redes sociales, el renderizado de bloques de citas y llamadas de atención en el cuerpo, la tarjeta de perfil extendido del autor y el carrusel/grilla de recomendaciones de artículos afines.

### Modified Capabilities
*(Ninguna; las especificaciones de Fase 1 residen en el cambio previo y la presente propuesta introduce una capacidad cohesiva específica para la experiencia de lectura avanzada).*

## Impact
- **Domine (`Article.cs`)**: Adición de campos opcionales para autor extendido (biografía del autor `AuthorBio`, avatar del autor `AuthorAvatarUrl`, enlaces sociales del autor `AuthorSocialUrl`) preservando compatibilidad.
- **Aplication (`ArticleDto.cs`, `IArticleService`, `ArticleService`)**: Inclusión de los nuevos campos en `ArticleDto` y `ArticleResponseDto`; adición de método `GetRecommendedAsync(string articleId, string? categoryId, int take = 3)` en `IArticleService`.
- **Infrastructure (`MongoDbContext`, Repositorios)**: Consulta optimizada para obtener artículos recomendados filtrados por categoría y estado publicado, excluyendo el ID del artículo activo.
- **paginaWeb (Vistas y Controladores)**:
  - Actualización de `HomeController.Articulo(string slug)` para suministrar la lista de recomendados.
  - Actualización de `Views/Home/Articulo.cshtml`: incorporación de la barra social debajo de la portada, estilos CSS para callouts y blockquotes en el cuerpo `.prose`, renderizado de la tarjeta de autor y sección de recomendados.
  - Scripts en `Articulo.cshtml` para copia en portapapeles y ventanas emergentes de compartir.
