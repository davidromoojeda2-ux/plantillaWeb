# Plan de desacoplamiento de la capa de datos

> Estado: **Borrador — pendiente de revisión**
> Alcance: desacoplar el proyecto de MongoDB **sin definir aún un motor de destino**.
> Al terminar, cambiar de motor debe ser una decisión confinada a la capa `Infrastructure`.

---

## 1. Objetivo y alcance

**Objetivo.** Eliminar toda dependencia de MongoDB de las capas internas (`Domine` y `Aplication`), de modo que:

1. El dominio no conozca ningún motor de base de datos (sin atributos BSON, sin `ObjectId`, sin paquete `MongoDB.Driver`).
2. La capa de aplicación no valide ni manipule IDs con reglas propias de Mongo.
3. Toda la persistencia quede confinada en `Infrastructure`, detrás de las interfaces ya existentes.

**Fuera de alcance (ahora).** Migrar los datos a un motor concreto, reescribir las consultas full-text o la agregación. Eso es trabajo posterior, cuando se elija el motor. Este plan solo **prepara el terreno** para que esa migración sea barata.

**Criterio de éxito (Definition of Done).**

- `Domine` y `Aplication` compilan **sin** referencia a `MongoDB.*`.
- El comportamiento y el formato de datos en MongoDB **no cambian** (cero regresiones, cero migración de datos).
- Una prueba de arquitectura falla si alguien reintroduce una dependencia de `MongoDB.*` en `Domine` o `Aplication`.
- Los repositorios actuales (`Infrastructure/Repositories/*`) siguen pasando su lógica sin cambios de contrato.

---

## 2. Inventario actual de acoplamiento

### 2.1 Referencias de paquete

| Proyecto | Referencia a MongoDB | Estado |
|---|---|---|
| `Domine/Domine.csproj` | `MongoDB.Driver` v3.13.0 | 🔴 Debe eliminarse |
| `Aplication/Aplication.csproj` | Ninguna directa (heredada por `Domine`) | 🟡 Hereda el paquete vía referencia |
| `Infrastructure/Infrastructure.csproj` | `MongoDB.Driver` v3.13.0 | 🟢 Correcto (aquí debe vivir) |
| `paginaWeb/paginaWeb.csproj` | Ninguna directa | 🟢 Correcto |

### 2.2 Capa `Domine` (acoplamiento en el dominio)

Atributos y tipos BSON presentes en **11 entidades + 3 value objects + 1 archivo de bloques**:

| Archivo | Acoplamiento |
|---|---|
| `Entities/Article.cs` | `[BsonIgnoreExtraElements]`, `[BsonId]`, `[BsonRepresentation(ObjectId)]` (Id y 5 FKs), `[BsonRepresentation(String)]` (Status), `ObjectId.GenerateNewId()` |
| `Entities/Category.cs` | Idem + `ParentId` como ObjectId |
| `Entities/Comment.cs` | Idem + `ArticleId`, `VisitorId` como ObjectId, `Status` como String |
| `Entities/AdminUser.cs` | `[BsonId]`, ObjectId |
| `Entities/Visitor.cs` | `[BsonId]`, ObjectId |
| `Entities/DocumentMetadata.cs` | `[BsonId]`, ObjectId |
| `Entities/ImageMetadata.cs` | `[BsonId]`, ObjectId + `ArticleId` como ObjectId |
| `Entities/InformativePage.cs` | `[BsonId]`, ObjectId |
| `Entities/AutomationRun.cs` | `[BsonId]`, ObjectId + `[BsonIgnoreExtraElements]` en clases anidadas |
| `Entities/AutomationSettings.cs` | `[BsonId]`, ObjectId |
| `Entities/SiteSettings.cs` | `[BsonId]` (Id fijo `"site-settings"`), `ObjectId.GenerateNewId()` en `WhatsAppGroupLink.Id` |
| `ValueObjects/SafeUrl.cs` | `[BsonElement("value")]`, `[BsonIgnore]` (x2) |
| `ValueObjects/RichText.cs` | `[BsonElement("html")]`, `[BsonElement("text")]` |
| `ValueObjects/PlainText.cs` | `[BsonElement("value")]` |
| `ContentBlocks/ContentBlocks.cs` | `[BsonDiscriminator]`, `[BsonKnownTypes]`, `[BsonIgnore]` en 11 subtipos |

> Nota: `ImageVariant.cs` y `ParagraphStyleSetting.cs` ya están limpios.

### 2.3 Capa `Aplication` (fuga puntual)

| Archivo | Línea | Acoplamiento |
|---|---|---|
| `Services/ArticleService.cs` | 12, 196 | `using MongoDB.Bson` + `ObjectId.TryParse(...)` |
| `Services/ContentBodySanitizer.cs` | 8, 129, 166 | `using MongoDB.Bson` + `ObjectId.TryParse(...)` |

La validación de IDs de imágenes/documentos asume que todo ID es un ObjectId de Mongo.

### 2.4 Capa `Infrastructure` (acoplamiento esperado)

Aquí **debe** quedar MongoDB. Los repositorios (`ArticleRepository`, `CategoryRepository`, `CommentRepository`, `DocumentRepository`, `AutomationStateRepository`, `SiteSettingsRepository`, `OtherRepositories`) usan `MongoDbContext` y `Builders<T>`/`IMongoCollection<T>`. Ya están detrás de interfaces, por lo que **no es necesario tocarlos** salvo para el punto de la sección 6.4.

### 2.5 Arranque (`paginaWeb/Program.cs`)

Registra `MongoDbContext` y ejecuta `MongoDbIndexer.EnsureIndexesAsync`, `MigrateLegacyPageContentAsync` y `EnsureSeedDataAsync`. También `builder.Services.Configure<MongoDbSettings>(...)` en dos puntos (líneas 40-42 y 135-136, duplicado a limpiar).

---

## 3. Estrategia general

Regla de oro: **el mapping BSON sale del dominio y se expresa en `Infrastructure`**, replicando *exactamente* el comportamiento actual para no alterar el formato almacenado.

```
Antes:  Domine ──(atributos BSON)──> MongoDB
Ahora:  Domine (limpio)  ←──  Aplication (limpio)  ←──  Infrastructure (mapping + repos)
```

Dónde debe vivir cada cosa al final:

| Responsabilidad | Ubicación |
|---|---|
| Entidades, value objects, reglas de negocio | `Domine` (sin MongoDB) |
| Generación/validación de IDs | `Domine/Common/EntityId.cs` (neutral) |
| Mapping BSON (class maps / convenciones) | `Infrastructure/Persistence/MongoClassMaps.cs` |
| Repositorios y consultas | `Infrastructure/Repositories` |
| Registro DI y arranque | `paginaWeb/Program.cs` (solo orquestación) |

---

## 4. Decisiones de diseño

### D1 — Estrategia de IDs (la decisión más importante)

**Problema.** Hoy el dominio genera `ObjectId.GenerateNewId().ToString()` (24 hex) y Mongo guarda `_id` como `ObjectId`. Si se cambia la generación a `Guid` (32 hex), el formato almacenado cambia y aparecería una migración de datos.

**Recomendación (cero migración, cero regresión).** Mantener `Id` como `string`. Introducir `Domine.Common.EntityId` como **único punto** que conoce el formato del ID:

```csharp
// Domine/Common/EntityId.cs
namespace Domine.Common;

public static class EntityId
{
    // 24 hex, compatible con el formato ObjectId actual (cero migración).
    // En la futura migración a SQL, aquí se cambia a Guid.NewGuid().ToString("N").
    public static string New() => Guid.NewGuid().ToString("N")[..24];

    public static bool IsValid(string? id) =>
        id is { Length: 24 } && id.All(Uri.IsHexDigit);

    public static readonly string Empty = string.Empty;
}
```

Sustituir `ObjectId.GenerateNewId().ToString()` → `EntityId.New()` en todas las entidades. El mapeo `_id ↔ ObjectId` se declara en `Infrastructure` (ver Fase 2), de modo que el almacenamiento **no cambia**.

**Alternativa (más "limpia" a futuro, con migración).** Usar `Guid` de 32 hex y guardar `_id` como string. Requiere una migración puntual de los `_id` existentes (ObjectId → hex) y tocar la serialización. **Se descarta para este alcance**; se evaluará en la migración real.

> **Validación necesaria:** revisar si algún flujo usa `Id` antes de persistir (p. ej. crear artículo y en la misma operación referenciar su `Id` en imágenes). Con `EntityId.New()` el comportamiento es idéntico al actual, así que el riesgo es nulo.

### D2 — Dónde vive el mapping BSON

**Recomendación.** `BsonClassMap.RegisterClassMap<T>(...)` en `Infrastructure/Persistence/MongoClassMaps.cs`, replicando atributo por atributo. Es más verboso que una convención global, pero **evita cambios de formato** (p. ej. una convención de enums convertiría `AutomationRunStatus` de `int` a `string`, alterando datos).

### D3 — ¿Abstraer el contexto (`IAppDbContext`/`IUnitOfWork`)?

**Recomendación.** No ahora. Los repositorios ya están detrás de interfaces; el `MongoDbContext` concreto es un detalle interno de `Infrastructure`. Se añadirá una abstracción de transacciones **solo** cuando se elija motor relacional. (Ver Fase 4, opcional.)

### D4 — Full-text y agregación

Ya están encapsulados tras métodos de repositorio (`IArticleRepository.SearchAsync`, `IAutomationStateRepository.GetUsageTotalsAsync`). No requieren acción ahora; se reimplementarán en la futura migración sin tocar el contrato.

---

## 5. Plan por fases

Las fases son incrementales y cada una deja el proyecto compilando y pasando tests. Se recomienda implementar en ramas separadas por fase.

### Fase 0 — Línea base y red de seguridad

**Objetivo.** Garantizar que podemos detectar cualquier regresión durante el desacoplamiento.

**Tareas.**

1. Confirmar que `dotnet build` y `dotnet test` pasan desde cero.
2. Añadir el proyecto `tests/Arquitectura.Tests` (xUnit, como los existentes) con una prueba que **falle hoy** y pase al final:
   - `Domine` no referencia `MongoDB.*`.
   - `Aplication` no referencia `MongoDB.*`.
   - Solo `Infrastructure` referencia `MongoDB.Driver`.

   Opciones de implementación: `NetArchTest.Rules` (analiza dependencias de ensamblados) o un test simple que inspeccione las `Assembly.GetReferencedAssemblies()` y los tipos. Recomendado: `NetArchTest.Rules`.

**Criterio de verificación.** La prueba de arquitectura queda escrita y en rojo (esperado). El resto de tests pasa.

---

### Fase 1 — Neutralizar los IDs

**Objetivo.** Eliminar `ObjectId.GenerateNewId()` del dominio sin tocar aún el mapping.

**Tareas.**

1. Crear `Domine/Common/EntityId.cs` (código de la sección D1).
2. Sustituir en todas las entidades:
   - `ObjectId.GenerateNewId().ToString()` → `EntityId.New()`.
   - Quitar el `using MongoDB.Bson;` donde ya no se use (se elimina por completo en Fase 2).
3. Archivos: `Article.cs`, `Category.cs`, `Comment.cs`, `AdminUser.cs`, `Visitor.cs`, `DocumentMetadata.cs`, `ImageMetadata.cs`, `InformativePage.cs`, `AutomationRun.cs`, `AutomationSettings.cs`, `SiteSettings.cs`.
   - `SiteSettings.Id` sigue siendo el fijo `"site-settings"` (no tocar).
4. Compilar y correr tests.

**Criterio de verificación.** Compila y los tests pasan. Los `Id` nuevos siguen siendo 24 hex (compatibles con `ObjectId`).

---

### Fase 2 — Desacoplar el dominio (quitar `MongoDB.Driver`)

**Objetivo.** Eliminar el paquete y todos los atributos BSON de `Domine`, moviendo el mapping a `Infrastructure`.

**Tareas.**

1. Crear `Infrastructure/Persistence/MongoClassMaps.cs` con `BsonClassMap.RegisterClassMap<T>` para cada entidad/value object, replicando el mapeo actual.

   Ejemplo para `Article`:

   ```csharp
   using Domine.ContentBlocks;
   using Domine.Entities;
   using MongoDB.Bson;
   using MongoDB.Bson.Serialization;
   using MongoDB.Bson.Serialization.IdGenerators;
   using MongoDB.Bson.Serialization.Serializers;

   namespace Infrastructure.Persistence;

   public static class MongoClassMaps
   {
       public static void Register()
       {
           BsonClassMap.RegisterClassMap<Article>(cm =>
           {
               cm.AutoMap();
               cm.SetIgnoreExtraElements(true);
               cm.MapIdProperty(a => a.Id)
                 .SetIdGenerator(StringObjectIdGenerator.Instance)
                 .SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.CategoryId).SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.SubcategoryId).SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.CoverImageId).SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.ImageIds).SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.DocumentIds).SetRepresentation(BsonType.ObjectId);
               cm.GetMemberMap(a => a.Status).SetRepresentation(BsonType.String);
           });
           // ... replicar para Category, Comment, etc.
       }
   }
   ```

2. Mapear la herencia de `ContentBlock` (discriminadores):

   ```csharp
   BsonClassMap.RegisterClassMap<ContentBlock>(cm =>
   {
       cm.AutoMap();
       cm.SetIsRootClass(true);
       cm.UnmapProperty(c => c.Type); // era [BsonIgnore]
       cm.AddKnownType(typeof(ContentParagraphBlock));
       // ... resto de subtipos
   });
   BsonClassMap.RegisterClassMap<ContentParagraphBlock>(cm => cm.SetDiscriminator("paragraph"));
   // ... resto: heading, ordered-list, bullet-list, quote, callout,
   //          image, document, table, code, divider
   ```

3. Mapear value objects (`[BsonElement]`/`[BsonIgnore]`):
   - `SafeUrl`: mapear `Value` a `"value"`, ignorar `Host` e `IsRelative`.
   - `RichText`: `html`, `text`.
   - `PlainText`: `value`.

4. Quitar de `Domine`:
   - `Domine.csproj`: eliminar `<PackageReference Include="MongoDB.Driver" ... />`.
   - Eliminar todos los `using MongoDB.Bson;` y `using MongoDB.Bson.Serialization.Attributes;`.
   - Eliminar todos los atributos `[Bson...]`.

5. Invocar `MongoClassMaps.Register()` **una vez** en el arranque, antes de `EnsureIndexesAsync` (en `Program.cs`), o dentro del constructor de `MongoDbContext`.

   > ⚠️ El registro de class maps debe ocurrir antes de la primera serialización. Conviene hacerlo en un punto determinista (p. ej. inicio de `Program.cs`, junto a la configuración de Mongo).

6. Compilar y correr tests. Verificar manualmente un CRUD completo (crear/leer/actualizar artículo, categoría y página) contra una base local para confirmar que el formato no cambió.

**Criterio de verificación.** `Domine.csproj` sin `MongoDB.Driver`. `Domine` compila sin `using MongoDB`. Los documentos siguen guardándose con `_id` ObjectId y enums/strings igual que antes.

---

### Fase 3 — Desacoplar la aplicación (eliminar `ObjectId.TryParse`)

**Objetivo.** Quitar la validación Mongo de la capa `Aplication`.

**Tareas.**

1. En `Aplication/Services/ArticleService.cs` (línea 196) y `Aplication/Services/ContentBodySanitizer.cs` (líneas 129 y 166), sustituir `ObjectId.TryParse(id, out _)` por `EntityId.IsValid(id)` (de `Domine.Common`).
2. Eliminar `using MongoDB.Bson;` de ambos archivos.
3. Compilar y correr tests.

**Criterio de verificación.** `Aplication` no referencia `MongoDB.*`. La validación de IDs mantiene el mismo comportamiento (24 hex).

---

### Fase 4 — (Opcional/recomendado) Abstraer el acceso a datos

**Objetivo.** Hacer el swap de motor aún más limpio, eliminando la dependencia directa de los repositorios hacia el concreto `MongoDbContext`.

**Tareas (solo si se quiere endurecer).**

1. Definir `Infrastructure/Persistence/IAppDbContext.cs` (o mover la abstracción a `Aplication` si se prefiere) con accesores genéricos neutrales, p. ej. `ICollection<T>` propio, o dejarlo como seam de transacciones:
   - Hoy los repositorios reciben `MongoDbContext`. Se puede dejar así (ya está confinado a `Infrastructure`).
2. **No introducir** `IUnitOfWork`/transacciones hasta elegir motor (D3).

**Criterio de verificación.** Sin cambios de comportamiento; opcional según decisión del equipo.

---

### Fase 5 — Blindaje con pruebas de contrato

**Objetivo.** Asegurar que el desacoplamiento se mantiene en el tiempo.

**Tareas.**

1. Terminar la prueba de arquitectura de la Fase 0 para que quede en verde y forme parte del CI.
2. Añadir pruebas de contrato para los repositorios usando el driver real contra una base Mongo efímera (p. ej. `Mongo2Go`/testcontainer) o mocks de `IMongoCollection`. Objetivo: confirmar que el mapping (class maps) redondea correctamente (serializar → deserializar) para `Article`, `Category`, `ContentBlock` y `SiteSettings`.

**Criterio de verificación.** La suite de arquitectura + contrato pasa en CI.

---

## 6. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Cambiar el formato almacenado por error (p. ej. enums `int` → `string`, `_id` ObjectId → string) | Datos corruptos o incompatibles | Replicar atributo por atributo con class maps (D2). Prueba de redondeo (Fase 5). Probar CRUD contra BD local. |
| Registrar class maps tarde (serialización previa) | `BsonClassMap` duplicado o mapeo por defecto | Registrar `MongoClassMaps.Register()` una sola vez, antes de cualquier acceso, en un punto determinista. |
| Algún flujo usa `Id` antes de persistir | Referencias rotas | `EntityId.New()` mantiene el comportamiento actual (genera 24 hex al construir). Revisar usos pre-persistencia (D1). |
| Regresión de búsqueda full-text / agregación | Funcionalidad rota | No se tocan en este plan; ya están tras interfaces (D4). Cubrir con pruebas de contrato. |
| Duplicación de `Configure<MongoDbSettings>` en `Program.cs` | Confusión/deriva | Limpiar en Fase 2 (dejar una sola). |

---

## 7. Checklist final de verificación

- [ ] `Domine/Domine.csproj` no referencia `MongoDB.Driver`.
- [ ] `grep -r "MongoDB\|ObjectId\|Bson" Domine Aplication` devuelve **cero** coincidencias de código (solo comentarios neutros si acaso).
- [ ] `Aplication` compila sin `using MongoDB`.
- [ ] `MongoClassMaps.Register()` se ejecuta una vez al arrancar.
- [ ] CRUD manual (artículo, categoría, página, settings) funciona contra Mongo local y el formato de `_id`/enums no cambió.
- [ ] La prueba de arquitectura está en verde.
- [ ] `dotnet build` y `dotnet test` pasan en CI.

---

## 8. Trabajo futuro (cuando se elija motor)

Al definir el motor de destino, la migración real se reduce a:

1. Cambiar `EntityId.New()`/`IsValid()` (D1) al formato del nuevo motor (p. ej. `Guid`).
2. Implementar nuevos repositorios que cumplan las interfaces existentes (`ICategoryRepository`, `IArticleRepository`, etc.).
3. Reimplementar en el nuevo motor:
   - Búsqueda full-text (`SearchAsync`/`CountSearchAsync`).
   - Agregación de uso (`GetUsageTotalsAsync`).
   - Índices/seed/migraciones (hoy en `MongoDbIndexer`).
4. Reescribir el arranque en `Program.cs` (DI + inicialización).
5. Migración de datos con script externo.

Sin tocar `Domine`, `Aplication` ni la presentación.

---

## 9. Estado de implementación (v1)

**Fases implementadas:** 0, 1, 2, 3 y 5. La Fase 4 (`IAppDbContext`/`IUnitOfWork`) se
deja **intencionalmente** para cuando se elija motor relacional (decisión D3).

### Cambios principales

- **Nuevo** `Domine/Common/EntityId.cs`: fuente única de generación/validación de IDs
  (`New()` y `IsValid()`), neutral respecto al motor.
- **Dominio limpio:** se eliminó `MongoDB.Driver` de `Domine.csproj` y todos los
  atributos `[Bson...]` de entidades, value objects y bloques de contenido. Las
  entidades usan `EntityId.New()`.
- **Aplicación limpia:** `ObjectId.TryParse` sustituido por `EntityId.IsValid` en
  `ArticleService` y `ContentBodySanitizer`.
- **Nuevo** `Infrastructure/Persistence/MongoClassMaps.cs`: registra el mapeo BSON
  (class maps) replicando el comportamiento de los antiguos atributos. Se invoca de
  forma idempotente desde el constructor estático de `MongoDbContext`.
- **Arranque:** se eliminó el `Configure<MongoDbSettings>` duplicado en `Program.cs`.

### Verificación del formato (cero migración)

Se añadieron `BsonMappingTests` que fijan el formato almacenado (`_id` como ObjectId,
enums como string, FKs como ObjectId, listas de IDs como ObjectId, `SiteSettings._id`
string fijo, discriminadores de bloques). Estos tests se validaron contra el
comportamiento real del driver: se comprobó que `[BsonRepresentation]` sobre
`List<string>` **sí** afecta a los elementos (se almacenan como ObjectId), por lo que el
mapeo se replica con el propio `BsonRepresentationAttribute` aplicado programáticamente.

### Guarda de arquitectura

`ArchitectureDecouplingTests` falla si `Domine` o `Aplication` vuelven a referenciar
`MongoDB.*`, y verifica que `Infrastructure` sí lo referencia.

### Notas

- El discriminador `_t` de los bloques se serializa como arreglo de jerarquía
  (`["ContentBlock", "paragraph"]`); es el mismo formato que producían los atributos
  originales.
- Los tests de round-trip existentes ahora dependen de `MongoClassMaps.Register()`,
  registrado una vez por `TestBootstrap` (`[ModuleInitializer]`).
