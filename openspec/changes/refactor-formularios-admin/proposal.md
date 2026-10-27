# Proposal

## Why

La auditoría de duplicación con `jscpd` encontró 53 clones y 927 líneas duplicadas en Razor (15.37 %). La mayor parte está en los formularios `Create`/`Edit` de artículos, páginas informativas y categorías: markup, mensajes, scripts de portada y lógica de slug repetidos casi línea por línea. Esto obliga a mantener dos copias de cada cambio de UI y aumenta el riesgo de que creación y edición se comporten distinto.

## What Changes

- Extraer el formulario de artículos a `_ArticleForm.cshtml` y sus scripts a `_ArticleFormScripts.cshtml`.
- Extraer el formulario de páginas informativas a `_PageForm.cshtml` y sus scripts a `_PageFormScripts.cshtml`.
- Extraer el formulario de categorías a `_CategoryForm.cshtml` y sus scripts a `_CategoryFormScripts.cshtml`.
- Reducir `Create.cshtml` y `Edit.cshtml` a envoltorios que fijan el título, el modo de edición y renderizan los parciales.
- Usar `ViewData["IsEdit"]` para conservar las diferencias reales: acción del formulario, campos ocultos, textos de botón, slugs automáticos y mensajes de edición.

## Capabilities

### New Capabilities

<!-- None: es una refactorización de vistas sin cambios de comportamiento. -->

### Modified Capabilities

<!-- None. -->

## Impact

- **Código afectado**: `paginaWeb/Areas/Admin/Views/Articulos`, `Secciones` y `Categorias`.
- **Comportamiento**: se conservan las URLs, modelos, validaciones e interacciones existentes; los formularios de creación y edición comparten estructura.
- **Persistencia**: sin cambios.
- **Pruebas**: build, tests y validación de OpenSpec; se repetirá `jscpd` para comprobar la reducción de clones Razor.
