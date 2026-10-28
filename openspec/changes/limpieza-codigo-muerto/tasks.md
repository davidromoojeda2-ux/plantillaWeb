# Tasks

## 1. Auditoría de código muerto

- [x] 1.1 Buscar declaraciones sin referencias en C# y JavaScript, distinguiendo rutas MVC invocadas desde JavaScript y miembros usados por serialización.
- [x] 1.2 Verificar que `ConsentCookies` sigue formando parte del documento persistido de `Visitor` y no eliminarlo.
- [x] 1.3 Verificar que `ToggleFeatured`, `GetAvailableImages`, `QuickCreate` y `CheckName` se invocan desde vistas o JavaScript.

## 2. Limpieza de miembros sin uso

- [x] 2.1 Eliminar `AuthorMapper.ApplyTo`, `AuthorMapper.ToAuthorDto(Article)`, `PageMapper.ToEditDto` y las dos sobrecargas de `CategoryMapper.ToEditDto`.
- [x] 2.2 Eliminar `Article.MarcarBorrador` y `ContentBlockViewHelpers.CalloutLabel`; hacer privado `ContentBlockViewHelpers.Slugify`.
- [x] 2.3 Eliminar `ImageUploadDto`, `StorageStatusDto.ActivePriorityStorage`, `S3Storage.IsConfigured`, los alias calculados de `CategoryPageViewModel`, `CategoryPageViewModel.Subcategory`, `CategoryBentoCardViewModel.ArticleCount`, `FeaturedBlockViewModel` y los parámetros opcionales no leídos de `HomePageViewModel`.
- [x] 2.4 Eliminar `ImagenesController.LinkArticle` y la cadena sin consumidores `ImageService.UpdateArticleIdAsync` / `IImageMetadataRepository.UpdateArticleIdAsync`.
- [x] 2.5 Eliminar las vistas parciales sin referencias `_ArticleCard.cshtml`, `_FeatureCard.cshtml`, `_FeaturedBlock.cshtml` y `_ValidationScriptsPartial.cshtml`.

## 3. Reducción de duplicación

- [x] 3.1 Centralizar en `ImageMapper` la URL de tarjeta y el diccionario de miniaturas.
- [x] 3.2 Reutilizar los helpers en `CategoryController`, `PublicacionesController`, `ArticulosController` y `DashboardController`.
- [x] 3.3 Reducir la duplicación de `ArticleMapper.ToEditDto(Article)` reutilizando `ToDto`.

## 4. Verificación

- [x] 4.1 Ejecutar `dotnet build paginaWeb.slnx` y confirmar cero errores.
- [x] 4.2 Ejecutar `dotnet test paginaWeb.slnx` y confirmar las 27 pruebas en verde.
- [x] 4.3 Ejecutar `pnpm run build:css` y `node --check` sobre el editor de bloques.
- [x] 4.4 Volver a ejecutar el detector de duplicados y revisar los clones restantes.
- [x] 4.5 Revisar `git status`, verificar que no quedan archivos temporales y registrar el commit de limpieza.
