# Design

## Context

Ver `proposal.md` para la motivación y `specs/structured-content/spec.md` para el contrato de comportamiento.

El proyecto es una solución .NET 10 con capas `Domine`, `Aplication`, `Infrastructure` y `paginaWeb`, persistencia MongoDB, vistas Razor y Tailwind CSS. Hoy el cuerpo editorial es un string HTML:

- `Article.Content` e `InformativePage.Content` se copian sin validación estructural (`ArticleMapper`, `PageMapper`).
- El editor administrativo (`wwwroot/js/admin-editor.js`) ya maneja 11 tipos de bloque en el cliente, pero serializa a HTML.
- La vista pública usa `Html.Raw` sobre el string (`Articulo.cshtml`, `Info.cshtml`).
- La búsqueda de MongoDB indexa `Content`; el tiempo de lectura se calcula con una regex HTML; la portada/Hero se detecta con `Contains("<img")` y expresiones regulares.

La base de datos es de prueba, por lo que no se requiere migración de contenido legado ni compatibilidad con documentos antiguos.

## Goals / Non-Goals

**Goals:**

- Un modelo de bloques compartido por artículos y páginas informativas.
- Un contrato JSON estricto y tipado entre el editor y el servidor.
- Sanitización en servidor y renderizado Razor seguro.
- Un editor administrativo nuevo, propio y reutilizable.
- Campos derivados (búsqueda, tiempo de lectura, primera imagen) calculados desde los bloques.
- Una base de pruebas automatizadas para las reglas de seguridad y el contrato.

**Non-Goals:**

- Migrar artículos existentes de MongoDB.
- Mantener `Content` como campo de compatibilidad o fallback.
- Introducir un editor de terceros o un pipeline de build para JavaScript.
- Cambiar la autenticación, los comentarios, las categorías o el almacenamiento de imágenes.
- Implementar colaboración en tiempo real, historial de versiones o autosave.

## Decisions

### 1. Jerarquía de bloques compartida y sellada

**Decisión**: crear una jerarquía de dominio `ContentBlock` con subtipos sellados para los 10 tipos editoriales: párrafo, encabezado (H2/H3), lista ordenada, lista con viñetas, cita, callout, imagen, tabla, código y separador.

Cada subtipo expone solo los campos de su tipo. La lista se guarda como `List<ContentBlock>` en `Article` e `InformativePage`, con un `SchemaVersion` explícito.

**Razón**: el editor ya usa esa taxonomía; reutilizarla reduce el riesgo de diseño y permite que ambas entidades compartan renderizador y validador.

**Alternativas descartadas**:

- Un `string` con HTML tipado solo por convención: no aporta validación real.
- Una unión plana `{ type, data: object }`: obliga a validar manualmente cada campo y pierde seguridad de tipos.
- Tipos separados para artículos y páginas: duplicaría el modelo y el renderizado sin beneficio.

### 2. Dos representaciones: DTO de entrada y bloque de dominio

**Decisión**: separar el contrato de entrada/salida (`ContentBlockDto`, con JSON polimórfico) del modelo persistido (`ContentBlock`, con BSON polimórfico).

- `ContentBlockDto` se usa para el JSON del editor y para las respuestas.
- `ContentBlock` es el modelo de dominio ya sanitizado y validado.
- `ContentBodySanitizer` convierte DTO → dominio.
- `ContentBlockMapper` convierte dominio → DTO de respuesta.

**Razón**: el DTO de entrada contiene strings sin confiar; el dominio solo debe contener valores ya validados. Mezclar ambos permitiría que un bloque "de dominio" contenga HTML sin sanear.

**Alternativas descartadas**:

- Usar el mismo tipo en todas las capas: mezcla niveles de confianza y acopla el contrato HTTP a los atributos BSON.
- Persistir el DTO directamente: impide garantizar que lo almacenado pasó por el sanitizador.

### 3. Objetos de valor para texto enriquecido, texto plano y URL

**Decisión**: introducir en `Domine/ValueObjects`:

- `RichText`: contiene `Html` ya sanitizado y `Text` plano derivado. Su fábrica es interna y solo la capa de aplicación puede construirla.
- `PlainText`: texto sin HTML, con validación de longitud.
- `SafeUrl`: URL con esquema permitido; la política de hosts se aplica en la capa de aplicación.

**Razón**: el renderizador necesita distinguir entre HTML confiable y texto que debe codificarse. Un `string` no comunica esa distinción y permite que un dato no confiable llegue a `Html.Raw`.

**Alternativas descartadas**:

- Usar `string` en todos los campos: simple, pero no expresa la frontera de confianza.
- Usar `HtmlString` de ASP.NET Core en el dominio: acopla el dominio a la capa web.

### 4. Sanitización con `Ganss.Xss` y lista blanca explícita

**Decisión**: definir `IHtmlSanitizer` en `Aplication` e implementarlo con `Ganss.Xss.HtmlSanitizer` en `Infrastructure`. `ContentBodySanitizer` aplica la política por tipo de bloque:

- Etiquetas inline permitidas: `strong`, `b`, `em`, `i`, `u`, `s`, `a`, `code`, `br`.
- Atributos permitidos: `a[href,title,rel,target]`; sin `class`, `style`, `id` ni eventos.
- Esquemas de URL: `https`, `http`, `mailto`; se rechazan `javascript:`, `data:`, `vbscript:`.
- Enlaces externos: se fuerza `rel="noopener noreferrer"` y `target="_blank"`.
- Campos planos: `alt`, `caption`, `credit`, `cite`, celdas de tabla y código nunca se interpretan como HTML.
- Límites: máximo de bloques, longitud por bloque, filas/columnas de tabla y tamaño total del JSON.

**Razón**: el HTML inline es el único punto donde se conserva formato enriquecido. Una lista blanca cerrada y probada reduce la superficie de ataque sin perder negritas, cursivas ni enlaces.

**Alternativas descartadas**:

- Sanitizar el HTML completo del cuerpo: la estructura de bloques se perdería y la política sería más difícil de razonar.
- Confiar en el editor: el cliente es siempre no confiable.
- Usar solo texto plano: seguro, pero incompatible con el requisito de formato inline.

### 5. Imágenes por referencia o URL permitida

**Decisión**: el bloque de imagen admite `ImageId` de la biblioteca de medios o una URL externa. La resolución se hace en servidor:

- Si hay `ImageId`, se resuelve la variante adecuada desde `ImageService`.
- Si hay URL, debe ser relativa al sitio o `https` con host en `ContentSecurity:AllowedImageHosts`.
- Se rechazan `data:`, `javascript:`, `file:` y hosts no permitidos.
- `alt`, `caption` y `credit` son `PlainText`.

**Razón**: la biblioteca de medios es la vía principal y segura; la URL externa se conserva para compatibilidad editorial, pero con allowlist configurable.

**Alternativas descartadas**:

- Permitir cualquier URL: facilita tracking, contenido mágico y referencias rotas.
- Permitir solo `ImageId`: obliga a subir todas las imágenes, incluso las de agencias.

### 6. Persistencia BSON polimórfica y búsqueda derivada

**Decisión**: almacenar `Blocks` como arreglo de documentos embebidos con discriminador BSON (`[BsonDiscriminator]` / `[BsonKnownTypes]`). Añadir `SchemaVersion` y `SearchText`.

- `SearchText` se calcula desde el texto plano de los bloques.
- `ReadingTimeMinutes` se calcula desde `SearchText`.
- La primera imagen se obtiene de los bloques al mapear la respuesta; no se duplica en base de datos.
- El índice de texto completo se reemplaza por uno sobre `SearchText`.

**Razón**: MongoDB soporta documentos embebidos y el esquema actual ya tolera campos nuevos. Un campo de búsqueda denormalizado evita indexar estructuras heterogéneas y mantiene el rendimiento.

**Alternativas descartadas**:

- Indexar `Blocks.text` directamente: los bloques tienen formas distintas y el índice se vuelve frágil.
- Guardar HTML plano como `Content` solo para búsqueda: reintroduce el problema de seguridad y la doble fuente de verdad.
- Una colección separada de bloques: añade joins manuales y complejidad sin necesidad.

### 7. Renderizado con parciales Razor compartidos

**Decisión**: crear `Views/Shared/Partials/ContentBlocks/_ContentBlocks.cshtml` como despachador y una parcial por tipo. Artículos y páginas usan el mismo componente.

- Texto plano: se renderiza con Razor (codificación por defecto).
- HTML inline: se emite con `Html.Raw` únicamente desde `RichText.Html` sanitizado.
- Imágenes: `<figure>` + `<figcaption>`, con `loading="lazy"` y dimensiones cuando estén disponibles.
- Tablas: contenedor `.table-responsive-wrapper` y tabla semántica.
- Callouts: variantes `info`, `tip`, `warning`, `success` con las clases existentes.
- Encabezados: `id` determinista para el índice lateral.
- El JavaScript existente de progreso, índice, copia de código y tablas responsivas se adapta a los nuevos bloques.

**Razón**: el renderizado en servidor preserva SEO y evita depender de JavaScript para el contenido. Reutilizar las clases de `Styles/tailwind.css` mantiene la paridad visual.

**Alternativas descartadas**:

- Renderizar en cliente desde JSON: peor SEO, más superficie XSS y más dependencia de JS.
- Un ViewComponent por bloque: más ceremonia y ciclo de vida para un renderizado puramente de presentación.

### 8. Editor nuevo propio y contrato JSON

**Decisión**: crear `wwwroot/js/content-blocks-editor.js`, un editor propio sin dependencias, inicializado por `data-*` y reutilizable en artículos y páginas.

- Estado interno: arreglo de bloques con la misma forma que `ContentBlockDto`.
- Serialización: `JSON.stringify` a un campo oculto `BlocksJson` en cada cambio y antes de enviar.
- Hidratación: lectura del JSON almacenado; si está vacío, un párrafo inicial.
- Funciones: añadir, eliminar, mover, cambiar tipo, formato inline (negrita, cursiva, enlace), imagen (biblioteca o URL), tabla (filas/columnas), callout (variante), cita, código y separador.
- Seguridad en cliente: construir el DOM con APIs y `textContent`; no interpolar datos de usuario en `innerHTML`.
- Errores de servidor: conservar `BlocksJson` en el ViewModel y mostrar el error asociado al cuerpo.

**Razón**: el editor actual está acoplado a HTML y a un DOM frágil. Un editor nuevo con el contrato JSON como fuente de verdad evita conversiones inversas y permite validar el mismo modelo en cliente y servidor.

**Alternativas descartadas**:

- Adaptar el editor actual: es posible, pero mantiene la deuda de parseo/serialización HTML y el riesgo de regresión.
- Usar TipTap/Editor.js: añade dependencias, build y una curva de integración mayor para el alcance actual.

### 9. ViewModels de administración y binder JSON

**Decisión**:

- `ArticleFormViewModel` y un nuevo `PageFormViewModel` exponen `BlocksJson` y los metadatos del formulario.
- Un helper `ContentBlockJson` deserializa el JSON con opciones estrictas, valida y añade errores a `ModelState`.
- Los controladores asignan los bloques al DTO de aplicación y delegan en los servicios.
- `PageDto` deja de usarse directamente como ViewModel.

**Razón**: mantiene la separación entre presentación y aplicación que exige la especificación de validación existente, y permite conservar el JSON enviado cuando hay errores.

### 10. Pruebas desde el inicio

**Decisión**: crear un proyecto de pruebas xUnit para:

- Sanitización: corpus de XSS, esquemas peligrosos, atributos no permitidos.
- Validación: tipos desconocidos, límites, campos obligatorios.
- Contrato JSON: polimorfismo, propiedades desconocidas, JSON malformado.
- Conversión de seeds HTML → bloques.
- Renderizado: snapshots de parciales con AngleSharp.

**Razón**: el proyecto no tiene pruebas y este cambio toca la frontera de seguridad. La red de seguridad debe existir antes de modificar la vista pública.

## Risks / Trade-offs

- **[Riesgo] El editor nuevo es la pieza más costosa y con más interacción.** → Mitigación: implementarlo por incrementos, empezar por los tipos usados en los seeds y cubrir cada tipo con una prueba manual y una tarea de verificación.
- **[Riesgo] La lista blanca del sanitizador puede quedarse corta o ser demasiado permisiva.** → Mitigación: allowlist cerrada, pruebas con corpus XSS y revisión de los tags realmente usados por los seeds.
- **[Riesgo] El polimorfismo BSON falla si aparece un discriminador desconocido.** → Mitigación: validar en escritura, versión de esquema y, si se detecta un bloque inválido al leer, registrar el error y no renderizar contenido no confiable.
- **[Riesgo] El cambio de índice de texto puede fallar si el índice anterior sigue existiendo.** → Mitigación: eliminar condicionalmente `tx_articles_fulltext` antes de crear el nuevo índice sobre `SearchText`.
- **[Riesgo] La conversión de seeds HTML → bloques puede perder estructuras raras.** → Mitigación: el conversor cubre los tipos usados por los seeds; los nodos desconocidos se convierten en párrafo y se revisan visualmente.
- **[Riesgo] Tailwind no detecta clases generadas dinámicamente desde JavaScript.** → Mitigación: usar clases estáticas en las plantillas y reservar CSS propio para estados dinámicos.
- **[Riesgo] El renderizado público cambia y afecta el índice lateral, las tablas o el copiado de código.** → Mitigación: conservar las clases y etiquetas actuales, y probar artículos y páginas reales.
- **[Riesgo] La eliminación de `Content` rompe consumidores ocultos (regex de portada, Hero, listados).** → Mitigación: buscar todas las referencias a `Content` y sustituirlas por bloques o campos derivados antes de borrar el campo.

## Migration Plan

1. Detener la aplicación y respaldar la base de datos de prueba (opcional).
2. Implementar el modelo, la sanitización, la persistencia y el renderizado.
3. Eliminar `Content` de entidades, DTOs, mappers, servicios y vistas; actualizar seeds.
4. Al arrancar, eliminar el índice de texto anterior si existe y crear el índice sobre `SearchText`.
5. Limpiar las colecciones `articles` y `pages` de prueba y dejar que los seeds generen documentos con bloques.
6. Validar compilación, ejecutar pruebas y revisar artículos y páginas en el navegador.
7. Rollback: volver a la rama anterior y restaurar el respaldo de la base de datos de prueba.

## Open Questions

Ninguna pendiente. El alcance (artículos y páginas), el formato inline sanitizado, la estrategia de imágenes y la eliminación de `Content` fueron confirmados por el usuario.
