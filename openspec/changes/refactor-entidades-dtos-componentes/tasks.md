# Tasks

## 1. Desacoplamiento de Entidades de Dominio (Domine/Entities/)

- [x] 1.1 Extraer el enum `CommentStatus` de `Comment.cs` a su propio archivo `Domine/Entities/CommentStatus.cs` y verificar compilación de `Domine.csproj`
- [x] 1.2 Extraer la clase `Visitor` de `Comment.cs` a su propio archivo `Domine/Entities/Visitor.cs` dejando únicamente `Comment` en `Comment.cs` y verificar compilación de `Domine.csproj`
- [x] 1.3 Extraer el enum `ArticleStatus` de `Article.cs` a su propio archivo `Domine/Entities/ArticleStatus.cs` dejando únicamente `Article` en `Article.cs` y verificar compilación de `Domine.csproj`
- [x] 1.4 Extraer la clase `ImageVariant` de `ImageMetadata.cs` a su propio archivo `Domine/Entities/ImageVariant.cs` dejando únicamente `ImageMetadata` en `ImageMetadata.cs` y verificar compilación de `Domine.csproj`

## 2. Desacoplamiento de DTOs en Archivos Individuales (Aplication/DTOs/)

- [x] 2.1 Crear `AuthorDto.cs` en `Aplication/DTOs/` para representar el perfil estructurado del autor y verificar su disponibilidad
- [x] 2.2 Separar `ArticleResponseDto` de `ArticleDto.cs` a su propio archivo `ArticleResponseDto.cs` dejando únicamente `ArticleDto` en `ArticleDto.cs`
- [x] 2.3 Separar `AdminUserDto` de `AuthDto.cs` a su propio archivo `AdminUserDto.cs` y aislar `AdminLoginDto.cs` para el DTO de inicio de sesión
- [x] 2.4 Separar `CategoryResponseDto` de `CategoryDto.cs` a su propio archivo `CategoryResponseDto.cs` dejando únicamente `CategoryDto` en `CategoryDto.cs`
- [x] 2.5 Separar `CommentResponseDto` de `CommentDto.cs` a su propio archivo `CommentResponseDto.cs` y aislar `CommentSubmissionDto.cs` para el envío de comentarios
- [x] 2.6 Separar `ImageVariantDto` e `ImageMetadataResponseDto` de `ImageDto.cs` a sus propios archivos `ImageVariantDto.cs` e `ImageMetadataResponseDto.cs`, manteniendo `ImageUploadDto.cs`
- [x] 2.7 Separar `PageResponseDto` de `PageDto.cs` a su propio archivo `PageResponseDto.cs` dejando únicamente `PageDto` en `PageDto.cs`
- [x] 2.8 Verificar compilación limpia de la capa `Aplication.csproj` con todos los DTOs en archivos individuales

## 3. Especialización de Mapeadores en Archivos Individuales (Aplication/Mappers/)

- [x] 3.1 Implementar `AuthorMapper.cs` en `Aplication/Mappers/` para convertir entre datos de autor en `Article`/`ArticleResponseDto` y `AuthorDto`
- [x] 3.2 Refactorizar y verificar aislamiento de `ArticleMapper.cs`, `CategoryMapper.cs`, `CommentMapper.cs`, `ImageMapper.cs`, `PageMapper.cs` y `AuthMapper.cs`
- [x] 3.3 Verificar compilación de `Aplication.csproj` e `Infrastructure.csproj` asegurando que todos los servicios consuman los nuevos mappers y DTOs

## 4. Creación de Componentes de Vista Modulares (Views/Shared/Partials/)

- [x] 4.1 Crear la vista parcial `_SocialShareBar.cshtml` en `paginaWeb/Views/Shared/Partials/` conteniendo los botones interactivos de redes y copiado de enlace con toast
- [x] 4.2 Crear la vista parcial `_AuthorBioCard.cshtml` en `paginaWeb/Views/Shared/Partials/` tipada con `AuthorDto` para la ficha visual extendida del autor
- [x] 4.3 Crear las vistas parciales `_CommentsSection.cshtml` y `_CommentForm.cshtml` en `paginaWeb/Views/Shared/Partials/` para el listado de comentarios y formulario de envío con honeypot
- [x] 4.4 Crear las vistas parciales `_ArticleCard.cshtml` y `_RecommendedArticles.cshtml` en `paginaWeb/Views/Shared/Partials/` para el renderizado modular de tarjetas y grilla de recomendaciones

## 5. Integración en Vistas Principales y Verificación

- [x] 5.1 Refactorizar `Articulo.cshtml` reemplazando los bloques monolíticos con las llamadas `<partial name="Partials/_..." />`, reduciendo la vista y garantizando que mantenga su funcionalidad visual completa
- [x] 5.2 Refactorizar `Index.cshtml` integrando `<partial name="Partials/_ArticleCard" ... />` en el feed y grillas secundarias
- [x] 5.3 Compilar los assets CSS mediante `pnpm run build:css` y verificar que los estilos no presenten regresiones
- [x] 5.4 Compilar la solución completa y verificar cero errores de compilación (`dotnet build`)
- [x] 5.5 Registrar commit en Git con el punto de control de la refactorización arquitectónica
