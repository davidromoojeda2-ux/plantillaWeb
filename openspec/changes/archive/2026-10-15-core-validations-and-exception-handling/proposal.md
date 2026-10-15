# Proposal

## Why

Actualmente el sistema presenta inconsistencias arquitectónicas críticas entre la capa de presentación (Controladores y Vistas MVC), la capa de aplicación (Servicios y DTOs) y el dominio (Entidades):
1. **Duplicidad de consultas a la base de datos:** En controladores como `CategoriasController`, se ejecuta `SlugExistsAsync` antes de enviar el DTO al servicio, y el servicio vuelve a ejecutar la misma consulta a MongoDB, generando viajes innecesarios a la base de datos.
2. **Duplicidad y mal manejo de excepciones:** En los controladores se utiliza de forma repetitiva `catch (Exception ex)` genérico, volcando cualquier fallo a `ModelState.AddModelError`. Esto enmascara caídas de infraestructura (MongoDB o Cloudflare R2 caído) presentándolas al usuario como errores de validación de formulario. Además, los servicios arrojan excepciones genéricas de sistema (`InvalidOperationException`, `KeyNotFoundException`) en lugar de excepciones de dominio/aplicación tipadas.
3. **Modelo de dominio anémico sin invariantes:** Las entidades (`Article`, `Category`, `InformativePage`) son anémicas y carecen de métodos de validación de invariantes de negocio (ej. un artículo publicado sin contenido o slug inválido, o una categoría configurada como su propio padre). La validación depende exclusivamente de anotaciones en DTOs o validaciones ad-hoc en servicios.
4. **Ausencia de ViewModels en la Capa de Presentación (Falta de MVC puro):** La capa de presentación carece de modelos propios (`ViewModels`). Las vistas Razor reciben directamente DTOs de la capa de aplicación y dependen excesivamente de `ViewBag` no tipado (`ViewBag.Parents`, `ViewBag.LinkedImages`, etc.) para renderizar listas y estados, diluyendo el rol del controlador como adaptador/orquestador y del ViewModel como modelo de presentación.
5. **Enlaces internos hardcodeados en vistas:** Gran parte de las vistas públicas y del panel administrativo utilizan rutas estáticas en texto crudo (`href="/admin/..."`, `href="/articulos/..."`, `href="/categoria/..."`) en lugar de los **Tag Helpers** nativos de ASP.NET Core (`asp-area`, `asp-controller`, `asp-action`, `asp-route-*`), lo que genera fragilidad ante cambios de rutas y rompe la aplicación si se despliega bajo un subdirectorio o proxy inverso.

Este cambio estandariza las reglas de validación en el Core, introduce una jerarquía de excepciones tipadas, elimina consultas redundantes, centraliza el manejo de errores, consolida el patrón MVC mediante ViewModels fuertemente tipados y estandariza los enlaces de vistas mediante Tag Helpers.

## What Changes

- **Jerarquía de Excepciones del Dominio y Aplicación:**
  - Creación de `Domine/Exceptions/DomainException.cs` y `DomainValidationException.cs` para invariantes y reglas de negocio.
  - Creación de `Aplication/Exceptions/NotFoundException.cs` y `DuplicateSlugException.cs` (derivadas de `AppValidationException`).
- **Validación de Invariantes en el Dominio (Rich Domain Model):**
  - Implementación de métodos de comportamiento y validación en `Article` (`ValidarInvariantes()`, `Publicar()`, `MarcarBorrador()`).
  - Implementación de métodos de invariantes en `Category` (`ValidarInvariantes()`, `AsignarPadre(string? parentId)` impidiendo auto-referencia).
- **Consolidación del Patrón MVC en Presentación (ViewModels):**
  - Creación de `ViewModels` dedicados en `paginaWeb/Areas/Admin/Models/` (e.g., `ArticleFormViewModel`, `CategoryFormViewModel`) que agrupan los campos de formulario junto a colecciones auxiliares (categorías para dropdown, imágenes disponibles, etc.).
  - Reemplazo de dependencias débiles de `ViewBag` por propiedades tipadas del `ViewModel`.
  - El controlador asume estrictamente el rol de orquestador: recibe el ViewModel, mapea a DTO, invoca los servicios internos, atrapa excepciones tipadas y retorna la vista con su ViewModel.
- **Estandarización de Enlaces Internos mediante Tag Helpers:**
  - Reemplazo sistemático de URLs relativas hardcodeadas (`href="/..."`) en vistas públicas y de administración por Tag Helpers declarativos (`asp-area`, `asp-controller`, `asp-action`, `asp-route-*`).
- **Eliminación de Consultas Redundantes:**
  - Remoción de llamadas preventivas `SlugExistsAsync` en `CategoriasController` (líneas 45 y 81).
  - Delegación completa de la verificación de unicidad al servicio correspondiente (`CategoryService`, `ArticleService`, `PageService`), que arrojará `DuplicateSlugException` si colisiona.
- **Mapeo Tipado de Excepciones en Controladores:**
  - Sustitución de `catch (Exception ex)` genérico en controladores administrativos (`ArticulosController`, `CategoriasController`, `SeccionesController`, `ImagenesController`) por captura de excepciones específicas (`DomainValidationException`, `DuplicateSlugException`, `NotFoundException`).
  - Mapeo preciso al campo correspondiente en `ModelState` (ej. `ModelState.AddModelError(nameof(model.Slug), ex.Message)`).
  - Propagación natural de excepciones de infraestructura no controladas (`MongoException`, fallos de red R2, errores 500) al middleware global de la aplicación.

## Capabilities

### New Capabilities
- `core-validation-exceptions`: Define el contrato para la validación de invariantes en el dominio, la jerarquía de excepciones tipadas del core, la adopción de ViewModels y Tag Helpers en la capa de presentación MVC y el manejo de errores limpios sin duplicidad de consultas entre controladores y servicios.

### Modified Capabilities
*(Ninguna; no existen especificaciones previas en main)*

## Impact

- **Capa Domine:** Nuevas excepciones en `Domine/Exceptions/`; métodos de invariantes agregados a `Article` y `Category`.
- **Capa Aplication:** Nuevas excepciones en `Aplication/Exceptions/`; actualización de servicios (`ArticleService`, `CategoryService`, `PageService`) para validar invariantes y arrojar excepciones tipadas.
- **Capa Presentación (`paginaWeb`):** 
  - Nuevos ViewModels en `paginaWeb/Areas/Admin/Models/`.
  - Controladores refactorizados para eliminar duplicidad de consultas a BD, usar ViewModels eliminando `ViewBag`, y sustituir `catch (Exception)` genérico por manejo tipado de excepciones de negocio.
  - Vistas administrativas y públicas migradas a Tag Helpers (`asp-controller`, `asp-action`, `asp-area`, `asp-route-*`), eliminando URLs hardcodeadas y consumiendo modelos fuertemente tipados.
