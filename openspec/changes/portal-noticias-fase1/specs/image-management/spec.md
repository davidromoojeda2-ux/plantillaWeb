# Spec Delta: image-management

## Purpose
Gestiona la subida de imágenes, su optimización a formato WebP con múltiples variantes de resolución y su almacenamiento seguro en Cloudflare R2.

## ADDED Requirements

### Requirement: Validación y subida de archivos de imagen
El sistema SHALL validar el tipo mime real y el tamaño máximo de 10 MB para archivos JPG, PNG y WebP antes de procesarlos.

#### Scenario: Subida de archivo válido
- **WHEN** un administrador sube una imagen JPG o PNG menor a 10 MB
- **THEN** el sistema acepta el archivo para procesamiento y generación de variantes

#### Scenario: Archivo no admitido o excedido en tamaño
- **WHEN** se intenta subir un archivo que no corresponde a una imagen válida o supera 10 MB
- **THEN** el sistema rechaza la carga notificando el motivo del fallo

### Requirement: Conversión automática a WebP y variantes responsivas
El sistema SHALL convertir toda imagen a formato WebP generando tres variantes de ancho (400 px, 800 px y 1600 px) conservando la proporción original.

#### Scenario: Generación exitosa de variantes
- **WHEN** una imagen es procesada por el servicio
- **THEN** el sistema produce las 3 versiones en WebP optimizadas y registra sus dimensiones y pesos

### Requirement: Almacenamiento en Cloudflare R2
El sistema SHALL persistir las variantes procesadas en el bucket de Cloudflare R2 utilizando cabeceras de caché de larga duración.

#### Scenario: Carga a Cloudflare R2
- **WHEN** las variantes WebP son generadas
- **THEN** el sistema las transfiere al almacenamiento R2 asignando las claves únicas correspondientes

### Requirement: Catálogo y eliminación de imágenes
El sistema SHALL mantener en la colección de imágenes los metadatos y texto alternativo, advirtiendo al usuario si una imagen a eliminar se encuentra en uso.

#### Scenario: Eliminación de imagen en uso
- **WHEN** un administrador intenta eliminar una imagen referenciada por un artículo publicado
- **THEN** el sistema advierte que la imagen está en uso antes de proceder con el borrado
