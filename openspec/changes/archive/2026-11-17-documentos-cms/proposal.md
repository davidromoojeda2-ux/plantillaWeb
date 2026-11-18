# Proposal

## Why

El portal ya permite subir y servir imágenes desde el CMS, pero no existe una forma gobernada de publicar documentos descargables (PDF, Office, texto, hojas de cálculo, comprimidos) ni de asociarlos a artículos. Los editores necesitan adjuntar documentos a una nota y los lectores deben poder descargarlos con un enlace estable, con validación de formato, control de acceso y trazabilidad, reutilizando el almacenamiento S3/R2 con fallback local que ya usa la biblioteca de imágenes.

## What Changes

- Nueva biblioteca de documentos en el CMS: subida multipart, metadatos (nombre visible, descripción, tipo, tamaño, hash), búsqueda, filtros, paginación, edición de metadatos, reemplazo y eliminación.
- Validación estricta en servidor: lista configurable de extensiones permitidas, tamaño máximo, coherencia extensión/MIME/firma real (magic bytes), saneado de nombres y rechazo de tipos activos (HTML, SVG, JS, ejecutables).
- Nuevo almacenamiento de documentos reutilizando `S3Storage`/`LocalStorage`, con claves generadas (`documents/{yyyy}/{MM}/{guid}-{slug}.{ext}`), persistencia del proveedor usado y capacidad de lectura/streaming para descargas.
- Asociación de documentos a artículos mediante `Article.DocumentIds`, con una sección “Documentos adjuntos” en el formulario del CMS y una lista de descargas en la vista pública del artículo.
- Endpoint público de descarga same-origin que sirve el archivo con `Content-Disposition: attachment`, soporte de rangos, caché validable y verificación de que el documento pertenece a un artículo publicado o está marcado como público.
- Nueva pantalla administrativa “Documentos” y acceso rápido desde el formulario de artículo; estado de almacenamiento y errores de validación visibles para el editor.
- Configuración nueva (`Documents:*`) para formatos, tamaño máximo, documentos por artículo y ruta local de documentos; ajuste de límites de subida en Kestrel/Nginx cuando se requiera.
- **Fuera de alcance de esta fase**: bloque `document` dentro del editor de bloques, portal público de documentos independiente de los artículos, previsualización de Office, versionado de documentos y análisis antivirus en línea. La arquitectura deja preparadas estas extensiones.

## Capabilities

### New Capabilities

- `document-management`: ciclo de vida completo de documentos en el CMS (subida, validación, metadatos, almacenamiento, búsqueda, edición, eliminación y asociación a artículos) y entrega pública controlada mediante descarga.

### Modified Capabilities

<!-- Ninguna. No existe una capacidad de documentos en las specs principales y el comportamiento de artículos no cambia a nivel de contrato: solo se añade una referencia opcional a documentos. -->

## Impact

- **Domine**: nueva entidad `DocumentMetadata`; nueva propiedad `Article.DocumentIds` con `[BsonRepresentation(ObjectId)]`; sin cambios incompatibles en documentos existentes.
- **Aplication**: DTOs de documento y de asociación, `IDocumentRepository`, `IDocumentService`, mapeadores y validadores de documentos; ampliación de `ArticleDto`/`ArticleResponseDto` y `ArticleMapper`.
- **Infrastructure**: `DocumentRepository`, `DocumentService`, lectura/streaming en `IImageStorage` (o abstracción equivalente), índices `documents` en `MongoDbIndexer` y colección en `MongoDbContext`; reutilización de `S3Storage` y `LocalStorage`.
- **paginaWeb**: `DocumentosController` (área Admin), controlador público de descargas, vistas y ViewModels, partial de adjuntos en `Views/Home/Articulo.cshtml`, navegación del panel, registro en `Program.cs`, configuración de límites y rutas.
- **Configuración/Despliegue**: `Documents:*` en `appsettings.json`; `LocalStorage:DocumentsPath` recomendado fuera de `wwwroot`; `client_max_body_size` en Nginx y límites de Kestrel/FormOptions si el tamaño máximo supera el valor por defecto.
- **Seguridad**: allowlist de formatos, validación por firma, `X-Content-Type-Options: nosniff`, descarga forzada, control de acceso a borradores y saneado de nombres; sin exponer claves ni rutas físicas.
- **Pruebas**: nuevos tests de validación de documentos, control de acceso, mapeo BSON/DTO, servicio de almacenamiento y endpoint de descarga; ajuste de tests de artículos para `DocumentIds`.
- **Dependencias**: ninguna nueva obligatoria. El hash SHA-256 usa `System.Security.Cryptography`; la detección de firma es propia y acotada a los formatos permitidos.
