# POC: ImageSharp vs Magick.NET

Banco de pruebas para comparar el pipeline actual de imágenes (ImageSharp 3.1.12)
con una alternativa basada en Magick.NET 15.1.0.

No forma parte de la aplicación ni del `paginaWeb.slnx`: es una herramienta de
evaluación aislada.

## Qué mide

- Generación de variantes WebP a 400, 800 y 1600 px con calidad 80.
- Generación de variantes AVIF a las mismas anchuras con calidad 60.
- Peso final de cada archivo y tiempo de codificación.
- Diferencia SSIM/PSNR para la variante de 800 px (SSIM-diff: menor es mejor).
- Cobertura de entrada: JPEG, PNG, WebP, TIFF, AVIF y HEIC.

## Cómo se ejecuta

```powershell
dotnet run --project tools/ImageBench -c Release
```

Sin argumentos usa las imágenes reales de
`paginaWeb/wwwroot/uploads/images`. También se puede indicar un directorio:

```powershell
dotnet run --project tools/ImageBench -c Release -- "C:\ruta\a\imagenes" "C:\ruta\salida"
```

La primera vez genera en `tools/ImageBench/samples` variantes de formato a partir
de una imagen real:

- `sample.jpg`, `sample.png`, `sample.tiff`, `sample.avif`.
- Si existe `sample.heic` en esa carpeta, también se incluye.

`samples/` y `out/` están en `.gitignore`; no se versionan binarios.

## Resultados de la ejecución

Entorno: Windows, .NET 10, ImageSharp 3.1.12, Magick.NET-Q8-AnyCPU 15.1.0.
Entradas: 5 WebP reales de 1600x900 y 5 muestras generadas (JPEG, PNG, TIFF,
AVIF y HEIC).

| Biblioteca / formato | Variantes | Peso total | Tiempo total |
|---|---:|---:|---:|
| ImageSharp WebP (configuración actual) | 24 | 15,84 MB | 10.435 ms |
| ImageSharp WebP (lossy explícito) | 24 | 2,79 MB | 4.783 ms |
| Magick.NET WebP | 29 | 3,30 MB | 2.465 ms |
| Magick.NET AVIF | 29 | 2,16 MB | 8.528 ms |

Comparaciones:

- **ImageSharp lossy vs actual:** −82,4 % de peso y −54,2 % de tiempo.
- **Magick.NET WebP vs ImageSharp actual:** −79,1 % de peso y −76,4 % de tiempo.
- **Comparación justa WebP** (24 variantes con los mismos inputs y anchos):
  Magick.NET 2,84 MB / 2.086 ms vs ImageSharp lossy 2,79 MB / 4.783 ms.
  Es decir, **+1,8 % de peso y −56,4 % de tiempo** para Magick.NET.
- **AVIF vs WebP lossy** en las mismas 24 variantes:
  AVIF 1,75 MB / 7.203 ms vs WebP 2,66 MB / 4.783 ms.
  Es decir, **−34,1 % de peso y +50,6 % de tiempo**.

## Hallazgos

1. **El pipeline actual está generando WebP muy pesados.** Las imágenes de
   `wwwroot/uploads/images` pesan 1,6–2,1 MB a 1600x900. En la POC, ImageSharp
   con la configuración actual produce prácticamente el mismo peso; al forzar
   `WebpFileFormatType.Lossy` el peso cae un ~82 %.
   - Antes de cambiar de proveedor, merece la pena revisar el encoder actual:
     ```csharp
     new WebpEncoder
     {
         Quality = quality,
         FileFormat = WebpFileFormatType.Lossy
     }
     ```
   - Es el cambio de menor esfuerzo y probablemente el de mayor impacto inmediato.

2. **Magick.NET es viable y cubre más formatos.** Procesa AVIF y HEIC, que
   ImageSharp 3.1.12 no puede abrir (la POC lo confirma). Además es ~56 % más
   rápido que ImageSharp lossy generando WebP, con solo ~1,8 % más de peso.

3. **AVIF aporta una reducción adicional.** Con Magick.NET, AVIF es ~34 % más
   ligero que WebP lossy, a costa de ~50 % más de tiempo de codificación.
   Para un CMS conviene pre-generar y cachear, no codificar en cada petición.

4. **Tamaño de dependencias.** Medido sobre los paquetes NuGet:
   - ImageSharp 3.1.12: ~7 MB extraídos (sin nativos).
   - Magick.NET-Q8-x64: paquete de 42 MB, ~104 MB extraídos (76 MB de `.so`).
   - NetVips.Native.linux-x64: ~17,6 MB extraídos.
   - SkiaSharp NativeAssets Linux: paquete de ~37,7 MB.
   - No se pudo medir el tamaño final de la imagen Docker porque Docker no está
     disponible en esta máquina.

## Limitaciones

- SSIM-diff se calcula solo para 800 px y es una diferencia (menor es mejor),
  no una similitud. No sustituye a una revisión visual.
- Las imágenes WebP de entrada ya están comprimidas; los samples JPEG/PNG/TIFF
  permiten comparar el camino de decodificación real.
- La POC no mide memoria de forma fiable ni el tamaño de la imagen Docker.
- AVIF/HEIC dependen de los delegados nativos incluidos en Magick.NET; hay que
  verificar la build concreta en el contenedor Linux.

## Conclusión

- Si el objetivo es **optimizar el WebP actual**, primero corregir el modo del
  encoder de ImageSharp (`Lossy`). Es un cambio pequeño y de gran impacto.
- Si el objetivo es **soportar AVIF, HEIC y otros formatos**, **Magick.NET** es
  la alternativa más completa y ya está validada en esta POC.
- Si el tamaño de la imagen Docker es crítico y no se necesita HEIC, **NetVips**
  merece una segunda POC por su menor huella y rendimiento.
