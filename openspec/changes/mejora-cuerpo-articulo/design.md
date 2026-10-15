# Design: Mejora Integral del Cuerpo del Artículo

## Context
Ver `proposal.md` y `specs/article-reading-experience/spec.md`.
La vista pública de artículos (`Views/Home/Articulo.cshtml`) actualmente renderiza la portada, el contenido HTML crudo dentro de un contenedor `.prose` y la sección de comentarios. La solución está construida sobre .NET 10 con arquitectura limpia (`Domine`, `Aplication`, `Infrastructure`, `paginaWeb`), persistencia en MongoDB y estilos mediante Tailwind CSS v4.

## Goals / Non-Goals

**Goals:**
- Proveer una barra de interacción social con botones nativos para compartir en Twitter/X, Facebook, LinkedIn, WhatsApp y copiado rápido de URL con feedback visual.
- Diseñar y estilar bloques tipográficos enriquecidos para citas destacadas (`blockquote`) y llamadas de atención (`.callout` / variantes `info`, `tip`, `warning`, `success`) dentro de `#articleBody`.
- Extender el modelo de datos de artículo para soportar información biográfica de autor (`AuthorBio`, `AuthorAvatarUrl`, `AuthorRole`, `AuthorSocialUrl`) con compatibilidad retroactiva hacia documentos existentes de MongoDB.
- Crear el componente visual de ficha de autor extendida al término del artículo con fallbacks limpios.
- Implementar en `IArticleService` y `IArticleRepository` la consulta optimizada de hasta 3 artículos recomendados afines a la categoría (con fallback a recientes y exclusión del artículo en lectura), mostrándolos en una grilla responsiva antes de la sección de comentarios.

**Non-Goals:**
- No se creará una colección independiente de autores en MongoDB en esta etapa; los datos se encapsulan en el artículo o se heredan de los datos del redactor para evitar migraciones complejas de esquemas.
- No se implementa tracking analítico de clics sociales hacia plataformas de terceros (Google Analytics o Meta Pixel), únicamente los intents web estándar de compartición.
- No se altera la lógica de moderación de comentarios existente.

## Decisions

### 1. Ubicación y Mecánica de la Botonera Social
- **Decisión**: Colocar la barra de compartir justo debajo del contenedor de la imagen de portada y antes del layout de dos columnas, en una barra horizontal elegante con fondo blanco/slate-50 y sombra suave.
- **Razón**: Es el punto focal donde el usuario asimila el impacto inicial de la noticia antes de comenzar la lectura profunda, y permanece accesible sin obstruir la columna de lectura ni competir con el índice interactivo lateral.
- **Alternativa descartada**: Barra flotante lateral pegada a la pantalla (interfiere con el índice interactivo adhesivo en resoluciones intermedias).
- **Mecanismo**: Uso de Web Intents universales (`https://twitter.com/intent/tweet?url=...`, `https://www.facebook.com/sharer/sharer.php?u=...`, `https://www.linkedin.com/sharing/share-offsite/?url=...`, `https://api.whatsapp.com/send?text=...`) y la API `navigator.clipboard.writeText` con popup toast temporal.

### 2. Estilos para Citas y Llamadas de Atención en el Cuerpo del Artículo
- **Decisión**: Definir clases CSS de utilidad y componentes en `Styles/tailwind.css` (con fallback de estilos directos o scoped) que actúen sobre el contenedor `#articleBody`:
  - `blockquote`: Borde izquierdo grueso en azul corporativo (`#00296B`) o acento (`#FDC500`), fondo `slate-50`, tipografía serif en cursiva, comillas decorativas y estilos para `<cite>`.
  - `.callout` o `[data-callout]`: Contenedores con borde izquierdo de 4px, esquinas redondeadas (`rounded-r-2xl`), fondo suave acorde a la categoría (`callout-info`, `callout-tip`, `callout-warning`, `callout-success`) e ícono SVG ilustrativo correspondiente.
- **Razón**: Permite que cualquier HTML generado por editores WYSIWYG/Markdown o redactores se renderice inmediatamente con alta calidad estética sin requerir plugins pesados.

### 3. Extensión de Datos del Autor en Dominio y DTOs
- **Decisión**: Añadir propiedades opcionales a `Domine.Entities.Article`:
  ```csharp
  public string? AuthorBio { get; set; }
  public string? AuthorAvatarUrl { get; set; }
  public string? AuthorRole { get; set; } = "Redacción Especial";
  public string? AuthorSocialUrl { get; set; }
  ```
- **Razón**: Al usar MongoDB con atributos `[BsonIgnoreExtraElements]`, la adición de campos con valores por defecto o nulos no genera errores al deserializar documentos antiguos.
- **Mappers & DTOs**: Actualizar `ArticleDto`, `ArticleResponseDto` y `ArticleMapper` para propagar estos atributos bidireccionalmente.

### 4. Algoritmo de Recomendación de Artículos
- **Decisión**: Implementar `GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3)`:
  1. Si `categoryId` está presente, buscar hasta `take` artículos con `Status == Publicado`, `CategoryId == categoryId` y `Id != currentArticleId` ordenados por `PublishedAt` descendente.
  2. Si la consulta retorna menos de `take` artículos (e.g. 1 o 2), completar con artículos publicados recientes del portal que no sean el artículo actual ni estén ya en la lista de recomendados.
  3. Cargar las variantes WebP de las portadas de los artículos recomendados mediante `ImageService.GetVariantsDictAsync` para mantener el estándar de rendimiento.
- **Razón**: Garantiza que la sección siempre esté poblada con 3 sugerencias relevantes, evitando espacios vacíos en categorías nuevas.

## Risks / Trade-offs

- **[Riesgo] Portapapeles bloqueado en contextos no HTTPS o navegadores restrictivos**  
  → **Mitigación**: Implementar fallback en JavaScript con `document.execCommand('copy')` o selección manual con notificación al usuario si `navigator.clipboard` no está disponible.
- **[Riesgo] Artículos legados en base de datos sin datos de autor extendidos**  
  → **Mitigación**: En la vista Razor, si `AuthorBio` es nulo o vacío, mostrar una tarjeta simplificada y pulcra con iniciales y descripción por defecto ("Periodismo de investigación y análisis editorial"), manteniendo una estética intachable.
- **[Riesgo] Consulta adicional a MongoDB en la vista de detalle**  
  → **Mitigación**: La consulta de recomendados se limita estrictamente a `take = 3` con proyección y se aprovecha el índice compuesto existente sobre `{ status: 1, categoryId: 1, publishedAt: -1 }`.

## Migration Plan
1. Modificar las clases de Dominio (`Article.cs`) y DTOs (`ArticleDto.cs`).
2. Actualizar `ArticleRepository` e `IArticleRepository` para soportar la consulta de recomendados.
3. Actualizar `ArticleService` e `IArticleService`.
4. Extender `HomeController.Articulo` para consultar recomendados e imágenes afines.
5. Diseñar los componentes en `Articulo.cshtml` y los estilos en `Styles/tailwind.css`.
6. Compilar CSS y validar compilación limpia de la solución (.NET 10).
