# Contenido enriquecido (datos estructurados Schema.org)

Esta funcionalidad añade **datos estructurados JSON-LD** al portal para que Google los valide con el
[Rich Results Test](https://search.google.com/test/rich-results) y muestre *resultados enriquecidos*
(fragmentos con imagen, autor, fecha, migas de pan, etc.), además de habilitar superficies como
Google News, Top Stories y Discover.

## Objetivo y ventajas

- Mayor **CTR** orgánico: los resultados con imagen, autor y fecha destacan visualmente.
- Elegibilidad para **Top Stories / Google News** mediante `NewsArticle`.
- **Migas de pan** (`BreadcrumbList`) en los resultados.
- **Caja de búsqueda** en el sitio (`WebSite + SearchAction`).
- Mayor rastreabilidad de imágenes (`ImageObject`).
- (Opcional) **FAQ desplegables** en la SERP si se incorporan bloques de preguntas.

## Estado actual

El portal ya cuenta con una base SEO sólida:

- Sitemaps (páginas, blog, categorías) con extensión de imágenes en `SeoController`.
- `meta description`, Open Graph y Twitter Cards en `_Layout.cshtml`.
- Migas de pan **visuales** en artículos y listados.

Falta únicamente la capa de **datos estructurados JSON-LD**, que se valida con el Rich Results Test.

## Tipos de datos estructurados recomendados

| Tipo | Dónde | Elegibilidad / ventaja | Prioridad |
|---|---|---|---|
| `NewsArticle` | `/articulos/{slug}` | Top Stories, Google News, Discover | Alta |
| `BreadcrumbList` | Artículo, categoría, info, listados | Migas en SERP | Alta |
| `Organization` + `WebSite` (+ `SearchAction`) | Global (`_Layout`) | Logotipo, caja de búsqueda | Media |
| `Person` (autor) | Dentro de `NewsArticle` | Ficha de autor | Alta |
| `ImageObject` | Dentro de `NewsArticle` | Miniaturas / portada | Alta |
| `ItemList` / `CollectionPage` | `/publicaciones`, `/categoria/{slug}` | Listados estructurados | Media |
| `AboutPage` / `WebPage` | `/info/{slug}` | Contexto institucional | Baja |
| `FAQPage` / `HowTo` | Solo si se añaden bloques FAQ/HowTo | Desplegables SERP | Opcional |

## Diseño técnico

Se sigue el mismo patrón que `SitemapDocumentBuilder`, manteniendo los builders **puros y testables**
(reciben DTOs y `baseUrl`, sin depender de `HttpContext`):

- `paginaWeb/Seo/StructuredDataBuilder.cs` — builders que devuelven JSON:
  - `BuildOrganization(siteSettings, baseUrl)`
  - `BuildWebSite(baseUrl)`
  - `BuildNewsArticle(article, coverImage, category, baseUrl)`
  - `BuildBreadcrumbList(items)`
  - `BuildItemList(...)`
- `paginaWeb/Seo/JsonLd.cs` — utilidad de serialización que **escapa `</` → `<\/`** y devuelve `HtmlString`.
- `paginaWeb/Views/Shared/_Layout.cshtml` — añadir en el `<head>`:

  ```razor
  @await RenderSectionAsync("StructuredData", required: false)
  ```

  y el `Organization` + `WebSite` global (ya se inyecta `ISiteSettingsService`).

- Cada vista (`Articulo.cshtml`, `Categoria`, `Info`, `Publicaciones/Index`) define
  `@section StructuredData { ... }` con su JSON-LD.

Reutilización de infraestructura existente:

- URL absoluta: `SitemapDocumentBuilder.ToAbsoluteUrl` (o helper equivalente).
- Imagen 1600 px: `ImageMapper.GetBestUrl(...)` + `ImageMetadata.OriginalWidth/Height`.
- Fechas ISO-8601 con offset: mismo criterio que `SitemapDocumentBuilder.FormatLastModified`.
- Serialización: `System.Text.Json` (ya en uso en el proyecto).

## Plan de implementación por fases

### Fase 0 — Base y site-wide

1. Crear `JsonLd.cs` y `StructuredDataBuilder.cs` en `paginaWeb/Seo`.
2. En `_Layout.cshtml`, inyectar `Organization` (con `logo`, `sameAs` desde `SocialProfiles`) y
   `WebSite` (con `potentialAction/SearchAction` apuntando a `/publicaciones?q={search_term_string}`).
3. Tests unitarios de serialización y escape (`StructuredData.Tests`).

### Fase 1 — Artículos (`NewsArticle`)

4. En `Articulo.cshtml`, emitir `NewsArticle` con:
   - `headline`, `description`, `image` (variante 1600 + `width`/`height`).
   - `datePublished`, `dateModified`.
   - `author` (`Person` con `name` y `url` si existe `AuthorSocialUrl`).
   - `publisher` (`Organization`), `mainEntityOfPage`.
   - `articleSection` (categoría), `keywords` (tags).
   - `timeRequired` (`PT{ReadingTimeMinutes}M`).
   - `BreadcrumbList`: Inicio → Categoría → (Subcategoría) → Título.
5. Fallbacks: autor vacío → `Organization`; fecha nula → `CreatedAt`.

### Fase 2 — Listados

6. `Category/Categoria.cshtml` y `Publicaciones/Index.cshtml`: `BreadcrumbList` +
   `CollectionPage`/`ItemList` con los `ArticleCardViewModel`.

### Fase 3 — Páginas informativas

7. `Home/Info.cshtml`: `BreadcrumbList` + `AboutPage`/`WebPage`.

### Fase 4 — Opcional: bloques FAQ/HowTo

8. Solo si se desean desplegables FAQ en la SERP: nuevo `ContentFaqBlock` en `Domine`, DTO,
   sanitizador, parcial de render y `FAQPage` JSON-LD. No se recomienda como primer paso.

### Fase 5 — Verificación y monitorización

9. Validar con el [Rich Results Test](https://search.google.com/test/rich-results) y el
   [Schema.org Validator](https://validator.schema.org).
10. **Google Search Console → Informes → Resultados enriquecidos** (cobertura de `Article` y `BreadcrumbList`).
11. Reenviar el sitemap y, si se busca Top Stories, registrar el portal en Google News Publisher Center.
12. KPIs: CTR orgánico, impresiones, posiciones y número de resultados enriquecidos.

## Requisitos de elegibilidad (Google)

- El JSON-LD debe **coincidir con el contenido visible** de la página.
- Fechas en formato ISO-8601 **con zona horaria** (`+00:00`).
- Imagen de artículo de **al menos 696 px** de ancho (usar la variante 1600).
- `author` debe ser un `Person` u `Organization` válido.
- `publisher` debe incluir `logo` (imagen válida).

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| JSON-LD no coincide con el contenido visible | Generar siempre desde los mismos DTOs/entidades que pinta la vista |
| Fechas sin zona horaria | ISO-8601 con offset, como en el sitemap |
| Imagen < 696 px | Usar siempre la variante 1600 |
| Inyección / rotura de `<script>` | Escapar `</` → `<\/` en el helper `JsonLd` |
| `author`/`publisher` inválidos | Fallback a `Organization`; validar el logo |
| Sobre-marcado | Priorizar `NewsArticle` + `BreadcrumbList` + `Organization`/`WebSite` |

## Impacto en rendimiento

El JSON-LD se emite **inline en el HTML** y el navegador **no lo ejecuta** (es un *data block*), por lo
que no afecta a TBT, INP ni FCP, y no añade peticiones de red. El coste real es únicamente el tamaño
del documento y, en un caso concreto, el orden dentro del `<head>`.

### Lo que no afecta

| Preocupación | Realidad |
|---|---|
| Bloqueo del renderizado | `<script type="application/ld+json">` no se ejecuta ni bloquea el parser. |
| TBT / INP / FCP | Sin ejecución JS, sin tareas largas, sin impacto en interactividad. |
| Peticiones de red | Inline: 0 requests extra (mejor que servirlo en un `.json` aparte). |
| Penalización por JSON inválido | Se ignora; solo el marcado engañoso/spam puede acarrear acciones manuales. |
| Ranking | No es factor de ranking directo; mejora CTR y visibilidad en la SERP. |

### Riesgos reales y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| **LCP** por orden en el `<head>` | El `@section Preloads` de `_Layout.cshtml` preloadea la portada (elemento LCP). Si el JSON-LD va delante, retrasa el descubrimiento. | Emitir la sección **al final del `<head>`** (tras `@section Styles`) o al **final del `<body>`**. Google acepta ambos. |
| Tamaño del HTML / TTFB | Bytes extra en el documento. | Presupuesto ≤ ~3 KB sin comprimir por página; `WriteIndented = false`. |
| `articleBody` completo | Infla el HTML con el cuerpo entero. | No incluir `articleBody`; usar `headline`/`description`. |
| `ItemList` con artículos completos | Duplica 9 `NewsArticle` en los listados. | Usar `ListItem` con `url` (+ `name`/`image` como máximo). |
| Variantes de imagen redundantes | Descargas de crawler innecesarias. | Referenciar la **misma URL** (1600) que el `<img>` visible. |
| Coste de generación | CPU por request. | Construir desde los DTOs ya cargados; `SiteSettingsService` ya está cacheado en `IMemoryCache` (2 min). |
| Duplicación de entidades | JSON inflado y redundante. | Usar `@id` para `Organization`/`WebSite` y no repetirlos por página. |

### Criterios de implementación

1. **Ubicación:** final del `<head>` (después del CSS) o final del `<body>`; nunca antes de `Preloads`.
2. **Presupuesto:** ≤ ~3 KB por página sin comprimir (`NewsArticle` + `BreadcrumbList` ≈ 1,5–3 KB).
3. **Compresión:** ya activa en `Program.cs` (`AddResponseCompression`, `EnableForHttps = true`); el JSON
   comprime muy bien, por lo que el coste real en gzip/brotli es de unos cientos de bytes.
4. **Sin JS de cliente:** generarlo siempre server-side en Razor.
5. **Sin queries extra:** reutilizar los DTOs y la caché de `SiteSettingsService`.
6. **Medición:** comparar Lighthouse/PageSpeed antes y después (TTFB, tamaño de documento, LCP);
   la diferencia esperada es prácticamente nula.

## Verificación

```bash
# Compilar la solución
dotnet build paginaWeb.slnx

# Ejecutar todas las pruebas
dotnet test paginaWeb.slnx

# Validar una URL pública (una vez desplegado)
# https://search.google.com/test/rich-results
# https://validator.schema.org
```

## Estado de la implementación

Implementado en la rama `feature/contenido-enriquecido` (fases 0 a 3):

| Fase | Estado | Dónde |
| --- | --- | --- |
| 0 - Base site-wide (`Organization` + `WebSite`) | Hecho | `paginaWeb/Seo/*`, `_Layout.cshtml` |
| 1 - Artículos (`NewsArticle` + `BreadcrumbList`) | Hecho | `Views/Home/Articulo.cshtml` |
| 2 - Listados (`CollectionPage` + `ItemList` + migas) | Hecho | `Views/Publicaciones/Index.cshtml`, `Views/Category/Categoria.cshtml` |
| 3 - Informativas (`WebPage` + migas) | Hecho | `Views/Home/Info.cshtml` |
| 4 - FAQ/HowTo | Pendiente | Requiere bloques nuevos de CMS |
| 5 - Verificación en Google | Pendiente | Al desplegar |

Notas de implementación:

- `SiteBrand` expone `LogoPath`, `LogoWidth` y `LogoHeight` para no repetir rutas en las vistas.
- `logo.png` (512×512, ARGB con transparencia) se generó rasterizando `logo.svg` con `sharp-cli`;
  el SVG se mantiene como favicon. `SiteBrand.LogoPath` apunta al PNG porque Google no admite SVG
  en `publisher.logo`.
- El `SearchAction` de `WebSite` apunta a `/publicaciones?q={search_term_string}`, que coincide con la
  acción `Index(string? q, int page)` del `PublicacionesController`.
- En la búsqueda de `/publicaciones` se omite el `ItemList` (resultados efímeros) y solo se emiten migas.

## URL canónica

`paginaWeb/Seo/CanonicalUrl.cs` + `<link rel="canonical">` en `_Layout.cshtml`:

- Por defecto se usa la ruta actual **sin** parámetros de seguimiento (`utm_*`, `ref`, `q`…) para que
  los enlaces compartidos no generen duplicados.
- Se conserva `page` (paginación) y se omite `page=1`, que equivale a la URL base.
- Es *self-referencing*: cada página de listado se canoniza a sí misma, no a la página 1.
- Cualquier vista puede sobrescribirla con `ViewData["CanonicalUrl"]` (absoluta o relativa).
- No se emite en respuestas distintas de 200 (404, etc.).

Las vistas pueden añadir `<meta name="robots">` con `ViewData["Robots"]`; se usa en
`/publicaciones?q=...` (`noindex, follow`, porque los resultados no aportan contenido propio y
`/buscar` ya redirige ahí).

## Rollback

El cambio es aditivo:

- El JSON-LD se limita a `<script type="application/ld+json">` en el `<head>`.
- Para desactivar, basta con retirar la sección `StructuredData` y el bloque global del `_Layout`.
- No altera el modelo de datos ni requiere migración de colecciones.
