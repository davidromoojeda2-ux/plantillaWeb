# Tasks: Mejora Integral del Cuerpo del Artículo

## 1. Modelo de Dominio y DTOs (Domine & Aplication)

- [x] 1.1 Extender `Domine.Entities.Article` con propiedades opcionales para datos extendidos del autor (`AuthorBio`, `AuthorAvatarUrl`, `AuthorRole`, `AuthorSocialUrl`) y verificar compilación limpia de `Domine.csproj`
- [x] 1.2 Actualizar `ArticleDto`, `ArticleResponseDto` y `ArticleMapper` en `Aplication` para incluir los nuevos campos del autor y verificar compilación de `Aplication.csproj`

## 2. Repositorio y Servicio de Recomendaciones (Infrastructure & Aplication)

- [x] 2.1 Declarar en `IArticleRepository` e implementar en `ArticleRepository` el método `GetRecommendedAsync(string currentArticleId, string? categoryId, int take = 3)` con filtro por categoría, exclusión del artículo actual y fallback a recientes
- [x] 2.2 Declarar en `IArticleService` e implementar en `ArticleService` la obtención y mapeo a DTOs de los artículos recomendados y verificar compilación de la capa de aplicación

## 3. Controlador y Componentes Visuales (paginaWeb)

- [x] 3.1 Actualizar el método `Articulo` en `HomeController` para consultar los artículos recomendados y obtener los diccionarios de imágenes WebP en `ViewBag.RecommendedArticles` y `ViewBag.RecommendedImages`
- [x] 3.2 Diseñar e incorporar la barra interactiva de botones para compartir (Twitter/X, Facebook, LinkedIn, WhatsApp y copiar enlace con toast visual) ubicada inmediatamente debajo de la portada en `Views/Home/Articulo.cshtml`
- [x] 3.3 Configurar estilos para citas enriquecidas (`blockquote`) y bloques de llamada de atención (`callout` con variantes `info`, `warning`, `tip`, `success`) en `Styles/tailwind.css` y `Articulo.cshtml`
- [x] 3.4 Diseñar y renderizar la ficha extendida del autor al pie del cuerpo del artículo con avatar, nombre, biografía y enlaces sociales o fallback elegante
- [x] 3.5 Diseñar y renderizar la sección de artículos recomendados (grilla responsiva de 3 tarjetas con imagen, categoría, título y tiempo de lectura) antes de los comentarios

## 4. Estilos y Verificación Integral

- [x] 4.1 Compilar los assets frontend mediante `pnpm run build:css` y verificar que los estilos y tokens se generen correctamente en `wwwroot/css/tailwind.css`
- [x] 4.2 Ejecutar `dotnet build` en la solución completa y verificar cero errores y cero advertencias de compilación
- [x] 4.3 Registrar commit de punto de control en Git (`feat(article): barra social, bloques enriquecidos, ficha de autor y recomendados`)
