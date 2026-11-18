# Design

## Context

Ver `proposal.md` (motivación) y `specs/document-management/spec.md` (contrato de comportamiento).

La solución es ASP.NET Core MVC (.NET 10) con capas `Domine`, `Aplication`, `Infrastructure` y `paginaWeb`, MongoDB como persistencia y un único contenedor Docker sobre VPS. Puntos del estado actual que condicionan el diseño:

- `IImageStorage` ya abstrae subida, borrado, URL pública y disponibilidad, con `S3Storage` (AWS S3 / Cloudflare R2) y `LocalStorage` (fallback en `wwwroot/uploads`). No existe lectura ni streaming.
- `ImageService` demuestra el patrón de biblioteca multimedia: metadatos en MongoDB, almacenamiento prioritario en S3, fallback local, borrado compensatorio y desvinculación de artículos.
- Los artículos referencian imágenes con `CoverImageId` e `ImageIds`; el contenido editorial usa bloques tipados y el formulario administrativo ya integra un modal de biblioteca de imágenes.
- El área Admin usa cookies, `[Authorize]` y antiforgery. La capa pública consume `ArticleResponseDto` y partials Razor.
- Kestrel limita por defecto el cuerpo de la petición a ~30 MB y Nginx del ejemplo permite 50 MB. `LocalStorage` sirve todo lo que esté bajo `wwwroot/uploads` mediante middleware estático.
- `Article` y `InformativePage` usan `[BsonIgnoreExtraElements]`, por lo que añadir campos nuevos no rompe documentos existentes.
- No hay servicio de antivirus ni cola de trabajos; el proceso es único.

## Goals / Non-Goals

**Goals:**

- Añadir un ciclo de vida de documentos con validación fuerte y almacenamiento privado por defecto.
- Reutilizar S3/R2 con fallback local sin acoplar el dominio a un proveedor.
- Servir descargas mediante un endpoint same-origin con control de acceso, rangos y cabeceras seguras.
- Integrar los documentos en el CMS y en la vista pública con el menor cambio posible en el editor de bloques.
- Mantener compatibilidad con los documentos MongoDB existentes y permitir rollback sin migración destructiva.

**Non-Goals:**

- Bloque `document` dentro del editor de bloques (fase posterior).
- Portal público de documentos independiente de artículos.
- Antivirus en línea, previsualización de Office o versionado de archivos.
- Reemplazar o renombrar `IImageStorage`/`ImageService` de forma masiva.
- Convertir documentos a otros formatos o extraer su texto para búsqueda.

## Decisions

### 1. Entidad y colección separadas de las imágenes

**Decisión**: crear `DocumentMetadata` en `Domine/Entities` y la colección `documents` en MongoDB. Campos: `Id`, `OriginalFileName`, `DisplayName`, `Description`, `Extension`, `ContentType`, `SizeBytes`, `Sha256`, `Storage`, `Key`, `IsPublic`, `DownloadCount`, `UploadedAt`.

**Razón**: `ImageMetadata` contiene `Variants`, `OriginalWidth` y `OriginalHeight`, que no aplican a un PDF o DOCX. Separar evita contaminar la biblioteca de imágenes y permite evolucionar documentos sin afectar el procesamiento WebP.

**Alternativas descartadas**:
- Reutilizar `ImageMetadata` con un campo `Kind`: obliga a que `ImageService` distinga tipos y a arrastrar campos irrelevantes; además `ImageSharp` fallaría con documentos.
- Guardar documentos como bloques de contenido: mezcla binarios con el contrato editorial y complica el borrado y la biblioteca.

### 2. `Article.DocumentIds` como asociación, no un bloque nuevo

**Decisión**: añadir `List<string> DocumentIds` a `Article`, con `[BsonRepresentation(BsonType.ObjectId)]`, y propagarlo a `ArticleDto`, `ArticleResponseDto`, `ArticleFormViewModel` y `ArticleMapper`. Los documentos se muestran en una sección “Documentos adjuntos” al final del artículo.

**Razón**: resuelve el caso principal (adjuntos de una nota) con bajo riesgo y sin tocar el editor de bloques. El bloque `document` puede añadirse después reutilizando la misma entidad y servicio.

**Alternativas descartadas**:
- Solo bloque de contenido: obliga a modificar `ContentBlock`, DTO, mapper, sanitizador, editor JS y partials en la primera fase.
- Asociación inversa (`DocumentMetadata.ArticleId`): un documento reutilizado en varios artículos requeriría duplicar metadatos o una tabla de unión; `DocumentIds` permite reutilización y el borrado se resuelve desvinculando.

### 3. Almacenamiento privado con lectura por streaming

**Decisión**: introducir `IDocumentStorage` con `UploadAsync`, `OpenReadAsync`, `DeleteAsync`, `IsAvailableAsync` y `StorageType`. `S3Storage` la implementa para documentos con ACL privada; `LocalDocumentStorage` guarda en una ruta configurable fuera de `wwwroot` (por defecto `storage/documents`). El servicio de documentos elige S3/R2 si está disponible y cae a local si no.

**Razón**: los documentos pueden ser borradores o contener información sensible; no deben quedar accesibles por URL pública directa. La descarga pasa siempre por la aplicación, que valida permisos y aplica cabeceras.

**Alternativas descartadas**:
- Reutilizar `LocalStorage` bajo `wwwroot/uploads`: el middleware estático serviría el archivo sin pasar por el control de acceso; un HTML/SVG subido podría ejecutarse.
- URLs públicas prefirmadas: eficientes, pero rompen el requisito same-origin y exponen el dominio del bucket; se pueden considerar en el futuro para descargas masivas.

**Rangos**: `OpenReadAsync` aceptará un rango opcional. En local se usa `FileStream` con `Seek`; en S3/R2 se usa `GetObjectRequest.ByteRange`. Así se evita bufferizar el archivo completo y se soporta reanudación.

### 4. Validación en servidor basada en firma real

**Decisión**: bufferizar la subida en memoria hasta el límite configurado, calcular SHA-256 y validar la firma antes de subir. La lista de extensiones y las firmas se declaran en un validador dedicado:

- PDF: `%PDF-`.
- DOCX/XLSX/PPTX/ODT/ODS/ODP/ZIP: firma ZIP `PK\x03\x04` (y variantes).
- DOC/XLS/PPT: firma OLE `D0 CF 11 E0 A1 B1 1A E1`.
- RTF: `{\rtf`.
- TXT/CSV: extensión permitida, ausencia de bytes NUL y decodificación razonable; sin firma concluyente.

Se rechazan explícitamente HTML, SVG, JavaScript, ejecutables, scripts y archivos con doble extensión. Los formatos con macros (`.docm`, `.xlsm`, `.pptm`) quedan fuera de la lista por defecto.

**Razón**: el `Content-Type` y la extensión enviados por el cliente no son confiables. La firma real es la comprobación mínima viable sin antivirus.

**Trade-offs**: el buffer en memoria limita el tamaño práctico a decenas de MB; es aceptable para el límite inicial de 25 MB. Si se sube el límite, se cambiará a archivo temporal.

### 5. Claves generadas y metadatos como fuente de verdad

**Decisión**: la clave se genera como `documents/{yyyy}/{MM}/{guid:N}-{slug}{ext}` y nunca se deriva del nombre original. El nombre visible se saneado y se guarda en `DisplayName`; el nombre original se conserva solo como referencia. La base de datos guarda `Storage` y `Key`; la URL de descarga se calcula con el `Id`.

**Razón**: evita traversal, colisiones y filtración de rutas. Permite cambiar el dominio público o migrar de proveedor sin tocar los artículos.

**Compensación**: si la subida al storage tiene éxito pero falla la inserción en MongoDB, se elimina el objeto subido. Si falla el borrado del objeto, se registra el error y se elimina el metadato para no dejar un registro roto visible.

### 6. Control de acceso en la descarga pública

**Decisión**: un documento es descargable públicamente si `IsPublic` es verdadero o si está referenciado por al menos un artículo con estado `Publicado`. La consulta se resuelve con un índice sobre `articles.DocumentIds` y `articles.Status`.

**Razón**: evita que un documento adjunto a un borrador se filtre por conocer su identificador. Mantener `IsPublic` permite publicar documentos sueltos sin artículo.

**Alternativas descartadas**:
- Confiar solo en `IsPublic`: obliga a actualizar el documento cada vez que cambia el estado del artículo.
- Denegar todo lo no publicado: impediría descargar documentos institucionales sueltos.

### 7. Endpoint de descarga con resultado de streaming y rangos

**Decisión**: `GET /documentos/descargar/{id}/{slug?}` en un controlador público. Valida acceso, obtiene metadatos y abre el stream en el proveedor registrado. Responde con:

- `Content-Disposition: attachment; filename*=UTF-8''...`
- `Content-Type` permitido (nunca `text/html` ni `image/svg+xml`).
- `X-Content-Type-Options: nosniff`.
- `Accept-Ranges: bytes`, `Content-Range` y `206` cuando corresponda.
- `ETag`/`Last-Modified` y `Cache-Control` para documentos públicos; `private, no-store` para el endpoint administrativo.

El panel Admin usa `/admin/documentos/descargar/{id}` para previsualizar o descargar borradores sin depender de la política pública.

**Razón**: same-origin, control de acceso centralizado y compatibilidad con reanudación de descargas. `FileStreamResult` no basta para S3 si el stream no es seekable; por eso el rango se delega a `IDocumentStorage`.

### 8. UI del CMS: biblioteca propia y modal de selección

**Decisión**: crear `DocumentosController` (área Admin) con `Index`, `List`, `Upload`, `UploadAjax`, `Rename`, `Delete`, `Download`. La vista `Documentos/Index.cshtml` replica el patrón de `Imagenes/Index.cshtml`. En el formulario de artículo se añade una sección “Documentos adjuntos” con un modal reutilizable `_DocumentManagerModal` que permite buscar, subir y seleccionar varios documentos; los IDs se guardan en inputs ocultos de `DocumentIds`.

**Razón**: consistencia con la biblioteca de imágenes y buena experiencia para el editor, sin acoplar el editor de bloques. La selección múltiple es necesaria porque un artículo puede tener varios adjuntos.

**Alternativas descartadas**:
- `<select multiple>` simple: funcional, pero pobre para buscar y subir; el modal ya es un patrón del proyecto.
- Gestión solo desde la biblioteca: obliga al editor a salir del artículo y volver; el modal reduce fricción.

### 9. Configuración y límites

**Decisión**: añadir la sección `Documents` en `appsettings.json`:

```json
"Documents": {
  "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".rtf", ".odt", ".ods", ".odp"],
  "MaxSizeBytes": 26214400,
  "MaxPerArticle": 10,
  "LocalPath": "storage/documents"
}
```

Si `MaxSizeBytes` supera el límite de Kestrel, configurar `KestrelServerOptions.Limits.MaxRequestBodySize` y `FormOptions.MultipartBodyLengthLimit`; si se despliega detrás de Nginx, ajustar `client_max_body_size`. El volumen Docker debe incluir `storage/documents` cuando se use fallback local.

**Razón**: el límite de 25 MB queda por debajo del default de Kestrel y evita cambios obligatorios de infraestructura. La configuración permite habilitar más formatos sin recompilar.

### 10. Índices y compatibilidad

**Decisión**: en `MongoDbIndexer` crear:

- `ix_documents_uploaded` (`UploadedAt` descendente).
- `ix_documents_article` (`ArticleId` si se conserva como referencia directa; en su defecto, índice sobre `articles.DocumentIds`).
- `ix_articles_documents` (`DocumentIds` ascendente, `Status` ascendente).

**Razón**: soporta la biblioteca paginada y la comprobación de publicación. La colección es nueva y los índices se crean de forma idempotente al arrancar.

**Rollback**: el cambio es aditivo. Una versión anterior con `[BsonIgnoreExtraElements]` ignora `Article.DocumentIds`; los documentos quedan en la colección `documents` y los objetos en storage. Para desactivar la función basta con retirar la UI y el registro del controlador; no hace falta migración destructiva.

## Risks / Trade-offs

- **XSS por archivos activos** → allowlist estricta, validación por firma, `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff` y almacenamiento fuera de `wwwroot`.
- **Malware en documentos** → sin antivirus en esta fase; se mitiga con formatos permitidos, tamaño limitado y ausencia de ejecución. Se documenta como siguiente paso.
- **Buffer en memoria en subidas** → limitado a `MaxSizeBytes` (25 MB). Si se aumenta, migrar a archivo temporal antes de subir.
- **Streams S3 no seekables** → rangos resueltos con `ByteRange` en `GetObjectRequest`; no se depende de `FileStreamResult` para cloud.
- **Documentos privados y caché** → `Cache-Control: private, no-store` para borradores; `public` solo para documentos públicos. El `ETag` se basa en SHA-256.
- **Coste de consultar artículos por documento** → índice compuesto en `DocumentIds`/`Status`; además se puede cachear la decisión por documento durante pocos minutos.
- **Fallo entre subida y persistencia** → compensación con borrado del objeto; si el borrado falla se registra y se elimina el metadato para evitar registros huérfanos visibles.
- **Cuota de almacenamiento** → límite por artículo y tamaño máximo; no se implementa cuota global en esta fase.
- **Nombres visibles duplicados** → se permiten; la clave es única. La UI desambigua por tipo y fecha.

## Migration Plan

1. Añadir la entidad, DTOs, repositorio, índices y `DocumentService` sin tocar el flujo actual de imágenes.
2. Añadir `Article.DocumentIds` (aditivo) y propagarlo a DTOs/mappers; los documentos existentes se leen sin cambios.
3. Registrar `IDocumentStorage`, `LocalDocumentStorage`, `IDocumentRepository` y `IDocumentService` en `Program.cs`.
4. Añadir configuración `Documents` y, si aplica, límites de Kestrel/FormOptions y volumen Docker.
5. Publicar la UI del CMS y el partial público; verificar que una versión anterior sigue leyendo artículos (ignora `DocumentIds`).
6. Rollback: desactivar la sección de configuración/UI; los datos nuevos permanecen y no afectan a la versión anterior.
