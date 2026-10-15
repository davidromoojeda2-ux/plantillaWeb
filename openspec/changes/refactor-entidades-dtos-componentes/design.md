# Design

## Context

El proyecto `paginaWeb` está compuesto por una arquitectura en capas (`Domine`, `Aplication`, `Infrastructure`, `paginaWeb`). Con el crecimiento de funcionalidades (como la reciente incorporación de la ficha de autor extendida, barra de compartir y artículos recomendados), varios archivos acumulan múltiples clases y las vistas principales se han vuelto extensas y monolíticas.

Específicamente:
- En `Domine/Entities/`: `Comment.cs` incluye `Comment`, `Visitor` y `CommentStatus`; `Article.cs` incluye `Article` y `ArticleStatus`; `ImageMetadata.cs` incluye `ImageMetadata` e `ImageVariant`.
- En `Aplication/DTOs/`: Cada archivo agrupa el DTO de entrada con el DTO de respuesta y modelos auxiliares (`ArticleDto.cs`, `AuthDto.cs`, `CategoryDto.cs`, `CommentDto.cs`, `ImageDto.cs`, `PageDto.cs`).
- En `paginaWeb/Views/Home/Articulo.cshtml`: Supera las 600 líneas, conteniendo en un solo archivo la barra social flotante y fija, la ficha de autor, el bloque de comentarios, el formulario con honeypot y la grilla de recomendados.

## Goals / Non-Goals

**Goals:**
- **Regla de 1 Tipo por Archivo**: Cada entidad de dominio, sub-entidad y enum tendrá su archivo `.cs` individual en `Domine/Entities/`.
- **DTOs Desacoplados**: Cada clase de DTO (entrada, respuesta, variante o auxiliar) residirá en su propio archivo `.cs` en `Aplication/DTOs/`.
- **Mapeadores Especializados**: Garantizar un mapeador estático independiente por agregado/concepto en `Aplication/Mappers/`, incorporando `AuthorMapper.cs`.
- **Componentes de Vista Modulares**: Extraer bloques de interfaz de `Articulo.cshtml` e `Index.cshtml` a vistas parciales dedicadas en `paginaWeb/Views/Shared/Partials/`:
  - `_SocialShareBar.cshtml`: Botones de compartir (X, Facebook, LinkedIn, WhatsApp) y copiado de enlace con toast.
  - `_AuthorBioCard.cshtml`: Ficha visual del autor (avatar, nombre, rol, biografía y redes).
  - `_CommentsSection.cshtml`: Listado de comentarios aprobados y estado vacío.
  - `_CommentForm.cshtml`: Formulario de envío con validación y honeypot antispam.
  - `_RecommendedArticles.cshtml`: Grilla de artículos recomendados.
  - `_ArticleCard.cshtml`: Tarjeta reutilizable de artículo para listados, home y recomendados.
- **Compilación Limpia y Compatibilidad**: Cero advertencias ni errores en la solución completa y 100% de compatibilidad con MongoDB y endpoints existentes.

**Non-Goals:**
- Modificar el esquema de colecciones o documentos en MongoDB (los atributos BSON y tipos subyacentes se conservan idénticos).
- Reemplazar los mapeadores manuales por librerías pesadas como AutoMapper; se mantienen mapeadores estáticos optimizados.
- Reescribir la lógica de controladores del panel administrativo.

## Decisions

### Decisión 1: Preservación de Namespaces Cohesivos
- **Elección**: Mantener `Domine.Entities`, `Aplication.DTOs` y `Aplication.Mappers` como los namespaces de todos los archivos separados.
- **Razón**: Evita romper las directivas `using` en controladores, repositorios, servicios y vistas (`_ViewImports.cshtml`).
- **Alternativas consideradas**: Crear sub-namespaces (`Aplication.DTOs.Articles`, `Domine.Entities.Comments`), pero agregaría verbosidad innecesaria y rompería decenas de llamadas existentes sin beneficio arquitectónico.

### Decisión 2: Modelado Tipado de Autor (`AuthorDto` y `AuthorMapper`)
- **Elección**: Definir `AuthorDto.cs` en `Aplication/DTOs/` y `AuthorMapper.cs` en `Aplication/Mappers/` para representar el perfil del autor de forma estructurada.
- **Razón**: Permite que el componente `_AuthorBioCard.cshtml` reciba un modelo `AuthorDto` reutilizable (aprovechable en el futuro para páginas de autor o firmas), mientras `ArticleResponseDto` puede seguir exponiendo sus propiedades o mapear hacia `AuthorDto`.
- **Alternativas consideradas**: Pasar `ArticleResponseDto` completo a la tarjeta de autor, lo cual acoplaría innecesariamente el componente de autor a todo el artículo.

### Decisión 3: Vistas Parciales (`Partial Views`) para Componentes de Presentación
- **Elección**: Utilizar Razor Partial Views (`<partial name="Partials/_AuthorBioCard" model="..." />`) en lugar de ViewComponents completos para las secciones del artículo.
- **Razón**: Los datos ya son obtenidos de forma eficiente y en bloque por `HomeController.Articulo` (artículo, comentarios, recomendados, imágenes WebP). Las vistas parciales renderizan sin sobrecarga de ciclo de vida ni consultas adicionales a base de datos.
- **Alternativas consideradas**: ViewComponents completos; sin embargo, requerirían inyección de servicios y consultas asíncronas separadas en cada renderizado, provocando sobrecarga innecesaria para datos que el controlador ya preparó.

### Decisión 4: Ubicación de las Vistas Parciales en `Views/Shared/Partials/`
- **Elección**: Centralizar los componentes compartidos en `paginaWeb/Views/Shared/Partials/`.
- **Razón**: Permite que tanto `Articulo.cshtml`, `Index.cshtml`, `Categoria.cshtml` y `Buscar.cshtml` puedan invocar `<partial name="Partials/_ArticleCard" ... />` o `<partial name="Partials/_SocialShareBar" ... />` de forma transparente.

## Risks / Trade-offs

- **Riesgo**: Bloqueo de archivos por procesos `dotnet watch` en Windows durante la recompilación tras mover o crear clases.
  - **Mitigación**: Crear los archivos de forma limpia, validar la compilación por proyectos individuales si es necesario, y asegurar que ningún archivo quede duplicado.
- **Riesgo**: Discrepancias de modelo en las vistas parciales.
  - **Mitigación**: Especificar `@model` fuertemente tipado en cada vista parcial (`AuthorDto`, `List<CommentResponseDto>`, etc.) y validar el paso de parámetros en tiempo de compilación.
