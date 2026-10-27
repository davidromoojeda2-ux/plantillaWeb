# Proposal

## Why

Después del refactor de contenido a bloques tipados se hizo una auditoría de código muerto y duplicado. La auditoría encontró miembros y servicios sin consumidores, un DTO sin referencias y pequeñas duplicaciones en la resolución de imágenes de tarjetas. También se revisaron las rutas MVC invocadas desde JavaScript y los campos persistidos en MongoDB para no eliminar código que no aparece por búsqueda estática pero sigue siendo necesario en tiempo de ejecución.

## What Changes

- Eliminar mapeadores y miembros de dominio sin uso: `AuthorMapper.ApplyTo`, la sobrecarga `ToAuthorDto(Article)`, `PageMapper.ToEditDto`, las dos sobrecargas de `CategoryMapper.ToEditDto` y `Article.MarcarBorrador`.
- Eliminar infraestructura sin consumidores: la acción `ImagenesController.LinkArticle`, `ImageService.UpdateArticleIdAsync` y su método de repositorio.
- Eliminar DTOs y propiedades calculadas sin uso: `ImageUploadDto`, `StorageStatusDto.ActivePriorityStorage`, `S3Storage.IsConfigured`, los alias de `CategoryPageViewModel`, `CategoryPageViewModel.Subcategory`, `CategoryBentoCardViewModel.ArticleCount`, `FeaturedBlockViewModel` y los parámetros opcionales no leídos de `HomePageViewModel`.
- Eliminar el helper de presentación `ContentBlockViewHelpers.CalloutLabel` y hacer privado `Slugify`, que solo se usa internamente.
- Eliminar las vistas parciales sin referencias `_ArticleCard.cshtml`, `_FeatureCard.cshtml`, `_FeaturedBlock.cshtml` y `_ValidationScriptsPartial.cshtml`.
- Reducir duplicación en la resolución de URLs de tarjetas y en los diccionarios de miniaturas de los listados administrativos mediante helpers de `ImageMapper`.
- Mantener `Visitor.ConsentCookies` porque forma parte de los documentos persistidos de MongoDB, aunque el código actual no lo consulte.
- Mantener las rutas MVC detectadas solo por nombre en JavaScript (`ToggleFeatured`, `GetAvailableImages`, `QuickCreate`, `CheckName`).

## Capabilities

### New Capabilities

<!-- None: es una limpieza de mantenimiento sin cambios funcionales. -->

### Modified Capabilities

<!-- None. -->

## Impact

- **Código afectado**: `Aplication`, `Domine`, `Infrastructure`, controladores y view models de `paginaWeb`.
- **Comportamiento**: no cambia el contrato público de las páginas ni los flujos de administración; se eliminan solo miembros y endpoints sin consumidores.
- **Persistencia**: no se modifica el esquema de MongoDB. `ConsentCookies` se conserva por compatibilidad con documentos existentes.
- **Duplicación restante**: las copias entre `ArticleDto`, `ArticleResponseDto` y `ArticleFormViewModel` son contratos de capas distintas; los formularios `Create`/`Edit` comparten estructura visual y se revisaron como duplicación estructural, no como código muerto.
