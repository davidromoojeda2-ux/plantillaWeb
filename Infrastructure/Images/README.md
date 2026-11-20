# Procesamiento de imágenes

La aplicación usa **Magick.NET-Q8-x64** como motor de imágenes, pero el resto del
código no depende de ImageMagick. La integración sigue estos patrones:

## Patrones

- **Strategy**: `Aplication.Interfaces.IImageProcessor` define el contrato de
  procesamiento.
- **Adapter**: `Infrastructure.Images.MagickImageProcessor` adapta Magick.NET a
  ese contrato. Toda la API de ImageMagick queda contenida en esta clase.
- **Facade**: `Infrastructure.Storage.ImageService` coordina el procesamiento y
  el almacenamiento (S3/R2/local). No conoce tipos de ImageMagick.
- **Parameter Object**: `Aplication.DTOs.Images.ImageProcessingOptions` agrupa
  anchos, calidades y formato de salida.

```
ImageService (Facade)
    └── IImageProcessor (Strategy)
            └── MagickImageProcessor (Adapter)
                    └── Magick.NET
```

## Comportamiento

- Acepta JPEG, PNG, WebP, GIF, BMP, TIFF, TGA, PBM, PNM, QOI, AVIF y HEIC.
- Genera variantes WebP a 400, 800 y 1600 px con la calidad configurada.
- Corrige la orientación EXIF (`AutoOrient`).
- Elimina EXIF, XMP, IPTC, ICC y comentarios (`Strip`).
- Conserva animaciones (GIF/WebP) usando `MagickImageCollection`.
- Limita memoria, dimensiones y hilos para reducir el impacto de archivos
  manipulados.

## Cambiar de motor

1. Implementar `IImageProcessor` en Infrastructure.
2. Registrar la implementación en `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IImageProcessor, NuevoProcesador>();
   ```
3. No es necesario tocar `ImageService`, controladores ni vistas.

## Publicación

El `Dockerfile` publica con `-r linux-x64` para incluir solo los binarios nativos
de Linux y evitar copiar las variantes de otros sistemas operativos.

## Pruebas

`tests/ImageOptimization.Tests` valida:

- generación de variantes WebP,
- dimensiones,
- eliminación de metadatos,
- conservación de animaciones,
- rechazo de formatos no permitidos,
- soporte nativo de AVIF/HEIC,
- generación opcional de AVIF.
