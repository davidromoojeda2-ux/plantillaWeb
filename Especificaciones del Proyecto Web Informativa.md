# Especificaciones del Proyecto: Web Informativa (Capa Pública y Administrador)

Documento de especificaciones técnicas y funcionales · Versión 3.0 (.NET 10 + Razor + Tailwind + MongoDB + Cloudflare R2) · 12 de octubre de 2026

## 1. Resumen y alcance

Se construirá una **web informativa** en **.NET 10 con Razor y Tailwind CSS** que muestra **artículos con imágenes** organizados por **categorías y subcategorías**, las cuales alimentan dinámicamente el menú de navegación (navbar). El contenido (artículos, comentarios, visitantes y secciones informativas) se guarda en **MongoDB**, los administradores en **PostgreSQL** y las imágenes, optimizadas a WebP, en **Cloudflare R2** (almacenamiento compatible con S3). Un administrador de contenido con inicio de sesión básico permite gestionar todo el contenido.

**Incluido (Capa Pública):**

- Inicio con artículos destacados y recientes.
- Navbar dinámico (categorías y subcategorías) leído desde MongoDB.
- Listado de artículos por categoría o subcategoría, con paginación.
- Detalle de artículo con imágenes y comentarios de visitantes (moderados).
- Búsqueda por texto.
- Secciones informativas administrables (Nosotros, Política de privacidad, Política de cookies y otras) y aviso de cookies.

**Incluido (Administrador de contenido, prioridad primordial):**

- Inicio de sesión básico de administradores (usuario y contraseña, sin roles ni permisos por ahora).
- CRUD de artículos: crear, listar, editar, publicar y eliminar.
- Gestión de imágenes: subida, conversión a WebP, almacenamiento en Cloudflare R2, texto alternativo y eliminación.
- Gestión de categorías y subcategorías, que alimenta el navbar.
- Moderación de comentarios: aprobar, rechazar y eliminar.
- Edición de las secciones informativas.

**Fuera de alcance (fases posteriores):** autorización por roles y permisos, autenticación avanzada (recuperación de contraseña, doble factor), programación de publicaciones, versionado de artículos y multi-idioma.

## 2. Stack tecnológico

El sitio es una aplicación **ASP.NET Core (.NET 10)** con renderizado en servidor mediante Razor, estilos con Tailwind CSS, persistencia en MongoDB y PostgreSQL e imágenes en Cloudflare R2.

| Capa | Tecnología | Uso en el proyecto |
| --- | --- | --- |
| Runtime y framework | .NET 10 / ASP.NET Core | Servidor web, enrutamiento, inyección de dependencias. |
| Interfaz | Razor Pages (con un Area `Admin`) | Sitio público y administrador con renderizado en servidor, *partials* y *view components*. |
| Estilos | Tailwind CSS (CLI independiente) | Diseño responsivo *mobile-first*, compilado a un único CSS. |
| Base de datos | MongoDB | Artículos, categorías, subcategorías, imágenes (metadatos) y usuarios. |
| Acceso a datos | `MongoDB.Driver` | Repositorios tipados, consultas e índices. |
| Autenticación | ASP.NET Core Identity (cookies) con almacén en MongoDB | Acceso protegido al administrador. |
| Procesamiento de imágenes | SixLabors.ImageSharp | Conversión a WebP, redimensionado y variantes. |
| Almacenamiento de imágenes | Cloudflare R2 (API compatible con S3) con `AWSSDK.S3` | Archivos de imagen servidos desde un dominio público o CDN. |
| Caché | `IMemoryCache` / `OutputCache` | Menú, listados y páginas de artículo. |
| Pruebas | xUnit + Testcontainers (MongoDB) | Pruebas unitarias e integración. |
| Despliegue | Docker | Imagen de la aplicación y contenedor de MongoDB. |

## 3. Arquitectura del sitio web

La aplicación sigue una arquitectura por capas: presentación (Razor), servicios, acceso a datos y MongoDB.

&#91;embedded content: arquitectura por capas · navegador, aplicación y MongoDB\]

Una solicitud pasa de Razor al servicio y de ahí al repositorio; solo la capa de datos habla con MongoDB y con Cloudflare R2.

**Estructura de la solución:**

```text
src/
  WebInformativa.Web/           (proyecto ASP.NET Core con Razor)
    Pages/                      Index, Categoria, Articulo, Buscar, Acerca, Contacto, Error
    Pages/Shared/               _Layout, _Navbar, _ArticleCard, _Pagination
    Areas/Admin/Pages/          Login, Articulos, Categorias, Imagenes
    ViewComponents/             NavbarViewComponent
    Styles/                     input.css (Tailwind)
    wwwroot/                    css compilado, js, favicon
    Program.cs
  WebInformativa.Core/          modelos, DTOs, interfaces y servicios (Menu, Article, Image)
  WebInformativa.Data/          repositorios e índices de MongoDB; IImageStorage con Cloudflare R2
tests/
  WebInformativa.Tests/         xUnit + Testcontainers
```

## 4. Navbar dinámico (categorías y subcategorías)

El navbar no contiene enlaces fijos: se construye en cada página desde la colección `categories` de MongoDB, con dos niveles (categoría → subcategoría).

**Flujo de construcción:**

1. `_Layout` invoca el `NavbarViewComponent`.
2. El componente llama a `MenuService.GetMenuAsync()`.
3. Si el árbol está en `IMemoryCache`, lo devuelve; si no, consulta `categories` (activas, ordenadas por `order`) y arma el árbol.
4. Razor renderiza el `<nav>` con clases Tailwind: cada categoría raíz es un enlace o un desplegable con sus subcategorías.
5. La caché expira por tiempo (valor sugerido: 5 minutos) o al invalidarla manualmente, de modo que un cambio hecho en el administrador de contenido se refleja sin reiniciar el sitio (al guardar una categoría se invalida la caché del menú).

**Reglas del menú:**

- Una categoría raíz tiene `parentId` nulo; una subcategoría referencia a su categoría padre en `parentId`.
- Solo se muestran categorías con `isActive` y `showInMenu` verdaderos, ordenadas por `order`.
- El menú admite dos niveles; una categoría sin subcategorías es un enlace directo.
- URLs: `/categorias/{slug}` para una categoría y `/categorias/{slug-padre}/{slug}` para una subcategoría.
- En móvil el menú se convierte en hamburguesa con acordeón; en escritorio, en desplegables con soporte de teclado (`aria-expanded`, foco visible).
- Si MongoDB no responde, se sirve la última versión en caché en lugar de romper la página.

**Árbol que entrega `MenuService`** (ejemplo ilustrativo):

```json
[
  { "name": "Noticias", "slug": "noticias", "children": [
      { "name": "Locales", "slug": "locales" },
      { "name": "Nacionales", "slug": "nacionales" } ] },
  { "name": "Cultura", "slug": "cultura", "children": [] }
]
```

## 5. Administrador de contenido

El administrador es un área protegida (`/admin`) del mismo proyecto Razor donde los editores crean y publican el contenido que ve la capa pública; se implementa por prioridad, empezando por el CRUD de artículos y la gestión de imágenes.

| ID | Funcionalidad | Prioridad |
| --- | --- | --- |
| AD-01 | Inicio y cierre de sesión de administradores. | Esencial |
| AD-02 | CRUD de artículos: crear, listar con filtros y paginación, editar y eliminar con confirmación. | Esencial |
| AD-03 | Cambiar el estado de un artículo (borrador o publicado), marcarlo como destacado y asignarle categoría y subcategoría. | Esencial |
| AD-04 | Subir imágenes a un artículo (portada y galería) con conversión automática a WebP. | Esencial |
| AD-05 | Biblioteca de imágenes: listar, editar el texto alternativo y eliminar. | Esencial |
| AD-06 | CRUD de categorías y subcategorías con orden y visibilidad en el menú. | Esencial |
| AD-07 | Generación automática del *slug* a partir del título, editable y sin duplicados. | Importante |
| AD-08 | Vista previa del artículo antes de publicarlo. | Importante |
| AD-09 | Invalidar la caché del menú y de los listados al guardar cambios. | Importante |
| AD-10 | Roles (administrador y editor), programación de publicaciones, historial de versiones y papelera. | Deseable |

**Pantallas del administrador:** `/admin/login`, `/admin` (panel), `/admin/articulos`, `/admin/articulos/crear`, `/admin/articulos/editar/{id}`, `/admin/categorias` y `/admin/imagenes`.

**Formulario de artículo:** título, *slug*, resumen, contenido (editor Markdown o de texto enriquecido, a definir), categoría, subcategoría, etiquetas, autor, imagen de portada, galería, estado, destacado y fecha de publicación.

**Reglas de acceso y seguridad:**

- Autenticación con ASP.NET Core Identity (cookie segura) y todas las páginas de `/admin` con `[Authorize]`.
- Protección contra CSRF (*antiforgery*) en todos los formularios y validación del lado servidor.
- Bloqueo temporal tras varios intentos fallidos de inicio de sesión.
- El contenido se sanitiza antes de guardarse y de mostrarse.
- Los artículos guardan quién los creó y los modificó por última vez (`createdBy`, `updatedBy`).

## 6. Gestión de imágenes: WebP y Cloudflare R2

Toda imagen subida desde el administrador se convierte a **WebP** y se guarda en **Cloudflare R2**, el almacenamiento de objetos de Cloudflare con API compatible con S3; MongoDB conserva solo los metadatos.

**Flujo de subida:**

1. El editor sube un archivo JPG, PNG o WebP; se valida el tipo por su contenido (no solo la extensión) y un tamaño máximo (valor sugerido: 10 MB).
2. `ImageService` (SixLabors.ImageSharp) corrige la orientación EXIF, elimina metadatos y redimensiona la imagen.
3. Se generan variantes en WebP de varios anchos (valores sugeridos: 400, 800 y 1600 px) con calidad cercana a 80.
4. Cada variante se sube a un *bucket* de R2 con el cliente `AWSSDK.S3`, con una clave como `images/{id}/{ancho}.webp` y cabecera `Cache-Control` de larga duración.
5. Se guarda en la colección `images` el texto alternativo, las dimensiones y la clave de cada variante.
6. El sitio público entrega las imágenes desde el dominio público de R2 (dominio propio recomendado) con `<img srcset>` y `loading="lazy"`.

**Eliminación y reemplazo:** al borrar o reemplazar una imagen se eliminan sus variantes en R2 y su registro; si está en uso en un artículo publicado, el administrador lo advierte antes de continuar.

**Abstracción de almacenamiento:** `IImageStorage` con una implementación para Cloudflare R2 y otra local para desarrollo, de modo que el proveedor pueda cambiarse sin tocar el resto del código.

**Configuración (variables de entorno o *user secrets*, nunca en el repositorio):**

| Parámetro | Descripción |
| --- | --- |
| `R2:AccountId` | Identificador de la cuenta de Cloudflare; define el *endpoint* `https://<AccountId>.r2.cloudflarestorage.com`. |
| `R2:AccessKeyId` y `R2:SecretAccessKey` | Credenciales de un token de API de R2 con permisos solo sobre el *bucket*. |
| `R2:BucketName` | Nombre del *bucket* de imágenes. |
| `R2:PublicBaseUrl` | URL pública (dominio propio o `r2.dev`) desde donde se sirven las imágenes. |

## 7. Comentarios, visitantes y secciones informativas

Los artículos, los comentarios y los visitantes se guardan en MongoDB, y las secciones informativas (Nosotros, Política de privacidad, Política de cookies y otras) en una colección propia, separada de `articles`.

| Dato | Dónde se guarda | Motivo |
| --- | --- | --- |
| Administradores | PostgreSQL, tabla `admins` | Datos relacionales y credenciales, fuera de la base de contenido. |
| Artículos y categorías | MongoDB, `articles` y `categories` | Contenido flexible con lectura intensiva. |
| Comentarios | MongoDB, `comments` | Se leen junto al artículo y se moderan por estado. |
| Visitantes | MongoDB, `visitors` | Personas que comentan; no tienen cuenta ni contraseña. |
| Secciones informativas | MongoDB, `pages` | Contenido estático editable, independiente de los artículos. |
| Metadatos de imágenes | MongoDB, `images` | Los archivos viven en Cloudflare R2. |

**Comentarios de visitantes**

1. En el detalle de un artículo, el visitante escribe su nombre, correo y comentario.
2. El servicio crea o actualiza su registro en `visitors` y guarda el comentario en `comments` con estado `pendiente`.
3. Un administrador lo aprueba, rechaza o elimina desde `/admin/comentarios`.
4. Solo los comentarios `aprobado` se muestran en el artículo; el correo nunca se publica.

Protecciones básicas: campo trampa (*honeypot*), límite de envíos por IP, longitud máxima, texto plano sin HTML y almacenamiento de la IP solo como *hash*. Cada artículo puede desactivar sus comentarios.

**Visitantes:** un visitante se identifica por su correo. Se guardan solo los datos mínimos (nombre, correo, *hash* de IP, fechas y consentimiento de cookies) y se documentan en la política de privacidad.

**Secciones informativas**

- Cada sección es un documento de `pages` con *slug*, título, contenido, orden y si aparece en el pie de página.
- Se publican en `/info/{slug}` (por ejemplo `/info/nosotros`, `/info/politica-de-privacidad`, `/info/politica-de-cookies`).
- No aparecen en listados, búsquedas ni destacados de artículos.
- El pie de página se construye dinámicamente con las secciones marcadas para mostrarse ahí.
- Se editan desde `/admin/secciones`.
- El aviso de cookies enlaza a la política de cookies.

## 8. Diseño visual: colores y tipografía

El sitio público y el administrador usan una paleta azul y amarilla con neutros claros, y la combinación tipográfica Merriweather + Inter, ambas fuentes gratuitas de Google Fonts.

**Paleta de colores**

| Uso | Hex |
| --- | --- |
| Cabecera, pie de página y titulares | `#00296B` |
| Enlaces y títulos de sección | `#003F88` |
| Elementos secundarios, hover e iconos | `#00509D` |
| Acento principal (botones, CTA, "Último momento") | `#FDC500` |
| Acento claro (etiquetas, resaltados suaves) | `#FFD500` |
| Fondo | `#FFFFFF` |
| Tarjetas y secciones alternas | `#F5F6F8` |
| Texto principal | `#1F2933` |
| Bordes y divisores | `#E2E5EA` |

**Reglas de uso de los colores**

- Texto blanco (`#FFFFFF`) sobre cualquiera de los tres azules: el contraste es muy alto.
- Texto `#00296B` sobre los amarillos, nunca blanco.
- Los dos amarillos son muy parecidos: `#FDC500` para los botones y `#FFD500` solo para detalles menores; no se colocan juntos como si fueran colores distintos.
- Los enlaces dentro del texto van en `#003F88` con subrayado, y en *hover* pasan a `#00509D`.

**Fuentes recomendadas**

| Rol | Opción principal | Alternativa |
| --- | --- | --- |
| Texto de lectura e interfaz | Inter | Source Sans 3, Open Sans |
| Titulares (tono editorial) | Merriweather | Source Serif 4, Lora |
| Titulares (tono moderno) | Inter en peso 700-800 | Montserrat |

**Combinación sugerida:** Merriweather para titulares e Inter para cuerpo, menús, botones y etiquetas. Si se prefiere simplicidad y velocidad de carga, se usa solo Inter en dos o tres pesos.

**Tamaños y variaciones**

| Elemento | Escritorio | Móvil | Peso | Interlineado |
| --- | --- | --- | --- | --- |
| Titular principal (H1) | 40 px | 28 px | 700 | 1.2 |
| Sección (H2) | 30 px | 24 px | 700 | 1.3 |
| Subtítulo (H3) | 22 px | 20 px | 600 | 1.35 |
| Cuerpo de artículo | 18 px | 17 px | 400 | 1.65 |
| Cuerpo en tarjetas y listados | 16 px | 16 px | 400 | 1.5 |
| Bajada o entradilla | 20 px | 18 px | 400 | 1.5 |
| Menú y botones | 16 px | 16 px | 600 | 1 |
| Fecha, autor, etiquetas | 14 px | 14 px | 500 | 1.4 |
| Pie de foto y notas | 13 px | 13 px | 400 | 1.4 |

**Reglas de legibilidad**

- Nunca bajar de 16 px en el texto principal; en móvil evita que el navegador haga zoom automático.
- Ancho de línea de 60 a 75 caracteres (aproximadamente 680 px como máximo).
- Interlineado de 1.6 a 1.7 en artículos largos.
- Alinear a la izquierda, sin justificar el texto.
- Usar pocos pesos: 400 (normal), 600 (énfasis y botones) y 700 (titulares).
- Evitar mayúsculas en frases largas; reservarlas para etiquetas cortas como "ÚLTIMO MOMENTO", con un poco de espaciado entre letras.
- Párrafos cortos (3 a 4 líneas) y un margen de 1 a 1.5 em entre ellos.

**Aplicación en Tailwind** (configuración sugerida en `input.css`, Tailwind CSS 4):

```css
@import "tailwindcss";

@theme {
  --color-brand-900: #00296b;   /* cabecera, pie y titulares */
  --color-brand-700: #003f88;   /* enlaces y títulos de sección */
  --color-brand-500: #00509d;   /* secundarios, hover e iconos */
  --color-accent: #fdc500;      /* botones y CTA */
  --color-accent-light: #ffd500; /* detalles menores */
  --color-surface: #f5f6f8;     /* tarjetas y secciones alternas */
  --color-ink: #1f2933;         /* texto principal */
  --color-line: #e2e5ea;        /* bordes y divisores */
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
  --font-serif: "Merriweather", Georgia, serif;
}
```

**Aplicación en los componentes:**

- Navbar y pie de página con fondo `brand-900`, texto blanco y *hover* en `brand-500`.
- Botones y CTA con fondo `accent` y texto `brand-900`.
- Etiquetas como "ÚLTIMO MOMENTO" con fondo `accent`, texto `brand-900`, mayúsculas y espaciado entre letras.
- Tarjetas y secciones alternas sobre `surface`, con bordes `line`.

## 9. Modelo de datos (MongoDB y PostgreSQL)

Cuatro colecciones sostienen el sitio: `categories` (menú y clasificación), `articles` (contenido), `images` (metadatos de las imágenes, cuyos archivos viven en Cloudflare R2) y `users` (administradores, gestionados por ASP.NET Core Identity).

**Colección `categories`**

```json
{
  "_id": "ObjectId",
  "name": "string",
  "slug": "string (único dentro de su nivel)",
  "parentId": "ObjectId | null  (null = categoría raíz)",
  "order": "int",
  "isActive": "bool",
  "showInMenu": "bool",
  "description": "string",
  "createdAt": "Date",
  "updatedAt": "Date"
}
```

**Colección `articles`**

```json
{
  "_id": "ObjectId",
  "title": "string",
  "slug": "string (único)",
  "summary": "string",
  "content": "string (HTML sanitizado o Markdown)",
  "author": "string",
  "categoryId": "ObjectId (categoría raíz)",
  "subcategoryId": "ObjectId | null",
  "tags": ["string"],
  "coverImageId": "ObjectId",
  "imageIds": ["ObjectId"],
  "featured": "bool",
  "status": "borrador | publicado",
  "publishedAt": "Date",
  "createdBy": "string",
  "updatedBy": "string",
  "createdAt": "Date",
  "updatedAt": "Date"
}
```

**Colección `images`**

```json
{
  "_id": "ObjectId",
  "fileName": "string",
  "alt": "string",
  "mimeType": "image/webp",
  "originalSize": "long",
  "width": "int",
  "height": "int",
  "variants": [ { "width": "int", "key": "images/{id}/{ancho}.webp", "size": "long" } ],
  "storage": "r2",
  "bucket": "string",
  "createdBy": "string",
  "createdAt": "Date"
}
```

**Índices recomendados:**

| Colección | Índice | Motivo |
| --- | --- | --- |
| `categories` | `{ parentId: 1, order: 1 }` | Armar el árbol del navbar ordenado. |
| `categories` | `{ parentId: 1, slug: 1 }` único | Resolver URLs de categoría y subcategoría. |
| `articles` | `{ slug: 1 }` único | Resolver el detalle del artículo. |
| `articles` | `{ status: 1, categoryId: 1, subcategoryId: 1, publishedAt: -1 }` | Listados por categoría y subcategoría. |
| `articles` | `{ featured: 1, publishedAt: -1 }` | Destacados en la home. |
| `articles` | Índice de texto en `title`, `summary` y `content` | Búsqueda por texto. |

Los archivos de imagen no se guardan en MongoDB: se suben a Cloudflare R2 y `images` conserva su ubicación y metadatos. La colección `users` la administra ASP.NET Core Identity y no se expone al sitio público.

## 10. Rutas del sitio y endpoints

El sitio expone páginas Razor públicas de solo lectura y un área de administración protegida; las imágenes se sirven desde Cloudflare R2, y las rutas públicas usan *slugs* amigables.

| Ruta | Página / endpoint | Descripción |
| --- | --- | --- |
| `/` | `Index` | Destacados y últimos artículos. |
| `/categorias/{slug}` | `Categoria` | Artículos de una categoría raíz, con paginación (`?pagina=`). |
| `/categorias/{slug-padre}/{slug}` | `Categoria` | Artículos de una subcategoría. |
| `/articulos/{slug}` | `Articulo` | Detalle con portada y galería. |
| `/buscar?q=` | `Buscar` | Resultados de búsqueda por texto. |
| `/acerca`, `/contacto` | `Acerca`, `Contacto` | Páginas estáticas. |
| `/sitemap.xml`, `/robots.txt` | Endpoints SEO | Generados desde MongoDB. |
| `/error`, `/404` | `Error` | Páginas de error amigables. |
| `/admin/login` | `Admin/Login` | Inicio de sesión (acceso anónimo). |
| `/admin` | `Admin/Index` | Panel con accesos rápidos. |
| `/admin/articulos` | `Admin/Articulos` | Listado con filtros, paginación y acciones. |
| `/admin/articulos/crear`, `/admin/articulos/editar/{id}` | `Admin/Articulos/Editar` | Formulario de artículo y subida de imágenes. |
| `/admin/categorias` | `Admin/Categorias` | CRUD de categorías y subcategorías. |
| `/admin/imagenes` | `Admin/Imagenes` | Biblioteca de imágenes, texto alternativo y eliminación. |

## 11. Requisitos y criterios de aceptación

**Requisitos funcionales**

| ID | Requisito | Prioridad |
| --- | --- | --- |
| RF-01 | Construir el navbar desde MongoDB con categorías y subcategorías. | Alta |
| RF-02 | Mostrar en la home los artículos destacados y los más recientes. | Alta |
| RF-03 | Listar artículos por categoría o subcategoría con paginación (por ejemplo, 9 por página). | Alta |
| RF-04 | Mostrar el detalle de un artículo (título, autor, fecha, contenido e imágenes) en `/articulos/{slug}`. | Alta |
| RF-05 | Mostrar imágenes de portada y galería en WebP, servidas desde Cloudflare R2. | Alta |
| RF-06 | Mostrar solo artículos con estado `publicado` y categorías activas. | Alta |
| RF-07 | Administrador: CRUD de artículos con estado, destacado, categoría y subcategoría (AD-01 a AD-03). | Alta |
| RF-08 | Administrador: subida de imágenes con conversión a WebP y almacenamiento en R2, texto alternativo y eliminación (AD-04 y AD-05). | Alta |
| RF-09 | Administrador: CRUD de categorías y subcategorías con orden y visibilidad en el menú (AD-06). | Alta |
| RF-10 | Buscar artículos por texto. | Media |
| RF-11 | Páginas de error amigables (404 y 500). | Media |

**Requisitos no funcionales**

- **Rendimiento:** LCP menor a 2,5 s en 4G; caché de menú y listados; imágenes WebP con `srcset`, `loading="lazy"` y caché larga desde R2.
- **Responsivo:** diseño *mobile-first* con Tailwind para móvil, tablet y escritorio.
- **Accesibilidad:** WCAG 2.1 AA (texto alternativo, contraste, navegación por teclado, menú desplegable accesible).
- **SEO:** renderizado en servidor, metadatos y Open Graph por página, `sitemap.xml`, `robots.txt`, URLs con *slug*.
- **Seguridad:** HTTPS, cabeceras seguras, validación de parámetros, sanitización del HTML, *rate limiting*, área `/admin` autenticada con protección CSRF y credenciales de MongoDB y R2 fuera del código (variables de entorno o *user secrets*).
- **Mantenibilidad:** código por capas, inyección de dependencias, pruebas automatizadas y documentación.
- **Disponibilidad:** objetivo del 99,5 % mensual.

**Criterios de aceptación**

- El navbar refleja las categorías y subcategorías de MongoDB, sin enlaces fijos en el código.
- Todo el contenido visible proviene de MongoDB; los artículos en `borrador` no son accesibles.
- Un administrador autenticado puede crear, editar, publicar y eliminar artículos, categorías e imágenes sin tocar la base de datos.
- Toda imagen subida queda convertida a WebP, con variantes y texto alternativo, almacenada en Cloudflare R2.
- Sin sesión iniciada, cualquier ruta de `/admin` redirige al inicio de sesión.
- El sitio funciona en las últimas versiones de Chrome, Firefox, Safari y Edge, y en móvil.
- Puntuación de Lighthouse de al menos 90 en rendimiento, accesibilidad y SEO.
- Pruebas automatizadas aprobadas y documentación entregada.

## 12. Fases y entregables

1. **Análisis y diseño:** confirmación de requisitos, maquetas del navbar, del sitio público y del administrador, y modelo de datos.
2. **Base de datos:** colecciones, índices y datos de prueba (*seed*) con categorías, subcategorías, artículos e imágenes.
3. **Capa de datos y servicios:** repositorios, `MenuService` con caché y `ArticleService`, con pruebas.
4. **Imágenes:** `ImageService` (conversión a WebP y variantes) e `IImageStorage` con Cloudflare R2.
5. **Administrador de contenido:** inicio de sesión, CRUD de artículos, gestión de imágenes y de categorías; primero las funcionalidades esenciales.
6. **Sitio público:** layout, navbar dinámico, listados, detalle de artículo y búsqueda.
7. **Optimización:** SEO, rendimiento (caché y compresión), accesibilidad y Lighthouse.
8. **Pruebas y despliegue:** pruebas finales, Dockerfile, configuración de producción y documentación (README y guía de despliegue).

## 13. Asignación de agente especializado

Para la realización de esta tarea **se debe asignar un agente especializado en desarrollo de software**, que deberá apoyarse en **skills** (capacidades o instrucciones especializadas reutilizables) para ejecutar el trabajo con calidad y consistencia.

**Perfil del agente:** desarrollo full stack con C# y .NET 10, ASP.NET Core y Razor, Tailwind CSS, MongoDB (`MongoDB.Driver`), procesamiento de imágenes (WebP), almacenamiento S3 con Cloudflare R2, pruebas automatizadas, seguridad web y despliegue con Docker.

**Skills sugeridas:**

| Skill | Uso en el proyecto |
| --- | --- |
| C# y ASP.NET Core (.NET 10) | Estructura de la solución, Razor Pages, inyección de dependencias y *middleware*. |
| Diseño frontend con Tailwind | Interfaz responsiva, navbar desplegable y componentes reutilizables. |
| Modelado y consultas MongoDB | Colecciones, árbol de categorías e índices. |
| Administración de contenido (CRUD) | Formularios, validación, listados con filtros y autenticación con Identity. |
| Procesamiento de imágenes (WebP) | Conversión, redimensionado y variantes con ImageSharp. |
| Almacenamiento S3 / Cloudflare R2 | Subida y borrado con `AWSSDK.S3`, claves, caché y dominio público. |
| Caché y rendimiento | `IMemoryCache`, `OutputCache` y compresión. |
| SEO y accesibilidad | Metadatos, *sitemap*, WCAG 2.1 AA y Lighthouse. |
| Pruebas (xUnit y Testcontainers) | Pruebas unitarias e integración contra MongoDB. |
| Seguridad web | Validación, sanitización de HTML, cabeceras seguras y gestión de secretos. |
| Docker y documentación técnica | Contenerización, README y guía de despliegue. |

**Responsabilidades del agente:** seguir estas especificaciones, entregar código modular y documentado, ejecutar las pruebas antes de cada entrega, reportar avances por fase y señalar ambigüedades o riesgos antes de implementar.

## 14. Supuestos y riesgos

- El contenido inicial (categorías, artículos e imágenes) se entrega o se genera como datos de prueba.
- Se asume un máximo de dos niveles de categorías (categoría → subcategoría) en el menú.
- Se asume un único rol de administrador en la primera versión; los roles se dejan como mejora.
- Riesgo: un menú muy extenso afecta la usabilidad en móvil (mitigación: acordeón y límite de elementos por nivel).
- Riesgo: consultas del menú en cada solicitud (mitigación: caché en memoria con expiración e invalidación al guardar).
- Riesgo: HTML no confiable en los artículos (mitigación: sanitización antes de guardar y de mostrar).
- Riesgo: imágenes pesadas (mitigación: conversión a WebP, variantes por ancho y caché larga).
- Riesgo: credenciales de R2 expuestas (mitigación: token con permisos mínimos sobre el *bucket* y secretos fuera del repositorio).
- Riesgo: acceso no autorizado al administrador (mitigación: Identity, HTTPS, bloqueo por intentos fallidos y CSRF).
- Riesgo: licencia de ImageSharp para uso comercial (mitigación: verificarla o usar una alternativa de procesamiento de imágenes).
- Riesgo: costos de R2 (mitigación: revisar los precios vigentes de almacenamiento y operaciones antes de producción).
