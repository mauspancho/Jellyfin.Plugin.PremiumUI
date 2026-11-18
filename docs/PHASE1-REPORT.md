# Fase 1 — Informe de implementación

**Fecha:** 2026-11-16
**Objetivo:** plugin compilable, configurable y empaquetable para Jellyfin **12.4.0**, sin activar todavía la inyección en el cliente.

---

## 1. Alcance entregado

| Elemento | Estado |
|---|---|
| Solución y proyecto `net10.0` contra `Jellyfin.Controller` / `Model` 12.4.0 | Completo |
| Configuración persistente + validador | Completo |
| Panel de administración (6 secciones) | Completo |
| `IStartupFilter` con gate de versión | Completo (solo registra estado) |
| Endpoint `GET /PremiumUI/Diagnostics` | Completo |
| Pruebas unitarias | 31 pruebas, todas correctas |
| ZIP instalable (`meta.json` + DLL) | Completo |
| Inyección de `index.html` y assets | **Fase 2** |
| Frontend Home/Details | **Fase 3** |

---

## 2. Archivos implementados

| Archivo | Responsabilidad |
|---|---|
| `src/.../Jellyfin.Plugin.PremiumUI.csproj` | `net10.0`, referencias 12.4.0, `FrameworkReference Microsoft.AspNetCore.App`, `configPage.html` embebido |
| `src/.../PremiumUiPlugin.cs` | `BasePlugin<PluginConfiguration>`, `IHasWebPages`, GUID, normalización al construir y al guardar |
| `src/.../PluginServiceRegistrator.cs` | `IntegrationStatus` singleton + `IStartupFilter` |
| `src/.../Hosting/PremiumUiStartupFilter.cs` | Gate `Major == 12`; log y estado; punto de inserción del middleware |
| `src/.../Configuration/PluginConfiguration.cs` | Esquema XML: General, Design, Home, Details, Performance |
| `src/.../Configuration/ConfigurationValidator.cs` | Clamps, listas permitidas, orden canónico sin duplicados |
| `src/.../Configuration/configPage.html` | Panel completo con CSS/JS dentro de `div[data-role="page"]` |
| `src/.../Api/PremiumUiController.cs` | Diagnóstico con `Policies.RequiresElevation` y DTOs PascalCase |
| `src/.../Diagnostics/IntegrationStatus.cs` | Estado thread-safe, contadores y ring buffer de 20 errores |
| `build/meta.json` | Manifiesto del plugin |
| `build/package.sh` | Build Release + ZIP verificado |
| `tests/.../ConfigurationValidatorTests.cs` | 20 pruebas de normalización |
| `tests/.../PluginWiringTests.cs` | 4 pruebas de identidad, recurso embebido y XML |
| `tests/.../PluginLifecycleTests.cs` | 4 pruebas de construcción real del plugin, guardado normalizado y recarga |

---

## 3. Decisiones relevantes

1. **`IStartupFilter` en lugar de parchear archivos.** Verificado que `ApplicationHost.Init` registra los servicios de plugins antes de construir el host web y que `UseStartup` aplica los filtros de ASP.NET Core.
2. **Gate por `ApplicationVersion.Major`.** `targetAbi` es un mínimo; el gate evita que el plugin intente actuar en un servidor no soportado.
3. **Sin dependencias en el ZIP.** Solo el DLL y `meta.json`: se evita cargar una copia local de `IStartupFilter` con identidad de tipo distinta.
4. **Panel con JS dentro del `div[data-role="page"]`.** Es el patrón que `viewContainer.parseHtml` espera en 12.4; así el script se ejecuta al insertar la vista.
5. **Diagnóstico separado de la configuración.** El panel usa los endpoints nativos `/Plugins/{id}/Configuration` (elevados) y el endpoint propio solo expone estado y contadores.
6. **Configuración global.** No hay configuración por usuario en esta fase.

### Corrección detectada durante las pruebas

`XmlSerializer` **agrega** los elementos del XML a la colección ya inicializada de la propiedad. Con una lista por defecto de seis secciones, cada recarga duplicaba el orden. Se corrigió dejando `SectionOrder` vacía por defecto y aplicando el orden canónico en `ConfigurationValidator.Normalize`. Hay una prueba de regresión que serializa, deserializa y normaliza.

---

## 4. Evidencias

### Compilación

```
Compilación correcta.
    0 Advertencia(s)
    0 Errores
```

### Pruebas

```
Correctas! - Con error: 0, Superado: 31, Omitido: 0, Total: 31
```

### Paquete

```
Archive:  dist/Jellyfin.PremiumUI_1.0.0.0.zip
  Length      Date    Time    Name
     77824  ...            Jellyfin.Plugin.PremiumUI.dll
       738  ...            meta.json
```

### `meta.json`

```json
{
  "guid": "a15478cd-8875-46af-b528-e3c020f2cd71",
  "name": "Jellyfin Premium UI",
  "version": "1.0.0.0",
  "targetAbi": "12.4.0.0",
  "framework": "net10.0",
  "status": "Active",
  "artifacts": ["Jellyfin.Plugin.PremiumUI.dll"]
}
```

---

## 5. Validación pendiente (bloqueada)

No se ha podido validar la carga real del plugin porque el usuario no tiene permiso sobre `/var/run/docker.sock`:

```
permission denied while trying to connect to the Docker daemon socket
```

Pasos para completar la validación de Fase 1:

1. Copiar el ZIP a `/home/maus/docker/jellyfin/config/data/plugins/Jellyfin.PremiumUI_1.0.0.0/` (o instalar desde el Dashboard).
2. Reiniciar el contenedor.
3. Comprobar en **Dashboard → Plugins** que aparece activo y sin estado *Malfunctioned*.
4. Abrir la configuración: los valores por defecto deben cargarse y guardarse.
5. Consultar el diagnóstico: `Supported: true`, `MiddlewareRegistered: true`, `InjectionActive: false`, versión `12.4.0`.
6. Verificar en los logs que no hay excepciones de carga ni de serialización.

Criterio para abrir Fase 2: el panel funciona, guarda y el diagnóstico confirma el filter registrado en 12.4.0.

---

## 6. Riesgos abiertos

| Riesgo | Estado |
|---|---|
| Sin validación en servidor real | Abierto (permisos Docker) |
| Nombre del plugin en el Dashboard | A verificar visualmente |
| Serialización de la clase derivada en `/Plugins/{id}/Configuration` | A verificar con la instancia real |
| Compatibilidad futura 12.x | Cubierta por el gate de major |
