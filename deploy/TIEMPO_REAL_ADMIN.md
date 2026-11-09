# Tiempo real del panel administrativo

## Qué usa

El Admin usa islas Blazor Server dentro de las vistas MVC. Cada pestaña autenticada
mantiene un circuito SignalR contra `/_blazor`; no hay webhooks de negocio abiertos.

Las actualizaciones se publican en un bus en memoria con versiones por módulo
(`automation`, `articles`, `categories`, `comments`). El endpoint
`GET /admin/changes` solo devuelve esas versiones y se usa como respaldo cuando el
circuito no responde.

## Proxy reverso

El proxy debe permitir WebSocket y mantener el `Upgrade`:

```nginx
location /_blazor {
    proxy_pass http://127.0.0.1:8080;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_read_timeout 3600s;
}
```

Si el proxy bloquea WebSocket, Blazor cae a long polling y el panel sigue
funcionando, pero con mayor latencia. El polling de respaldo de `/admin/changes`
también sigue disponible.

## Una sola instancia

El bus de cambios es en proceso. El despliegue actual usa un solo contenedor, por lo
que todas las pestañas reciben los eventos.

Si se añaden réplicas:

- se necesita un backplane de SignalR (por ejemplo Redis) o change streams de MongoDB;
- sin backplane, cada réplica solo notifica a sus propios circuitos;
- conviene usar afinidad de sesión mientras no exista backplane.

## Límites

- `CmsRealtime:CoalesceWindowMilliseconds` agrupa ráfagas (250 ms por defecto).
- `CmsRealtime:SubscriberQueueCapacity` limita la cola por pestaña (256 por defecto).
- Un cliente lento descarta eventos antiguos y recibe una reconciliación; no bloquea
  a los demás ni a las escrituras.

## Rollback

Cada isla puede volver a la vista MVC anterior sin cambiar datos ni rutas. El bus y
el endpoint de versiones pueden quedar activos sin consumidores; no modifican la
persistencia. Para desactivar Blazor por completo, retirar los componentes de las
vistas y el script `~/_framework/blazor.server.js` del layout administrativo.
