# Tasks

## 1. Extraer formulario de artículos

- [x] 1.1 Crear `_ArticleForm.cshtml` con el formulario compartido y las diferencias de edición controladas por `ViewData["IsEdit"]`.
- [x] 1.2 Crear `_ArticleFormScripts.cshtml` con los scripts compartidos de editor, portada, taxonomía y destacado.
- [x] 1.3 Reducir `Articulos/Create.cshtml` y `Articulos/Edit.cshtml` a envoltorios sin duplicar markup ni JavaScript.
- [x] 1.4 Verificar que creación y edición conservan acción, campo `Id`, textos, slugs y carga de portada.

## 2. Extraer formularios de páginas y categorías

- [x] 2.1 Crear `Secciones/_PageForm.cshtml` y `_PageFormScripts.cshtml`, dejando `Create.cshtml` y `Edit.cshtml` como envoltorios.
- [x] 2.2 Crear `Categorias/_CategoryForm.cshtml` y `_CategoryFormScripts.cshtml`, dejando `Create.cshtml` y `Edit.cshtml` como envoltorios.
- [x] 2.3 Conservar los mensajes específicos de creación/edición y la generación automática de slug donde existía.

## 3. Verificación

- [x] 3.1 Ejecutar `dotnet build paginaWeb.slnx` y confirmar cero errores.
- [x] 3.2 Ejecutar `dotnet test paginaWeb.slnx` y confirmar las 27 pruebas en verde.
- [x] 3.3 Ejecutar `openspec validate --changes --strict`.
- [x] 3.4 Repetir `jscpd` en Razor y confirmar la reducción de clones en los formularios `Create`/`Edit`.
- [x] 3.5 Revisar `git status`, confirmar que no quedan archivos temporales y registrar el commit.
