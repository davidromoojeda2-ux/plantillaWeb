# Proposal

## Why

Actualmente, múltiples entidades, enums y DTOs residen agrupados en los mismos archivos de código fuente (por ejemplo, `Comment.cs` contiene `Comment`, `Visitor` y `CommentStatus`; `Article.cs` contiene `Article` y `ArticleStatus`; `ImageMetadata.cs` contiene `ImageVariant` e `ImageMetadata`; y cada archivo DTO agrupa tanto DTOs de entrada como de respuesta). Asimismo, las vistas principales (en particular `Articulo.cshtml` con cerca de 600 líneas de marcado e `Index.cshtml`) están sobrecargadas con bloques monolíticos para redes sociales, bio del autor, comentarios y artículos recomendados.

Esta refactorización modulariza la arquitectura de código separando cada entidad, DTO y mapeador en su propio archivo individual con responsabilidad única, y descompone las vistas saturadas en componentes y vistas parciales reutilizables, aumentando la claridad, mantenibilidad y facilidad de prueba.

## What Changes

- **Entidades de Dominio en Archivos Individuales**: Separar cada entidad, clase anidada y enum en su propio archivo `.cs` en `Domine/Entities` (`Comment.cs`, `Visitor.cs`, `CommentStatus.cs`, `Article.cs`, `ArticleStatus.cs`, `ImageMetadata.cs`, `ImageVariant.cs`).
- **DTOs Desacoplados por Archivo**: Cada DTO de comando/entrada y de consulta/respuesta tendrá su propio archivo dedicado en `Aplication/DTOs` (e.g., `ArticleDto.cs`, `ArticleResponseDto.cs`, `AuthorDto.cs`, `AdminLoginDto.cs`, `AdminUserDto.cs`, `CategoryDto.cs`, `CategoryResponseDto.cs`, `CommentSubmissionDto.cs`, `CommentResponseDto.cs`, `ImageUploadDto.cs`, `ImageVariantDto.cs`, `ImageMetadataResponseDto.cs`, `PageDto.cs`, `PageResponseDto.cs`).
- **Mapeadores Especializados en Archivos Individuales**: Mantener y afinar cada mapeador en su propio archivo (`ArticleMapper.cs`, `AuthorMapper.cs`, `CategoryMapper.cs`, `CommentMapper.cs`, `ImageMapper.cs`, `PageMapper.cs`) asegurando independencia funcional.
- **Modularización de Vistas Principales en Componentes**: Extraer secciones complejas y repetitivas de `Articulo.cshtml` e `Index.cshtml` a vistas parciales / componentes reutilizables (`_SocialShareBar.cshtml`, `_AuthorBioCard.cshtml`, `_CommentsSection.cshtml`, `_RecommendedArticles.cshtml`, `_ArticleCard.cshtml`) aliviando la saturación de las vistas principales.

## Capabilities

### New Capabilities
- `modular-view-components`: Descomposición y renderizado modular de vistas públicas a través de vistas parciales/componentes reutilizables para autor, comentarios, recomendados, tarjetas de artículo y botones de compartir.
- `domain-contracts`: Organización modular de entidades de dominio, DTOs de transferencia y mapeadores en archivos individuales con responsabilidad única.

### Modified Capabilities
<!-- None -->

## Impact

- **Código Afectado**:
  - `Domine/Entities/`: Creación de archivos individuales para tipos que estaban agrupados (`Visitor.cs`, `CommentStatus.cs`, `ArticleStatus.cs`, `ImageVariant.cs`).
  - `Aplication/DTOs/`: Creación de archivos individuales para cada DTO y respuesta (`ArticleResponseDto.cs`, `AdminUserDto.cs`, `CategoryResponseDto.cs`, `CommentResponseDto.cs`, `ImageVariantDto.cs`, `ImageMetadataResponseDto.cs`, `PageResponseDto.cs`, `AuthorDto.cs`).
  - `Aplication/Mappers/`: Asegurar aislamiento de cada mapper y adición de `AuthorMapper.cs`.
  - `paginaWeb/Views/`: Creación de carpeta de parciales/componentes y reducción significativa de líneas en `Articulo.cshtml` e `Index.cshtml`.
- **APIs y Base de Datos**: No hay cambios en la estructura de colecciones de MongoDB ni breaking changes en los contratos HTTP existentes; se preservan los namespaces y firmas públicas.
