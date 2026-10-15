# Design

## Context

El sistema sigue una arquitectura por capas (`Domine`, `Aplication`, `Infrastructure`, `paginaWeb`). Sin embargo, existen inconsistencias en la asignación de responsabilidades:
- Controladores como `CategoriasController` asumen tareas de verificación preventiva de existencia de slugs contra la base de datos, repitiendo la llamada que luego ejecuta `CategoryService`.
- Excepciones genéricas del runtime (`InvalidOperationException`, `KeyNotFoundException`) se utilizan para comunicar errores de negocio y de validación.
- Los controladores MVC capturan `Exception` indiscriminadamente, confundiendo errores de base de datos caída con fallas de validación de datos de usuario.
- Las entidades del dominio no protegen sus propias invariantes, permitiendo estados inválidos si no se validan previamente en DTOs o servicios.
- La capa de presentación (`paginaWeb`) no cuenta con un modelo propio (**ViewModel**) y utiliza DTOs de aplicación directamente en las vistas, complementándolos con propiedades dispersas en `ViewBag` (`ViewBag.Parents`, `ViewBag.LinkedImages`, etc.), lo que debilita el tipado y distorsiona el patrón MVC.
- Múltiples vistas Razor en el portal público y el panel administrativo usan cadenas de texto crudo en `href="/..."`, en lugar de **Tag Helpers** nativos (`asp-area`, `asp-controller`, `asp-action`, `asp-route-*`), volviendo frágil la navegación ante cambios de rutas.

Ver `proposal.md` para el contexto de motivación.

## Goals / Non-Goals

**Goals:**
- Establecer una jerarquía limpia de excepciones en el Core (`Domine/Exceptions` y `Aplication/Exceptions`).
- Enriquecer las entidades `Article` y `Category` con métodos de validación de invariantes de negocio.
- Eliminar la doble consulta a base de datos en controladores, delegando la verificación de unicidad exclusivamente al servicio de aplicación.
- Reemplazar los bloques genéricos `catch (Exception ex)` en los controladores administrativos por captura tipada de excepciones de negocio (`DomainValidationException`, `NotFoundException`, `ConflictException`).
- Implementar el patrón MVC de presentación con **ViewModels fuertemente tipados** en `paginaWeb/Areas/Admin/Models/`, eliminando el uso de `ViewBag` para datos estructurales de pantalla.
- Posicionar a los Controladores estrictamente como orquestadores/adaptadores que transforman ViewModels <-> DTOs y delegan a los Servicios.
- Sustituir sistemáticamente los enlaces internos hardcodeados en vistas públicas y administrativas por **Tag Helpers** nativos de ASP.NET Core.

**Non-Goals:**
- No se reemplazarán los DataAnnotations de formato en la interfaz de usuario.
- No se modificará el esquema de colecciones ni los índices de MongoDB.
- No se alterarán los enlaces externos a redes sociales o sitios de terceros (los cuales conservan su `href="https://..."` estándar).

## Decisions

### 1. Jerarquía de Excepciones Tipadas
- **Decisión:** Crear `DomainException` y `DomainValidationException` en `Domine/Exceptions/`, y `NotFoundException` y `ConflictException` (o `DuplicateSlugException`) en `Aplication/Exceptions/`.
- **Razón:** El dominio solo debe conocer sus propias excepciones sin dependencias externas. La capa de aplicación puede señalar entidades no encontradas o conflictos de persistencia (como unicidad de clave/slug) que dependen del almacenamiento.
- **Alternativas consideradas:**
  - *Usar un Result Pattern (`Result<T>`)*: Requeriría reescribir todas las firmas de `IArticleService`, `ICategoryService`, `IPageService` e impactaría masivamente toda la base de código. Se descarta para mantener compatibilidad idiomática en ASP.NET Core MVC.

### 2. Eliminación de `SlugExistsAsync` en Controladores
- **Decisión:** Retirar las llamadas `await _categoryService.SlugExistsAsync(...)` de `CategoriasController.cs`. El controlador simplemente invoca `await _categoryService.CreateAsync(model)` o `UpdateAsync(id, model)`. Si el slug está duplicado, el servicio lanza `ConflictException` (o `DuplicateSlugException`).
- **Razón:** Elimina la latencia de 1 viaje redundante a MongoDB por cada alta y edición. Alinea `CategoriasController` con el patrón ya utilizado en `ArticulosController`.
- **Alternativas consideradas:**
  - *Mantener la llamada en el controlador*: Provoca 2 consultas idénticas a MongoDB por cada submit.

### 3. Manejo de Invariantes en el Dominio (Modelo Rico)
- **Decisión:** Agregar métodos explícitos a `Article` (`ValidarInvariantes()`, `Publicar()`, `MarcarBorrador()`) y a `Category` (`ValidarInvariantes()`, `AsignarPadre(...)`).
- **Razón:** Evita que el dominio dependa enteramente de que los DTOs estén bien formados. Centraliza las reglas de negocio (ej. "artículo publicado requiere contenido", "categoría no puede ser padre de sí misma").
- **Alternativas consideradas:**
  - *Dejar las entidades como anémicas y validar todo en los servicios*: Dispersa las reglas de negocio en múltiples servicios o métodos, facilitando inconsistencias.

### 4. Captura Específica en Controladores MVC
- **Decisión:** En los métodos POST de los controladores administrativos, reemplazar la captura genérica por excepciones específicas:
  ```csharp
  catch (DomainValidationException ex)
  {
      ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
      ...
  }
  catch (ConflictException ex)
  {
      ModelState.AddModelError(ex.PropertyName ?? string.Empty, ex.Message);
      ...
  }
  catch (NotFoundException ex)
  {
      return NotFound();
  }
  ```
- **Razón:** Brinda retroalimentación exacta al usuario en el campo causante del error (`PropertyName`) y permite que fallas reales de infraestructura o bugs de código generen error 500 y se registren en los logs en lugar de ocultarse.

### 5. Adopción de ViewModels en la Capa de Presentación (MVC Puro)
- **Decisión:** Crear clases `ViewModel` en `paginaWeb/Areas/Admin/Models/` (como `ArticleFormViewModel`, `CategoryFormViewModel`).
  - El **ViewModel** contiene los campos editables del formulario más las listas para controles UI (`Categories`, `AvailableImages`, etc.).
  - El **Controller** llena el ViewModel consultando los servicios necesarios y se lo pasa a la Vista.
  - La **Vista** declara `@model ArticleFormViewModel` y accede a `Model.Categories` con intellisense y seguridad de tipos en lugar de `ViewBag.Parents`.
- **Razón:** Elimina `ViewBag` no tipado, previene errores de tipado o `NullReferenceException` en tiempo de ejecución, y delimita claramente la responsabilidad de la capa de presentación.

### 6. Estandarización de Enlaces Internos mediante Tag Helpers
- **Decisión:** Sustituir los atributos estáticos `href="/..."` por Tag Helpers:
  - Enlaces a administración: `<a asp-area="Admin" asp-controller="Articulos" asp-action="Index">...</a>`
  - Enlaces de edición con parámetros: `<a asp-area="Admin" asp-controller="Articulos" asp-action="Edit" asp-route-id="@item.Id">...</a>`
  - Enlaces públicos: `<a asp-area="" asp-controller="Home" asp-action="Articulo" asp-route-slug="@item.Slug">...</a>`
- **Razón:** Garantiza que los hipervínculos sean resueltos dinámicamente según la tabla de enrutamiento oficial de ASP.NET Core, evitando enlaces rotos (404) si se renombran rutas y respetando entornos con `PathBase` o proxy reverso.

## Risks / Trade-offs

- **[Riesgo de errores 500 en fallas de base de datos]** → Si MongoDB no está accesible, la vista ahora mostrará la página de error global de ASP.NET Core en lugar de recargar el formulario. *Mitigación:* Esto es el comportamiento correcto en arquitectura web moderna, ya que el usuario no puede solucionar una falla del servidor corrigiendo los campos del formulario.
- **[Sobrecarga de mapeo ViewModel ↔ DTO]** → Requiere mapear del ViewModel al DTO al recibir el POST. *Mitigación:* Se implementan métodos auxiliares simples `ToDto()` en los ViewModels o helpers estáticos en la capa de presentación.
