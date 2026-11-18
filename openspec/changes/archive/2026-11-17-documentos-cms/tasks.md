# Tasks

## 1. Dominio y contrato de documentos

- [x] 1.1 Crear `DocumentMetadata` en `Domine/Entities` con Id, nombres, descripción, extensión, ContentType, SizeBytes, Sha256, Storage, Key, IsPublic, DownloadCount y UploadedAt; añadir `Article.DocumentIds` con representación ObjectId. Verificar con `dotnet build` y una prueba de round-trip BSON que serialice y deserialice `Article` con documentos.
- [x] 1.2 Crear `DocumentDto`/`DocumentResponseDto`, el mapeador `DocumentMapper` y ampliar `ArticleDto`, `ArticleResponseDto` y `ArticleMapper` con `DocumentIds`. Verificar con pruebas de mapeo entidad→DTO→entidad.
- [x] 1.3 Crear `DocumentsOptions` (extensiones, tamaño máximo, máximo por artículo y ruta local) y registrarlo en configuración. Verificar con una prueba de enlace de configuración que lea valores por defecto y valores personalizados.

## 2. Persistencia e índices

- [x] 2.1 Añadir la colección `Documents` a `MongoDbContext`, crear `IDocumentRepository` y `DocumentRepository` (CRUD, búsqueda paginada, filtros por tipo/almacenamiento/artículo, actualización de metadatos y contador de descargas). Verificar con `dotnet build` y pruebas del servicio usando un repositorio falso que cubran las consultas esperadas.
- [x] 2.2 Crear en `MongoDbIndexer` los índices de `documents` (`UploadedAt`, `IsPublic`, `ArticleId` si existe) y el índice `articles.DocumentIds + Status`. Verificar que el arranque de la aplicación crea los índices sin errores y que una segunda ejecución es idempotente.

## 3. Almacenamiento y validación

- [x] 3.1 Crear `IDocumentStorage` con subida, lectura por rango, borrado, disponibilidad y `StorageType`. Implementarlo en `S3Storage` con ACL privada y en un nuevo `LocalDocumentStorage` con ruta fuera de `wwwroot`. Verificar con pruebas unitarias de lectura local por rango y con una prueba de integración S3/R2 cuando haya credenciales.
- [x] 3.2 Implementar el validador de documentos (extensión permitida, tamaño, firma PDF/ZIP/OLE/RTF, rechazo de tipos activos, saneado de nombre y hash SHA-256). Verificar con pruebas de PDF válido, PDF falso, DOCX, OLE, RTF, TXT, archivo demasiado grande, HTML/SVG y nombre con traversal.
- [x] 3.3 Implementar `DocumentService` con subida, selección S3→local, compensación si falla la persistencia, listado, renombrado, borrado, desvinculación de artículos y apertura de stream para descarga. Verificar con pruebas que usen storage y repositorio falsos.
- [x] 3.4 Implementar y probar la política de acceso: documento público, documento de artículo publicado, documento solo de borrador y documento no encontrado. Verificar con pruebas unitarias de la decisión de acceso y de los códigos de error esperados.

## 4. CMS administrativo

- [x] 4.1 Crear `DocumentosController` en `Areas/Admin/Controllers` con Index, List (JSON), Upload, UploadAjax, Rename, Delete y Download administrativo, usando antiforgery y `[Authorize]`. Verificar con `dotnet build` y pruebas de controlador para respuestas exitosas y errores de validación.
- [x] 4.2 Crear `DocumentLibraryViewModel`, `DocumentManagerModalViewModel` y `DocumentCatalogItem`; implementar la vista `Documentos/Index.cshtml` con búsqueda, filtros, paginación, subida, edición y eliminación. Verificar ejecutando la aplicación y recorriendo `/admin/documentos` con un documento de prueba.
- [x] 4.3 Añadir el enlace “Documentos” a `_AdminLayout.cshtml` y el acceso rápido en `Dashboard/Index.cshtml`. Verificar que ambas rutas renderizan y navegan a la biblioteca.
- [x] 4.4 Integrar la sección “Documentos adjuntos” en `_ArticleForm.cshtml` con inputs ocultos para `DocumentIds` y el modal `_DocumentManagerModal` (listar, subir, buscar y seleccionar varios). Verificar creando un artículo con dos documentos y comprobando que los IDs persisten al editar.
- [x] 4.5 Añadir el script del modal de documentos (carga de catálogo, subida AJAX, selección múltiple, eliminación y estados de error) y registrar su carga en `_ArticleFormScripts.cshtml`. Verificar manualmente que el modal funciona sin recargar la página y que cierra correctamente.

## 5. Capa visual y descarga pública

- [x] 5.1 Crear el controlador público de descargas con validación de acceso, `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Accept-Ranges`, `Content-Range`, `ETag` y `Cache-Control` según visibilidad. Verificar con pruebas de descarga completa, descarga por rango y acceso denegado a borradores.
- [x] 5.2 Cargar los documentos asociados en la acción pública de artículo y crear el partial `_ArticleDocuments.cshtml` con nombre visible, tipo, tamaño legible y enlace de descarga accesible. Verificar que un artículo publicado muestra solo sus documentos y que un borrador no expone descargas públicas.
- [x] 5.3 Añadir estilos y comportamiento de presentación de adjuntos en `home.css`/Tailwind y comprobar la vista en escritorio y móvil. Verificar con una revisión visual y con el HTML generado.

## 6. Configuración, despliegue y documentación

- [x] 6.1 Añadir la sección `Documents` a `appsettings.json`, registrar `IDocumentRepository`, `IDocumentStorage`, `LocalDocumentStorage` y `DocumentService` en `Program.cs`, y configurar límites de Kestrel/FormOptions si el tamaño máximo supera el default. Verificar arrancando la aplicación y subiendo un documento con el límite configurado.
- [x] 6.2 Actualizar `docker-compose.yml` con el volumen de documentos locales, `deploy/nginx-portal.conf.example` con `client_max_body_size` y `.env.example`/documentación con las variables nuevas. Verificar con `docker compose config` y revisando la configuración de Nginx.
- [x] 6.3 Documentar en `GUIA_COMANDOS.md` o en un documento dedicado los formatos permitidos, límites, variables de entorno, flujo de subida/descarga y rollback. Verificar que los comandos y rutas documentados funcionan tal como están escritos.

## 7. Verificación de integración

- [x] 7.1 Ejecutar `dotnet build` y toda la suite de pruebas; verificar que no hay regresiones en artículos, imágenes, automatización ni validación de contenido.
- [x] 7.2 Ejecutar un smoke test completo: subir un PDF desde el CMS, asociarlo a un artículo, publicarlo, descargarlo desde la vista pública y comprobar que un documento de borrador no es descargable. Verificar también una solicitud `Range`.
- [x] 7.3 Ejecutar una revisión de seguridad: archivo con extensión falsa, HTML/SVG/JS, nombre con traversal, tamaño excedido y documento sin permisos. Verificar que todos son rechazados y que las respuestas no exponen rutas físicas ni claves.

## Workflow follow-up

- Archivar el cambio cuando la revisión del proyecto esté aprobada.
- Verificar el resultado archivado y que la spec principal `document-management` quedó sincronizada.
