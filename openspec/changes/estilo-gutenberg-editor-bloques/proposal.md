# Proposal

## Why

El editor de bloques del CMS funciona, pero cada bloque se presenta como una tarjeta independiente con una cabecera fija. Visualmente se aleja de Gutenberg: el toolbar no flota sobre el bloque seleccionado, no hay inserción entre bloques y el contenido editable parece un formulario en lugar de un documento.

La referencia visual es el editor de WordPress: bloques limpios sobre el lienzo, barra contextual flotante, tipografía editorial y un botón `+` entre bloques.

## What Changes

- Convertir cada bloque en una superficie documental sin tarjeta ni cabecera permanente.
- Mostrar un toolbar flotante al pasar el ratón, enfocar o seleccionar un bloque.
- Mantener en ese toolbar el selector de tipo, los formatos inline, mover arriba/abajo y eliminar.
- Añadir un inserter `+` entre bloques, con un menú de tipos de bloque.
- Dar a párrafos, encabezados y citas una tipografía y espaciado editoriales; la cita adopta el tratamiento visual de la referencia.
- Conservar el contrato JSON, la serialización, los límites, la sanitización y las rutas de subida existentes.

## Capabilities

### New Capabilities

<!-- None: es una refinación visual y de interacción del editor existente. -->

### Modified Capabilities

<!-- None: no cambia el contrato de datos ni el comportamiento de persistencia. -->

## Impact

- **Código afectado**: `paginaWeb/wwwroot/js/content-blocks-editor.js` y `paginaWeb/Styles/tailwind.css`.
- **Vistas**: sin cambios; los formularios siguen usando `[data-content-blocks-editor]`.
- **Persistencia**: sin cambios.
- **Pruebas**: `node --check`, `pnpm run build:css`, build y verificación manual en artículos y páginas.
