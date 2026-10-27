# Proposal

## Why

El cuerpo de los artículos y de las páginas informativas se almacena hoy como un único string HTML (`Content`), se copia sin validación estructural y se renderiza con `Html.Raw`. Esto impide validar el contenido en servidor, deja abierta la puerta a XSS almacenado y acopla la búsqueda, el tiempo de lectura, la detección de portada y el editor a un HTML opaco. Se necesita un modelo de contenido por bloques tipados, sanitizado en servidor y renderizado de forma semántica.

## What Changes

- **BREAKING**: reemplazar el campo `Content` de `Article` e `InformativePage` por una colección `Blocks` de bloques polimórficos tipados. Se actualizan DTOs, mappers, servicios, controladores, vistas y seeds.
- Incorporar una jerarquía de bloques para: párrafo, encabezado H2/H3, lista ordenada, lista con viñetas, cita, callout, imagen, tabla, código y separador.
- Sanitizar el HTML inline en servidor con `Ganss.Xss` y una lista blanca estricta. Añadir tipos de dominio para texto plano, URL segura y HTML sanitizado.
- Validar estructura, longitudes, cantidad de bloques, dimensiones de tablas y esquemas de URL; rechazar tipos de bloque desconocidos.
- Definir un contrato JSON (`BlocksJson`) para el editor, con deserialización estricta en servidor y errores asociados al formulario.
- Crear un editor administrativo nuevo y propio: hidratación y serialización JSON, formato inline, selector de tipo, reordenamiento, biblioteca de imágenes, tablas, callouts, código y separador.
- Renderizar el contenido con parciales Razor compartidos entre artículos y páginas informativas. Usar Tailwind para layout y estilos base; reservar CSS y JavaScript para efectos avanzados (índice de lectura, progreso, copia de código y tablas responsivas).
- Derivar de los bloques el texto plano de búsqueda, el tiempo de lectura y la primera imagen disponible para portada/Hero.
- Actualizar el índice de texto completo de MongoDB para apuntar al nuevo campo de búsqueda.
- Actualizar los seeds para construir bloques tipados. No se migran datos existentes: la base de datos es de prueba y puede reiniciarse.

## Capabilities

### New Capabilities

- `structured-content`: modelo de contenido por bloques tipados para artículos y páginas, con validación, sanitización, contrato JSON del editor, renderizado seguro y campos derivados.

### Modified Capabilities

<!-- Ninguna. La validación de invariantes de dominio sigue existiendo; solo cambia la representación del contenido. -->

## Impact

- **Domine**: `Article`, `InformativePage`, nuevos tipos de bloque y objetos de valor.
- **Aplication**: DTOs de contenido, mappers, servicios de artículo y página, interfaz de sanitización y validación.
- **Infrastructure**: implementación de `Ganss.Xss`, mapeo BSON polimórfico, índice de búsqueda, seeds y consultas de artículo.
- **paginaWeb**: ViewModels, controladores administrativos, vistas de creación/edición, parciales públicos, nuevo editor JavaScript y estilos.
- **Dependencias**: `HtmlSanitizer` (`Ganss.Xss`); `AngleSharp` solo si se usa para convertir los seeds existentes o para pruebas.
- **Datos**: cambio de esquema incompatible con documentos antiguos; al ser datos de prueba, se recrean los seeds.
- **Búsqueda**: el índice de texto completo se reconstruye sobre el campo de búsqueda derivado.
- **Seguridad**: se elimina el renderizado de HTML arbitrario y se sustituye por sanitización en escritura y renderizado controlado.
