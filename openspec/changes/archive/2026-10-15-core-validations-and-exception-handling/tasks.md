# Tasks

## 1. Capa Dominio: Excepciones e Invariantes

- [x] 1.1 Crear `DomainException` y `DomainValidationException` en `Domine/Exceptions/` y verificar compilación de la capa Domine
- [x] 1.2 Implementar métodos de invariantes en `Domine/Entities/Article.cs` (`ValidarInvariantes()`, `Publicar()`, `MarcarBorrador()`) impidiendo estados inconsistentes
- [x] 1.3 Implementar validación de invariantes en `Domine/Entities/Category.cs` (`ValidarInvariantes()`, `AsignarPadre(string? parentId)`) impidiendo auto-referencias

## 2. Capa Aplicación: Excepciones Tipadas y Servicios

- [x] 2.1 Crear `NotFoundException` y `ConflictException` en `Aplication/Exceptions/`
- [x] 2.2 Refactorizar `ArticleService.cs` para invocar invariantes de dominio y lanzar `ConflictException` y `NotFoundException` en lugar de excepciones de sistema
- [x] 2.3 Refactorizar `CategoryService.cs` para invocar invariantes de dominio y lanzar `ConflictException` y `NotFoundException`
- [x] 2.4 Refactorizar `PageService` en `OtherServices.cs` para lanzar `NotFoundException` tipada

## 3. Capa Presentación: ViewModels MVC (Eliminación de ViewBag)

- [x] 3.1 Crear `CategoryFormViewModel` en `paginaWeb/Areas/Admin/Models/` agrupando los campos de la categoría y la lista fuertemente tipada de categorías padre
- [x] 3.2 Crear `ArticleFormViewModel` en `paginaWeb/Areas/Admin/Models/` agrupando los datos del artículo, lista de categorías, imágenes vinculadas y galería disponible

## 4. Capa Presentación: Orquestación en Controladores y Vistas

- [x] 4.1 Refactorizar `CategoriasController.cs` y vistas (`Create.cshtml`, `Edit.cshtml`) para usar `CategoryFormViewModel`, eliminar consultas previas a `SlugExistsAsync` y mapear excepciones tipadas a `ModelState`
- [x] 4.2 Refactorizar `ArticulosController.cs` y vistas para usar `ArticleFormViewModel`, eliminando el uso de `ViewBag` y sustituyendo `catch (Exception ex)` genérico por captura tipada
- [x] 4.3 Refactorizar `SeccionesController.cs` para reemplazar `catch (Exception ex)` genérico por manejo tipado de excepciones
- [x] 4.4 Refactorizar `ImagenesController.cs` para capturar excepciones específicas sin enmascarar caídas técnicas de infraestructura

## 5. Capa Presentación: Migración Sistemática a Tag Helpers

- [x] 5.1 Reemplazar enlaces internos hardcodeados en el panel administrativo (`_AdminLayout.cshtml`, `Articulos`, `Categorias`, `Secciones`, `Imagenes`, `Comentarios`, `Auth/Login`) por Tag Helpers nativos (`asp-area="Admin"`, `asp-controller`, `asp-action`, `asp-route-*`)
- [x] 5.2 Reemplazar enlaces internos hardcodeados en vistas públicas (`_Layout.cshtml`, `Navbar/Default.cshtml`, `_ArticleCard.cshtml`, `_RecommendedArticles.cshtml`, `Index.cshtml`, `Categoria.cshtml`, `Buscar.cshtml`, `Articulo.cshtml`, `Info.cshtml`) por Tag Helpers nativos (`asp-controller="Home"`, `asp-action`, `asp-route-*`)

## 6. Verificación y Calidad

- [x] 6.1 Ejecutar `dotnet build` en la solución y verificar 0 errores y 0 advertencias
- [x] 6.2 Validar la conformidad del cambio con OpenSpec mediante `openspec validate core-validations-and-exception-handling`
