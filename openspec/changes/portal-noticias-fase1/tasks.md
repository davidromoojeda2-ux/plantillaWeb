# Tasks: Web Informativa (Capa Pública y Administrador de Contenido)

## 1. Entidades del Dominio (Domine)

- [x] 1.1 Crear entidad `Category` con autorreferencia `ParentId`, orden y visibilidad, y verificar compilación de `Domine.csproj`
- [x] 1.2 Crear entidad `Article` con metadatos, slugs, contador de vistas, categorías e imágenes asociadas, y verificar compilación en `Domine`
- [x] 1.3 Crear entidades `Comment` y `Visitor` para soporte de comentarios moderados con honeypot y privacidad en `Domine`
- [x] 1.4 Crear entidad `InformativePage` para secciones institucionales editables y verificar compilación en `Domine`
- [x] 1.5 Crear entidad `ImageMetadata` para persistencia de variantes WebP y metadatos de Cloudflare R2 en `Domine`
- [x] 1.6 Crear entidad `AdminUser` para gestión de credenciales del panel administrativo y verificar compilación limpia de la capa de dominio
- [x] 1.7 Registrar commit de punto de control para la capa de Dominio (`feat(domain): entidades del modelo de noticias, comentarios y administración`)

## 2. Infraestructura y Almacenamiento (Infrastructure)

- [x] 2.1 Instalar paquetes `MongoDB.Driver`, `AWSSDK.S3` y `SixLabors.ImageSharp` en `Infrastructure.csproj` y verificar compilación
- [x] 2.2 Crear `MongoDbSettings` y `MongoDbContext` configurando las colecciones tipadas (`categories`, `articles`, `comments`, `visitors`, `pages`, `images`, `users`)
- [x] 2.3 Configurar índices de MongoDB (índices únicos en slugs, índices compuestos para listados e índice de texto para búsqueda)
- [x] 2.4 Implementar repositorios de acceso a datos para cada colección de MongoDB y verificar operaciones de consulta y escritura
- [x] 2.5 Implementar abstracción `IImageStorage` con proveedor `CloudflareR2Storage` para S3 y proveedor `LocalStorage` para desarrollo
- [x] 2.6 Implementar `ImageService` con ImageSharp para corrección EXIF, conversión WebP y variantes de 400, 800 y 1600 px
- [x] 2.7 Registrar commit de punto de control para la capa de Infraestructura (`feat(infrastructure): configuración de MongoDB, R2 y servicios de imagen`)

## 3. Capa de Aplicación y Servicios (Aplication)

- [x] 3.1 Definir DTOs con validaciones de entrada (`CategoryDto`, `ArticleDto`, `CommentSubmissionDto`, `AdminLoginDto`, `PageDto`, `ImageUploadDto`)
- [x] 3.2 Implementar `IMenuService` con `IMemoryCache` (expiración de 5 minutos, fallback de disponibilidad e invalidación reactiva)
- [x] 3.3 Implementar `IArticleService` con validaciones de slug único, gestión de estados (borrador/publicado) y paginación de 9 noticias
- [x] 3.4 Implementar `ICommentService` con validación de honeypot, hashing de IP y flujo de moderación (`pendiente`, `aprobado`, `rechazado`)
- [x] 3.5 Implementar `IPageService` para secciones informativas e `IAuthService` para validación de credenciales administrativas
- [x] 3.6 Registrar commit de punto de control para la capa de Aplicación (`feat(application): DTOs, validaciones y servicios de lógica de negocio`)

## 4. Diseño Visual y Configuración de Estilos (paginaWeb)

- [x] 4.1 Configurar tokens `@theme` en `Styles/tailwind.css` con la paleta de colores corporativa (`--color-brand-900`, `--color-accent`) y fuentes Merriweather + Inter
- [x] 4.2 Compilar el bundle de estilos mediante `pnpm run build:css` y verificar la generación exitosa de `wwwroot/css/tailwind.css`
- [x] 4.3 Configurar inyección de dependencias, configuración de MongoDB, R2, autenticación por cookies y mapeo de rutas para el Área `Admin` en `Program.cs`
- [x] 4.4 Registrar commit de punto de control para Estilos y DI (`feat(ui): configuración de Tailwind v4, tokens de diseño y setup de DI`)

## 5. Administrador de Contenido (paginaWeb/Areas/Admin)

- [x] 5.1 Implementar pantalla de inicio de sesión (`/admin/login`) con cookies de ASP.NET Core Identity y protección CSRF
- [x] 5.2 Implementar CRUD de artículos en `/admin/articulos` integrando el editor por bloques estilo Gutenberg (Bubble menu, comando `/`, drag handle y atajos Markdown), subida de imágenes a R2 y control de estado borrador/publicado
- [x] 5.3 Implementar CRUD de categorías en `/admin/categorias` con orden, visibilidad e invalidación automática de la caché del menú
- [x] 5.4 Implementar biblioteca de imágenes en `/admin/imagenes` con edición de texto alternativo y advertencia de uso al eliminar
- [x] 5.5 Implementar panel de moderación de comentarios en `/admin/comentarios` para aprobar, rechazar o eliminar comentarios de visitantes
- [x] 5.6 Implementar gestión de secciones informativas en `/admin/secciones` con opción de visibilidad en el pie de página
- [x] 5.7 Registrar commit de punto de control para el Administrador de Contenido (`feat(admin): panel administrativo, login, CRUDs y editor Gutenberg`)

## 6. Portal Público (paginaWeb)

- [ ] 6.1 Crear `NavbarViewComponent` con carga reactiva desde caché y menú responsivo según wireframe de referencia (`assets/frame navbar.png`) con dropdowns por categoría, botón de login y botón CTA
- [ ] 6.2 Implementar página de inicio (`Home/Index`) con sección hero para noticias destacadas y grid cronológico de publicaciones recientes
- [ ] 6.3 Implementar vistas de catálogo por categoría y subcategoría con paginación de 9 artículos y filtros activos
- [ ] 6.4 Implementar vista de detalle del artículo (`/articulos/{slug}`) según wireframe (`assets/frame articulo...png`) con barra de progreso superior, tiempo de lectura, índice interactivo lateral ("En esta página"), imágenes WebP y formulario de comentarios con honeypot
- [ ] 6.5 Implementar motor de búsqueda por texto en `/buscar` con resultados paginados y mensajes informativos cuando no hay coincidencias
- [ ] 6.6 Implementar rutas `/info/{slug}` con hero e índice navegable (`assets/frame politicas de calidad.png`), pie de página multiculumna con enlaces legales y redes sociales (`assets/frame footer.png`) y aviso de cookies
- [ ] 6.7 Registrar commit de punto de control para el Portal Público (`feat(public): vistas públicas, catálogo, detalle con lectura y comentarios`)

## 7. Verificación Integral del Sistema

- [ ] 7.1 Ejecutar `dotnet build` en la solución completa y verificar que compile con cero errores y cero advertencias
- [ ] 7.2 Ejecutar `pnpm run build:css` y verificar que los estilos del sitio público y administrador se generen correctamente
- [ ] 7.3 Validar que la navegación anónima a `/admin` redirija a login y que las rutas públicas operen sin exigir credenciales
- [ ] 7.4 Registrar commit de punto de control final y cierre de Fase 1 (`feat(release): cierre y verificación integral de fase 1 portal noticias`)
