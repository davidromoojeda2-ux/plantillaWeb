# Design: Web Informativa (Capa Pública y Administrador de Contenido)

## Context
El sistema está organizado en una solución Clean Architecture con 4 capas (`Domine`, `Aplication`, `Infrastructure`, `paginaWeb`) bajo .NET 10. La persistencia principal se realiza en MongoDB, los archivos de imagen optimizados en Cloudflare R2 (API S3) y la interfaz en Razor con Tailwind CSS v4. Para los requerimientos funcionales y de comportamiento, ver `proposal.md` y las especificaciones en `specs/`.

## Goals / Non-Goals

**Goals:**
- Diseñar entidades de dominio ricas y desacopladas en `Domine`.
- Implementar el Administrador de Contenido (`/admin`) con prioridad primordial para gestionar artículos, categorías, imágenes, comentarios y páginas.
- Implementar la abstracción `IImageStorage` compatible con Cloudflare R2 y almacenamiento local de desarrollo.
- Procesar imágenes automáticamente a WebP en variantes responsivas (400, 800 y 1600 px) mediante ImageSharp.
- Construir el Navbar dinámico desde MongoDB con autorreferencia `parentId` y caché en memoria (`IMemoryCache`).
- Implementar comentarios de visitantes con protección honeypot, almacenamiento de hash de IP y moderación administrativa previa a la publicación.
- Aplicar la identidad visual corporativa (paleta azul/amarillo y tipografía Merriweather + Inter) en Tailwind CSS v4.

**Non-Goals:**
- Roles y permisos avanzados de usuarios (un único nivel de administrador para esta fase).
- Autenticación avanzada como 2FA o recuperación de contraseña por correo.
- Programación de publicaciones diferidas y versionado de artículos.
- Soporte multi-idioma (diferido a fases posteriores).

## Decisions

### 1. Modelado de Datos en MongoDB
- **Categorías (`categories`)**: Documentos autorreferenciados con `parentId: ObjectId | null` (raíz cuando es null).
  - *Razón*: Permite estructurar hasta dos niveles jerárquicos ordenados (`order`) y filtrar por `isActive` y `showInMenu`.
  - *Alternativa descartada*: Categorías con listas embebidas fijas; descartada para permitir consultas directas por id y escalabilidad de subcategorías independientes.
- **Artículos (`articles`)**: Colección con `title`, `slug` único, `summary`, `content` (HTML sanitizado), `author`, `categoryId`, `subcategoryId`, `tags`, `coverImageId`, `imageIds`, `featured`, `status` (`borrador` | `publicado`), `publishedAt`.
- **Imágenes (`images`)**: Metadatos de imágenes con `variants` (width, key, size), `alt`, `width`, `height`, `storage: "r2"`. Los binarios residen en Cloudflare R2.
- **Comentarios (`comments`)**: Colección independiente con `articleId`, `visitorId`, `name`, `comment`, `status` (`pendiente` | `aprobado` | `rechazado`), `createdAt`.
- **Visitantes (`visitors`)**: Registro identificado por correo con `name`, `email`, `ipHash`, `consentCookies`. Sin contraseñas ni cuentas.
- **Páginas informativas (`pages`)**: Secciones estáticas con `title`, `slug`, `content`, `order`, `showInFooter`.
- **Usuarios administradores (`users`)**: Credenciales para administradores gestionadas por ASP.NET Core Identity.

### 2. Procesamiento de Imágenes y Almacenamiento en Cloudflare R2
- Procesamiento en servidor mediante `SixLabors.ImageSharp`:
  - Corrección de orientación EXIF, eliminación de metadatos.
  - Conversión a WebP con calidad ~80.
  - Generación de tres variantes por ancho: 400 px, 800 px y 1600 px manteniendo aspect ratio.
- Abstracción `IImageStorage`:
  - Implementación de producción `CloudflareR2Storage` mediante `AWSSDK.S3` apuntando al endpoint de la cuenta R2 (`https://<AccountId>.r2.cloudflarestorage.com`).
  - Implementación local `LocalStorage` para desarrollo local sin conexión externa.
- Entrega en la capa pública mediante dominio CDN público con directivas `srcset` y `loading="lazy"`.

### 3. Navegación Dinámica y Estrategia de Caché
- `NavbarViewComponent` invoca `IMenuService.GetMenuAsync()`.
- Se consulta `IMemoryCache`:
  - Si existe en memoria, se retorna inmediatamente.
  - Si expira (5 minutos) o es invalidado, se consulta MongoDB y se repuebla la caché.
  - Al guardar, editar o eliminar una categoría en el administrador, se invalida explícitamente la clave de caché del menú.
  - En caso de fallo de conexión con MongoDB, se sirve la última copia en caché como fallback.

### 4. Flujo de Comentarios de Visitantes y Moderación
- En el detalle del artículo, los visitantes ingresan nombre, correo y mensaje.
- Medidas de protección antispam:
  - Campo trampa *honeypot* no visible para usuarios reales.
  - Hash seguro de dirección IP para control de frecuencia sin almacenar la IP en texto plano.
- El comentario se persiste como `pendiente`. Solo los administradores pueden cambiar su estado a `aprobado` desde `/admin/comentarios`. Únicamente los comentarios aprobados son visibles en el portal público.

### 5. Autenticación y Seguridad del Administrador
- Acceso a `/admin` protegido mediante cookies con ASP.NET Core Identity.
- Rutas del área administrativa protegidas con atributo `[Authorize]`.
- Validación CSRF (*antiforgery*) obligatoria en todos los formularios de edición y subida.
- Sanitización de contenido HTML en artículos antes de persistir y al renderizar.

### 6. Sistema de Diseño Visual en Tailwind CSS v4
- Definición de tokens en `@theme` dentro de `tailwind.css`:
  - `--color-brand-900: #00296B` (cabeceras, pie, titulares principales)
  - `--color-brand-700: #003F88` (enlaces, titulares secundarios)
  - `--color-brand-500: #00509D` (hover, iconos)
  - `--color-accent: #FDC500` (botones, CTAs, destacados)
  - `--color-accent-light: #FFD500` (detalles y etiquetas)
  - `--color-surface: #F5F6F8` (fondos de tarjetas y secciones)
  - `--color-ink: #1F2933` (texto principal)
  - `--color-line: #E2E5EA` (bordes y separadores)
  - Fuentes: `Merriweather` (serif para titulares) e `Inter` (sans-serif para cuerpo e interfaz).

### 7. Especificaciones de UI y Wireframes de Referencia

Se incorporan los frames oficiales ubicados en `assets/` como lineamientos visuales y de interacción:

- **Navegación Superior (Navbar)**:
  - Referencia visual: ![Navbar de referencia](assets/frame%20navbar.png)
  - *Elementos clave*: Logotipo a la izquierda, menús desplegables por categoría con indicador de flecha, enlaces directos para categorías sin hijos, botón secundario de inicio de sesión y botón CTA destacado tipo *pill* con fondo de acento.

- **Pie de Página Dinámico (Footer)**:
  - Referencia visual: ![Footer de referencia](assets/frame%20footer.png)
  - *Elementos clave*: Fondo oscuro (`--color-brand-900`), estructura de navegación multiculumna para categorías y recursos, divisor sutil, fila inferior con selector de idioma/país, enlaces legales/privacidad (`/info/politica-de-privacidad`, `/info/terminos`) e iconografía social en línea.

- **Lectura Detallada de Artículo**:
  - Referencia visual: ![Detalle de artículo con barra de progreso y menú interactivo](assets/frame%20articulo%20con%20barra%20de%20progreso%20en%20la%20parte%20superior%20y%20menu%20interactivo%20en%20el%20margen%20izquierdo.png)
  - *Elementos clave*:
    1. **Barra de progreso de lectura**: Barra indicadora en el borde superior de la pantalla que se llena dinámicamente con el scroll.
    2. **Cabecera editorial**: Etiqueta de categorías/breadcrumbs, título principal H1, bajada destacada, ficha de autor con avatar, fecha de publicación y tiempo estimado de lectura en minutos.
    3. **Imagen de portada principal**: Formato WebP responsivo con ancho completo o destacado.
    4. **Layout de contenido a dos columnas**: Columna izquierda adhesiva (*sticky*) con índice interactivo ("En esta página") que resalta la sección activa y permite salto directo; columna derecha con el cuerpo del artículo formateado y tipografía de lectura optimizada.

- **Secciones Informativas e Institucionales (Políticas / Acerca de)**:
  - Referencia visual: ![Secciones informativas de calidad](assets/frame%20politicas%20de%20calidad.png)
  - *Elementos clave*: Cabecera hero oscura de alto contraste con etiqueta de fecha de última actualización ("ACTUALIZACIÓN: [Fecha]"), título principal y subtítulo explicativo. Cuerpo con índice navegable de secciones (Introducción, Valores, Derechos, etc.) y texto estructurado.

### 8. Motor de Edición de Contenido por Bloques (Experiencia Gutenberg / Block Editor)
Para la creación y edición de artículos y páginas informativas en el panel `/admin`, se implementa un editor enriquecido por bloques que replica la experiencia de usuario de Gutenberg:
- **Barra flotante al seleccionar texto (Bubble Menu / Floating Toolbar)**: Despliega controles de estilo inline (negrita, cursiva, enlaces, niveles de título) al seleccionar cualquier texto.
- **Menú de inserción rápida mediante slash (Slash Command `/` / Block Inserter)**: Al escribir `/` en una línea vacía o al inicio de un bloque, se abre un catálogo desplegable para insertar párrafos, encabezados H2/H3, imágenes desde la biblioteca de R2, citas o listas.
- **Reordenamiento visual con agarrador (Drag Handle `⋮⋮`)**: Cada bloque expone un manipulador lateral de seis puntos para arrastrar y soltar (*drag-and-drop*) reorganizando los contenidos visualmente.
- **Atajos Markdown en tiempo real (Markdown Shortcuts / Input Rules)**: Reconocimiento inmediato de prefijos (`#` para títulos, `-` para viñetas, `>` para bloques de cita) que transforman el bloque automáticamente.

### 9. Estrategia de Puntos de Control y Commits en Git
Para asegurar trazabilidad, atomicidad y facilidad de rollback o revisión durante el desarrollo:
- **Puntos de control por capa/fase**: Al completar y verificar cada capa funcional (Dominio, Infraestructura, Aplicación, Estilos, Administrador, Portal Público y Verificación Final), se registra un commit en Git como punto de control estable.
- **Validación previa obligatoria**: Ningún commit de punto de control se realiza sin verificar antes una compilación limpia (`dotnet build` con cero errores) y la generación correcta de estilos (`pnpm run build:css`).
- **Convención de mensajes estructurados (Conventional Commits)**:
  - `feat(domain): ...`
  - `feat(infrastructure): ...`
  - `feat(application): ...`
  - `feat(ui): ...`
  - `feat(admin): ...`
  - `feat(public): ...`
  - `feat(release): ...`

## Risks / Trade-offs

- **[Riesgo]** Latencia o costos imprevistos de red con Cloudflare R2  
  → **Mitigación:** Subida en segundo plano de variantes, caché de cliente con cabeceras `Cache-Control` largas y URLs directas desde CDN.
- **[Riesgo]** Inconsistencia del menú tras modificaciones en el administrador  
  → **Mitigación:** Invalidación reactiva de la clave de `IMemoryCache` inmediatamente después de cualquier operación de escritura en `categories`.
- **[Riesgo]** Saturación de comentarios por bots  
  → **Mitigación:** Filtrado estricto por honeypot, tasa límite basada en hash de IP y moderación predeterminada en estado pendiente.
- **[Riesgo]** Exposición de claves de Cloudflare R2 y cadenas de conexión  
  → **Mitigación:** Uso exclusivo de User Secrets en desarrollo y variables de entorno en producción; nunca almacenar credenciales en el repositorio.
