# Proposal

## Why

El editor ya usa una estética Gutenberg, pero los bloques todavía resultan poco amigables: no tienen un contenedor claramente reconocible, el tipo de bloque no se identifica con icono y los controles laterales de la referencia no existen. Además, al aplicar negrita o cursiva el usuario no recibe confirmación visual inmediata del estado del formato.

La referencia adjunta muestra bloques con borde redondeado, controles laterales, toolbar con icono de tipo y botones de formato que deben reflejar el estado activo al instante.

## What Changes

- Dar a cada bloque un contenedor redondeado, con borde y fondo suaves.
- Añadir controles laterales: subir, agarrador de arrastre y bajar; además de una acción de eliminar en el borde derecho.
- Mostrar el icono y la etiqueta dinámica del tipo de bloque en la toolbar contextual.
- Implementar arrastre de bloques mediante el agarrador de seis puntos.
- Reflejar de inmediato el estado de negrita, cursiva y enlace en los botones de formato.
- Mantener la barra flotante de selección de texto y el contrato JSON existente.

## Capabilities

### New Capabilities

<!-- None: refinamiento visual y de interacción del editor existente. -->

### Modified Capabilities

<!-- None: no cambia el contrato de datos ni la persistencia. -->

## Impact

- **Código afectado**: `paginaWeb/wwwroot/js/content-blocks-editor.js` y `paginaWeb/Styles/tailwind.css`.
- **Persistencia**: sin cambios.
- **Pruebas**: `node --check`, `pnpm run build:css`, build, tests y verificación manual de formato, arrastre y guardado.
