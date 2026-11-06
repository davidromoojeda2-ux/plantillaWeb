# Tasks

## 1. Base de versiones y eventos

- [x] 1.1 Definir los contratos de tiempo real en `Aplication` (`CmsModule`, `CmsChangeEvent`, `ICmsChangePublisher`, `ICmsChangeStream`) y verificar que la solución compila con `dotnet build`.
- [x] 1.2 Implementar un contador de versiones singleton por módulo que solo incremente después de escrituras exitosas; cubrir incremento independiente de `automation`, `articles`, `categories` y `comments` con pruebas unitarias.
- [x] 1.3 Implementar el bus en proceso con cola acotada, coalescencia por módulo/entidad y un único despachador; probar que una ráfaga no bloquea al publicador y que la cola no crece sin límite.
- [x] 1.4 Añadir el endpoint autorizado `GET /admin/changes` que devuelve solo versiones en memoria; verificar `401` sin autenticación y respuesta correcta con sesión admin.
- [x] 1.5 Añadir el polling de respaldo en `_AdminLayout.cshtml`, pausado con la pestaña oculta y sin consultar Mongo; verificar en DevTools que en reposo solo se pide `/admin/changes`.

## 2. Integración de Blazor Server con MVC

- [x] 2.1 Registrar Blazor Server, endpoint de circuitos, `_Imports.razor` y `~/_framework/blazor.server.js`; verificar arranque y `dotnet build`.
- [x] 2.2 Proteger el endpoint del circuito y validar la revalidación de la cookie admin en circuitos largos; probar acceso anónimo rechazado y sesión expirada.
- [x] 2.3 Incluir la carpeta de componentes en el escaneo de Tailwind y ejecutar `pnpm run build:css`; verificar que las clases usadas en `.razor` aparecen en el CSS generado.
- [x] 2.4 Crear el componente compartido de estado de conexión con mensajes de reconexión y modo degradado; verificar visualmente al detener y reanudar la red.

## 3. Automatización en vivo

- [x] 3.1 Implementar `AutomationLiveState` y la caché del estado consolidado; reemplazar las consultas pesadas por proyecciones/conteos y verificar que no se cargan `DraftJson` en el estado.
- [x] 3.2 Publicar eventos de inicio, fin, detención, artículo generado y decisión pendiente creada/resuelta; cubrir cada transición con pruebas.
- [x] 3.3 Crear `AutomationStatusPanel` y `PendingDecisionsList` como islas Blazor y conectarlos a los datos iniciales del controlador.
- [x] 3.4 Reemplazar el polling de 3 s de `Automation/Index` y el refresco automático de saldo cada 60 s; verificar que la página ya no llama a `/admin/automatizacion/estado`.
- [ ] 3.5 Probar una corrida manual, una programada y una detenida; verificar que estado, contadores y pendientes se actualizan en menos de 5 segundos sin recargar.
- [ ] 3.6 Probar que una ráfaga de artículos generados no produce renders duplicados ni bloquea la corrida; verificar coalescencia y estado final correcto.

## 4. Listado de artículos

- [x] 4.1 Publicar eventos de artículo en creación, actualización, eliminación, publicación programada y cambio de destacado; cubrir cada caso con pruebas.
- [x] 4.2 Crear `ArticleList` con filtros, búsqueda, imágenes de portada y toggle de Hero, reutilizando el marcado y estilos actuales.
- [x] 4.3 Reemplazar el listado de `Articulos/Index.cshtml` por la isla y conservar las URLs y acciones existentes.
- [ ] 4.4 Verificar que un artículo creado por automatización, una publicación programada y un cambio de destacado se reflejan en menos de 5 segundos.
- [x] 4.5 Verificar que un cambio de artículo no recarga ni consulta los módulos de categorías o comentarios.

## 5. Categorías

- [x] 5.1 Publicar eventos de categoría en creación, actualización y eliminación, incluyendo la invalidación del menú; cubrir con pruebas.
- [x] 5.2 Crear `CategoryTree` con jerarquía, contadores y acciones, reutilizando el marcado actual.
- [x] 5.3 Reemplazar el listado de `Categorias/Index.cshtml` por la isla y conservar creación, edición y eliminación.
- [x] 5.4 Verificar que altas, ediciones y bajas se reflejan en menos de 5 segundos y que los contadores coinciden.

## 6. Moderación de comentarios

- [x] 6.1 Publicar eventos de comentario en envío público, aprobación, rechazo y eliminación; cubrir con pruebas.
- [x] 6.2 Crear `CommentModerationList` con pestañas por estado, acciones y contador global de pendientes.
- [x] 6.3 Reemplazar el listado de `Comentarios/Index.cshtml` por la isla y conservar los filtros y formularios existentes.
- [x] 6.4 Verificar que un comentario enviado desde el sitio público aparece en menos de 5 segundos y que aprobar/rechazar/eliminar actualiza la lista y el badge.

## 7. Endurecimiento e integración

- [ ] 7.1 Implementar reconciliación al reconectar y verificar que no se duplican suscripciones ni se pierden cambios durante una desconexión.
- [ ] 7.2 Probar 20 pestañas admin simultáneas con ráfagas de eventos; verificar latencia menor a 5 segundos, memoria estable y cero consultas periódicas a Mongo en reposo.
- [x] 7.3 Verificar seguridad: circuito anónimo rechazado, eventos sin claves/saldos/configuración, componentes sin acceso a datos no autorizados y suscripciones liberadas al navegar.
- [x] 7.4 Verificar que no se añadieron webhooks de negocio y que las llamadas a Serper/IA siguen siendo HTTP de vida corta.
- [x] 7.5 Documentar la configuración de WebSocket en el proxy, la limitación de una sola instancia y el procedimiento de rollback por isla.
- [x] 7.6 Ejecutar `dotnet build`, `dotnet test` y una prueba de humo del sitio público, el editor y los formularios; confirmar que no se carga Blazor fuera del Admin.

## Workflow follow-up

- Archivar el cambio después de completar la implementación y las verificaciones.
- Sincronizar la nueva capability con las especificaciones principales.
