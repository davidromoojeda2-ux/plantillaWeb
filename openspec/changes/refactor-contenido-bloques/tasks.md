# Tasks

## 1. Dependencias y base de pruebas

- [x] 1.1 Añadir el paquete `HtmlSanitizer` (`Ganss.Xss`) a `Infrastructure` y `AngleSharp` para conversión de seeds y pruebas; verificar con `dotnet restore` y `dotnet build`.
- [x] 1.2 Crear el proyecto de pruebas `tests/ContenidoBloques.Tests` con xUnit, referenciar `Domine`, `Aplication` e `Infrastructure`, agregarlo a `paginaWeb.slnx` y verificar que `dotnet test` ejecuta una prueba trivial.
- [x] 1.3 Añadir la sección `ContentSecurity` en `appsettings.json` (hosts de imagen permitidos y límites) y verificar que el binding de opciones compila y se puede leer en una prueba unitaria.

## 2. Modelo de dominio y objetos de valor

- [x] 2.1 Crear la jerarquía `ContentBlock` con los subtipos párrafo, encabezado, lista ordenada, lista con viñetas, cita, callout, imagen, tabla, código y separador; verificar compilación de `Domine`.
- [x] 2.2 Implementar `RichText`, `PlainText` y `SafeUrl` con validaciones de longitud y fábricas controladas; verificar con pruebas unitarias de valores válidos e inválidos.
- [x] 2.3 Reemplazar `Content` por `Blocks` y `SchemaVersion` en `Article` e `InformativePage`; actualizar `ValidarInvariantes`, `Publicar` y el cálculo de tiempo de lectura; verificar con pruebas de dominio.
- [x] 2.4 Añadir `SearchText` a `Article` y el método `BuildSearchText()` basado en el texto plano de los bloques; verificar con pruebas de concatenación y ausencia de HTML.
- [x] 2.5 Configurar el polimorfismo BSON de los bloques con discriminadores y tipos conocidos; verificar ida y vuelta con `BsonSerializer` sin depender de MongoDB.

## 3. Contrato DTO, sanitización y capa de aplicación

- [x] 3.1 Crear la jerarquía `ContentBlockDto` con polimorfismo JSON y el helper de deserialización estricta; verificar con pruebas de tipo desconocido, propiedad desconocida y JSON malformado.
- [x] 3.2 Definir `IHtmlSanitizer` en `Aplication` e implementar `GanssHtmlSanitizer` en `Infrastructure` con lista blanca de etiquetas, atributos y esquemas; verificar con un corpus de XSS.
- [x] 3.3 Implementar `ContentBodySanitizer` para convertir DTO → dominio, aplicar límites, validar URLs de imagen y producir `RichText`/`PlainText`; verificar con pruebas de límites y URLs peligrosas.
- [x] 3.4 Actualizar `ArticleDto`, `ArticleResponseDto`, `PageDto` y `PageResponseDto` para usar bloques; actualizar `ArticleMapper` y `PageMapper`; verificar con pruebas de mapeo y roundtrip.
- [x] 3.5 Inyectar `ContentBodySanitizer` en `ArticleService` y `PageService`, sanitizar en creación y edición, y calcular `SearchText` y tiempo de lectura; verificar con pruebas de servicio.
- [x] 3.6 Implementar la resolución de la primera imagen del cuerpo y adaptar la detección de Hero/portada a bloques; verificar con pruebas de artículo con y sin imagen.

## 4. Persistencia, seeds y búsqueda

- [x] 4.1 Actualizar `MongoDbIndexer` para eliminar el índice de texto anterior y crear el índice sobre `SearchText`; verificar que el arranque no falla con un índice previo.
- [x] 4.2 Implementar el conversor HTML → bloques para los seeds con AngleSharp; verificar con pruebas de `h2`, `p`, `ol`, `ul`, `callout`, `table`, `figure/img`, `blockquote`, `pre` y `hr`.
- [x] 4.3 Actualizar `SeedArticlesData` y el seed de la página de privacidad para construir bloques; verificar que el seed genera documentos con `Blocks` y `SearchText`.
- [x] 4.4 Adaptar `ArticleRepository.SearchAsync` y las consultas que dependían de `Content`; verificar la búsqueda contra MongoDB de prueba con un término presente solo en un bloque.

## 5. Renderizado público

- [x] 5.1 Crear el despachador `_ContentBlocks.cshtml` y las parciales por tipo de bloque; verificar el renderizado de cada tipo en la página pública (los 10 tipos se revisaron manualmente en navegador; no se añadió infraestructura de snapshots).
- [x] 5.2 Reemplazar `Html.Raw(Model.Content)` en `Articulo.cshtml` e `Info.cshtml` por el componente de bloques; verificar que un artículo y una página se renderizan sin HTML no confiable.
- [x] 5.3 Adaptar `article.js` para el índice lateral, la barra de progreso, las tablas responsivas y el copiado de código sobre los nuevos bloques; verificar en navegador.
- [x] 5.4 Añadir los estilos Tailwind y CSS necesarios para los bloques y el editor; ejecutar `pnpm run build:css` y verificar que las clases se generan.

## 6. Administración: ViewModels, controladores y editor

- [x] 6.1 Añadir `BlocksJson` a `ArticleFormViewModel`, crear `PageFormViewModel` y actualizar los controladores `ArticulosController` y `SeccionesController` con el helper de deserialización; verificar con pruebas de ModelState y roundtrip.
- [x] 6.2 Actualizar las vistas `Create.cshtml` y `Edit.cshtml` de artículos y secciones para usar el editor de bloques; verificar que el JSON se conserva tras un error de validación.
- [x] 6.3 Implementar el núcleo de `content-blocks-editor.js`: estado, hidratación, serialización, añadir, mover, eliminar y cambiar tipo; verificar con una prueba manual de creación y edición.
- [x] 6.4 Implementar la edición de párrafo, encabezado, listas, cita, callout, código y separador; verificar cada tipo en el navegador.
- [x] 6.5 Implementar el bloque de imagen con biblioteca de medios y URL permitida; verificar la selección, la previsualización y el guardado.
- [x] 6.6 Implementar el bloque de tabla con añadir/eliminar filas y columnas y aplicar los límites del servidor; verificar el guardado y la renderización.
- [x] 6.7 Añadir estilos Tailwind/CSS y accesibilidad al editor (foco, etiquetas, teclado); verificar navegación básica con teclado.
- [x] 6.8 Eliminar las referencias y el archivo `admin-editor.js` heredado; verificar que no quedan llamadas ni selectores huérfanos.
- [x] 6.9 Adaptar el listado administrativo y la extracción de portada/miniatura a bloques; verificar que las tarjetas muestran la imagen correcta.

## 7. Integración y verificación final

- [x] 7.1 Ejecutar `dotnet build` y `dotnet test` sobre la solución completa; verificar cero errores y todas las pruebas en verde.
- [x] 7.2 Ejecutar la aplicación con la base de datos de prueba, crear y editar un artículo y una página, y verificar el renderizado público, la búsqueda, el índice lateral, las tablas y las imágenes.
- [x] 7.3 Ejecutar el corpus XSS de extremo a extremo (entrada, persistencia y renderizado); verificar que no se ejecuta HTML no permitido.
- [x] 7.4 Revisar que no quedan referencias a `Article.Content`/`Page.Content`, `Html.Raw` sobre datos no confiables ni al editor anterior; verificar con búsqueda global.
- [x] 7.5 Registrar el commit de cierre en la rama `refactor/bloques-articulo` y verificar el estado limpio del worktree.

## Workflow follow-up

- Revisar los artefactos de OpenSpec con el usuario antes de aplicar.
- Archivar el cambio después de completar la implementación y la verificación.
