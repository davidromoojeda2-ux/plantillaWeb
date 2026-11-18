# Spec Delta

## Purpose

Gestiona el ciclo de vida de documentos descargables en el CMS —subida, validación, metadatos, almacenamiento, asociación a artículos y eliminación— y expone una descarga pública controlada para los documentos publicados.

## ADDED Requirements

### Requirement: Subida de documentos desde el CMS

El sistema SHALL permitir a un administrador autenticado subir un documento mediante un formulario multipart, indicando al menos el archivo y un nombre visible; opcionalmente podrá indicar descripción, artículo asociado y visibilidad pública.

#### Scenario: Subida exitosa de un PDF

- **WHEN** un administrador autenticado envía un archivo PDF dentro de los límites permitidos con un nombre visible válido
- **THEN** el sistema almacena el binario, registra los metadatos y muestra el documento en la biblioteca del CMS

#### Scenario: Subida sin archivo o sin nombre visible

- **WHEN** un administrador envía el formulario sin archivo o sin nombre visible
- **THEN** el sistema rechaza la operación, conserva el formulario y muestra un mensaje de validación sin crear registros ni objetos de almacenamiento

#### Scenario: Usuario no autenticado intenta subir

- **WHEN** una petición de subida llega sin sesión administrativa válida
- **THEN** el sistema redirige al inicio de sesión y no procesa el archivo

### Requirement: Validación de formato, tamaño y contenido

El sistema MUST validar en servidor la extensión, el tamaño y la firma real del archivo antes de almacenarlo, usando una lista configurable de formatos permitidos y rechazando tipos activos o no autorizados.

#### Scenario: Archivo con extensión permitida pero contenido distinto

- **WHEN** un administrador sube un archivo con extensión `.pdf` cuyo contenido no corresponde a la firma de un PDF
- **THEN** el sistema rechaza la subida con un mensaje que indica que el contenido no coincide con el formato declarado y no almacena el archivo

#### Scenario: Archivo que supera el tamaño máximo

- **WHEN** un administrador sube un archivo mayor al límite configurado
- **THEN** el sistema rechaza la subida antes de persistir el binario y muestra el tamaño máximo permitido

#### Scenario: Intento de subir un tipo activo

- **WHEN** un administrador intenta subir un archivo HTML, SVG, JavaScript o un ejecutable
- **THEN** el sistema rechaza la operación aunque la extensión haya sido manipulada y registra el intento como error de validación

### Requirement: Almacenamiento con prioridad S3/R2 y fallback local

El sistema SHALL almacenar cada documento mediante la abstracción de almacenamiento existente, priorizando S3/R2 cuando esté disponible y usando el almacenamiento local como fallback, y MUST registrar el proveedor efectivo junto con la clave del objeto.

#### Scenario: S3/R2 disponible durante la subida

- **WHEN** el servicio S3/R2 responde disponible al momento de subir un documento
- **THEN** el binario se guarda en S3/R2 y los metadatos indican ese proveedor y la clave generada

#### Scenario: S3/R2 no disponible durante la subida

- **WHEN** el servicio S3/R2 no responde o no está configurado
- **THEN** el binario se guarda en el almacenamiento local configurado y los metadatos indican `local`, sin fallar la operación

#### Scenario: Descarga posterior a un cambio de proveedor

- **WHEN** un documento fue almacenado en local y posteriormente S3/R2 vuelve a estar disponible
- **THEN** la descarga sigue resolviendo el proveedor registrado en los metadatos y sirve el archivo correcto

### Requirement: Claves y nombres de archivo seguros

El sistema MUST generar claves de almacenamiento no controlables por el usuario y MUST sanear el nombre original para evitar traversal de rutas, dobles extensiones y caracteres peligrosos.

#### Scenario: Nombre con caracteres especiales o rutas

- **WHEN** un administrador sube un archivo llamado `../../reporte final.pdf` o con caracteres no permitidos
- **THEN** el sistema conserva una versión saneada para mostrar y guarda el objeto bajo una clave generada dentro de `documents/` sin permitir salir del directorio base

#### Scenario: Colisión de nombres visibles

- **WHEN** se sube un documento con el mismo nombre visible que otro existente
- **THEN** el sistema lo permite si la clave es distinta y la biblioteca muestra ambos registros sin sobrescribir archivos

### Requirement: Biblioteca y administración de documentos en el CMS

El sistema SHALL ofrecer una pantalla administrativa para listar, buscar, filtrar y paginar documentos, así como editar sus metadatos y eliminarlos.

#### Scenario: Búsqueda y filtrado

- **WHEN** un administrador busca por nombre, descripción o artículo asociado y aplica un filtro de tipo o almacenamiento
- **THEN** la biblioteca muestra únicamente los documentos que coinciden y conserva la paginación

#### Scenario: Edición de metadatos

- **WHEN** un administrador cambia el nombre visible o la descripción de un documento
- **THEN** el sistema actualiza los metadatos sin volver a subir ni renombrar el objeto de almacenamiento

#### Scenario: Eliminación de un documento

- **WHEN** un administrador confirma la eliminación de un documento
- **THEN** el sistema elimina el objeto del proveedor correspondiente, borra el registro de metadatos y lo desvincula de cualquier artículo que lo referencie

### Requirement: Asociación de documentos a artículos

El sistema SHALL permitir asociar uno o varios documentos a un artículo y MUST respetar un límite configurable de documentos por artículo.

#### Scenario: Asociación desde el formulario del artículo

- **WHEN** un editor selecciona documentos de la biblioteca o sube uno nuevo desde el formulario de artículo
- **THEN** el artículo guarda los identificadores de los documentos asociados y la vista pública los muestra en la sección de adjuntos

#### Scenario: Límite de documentos por artículo

- **WHEN** un editor intenta asociar más documentos de los permitidos
- **THEN** el sistema rechaza el exceso con un mensaje claro y conserva las asociaciones ya válidas

#### Scenario: Eliminación de la asociación

- **WHEN** un editor quita un documento de la lista de adjuntos de un artículo
- **THEN** el documento deja de mostrarse en la vista pública de ese artículo, pero permanece disponible en la biblioteca general

### Requirement: Descarga pública controlada

El sistema MUST exponer un endpoint de descarga same-origin que sirva el documento con `Content-Disposition: attachment`, soporte de rangos HTTP y sin revelar rutas físicas ni claves internas.

#### Scenario: Descarga de un documento asociado a un artículo publicado

- **WHEN** un visitante solicita la descarga de un documento asociado a un artículo publicado
- **THEN** el sistema responde con el binario, el nombre visible saneado y el tipo de contenido correspondiente

#### Scenario: Documento asociado a un artículo en borrador

- **WHEN** un visitante intenta descargar un documento asociado únicamente a un artículo no publicado y el documento no está marcado como público
- **THEN** el sistema responde con acceso denegado o no encontrado sin servir el binario

#### Scenario: Reanudación de una descarga grande

- **WHEN** el cliente envía una solicitud con cabecera `Range`
- **THEN** el sistema responde con el fragmento solicitado y el código de estado parcial correspondiente

### Requirement: Metadatos y presentación de los documentos

El sistema SHALL almacenar y exponer los metadatos necesarios para identificar y presentar cada documento: nombre visible, nombre original, descripción, extensión, tipo de contenido, tamaño, hash, proveedor de almacenamiento, fecha de subida y contador de descargas.

#### Scenario: Presentación en la capa visual

- **WHEN** se renderiza la sección de documentos adjuntos de un artículo
- **THEN** cada documento muestra su nombre visible, tipo, tamaño legible y un enlace de descarga accesible

#### Scenario: Registro de descarga

- **WHEN** un visitante descarga un documento correctamente
- **THEN** el contador de descargas se incrementa sin bloquear ni retrasar la entrega del archivo

### Requirement: Configuración y operación segura

El sistema MUST leer de configuración los formatos permitidos, el tamaño máximo, el máximo de documentos por artículo y la ruta local de documentos, y MUST aplicar cabeceras de seguridad en las respuestas de descarga.

#### Scenario: Cambio de formatos permitidos sin recompilar

- **WHEN** un operador modifica la lista de extensiones permitidas en la configuración y reinicia la aplicación
- **THEN** las nuevas subidas aceptan o rechazan formatos según la lista actualizada

#### Scenario: Respuesta de descarga con cabeceras seguras

- **WHEN** el endpoint sirve cualquier documento
- **THEN** la respuesta incluye `X-Content-Type-Options: nosniff` y no permite interpretar el archivo como HTML o script en el origen del portal
