# Fase 2 — Inyección del índice y servido de assets

Fecha: 2026-11-16
Objetivo: Jellyfin 12.4.0 (`net10.0`, `targetAbi 12.4.0.0`)

## Alcance

La Fase 2 conecta el plugin con la interfaz web sin escribir en disco ni tocar la
reproducción nativa. Se compone de cuatro piezas:

1. `PremiumUiStartupFilter` registra `WebIntegrationMiddleware` al inicio del
   pipeline, **antes** de `UseResponseCompression` y `UseStaticFiles`.
2. `IndexHtmlProvider` lee `index.html` del servidor, inyecta un bloque
   `<!-- premiumui:start --> … <!-- premiumui:end -->` justo antes de `</head>`
   y cachea el resultado en memoria. El archivo original nunca se modifica.
3. `AssetCatalog` descubre los recursos embebidos (`Web/app.js`, `Web/app.css`),
   calcula un hash SHA-256 corto para el nombre público y precalcula Brotli y Gzip.
4. `GET /PremiumUI/Bootstrap` entrega al frontend la configuración **no
   administrativa**, autenticado con la sesión normal de Jellyfin.

## Comportamiento HTTP

| Ruta | Resultado |
|---|---|
| `GET /web` | `301` a `/web/` conservando la query |
| `GET /web/` y `/web/index.html` | `200` con el bloque inyectado, `ETag` fuerte, `Cache-Control: no-cache` |
| `If-None-Match` coincidente | `304` sin cuerpo |
| `HEAD` | Cabeceras completas, sin cuerpo |
| `GET /web/premiumui/<asset>` | `200` desde memoria, `Cache-Control: public, max-age=31536000, immutable` |
| `Accept-Encoding: br`/`gzip` | Payload precomprimido con `Vary: Accept-Encoding` |
| Asset inexistente | `404` con `Cache-Control: no-store` |
| `BaseUrl` configurado | Se respeta (`/jf/web/…`) |

Gate de seguridad y compatibilidad:

- Si el plugin está deshabilitado, o Inicio y Detalles están apagados, el
  middleware deja pasar la petición sin inyectar.
- Si el servidor no es major 12, el middleware no se registra (modo pasivo).
- Cualquier excepción del middleware se registra en el buffer de diagnóstico y
  se propaga al pipeline normal de Jellyfin.

## Frontend incluido (0.2.0)

`Web/app.js` es un *stub* deliberado: marca `data-premiumui` en `<html>`,
obtiene `/PremiumUI/Bootstrap` y emite `premiumui:ready`. **No altera la
interfaz**; los estilos de Inicio y Detalles llegan en la Fase 3.

`Web/app.css` define solo tokens base (`--pui-*`) bajo `:root[data-premiumui]`,
respetando `prefers-reduced-motion`.

## Diagnóstico ampliado

`GET /PremiumUI/Diagnostics` (solo administradores) expone ahora:

- `AssetRequests`, `IndexRequests`, `Injections`, `BootstrapRequests`.
- `AssetCount` (assets embebidos detectados).
- Estado y versión del frontend.

El panel los muestra en la fila **Tráfico** del bloque Diagnóstico:
`Índice · Inyecciones · Assets (embebidos) · Bootstrap`.

## Simbología

El middleware se registra de forma no invasiva y calcula el `BaseUrl` por sí
mismo, porque corre fuera de `app.Map(config.BaseUrl)`. `UseMiddleware<T>`
crea la instancia por petición en .NET 10, por lo que los datos compartidos
(`PremiumUiWebAssets`, `IntegrationStatus`, `IBaseUrlProvider`) son singletons.

## Pruebas

`69` pruebas unitarias en verde:

- Configuración, XML y ciclo de vida.
- Negociación de codificación (`br`, `gzip`, `q=`, `*`).
- Catálogo de assets y nombres hasheados.
- Inyección de índice, caché por *timestamp*, BOM, ausencia de `</head>`.
- Middleware completo con `DefaultHttpContext`: `200`, `301`, `304`, `HEAD`,
  compresión, `BaseUrl`, desactivación y rutas ajenas.
- Creación del middleware vía `ActivatorUtilities` (DI real).

## Evidencias en el servidor 12.4.0

- Log: `2 assets embebidos (script "app.bb0dd1a22f.js")`.
- `/web/`: `200`, `X-Premium-UI: index`, `ETag: "1342a83cc567b42b"`,
  5635 bytes, bloque con `app.c898091167.css` y `app.bb0dd1a22f.js`.
- `If-None-Match` → `304`; `HEAD` → cabeceras sin cuerpo.
- `/web` → `301 /web/`; `/web/index.html` → `200` inyectado.
- Assets: identidad 1643 B, gzip 614 B, br 498 B; `304` por ETag; 404 para
  nombres inexistentes.
- `/PremiumUI/Diagnostics` y `/PremiumUI/Bootstrap` → `401` sin sesión.
- Sin errores nuevos en el log atribuibles al plugin.

## Pendiente (Fase 3)

- Frontend Vite/TypeScript real, con Enhance por defecto en Inicio y
  reestilizado de Detalles.
- Medición de tráfico y presupuesto de rendimiento.
- Documentación de reversión y presupuesto de tamaño.
