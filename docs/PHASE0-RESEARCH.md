# Jellyfin Premium UI — Fase 0: Informe de investigación

**Fecha:** 2026-11-16 (revisión 2)
**Objetivo:** determinar la arquitectura y el método real de integración con **Jellyfin 12.4.0** (versión más actual instalada) y fijar los criterios de las fases siguientes.
**Estado:** investigación completada y validada contra el código fuente de los tags `v12.4`. Fase 1 implementada.

> Revisión 1 de este informe se redactó para 10.11.11. Se descartó ese objetivo por indicación expresa: el plugin debe apuntar a la versión más actual, **12.4.0**.

---

## 1. Resumen ejecutivo

1. **El objetivo es el servidor local real:** Jellyfin **12.4.0** (`http://localhost:8096`, `ServerName: Arch`). No se degrada la instalación ni se crea una instancia 10.11.
2. **Stack verificado:** Jellyfin 12.4 apunta a **`net10.0`**; los paquetes `Jellyfin.Controller` y `Jellyfin.Model` **12.4.0** existen en NuGet. SDK instalado: **.NET 10.0.404**.
3. **`IHasWebPages` no inyecta JavaScript en el cliente.** Solo publica el HTML de configuración para el Dashboard (`GET /web/ConfigurationPage?name=...`). No modifica `index.html`.
4. **Método de integración elegido: `IStartupFilter` + middleware**, registrado desde `IPluginServiceRegistrator`. Verificado en 12.4: `ApplicationHost.Init` registra los servicios de plugins antes de construir el host web, y `WebHostBuilderExtensions` usa `.UseStartup(...)`, por lo que ASP.NET Core aplica los `IStartupFilter` del contenedor.
5. **Diseño no invasivo:** el índice `/web/` se sirve desde memoria con un bloque inyectado; los assets del plugin se sirven desde recursos embebidos con nombres hasheados. **No se escribe nada en disco** ni se tocan los archivos de Jellyfin Web.
6. **Reversibilidad total:** sin middleware no hay inyección; al desinstalar y reiniciar, la UI queda intacta. El `index.html` original nunca se modifica.
7. **Gate de compatibilidad por major:** `targetAbi` es una versión **mínima** (un servidor futuro seguiría cargando el plugin). El middleware comprueba `IServerApplicationHost.ApplicationVersion.Major == 12` y, si no coincide, queda pasivo.
8. **Cliente web 12.4 híbrido:** el layout por defecto es `modern` (React), pero Inicio sigue renderizando DOM legacy (`#homeTab`, `.sections`) y Detalles es una **ruta legacy** (`itemDetails/index`, `#itemDetailPage`) dentro del router moderno. Eso mantiene válidos `viewshow`/`viewbeforehide` para el ciclo de vida.

---

## 2. Entorno verificado

| Elemento | Valor comprobado | Estado |
|---|---|---|
| Servidor local | Jellyfin `12.4.0`, `ServerName: Arch` | **Objetivo del plugin** |
| Contenedor | `lscr.io/linuxserver/jellyfin:latest` | En uso; no modificar sin permiso |
| Config local | `/home/maus/docker/jellyfin/config` | Instalación real |
| Plugins | `/home/maus/docker/jellyfin/config/data/plugins` | Ruta de instalación del ZIP |
| Docker CLI | `permission denied` sobre `/var/run/docker.sock` | **Bloquea reiniciar/validar** |
| .NET SDK | `10.0.404` en `/home/maus/.dotnet` | OK |
| Node.js / npm | `v26.13.1` / `12.2.2` | OK para el frontend de Fase 3 |
| Web servido | `/web/` responde 200; `main.jellyfin.*.js` presente | OK |
| targetAbi real | `12.4.0.0` | Coherente con `Jellyfin.Controller 12.4.0` |

---

## 3. Fuentes inspeccionadas

| Fuente | Revisión | Uso |
|---|---|---|
| `jellyfin/jellyfin` | tag `v12.4` | Pipeline, plugins, APIs, autorización, serialización |
| `jellyfin/jellyfin-web` | tag `v12.4` | `index.html`, layout moderno/legacy, router, viewManager, config pages |
| `jellyfin/jellyfin-plugin-template` | última | Estructura de plugin y `configPage.html` |
| `jellyfin-apiclient` | 1.11.0 (usado por web 12.4) | Métodos disponibles en `window.ApiClient` |
| NuGet | `Jellyfin.Controller` / `Jellyfin.Model` `12.4.0` | Dependencias de compilación |
| `G-grbz/Jellyfin-MonWUI-Plugin` | 3.7.0 | Referencia funcional/visual, **sin reutilizar código** |

Los clones de investigación están en `/tmp/opencode/research/jellyfin12` y `/tmp/opencode/research/jellyfin-web12` (temporales).

---

## 4. Pipeline real de Jellyfin 12.4

### 4.1 Arranque y registro de plugins

1. `Jellyfin.Server/Program.cs` construye el host y llama a `ConfigureWebHostBuilder(...)`.
2. `Jellyfin.Server/Extensions/WebHostBuilderExtensions.cs` configura el host con `.UseStartup(context => new Startup(appHost, context.Configuration))`.
3. `ApplicationHost.Init(IServiceCollection)` ejecuta `RegisterServices(serviceCollection)` y después `_pluginManager.RegisterServices(serviceCollection)` (`ApplicationHost.cs:490`).
4. `PluginManager.RegisterServices` instancia cada `IPluginServiceRegistrator` de plugins **habilitados y soportados** (`IsEnabledAndSupported`) y ejecuta `RegisterServices(IServiceCollection, IServerApplicationHost)`.
5. ASP.NET Core resuelve los `IStartupFilter` del contenedor y envuelve el `Configure` de `Startup`.

**Consecuencia:** un middleware registrado desde un `IStartupFilter` se ejecuta **antes** de todo el pipeline web de Jellyfin.

### 4.2 Orden del pipeline web (`Startup.Configure`, 12.4)

```
app.Map(config.BaseUrl, mainApp => {
    mainApp.UseMiddleware<ExceptionMiddleware>();
    mainApp.UseMiddleware<ResponseTimeMiddleware>();
    mainApp.UseWebSockets();
    mainApp.UseResponseCompression();
    mainApp.UseCors();
    mainApp.UseRequestLocalization();
    if (config.RequireHttps) mainApp.UseHttpsRedirection();
    // if HostWebClient():
    mainApp.UseDefaultFiles(new DefaultFilesOptions { FileProvider = PhysicalFileProvider(WebPath), RequestPath = "/web" });
    mainApp.UseStaticFiles(new StaticFileOptions { ... });   // /web
    mainApp.UseStaticFiles();                               // resto
    ...
});
```

Nuestro middleware, al ejecutarse antes, ve la ruta **con** el prefijo `BaseUrl` y antes de `UseResponseCompression` / `UseDefaultFiles` / `UseStaticFiles`.

### 4.3 Opciones de integración evaluadas

| Mecanismo | Qué permite realmente en 12.4 | Veredicto |
|---|---|---|
| `IHasWebPages` + `PluginPageInfo` | Página del Dashboard servida desde recurso embebido. No toca el cliente principal. | Solo panel admin |
| `IStartupFilter` + middleware | Inyección en memoria del índice y servido de assets. Sin escrituras. Reversible. | **Elegido** |
| Sustituir `IWebHostEnvironment.WebRootFileProvider` | No afecta a `/web`: `Startup` crea un `PhysicalFileProvider(WebPath)` explícito. | Descartado |
| Parchear `index.html` / `.gz` / `.br` | Modifica la instalación, deja backups, se rompe con actualizaciones. | **Rechazado** |
| `Branding.CustomCss` | Solo CSS global, sin JavaScript. | Insuficiente |
| `sub_filter` en proxy inverso | Fuera del plugin, frágil, no distribuible. | Rechazado |

### 4.4 Diseño de integración (igual que en Fase 1, ampliado en Fase 2)

1. **Gate de versión:** `ApplicationVersion.Major != 12` → modo pasivo (solo log y diagnóstico).
2. **Gate de configuración:** `Enabled`, `EnableHome`, `EnableDetails` evaluados por petición sin bloqueos.
3. **Índice:**
   - Rutas `GET/HEAD` que terminen en `/web` (301 a `/web/`), `/web/` o `/web/index.html`, con cualquier prefijo de `BaseUrl`.
   - `index.html` leído de `IApplicationPaths.WebPath`, cacheado en memoria y revalidado por `LastWriteTimeUtc` (sin `FileSystemWatcher`, sin polling).
   - Inyección antes de `</head>` de un bloque marcado `<!-- premiumui:start/end -->` con `<link>` y `<script type="module">` de rutas **relativas** (`premiumui/app.<hash>.js`).
   - `Content-Type: text/html; charset=utf-8`, `Content-Length`, `ETag` fuerte, `Cache-Control: no-cache`, `Vary: Accept-Encoding`; `If-None-Match` → `304`; `HEAD` sin cuerpo.
   - HTML siempre en identidad (sin `Content-Encoding`): evita toda la problemática de compresión del documento.
   - Si `index.html` no existe o no contiene `</head>`: no interceptar, dejar pasar y registrar diagnóstico.
4. **Assets:** `/web/premiumui/<archivo>` desde recursos embebidos (catálogo en memoria, sin resolver rutas del sistema de archivos → sin path traversal), `Cache-Control: public, max-age=31536000, immutable`, negociación `br` → `gzip` → identidad, `404` si el plugin está desactivado.
5. **Panel admin:** `configPage.html` embebido vía `IHasWebPages`.
6. **API:** `GET /PremiumUI/Bootstrap` (`[Authorize]`, subconjunto no administrativo) y `GET /PremiumUI/Diagnostics` (`Policies.RequiresElevation`). La lectura/escritura completa usa los endpoints nativos `/Plugins/{id}/Configuration`.
7. **Desactivación/desinstalación:** sin escrituras; al desinstalar y reiniciar no se registra el middleware.

---

## 5. Cliente web 12.4: qué es igual y qué cambia

### 5.1 Layouts y rutas

- `src/components/layoutManager.js`: `LayoutMode.Modern` es el layout por defecto; `DesktopLegacy`, `MobileLegacy` y `Tv` usan la app legacy. `apphost.getDefaultLayout()` devuelve `modern`.
- `src/apps/modern/routes/home.tsx` monta la página de Inicio moderna, pero **reutiliza el DOM legacy**: `#indexPage.homePage`, `#homeTab`, `.sections`, y el módulo `hometab.js` + `homesections.js`.
- `src/apps/modern/routes/legacyRoutes/user.ts`: `details` → controlador `itemDetails/index` y vista `itemDetails/index.html` (ruta legacy dentro del router moderno). Esto mantiene `#itemDetailPage` y los eventos de vista.
- `src/components/router/routerHistory.ts`: `HISTORY_UPDATE` se emite con el registro interno `Events.trigger(document, 'HISTORY_UPDATE', ...)`, **no** como evento DOM. Los listeners DOM no lo reciben.

**Consecuencia para el frontend:** detectar navegación parcheando `history.pushState`/`replaceState` + `popstate`/`hashchange`, y confirmar el DOM con un `MutationObserver` acotado. Los eventos `viewshow` / `viewbeforehide` / `pageshow` siguen siendo válidos para Home y Details.

### 5.2 Ciclo de vida de vistas (`viewManager.js`)

- `viewbeforehide`, `viewhide`, `viewinit`, `viewbeforeshow`, `viewshow` con `bubbles: true`.
- `dispatchPageEvents` está activo por defecto: también emite `pagebeforehide`, `pagehide`, `pageinit`, `pagebeforeshow`, `pageshow`.
- Los eventos se emiten sobre el elemento de la vista, pero **burbujean** al `document`.

### 5.3 Página de configuración de plugin

- El Dashboard 12.4 enlaza a `/{dashboard}/configurationpage?name=<Name>` y renderiza con `ServerContentPage` → `viewManager.loadView`.
- El HTML se obtiene de `GET /web/ConfigurationPage?name=<Name>` (controlador `DashboardController`), que devuelve el recurso embebido del plugin.
- `viewContainer.parseHtml` extrae `div[data-role="page"]` del documento completo; el `<style>` y el `<script>` deben ir **dentro** de ese div (patrón oficial del template).
- Con `window.$` disponible (jQuery se expone globalmente en 12.4), la vista se inserta con jQuery y el script inline se ejecuta.
- La página coincide por **PluginId** (`findBestConfigurationPage`), no por nombre.
- La API de configuración (`/Plugins/{id}/Configuration`) usa **PascalCase** (`JsonDefaults.PascalCaseOptions`).

### 5.4 APIs y clases verificadas en 12.4

| API / clase | Ensamblado / namespace | Uso previsto |
|---|---|---|
| `BasePlugin<TConfiguration>` | `MediaBrowser.Common.Plugins` | Plugin y configuración XML |
| `IHasWebPages` / `PluginPageInfo` | `MediaBrowser.Model.Plugins` | Página de Dashboard |
| `IPluginServiceRegistrator` | `MediaBrowser.Controller.Plugins` | Registro de servicios y `IStartupFilter` |
| `IStartupFilter` | `Microsoft.AspNetCore.Hosting` | Inyección de middleware |
| `IApplicationPaths.WebPath` | `MediaBrowser.Common.Configuration` | Localizar `index.html` |
| `IServerApplicationHost.ApplicationVersion` | `MediaBrowser.Common` | Gate de compatibilidad major 12 |
| `Policies.RequiresElevation` | `MediaBrowser.Common.Api` | Diagnóstico solo admin |
| `BasePluginConfiguration` | `MediaBrowser.Model.Plugins` | Configuración global |
| `ApiClient` (JS) 1.11.0 | `jellyfin-apiclient` | `getItems`, `getLatestItems`, `getImageUrl`, `getCurrentUser`, `getPluginConfiguration`, `updatePluginConfiguration` |
| `viewshow` / `viewbeforehide` / `pageshow` | `viewManager.js` 12.4 | Ciclo de vida por ruta sin polling |

Notas:

- `PluginManager` solo registra servicios de plugins **habilitados y soportados**.
- `targetAbi` es una versión **mínima**: por eso el gate por `ApplicationVersion` es obligatorio.
- Los controladores del plugin se añaden como `ApplicationPart` y comparten las políticas de Jellyfin.
- **No hay CSP** en las fuentes 12.4. Aun así, el frontend usará assets same-origin, sin `eval` y sin `innerHTML` con contenido no confiable.
- jQuery 3.7.1 existe y se expone como `window.$`; el plugin no lo usará (solo el HTML de configuración se beneficia de su carga automática).

---

## 6. Dependencias y versiones

### Backend

| Dependencia | Versión | Notas |
|---|---|---|
| Target framework | `net10.0` | Igual que Jellyfin 12.4 |
| `Jellyfin.Controller` | `12.4.0` | `ExcludeAssets="runtime"` |
| `Jellyfin.Model` | `12.4.0` | `ExcludeAssets="runtime"` |
| `Microsoft.AspNetCore.App` | `FrameworkReference` | `IStartupFilter`; **no** copiar assemblies al ZIP |
| `targetAbi` | `12.4.0.0` | Mínimo |
| `CopyLocalLockFileAssemblies` | `false` | No duplicar dependencias de Jellyfin |

**Riesgo evitado:** si se copiara `Microsoft.AspNetCore.*` al directorio del plugin, el `PluginLoadContext` cargaría otra identidad de `IStartupFilter` y el filtro se ignoraría en silencio. El ZIP solo contiene el DLL del plugin y `meta.json`.

### Frontend (solo build, Fase 3)

| Dependencia | Versión | Notas |
|---|---|---|
| Vite | 8.x | Build estático, sin runtime Node |
| TypeScript | 5.9.x | `strict` |
| Vitest | 5.x | Pruebas unitarias |

Sin React/Vue/Angular/jQuery en el plugin.

---

## 7. Arquitectura

```
Navegador
  └─ Jellyfin Web 12.4 (/web/)
       ├─ index.html original (nunca se modifica en disco)
       └─ inyectado en memoria por Premium UI:
            ├─ premiumui/app.<hash>.js      (módulo ES)
            ├─ premiumui/app.<hash>.css
            └─ premiumui/assets.json

Premium UI (frontend)
  ├─ core/     init, navegación, lifecycle, observers, config, errores
  ├─ api/      ApiClient, consultas, imágenes, caché TTL + dedupe, AbortController
  ├─ home/     hero, carruseles, tarjetas, secciones
  ├─ details/  backdrop, metadatos, reparto, relacionado
  └─ styles/   variables, tema, home, details, tarjetas, responsive

Backend (plugin)
  ├─ Plugin + configuración XML + validación
  ├─ IStartupFilter + middleware de integración
  ├─ catálogo de assets embebidos + compresión + caché HTTP
  ├─ controladores /PremiumUI/Bootstrap y /PremiumUI/Diagnostics
  └─ diagnóstico (estado, contadores, ring buffer de errores)
```

### Decisiones de diseño del frontend

- **Sin tocar el reproductor.** El botón Reproducir reutiliza el flujo nativo; en Detalles se conservan los botones nativos.
- **Reutilización de DOM y datos.** Detalles ya renderiza backdrop, metadatos, reparto y “More Like This”: se reestiliza y reorganiza, sin repetir consultas.
- **Home: Enhance vs Replace.** Dos modos:
  - **Enhance (recomendado):** reutilizar las secciones nativas ya cargadas, reestilizarlas y añadir Hero. Cero consultas duplicadas.
  - **Replace:** ocultar secciones nativas y construir carruseles propios con consultas deduplicadas.
  - En Fase 3 se medirá el tráfico de ambos y se decidirá con datos.
- **Caché y deduplicación:** TTL en memoria + coalescencia de promesas idénticas; nunca dos peticiones concurrentes iguales.
- **Ciclo de vida:** listeners en `document`, observers y fetches creados al entrar y destruidos al salir. Sin polling.

---

## 8. Estructura del proyecto (estado real tras Fase 1)

```
/home/maus/docker/plugin/
├── Jellyfin.PremiumUI.slnx
├── Directory.Build.props
├── global.json
├── build/
│   ├── meta.json
│   └── package.sh
├── src/Jellyfin.Plugin.PremiumUI/
│   ├── Jellyfin.Plugin.PremiumUI.csproj
│   ├── PremiumUiPlugin.cs
│   ├── PluginServiceRegistrator.cs
│   ├── Configuration/
│   │   ├── PluginConfiguration.cs
│   │   ├── ConfigurationValidator.cs
│   │   └── configPage.html
│   ├── Hosting/
│   │   └── PremiumUiStartupFilter.cs
│   ├── Api/
│   │   └── PremiumUiController.cs
│   └── Diagnostics/
│       └── IntegrationStatus.cs
├── tests/Jellyfin.Plugin.PremiumUI.Tests/
│   ├── ConfigurationValidatorTests.cs
│   └── PluginWiringTests.cs
├── docs/
│   ├── PHASE0-RESEARCH.md
│   └── PHASE1-REPORT.md
└── dist/Jellyfin.PremiumUI_1.0.0.0.zip
```

---

## 9. Estrategia de rendimiento

| Métrica | Objetivo | Medición |
|---|---|---|
| Bundle principal JS | < 100 KB gzip | script de build |
| CSS | < 35 KB gzip | script de build |
| Inicialización (sin red) | < 100 ms | `performance.mark` + Playwright |
| Consultas duplicadas concurrentes | 0 | contador de caché + red E2E |
| Errores JS no controlados | 0 | `window.onerror` / `unhandledrejection` |
| Observers tras salir de ruta | 0 | tests de lifecycle |

Medidas obligatorias: `IntersectionObserver`, `loading="lazy"`, `decoding="async"`, imágenes con ancho ajustado a la densidad, `AbortController` por vista, caché TTL, sin autoplay ni vídeos de fondo, `prefers-reduced-motion`, animaciones con `transform`/`opacity`.

---

## 10. Plan de pruebas

### Unitarias backend (xUnit) — implementadas en Fase 1

- Normalización de configuración: rangos, enums, orden, duplicados, valores desconocidos.
- Identidad del plugin, recurso embebido y versión.
- Round-trip XML y ausencia de duplicados en listas.

Pendientes para Fase 2: inyección del índice (idempotencia, `</head>` ausente, ETag, `304`, `HEAD`), catálogo de assets (MIME, path traversal, compresión negociada) y gate de compatibilidad.

### Unitarias frontend (Vitest + jsdom) — Fase 3

Detección de rutas, limpieza de listeners/observers, caché y dedupe, sanitización, render de tarjetas, movimiento reducido.

### Integración real (Jellyfin 12.4.0)

- Instalar ZIP, iniciar, login por API.
- `GET /web/` contiene el marcador y las rutas hasheadas.
- Assets: `200`, MIME, `ETag`, gzip/br/identidad, `304`.
- Config API: `401` sin token, `403` para usuario no admin, `200` para admin.
- Desactivar → sin inyección; reactivar → inyección.
- Hash de `index.html` idéntico antes/después.
- Desinstalar → reiniciar → sin marcador ni assets.

### Matriz manual/E2E

1920×1080, 1366×768, 1024×768, 768×1024, 390×844. Comprobar Inicio, Detalles, carruseles, Hero, botones nativos, navegación, back/forward, tema oscuro y movimiento reducido.

---

## 11. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Sin acceso a Docker | No se puede validar en el servidor | Pedir permiso/credenciales; el ZIP y los tests quedan listos |
| `targetAbi` es mínimo | Un 13.x cargaría el plugin | Gate explícito por `ApplicationVersion.Major` |
| Cambios de pipeline en 12.x | El middleware no intercepta | Diagnóstico de estado + fallback pasivo; pruebas por versión |
| Cambios de DOM en web 12.x | La UI no se inserta | Detección de features, no-op seguro, selectores encapsulados |
| Duplicación de consultas en Inicio | Consumo innecesario | Enhance por defecto; medir antes de Replace |
| Proxies/CDN cachean HTML | Versión antigua o sin inyección | `no-cache` + ETag; rutas relativas |
| CSP futura | Scripts bloqueados | Assets externos same-origin, sin inline/eval |
| Serialización XML de listas | Duplicados al recargar | Lista por defecto vacía + normalización (corregido en Fase 1) |
| Licencia | Incumplimiento | Implementación propia; GPL-3.0 |

---

## 12. Licencia

El plugin se distribuye bajo **GPL-3.0**, compatible con Jellyfin y su ecosistema. No se copia código de MonWUI ni de File Transformation; solo se usan ideas funcionales y visuales implementadas desde cero.

---

## 13. Identidad del plugin

- Nombre visible: `Jellyfin Premium UI`
- Ensamblado: `Jellyfin.Plugin.PremiumUI`
- GUID: `a15478cd-8875-46af-b528-e3c020f2cd71`
- Versión inicial: `1.0.0.0`
- `targetAbi`: `12.4.0.0`
- Framework: `net10.0`
