# Proposal: Web Informativa - Capa Pública y Administrador de Contenido

## Why
El proyecto cuenta con una base de arquitectura limpia (.NET 10, ASP.NET Core y Tailwind CSS v4), pero carece del modelo de datos, la capa administrativa prioritaria y las funcionalidades públicas del portal informativo. Se requiere implementar la solución completa basada en las especificaciones técnicas v3.0: un panel de administración prioritario para la gestión de contenidos, categorías e imágenes optimizadas en Cloudflare R2, y un portal público responsivo y de alto rendimiento alimentado dinámicamente desde MongoDB con caché en memoria.

## What Changes
- **Administrador de Contenido (Prioridad Primordial)**: Área `/admin` protegida para gestión de artículos con editor por bloques estilo Gutenberg (Bubble menu, comando `/`, drag-handle y atajos Markdown), ciclo de vida (borrador/publicado, destacado, slug automático), gestión de categorías y subcategorías, moderación de comentarios y edición de páginas informativas.
- **Autenticación Administrativa**: Acceso exclusivo para administradores mediante ASP.NET Core Identity con cookies seguras y protección contra CSRF.
- **Gestión y Almacenamiento de Imágenes**: Subida de imágenes, conversión a WebP mediante SixLabors.ImageSharp con generación de variantes (400, 800 y 1600 px), almacenamiento en Cloudflare R2 (API compatible con S3) y biblioteca de imágenes con texto alternativo.
- **Navegación Dinámica con Caché (Navbar)**: Carga reactiva de categorías y subcategorías desde MongoDB con autorreferencia `parentId`, soporte de `IMemoryCache` (5 minutos / invalidación manual al guardar) y resiliencia ante desconexiones.
- **Catálogo y Lectura de Noticias**: Portada con destacados y recientes, listado paginado (9 artículos por página), detalle de artículo con imágenes WebP (`srcset`, `loading="lazy"`) y motor de búsqueda por texto.
- **Comentarios de Visitantes y Moderación**: Formulario para visitantes sin registro previo (nombre, correo no publicado, honeypot y control de tasa por IP), con estado `pendiente` y visualización pública únicamente tras aprobación del administrador.
- **Secciones Informativas y Aviso de Cookies**: Colección `pages` para páginas estáticas editables (`/info/{slug}`: Nosotros, Privacidad, Cookies), pie de página dinámico y componente de aviso de cookies.
- **Diseño Visual y Rendimiento**: Paleta azul/amarillo corporativa, combinación tipográfica Merriweather + Inter y diseño responsivo mobile-first con Tailwind CSS v4.

## Capabilities

### New Capabilities
- `news-navigation`: Carga y renderizado jerárquico del menú navbar desde la colección `categories` de MongoDB con soporte de caché en memoria e invalidación.
- `news-catalog`: Consulta, visualización de portada (destacados y recientes), listado paginado por categorías/subcategorías, búsqueda textual y detalle de artículos con entrega de imágenes WebP desde Cloudflare R2.
- `article-comments`: Publicación de comentarios de visitantes con validación honeypot y moderación administrativa de estados (`pendiente`, `aprobado`, `rechazado`).
- `admin-auth`: Inicio y cierre de sesión seguro mediante ASP.NET Core Identity y cookies para el acceso al panel administrativo.
- `admin-content`: Gestión administrativa completa de artículos (estados, destacados, slugs únicos automáticos), categorías, subcategorías y moderación de comentarios.
- `image-management`: Procesamiento con ImageSharp (conversión a WebP y variantes de ancho), almacenamiento en Cloudflare R2 y biblioteca de metadatos.
- `informative-pages`: Gestión y visualización de secciones informativas institucionales (`pages`) en `/info/{slug}`, pie de página dinámico y aviso de cookies.
- `version-control`: Puntos de control obligatorios mediante commits de Git tras cada fase o hito verificable del desarrollo.

### Modified Capabilities
*(Ninguna; el proyecto no contaba con especificaciones previas en `openspec/specs/`).*

## Impact
- **Domine**: Entidades de dominio ricas (`Category`, `Article`, `Comment`, `Visitor`, `InformativePage`, `ImageMetadata`, `AdminUser`).
- **Aplication**: DTOs, validaciones de entrada, interfaces de repositorio y servicios de negocio (`IMenuService`, `IArticleService`, `ICommentService`, `IPageService`, `IImageService`, `IImageStorage`, `IAuthService`).
- **Infrastructure**: Driver oficial de MongoDB (`MongoDB.Driver`), repositorios tipados, índices, cliente S3 (`AWSSDK.S3`) con proveedor para Cloudflare R2 y proveedor local de desarrollo, e ImageSharp para procesamiento WebP.
- **paginaWeb**: Área `Admin` (`/admin`), controladores y vistas Razor, ViewComponent de Navbar con `IMemoryCache`, estilos con variables CSS temáticas en Tailwind v4 y fuentes Merriweather + Inter.
- **Dependencias externas**: `MongoDB.Driver`, `AWSSDK.S3`, `SixLabors.ImageSharp`, `Microsoft.AspNetCore.Identity`.
