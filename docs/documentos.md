# Documentos descargables

La funcionalidad de documentos permite subir archivos desde el CMS, reutilizarlos en varios artículos y descargarlos desde la vista pública mediante un enlace estable y controlado.

## Formatos y límites

Formatos permitidos por defecto:

`.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt`, `.csv`, `.rtf`, `.odt`, `.ods`, `.odp`

Límites por defecto:

- Tamaño máximo por archivo: **25 MB** (`26214400` bytes).
- Máximo de documentos por artículo: **10**.
- Se rechazan HTML, SVG, JavaScript, ejecutables, scripts y archivos con doble extensión peligrosa.
- El contenido se valida por firma real (PDF, ZIP/OOXML, OLE, RTF) además de la extensión.

La configuración vive en `paginaWeb/appsettings.json`:

```json
"Documents": {
  "AllowedExtensions": [ ".pdf", ".docx" ],
  "MaxSizeBytes": 26214400,
  "MaxPerArticle": 10,
  "LocalPath": "storage/documents"
}
```

## Almacenamiento

- Si S3/R2 está disponible, el documento se sube con ACL privada.
- Si no está disponible, se guarda en `storage/documents` (fuera de `wwwroot`).
- La descarga siempre pasa por la aplicación; nunca se expone una URL pública directa del bucket.
- En Docker, el volumen `./storage/documents:/app/storage/documents` conserva los archivos locales.

Variables de entorno para producción:

```env
DOCUMENTS_MAX_SIZE_BYTES=26214400
DOCUMENTS_LOCAL_PATH=/app/storage/documents
```

Si se aumenta `MaxSizeBytes` por encima de ~30 MB, `Program.cs` amplía automáticamente los límites de Kestrel y del formulario multipart. También hay que ajustar `client_max_body_size` en Nginx.

## Flujo de uso en el CMS

1. Entrar a **Admin → Documentos** o al formulario de un artículo.
2. Subir un archivo y asignar nombre visible/descripción.
3. En el artículo, abrir **Documentos adjuntos → Gestionar documentos**.
4. Seleccionar uno o varios documentos y guardar el artículo.
5. Publicar el artículo.

Los documentos marcados como **públicos** pueden descargarse sin artículo. Los documentos privados solo se descargan si están vinculados a un artículo con estado **Publicado**.

## Bloque Documento dentro del artículo

El editor de bloques permite insertar un bloque **Documento** en cualquier parte del cuerpo:

1. **Añadir bloque → Documento**.
2. Pulsar **Elegir documento** y seleccionar uno de la biblioteca.
3. Opcionalmente, personalizar el título y la descripción.
4. Guardar el artículo.

El bloque se renderiza como una tarjeta de descarga con icono, tipo, tamaño y botón **Descargar**. En pantallas pequeñas la tarjeta se apila verticalmente y el botón ocupa todo el ancho, con un área táctil mínima de 44 px.

El documento usado en un bloque se añade automáticamente a la asociación del artículo, por lo que las reglas de visibilidad y el control de descargas siguen funcionando. La sección final **Documentos adjuntos** muestra los documentos que no estén ya representados por un bloque.

## Descarga pública

Ruta:

```text
GET /documentos/descargar/{id}
GET /documentos/descargar/{id}/{slug}
```

La respuesta incluye:

- `Content-Disposition: attachment` con el nombre visible.
- `X-Content-Type-Options: nosniff`.
- `Accept-Ranges` y `Content-Range` para descargas reanudables.
- `ETag` basado en SHA-256 y caché pública para documentos descargables.
- `Cache-Control: private, no-store` en el endpoint administrativo.

El contador de descargas se incrementa después de entregar el archivo y nunca bloquea la respuesta.

## Verificación

```bash
# Compilar la solución
dotnet build paginaWeb.slnx

# Ejecutar todas las pruebas
dotnet test paginaWeb.slnx

# Validar la configuración de Docker
docker compose config

# Recompilar Tailwind si se modificaron estilos
cd paginaWeb
pnpm run build:css
```

## Rollback

El cambio es aditivo:

- La colección `documents` y `Article.DocumentIds` pueden permanecer sin afectar a versiones anteriores.
- Para desactivar la función, retirar el enlace de navegación y el controlador; los archivos y metadatos quedan disponibles para una reactivación posterior.
- Si se desea limpiar, eliminar los documentos desde el CMS para borrar también sus objetos de almacenamiento.
