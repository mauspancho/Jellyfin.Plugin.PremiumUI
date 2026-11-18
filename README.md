# Jellyfin Premium UI

Plugin para **Jellyfin 12.4.0** que mejora únicamente la **pantalla de inicio** y las **páginas de detalles** de Jellyfin Web con una capa visual cinematográfica, sin modificar los archivos de Jellyfin ni el reproductor.

- Servidor objetivo: Jellyfin **12.4.0** (`targetAbi 12.4.0.0`, `net10.0`).
- Integración no invasiva: middleware en memoria; no escribe en el sistema de archivos.
- Reversible: al desinstalar y reiniciar, la interfaz original queda intacta.
- Configuración global desde **Dashboard → Plugins → Jellyfin Premium UI**.
- Sin frameworks de frontend (no React/Vue/Angular/jQuery en el plugin).

## Estado

**Fase 3 completada:** además de inyectar su bloque en `/web/` y servir assets
hasheados comprimidos, el plugin aplica la capa visual en **Inicio** y **Detalles**
mediante un frontend propio (≈ 48 KB JS + 34 KB CSS, ≈ 14 KB + 5 KB comprimidos con Brotli) y validado en un servidor real 12.4.0 con Chromium.

- Estrategia **Enhance**, no *Replace*: la interfaz nativa sigue cargando datos,
  reproduciendo y navegando; el plugin solo añade hero y estilos.
- Inicio y Detalles se pueden desactivar por separado; si ambos están apagados, la
  interfaz se sirve sin inyección.
- En servidores que no sean major 12, el middleware no se registra.
- El panel muestra contadores reales de índice, inyecciones, assets y bootstrap, y
  la versión del frontend.

## Requisitos

### Para usar el plugin

- Jellyfin **12.4.0**.
- Nada más: el plugin no incluye dependencias de runtime.

### Para compilar

- .NET SDK **10.0.x** (probado con `10.0.404`).
- Node.js **20+** y npm (probado con Node 26 / npm 12) para el frontend.
- `python3`, `zip` y `unzip` para el empaquetado.

## Compilar y empaquetar

```bash
cd Jellyfin.Plugin.PremiumUI
./build/package.sh
```

El script compila el frontend con Vite, lo deja en `src/Jellyfin.Plugin.PremiumUI/Web/`
(`app.js` y `app.css`), compila el backend en Release y genera
`dist/Jellyfin.PremiumUI_1.0.0.0.zip` con:

```
Jellyfin.Plugin.PremiumUI.dll
meta.json
```

Opciones:

```bash
./build/package.sh --no-build    # reutiliza la compilación Release existente
DOTNET=/ruta/a/dotnet ./build/package.sh
```

Si falta `frontend/node_modules`, el script ejecuta `npm ci` automáticamente.

## Frontend

```bash
cd frontend
npm ci
npm run build       # genera app.js y app.css y los minifica con esbuild
npm run typecheck   # TypeScript en modo estricto
npm run test:smoke  # prueba jsdom del orden, el hero y Detalles
```

El bundle se escribe directamente en `src/Jellyfin.Plugin.PremiumUI/Web/` porque el
catálogo del servidor calcula sus propios nombres hasheados a partir de esos archivos.

## Pruebas

```bash
DOTNET=${DOTNET:-dotnet}
"$DOTNET" test tests/Jellyfin.Plugin.PremiumUI.Tests/Jellyfin.Plugin.PremiumUI.Tests.csproj
cd frontend && npm run test:smoke
```

## Instalación

1. Detén Jellyfin (o prepárate para reiniciarlo).
2. En el servidor, crea la carpeta del plugin (en 12.4 vive en el directorio de datos, no en `/config/plugins`):

   ```bash
   mkdir -p /ruta/config/data/plugins/Jellyfin.PremiumUI_1.0.0.0
   ```

3. Copia el contenido del ZIP dentro de esa carpeta (`Jellyfin.Plugin.PremiumUI.dll` y `meta.json`).
4. Inicia Jellyfin.
5. Entra en **Dashboard → Plugins** y comprueba que aparece **Jellyfin Premium UI** en estado activo.
6. Abre su configuración y revisa la sección **Diagnóstico**.

La carpeta exacta depende de la plataforma (Docker, Linux nativo, Windows); en Docker suele ser `./config/data/plugins` dentro del volumen de configuración. También puedes descargar el ZIP de la última versión desde **GitHub → Releases**.

Instalación alternativa: **Dashboard → Plugins → Instalar** y subir el ZIP (requiere permisos de administrador y reinicio).

## Desinstalación

1. **Dashboard → Plugins → Jellyfin Premium UI → Desinstalar**, o elimina la carpeta del plugin:

   ```bash
   rm -rf /ruta/config/data/plugins/Jellyfin.PremiumUI_1.0.0.0
   ```

2. Reinicia Jellyfin.
3. La configuración guardada (`config/data/plugins/configurations/Jellyfin.Plugin.PremiumUI.xml`) puede borrarse si no se va a reinstalar.

No hay archivos modificados que restaurar: el plugin nunca escribe en Jellyfin Web.

## Configuración

Secciones del panel:

| Sección | Contenido |
|---|---|
| General | Activar plugin, Inicio y Detalles |
| Diseño | Tema, color de acento, tamaño de tarjetas, transparencia, altura del Hero, transiciones, intensidad del hover |
| Inicio | Hero (solo en Inicio), fuente del contenido, carruseles (incluida la fila de recomendaciones), orden de secciones, elementos por carrusel, colecciones por marca (checkboxes agrupados de los estudios del servidor) |
| Detalles | Backdrop, metadatos, información adicional, contenido relacionado |
| Rendimiento | Precarga, lazy loading, caché, movimiento reducido |
| Diagnóstico | Versión del servidor y del plugin, estado de integración, contadores y errores recientes |

Los valores se validan y normalizan en el servidor antes de guardarse: los números se limitan a su rango y los valores de lista se ajustan a las opciones permitidas.

Guardar no exige recargar: el panel invalida la configuración cacheada en el navegador y pide una copia nueva, así que al volver a Inicio o Detalles ya se ve lo guardado. La navegación normal entre vistas sigue reutilizando la caché con TTL para no repetir peticiones al servidor.

### Bibliotecas de Películas y Series

Desde la versión 0.10.0 el Hero vive únicamente en Inicio. Las páginas `#/movies` y `#/tv` no reciben carrusel: el plugin solo marca la página con `data-pui-library` para que las tarjetas de la cuadrícula usen el mismo hover de Inicio (elevación, anillo de acento y sombra) con la intensidad configurada en Diseño, sin JS recurrente ni peticiones adicionales.

### Colecciones por estudio

La opción "Colecciones por estudio" añade una fila de tiles apaisados con el logo de cada marca seleccionada. El panel lee todos los estudios que existen en el servidor y los agrupa por marca: "Disney" reúne Disney Channel, DisneyToon Studios, Walt Disney Pictures y todas sus variantes, y lo mismo ocurre con Marvel, DC, Warner Bros., Sony Pictures, 20th Century Fox, HBO, BBC, Nickelodeon, Cartoon Network, etc. Los estudios que solo cambian por el sufijo corporativo ("Working Title" / "Working Title Films") también se unen, mientras que nombres parecidos de empresas distintas ("Nippon Columbia" frente a Sony/Columbia, "Atomic Cartoons" frente a Cartoon Network) quedan separados. El número junto al nombre indica cuántos estudios incluye el grupo y el buscador lo encuentra por el nombre de cualquiera de sus miembros.

La lista admite hasta 40 selecciones y, si no se marca ninguna, se usan los cinco sugeridos: Disney, Marvel, DC, Paramount y Netflix. La sección se crea de inmediato justo debajo del hero. Los tiles se centran horizontalmente y su posición se ajusta en "Orden de secciones", donde figura como "Estudios". Los logos y los bucles de vídeo están incluidos en el plugin (WebP + MP4), tomados de Jellyfin-MonWUI-Plugin (GPL-3.0): Disney, Walt Disney Pictures, Marvel Studios, DC, Paramount, Netflix, Pixar, Universal, Warner Bros., Columbia, DreamWorks y Lucasfilm. Al pasar el cursor el tile reproduce el bucle del estudio (carga diferida en el primer hover y desactivado con movimiento reducido); la elevación se aplica a una superficie interior y el enlace mantiene un área sensible fija, de modo que el tile no tiembla ni pierde clics cerca de los bordes. Las marcas sin arte incluido usan el logo del servidor. Un clic abre la colección completa: si la marca tiene varios estudios, el plugin muestra una capa propia con todos sus títulos a la vez (`Items?StudioIds=…&IncludeItemTypes=Movie,Series`), rejilla de pósters, orden por título/año/añadidos, paginación de 60 en 60 y enlaces a la ficha nativa. Si el grupo tiene un solo estudio, el enlace conserva la lista nativa de Jellyfin (`#/list?type=Movie&studioId=…&serverId=…`), que también mezcla películas y series. Los logos y vídeos curados se sirven desde el propio plugin (`/web/premiumui/studios/…`) con nombres estables y `?v=<versión>`. Al desactivar la opción la fila desaparece.

### Créditos del arte de estudios

Los archivos de `studios/` (logos WebP y bucles MP4) provienen de [Jellyfin-MonWUI-Plugin](https://github.com/G-grbz/Jellyfin-MonWUI-Plugin), GPL-3.0, la misma licencia de este plugin. Los nombres y logotipos son marcas de sus titulares. Ver `THIRD-PARTY-NOTICES.md`.

### Fila "Mis medios"

La fila nativa de accesos a las bibliotecas ("Mis contenidos" en español de España, "Mis medios" en es-419/es-MX, "My Media" en inglés) es una sección más del orden: aparece como "Mis medios" en "Orden de secciones" y puede colocarse, por ejemplo, debajo de los logos de estudio. El check "Mis medios" de la sección Inicio la oculta por completo (clase `pui-section-hidden`, sin tocar el DOM de Jellyfin) y el espacio lo ocupa la siguiente fila. La migración de esquema 2 → 3 coloca `media` justo después de `estudios` en configuraciones existentes.

### Recomendaciones de Inicio

El check "Recomendaciones" crea una fila propia del plugin (no reemplaza ninguna fila nativa) con películas y series sugeridas para el usuario de la sesión. La lista se pide a `Items/Suggestions` con su `userId`; si el usuario acaba de crearse, no tiene historial o la API no devuelve resultados, el plugin consulta `Items?SortBy=Random&IncludeItemTypes=Movie,Series` sobre toda la biblioteca, de modo que la fila siempre tiene contenido si hay títulos con imagen. Las tarjetas llevan póster (o `Thumb` como respaldo), título, año y duración, abren la ficha del título y comparten el hover de acento de Inicio. La fila no muestra barra de desplazamiento inferior: usa los mismos botones laterales que Películas y Series (chevrons en la esquina superior derecha), que avanzan un 80 % del ancho visible, se deshabilitan en los extremos y se ocultan si no hay desbordamiento; el desplazamiento nativo con rueda, trackpad, teclado y gesto táctil sigue funcionando. La respuesta se guarda en memoria con el TTL de "Caché" por usuario y número de elementos; no hay polling ni peticiones fuera de ese intervalo, las imágenes usan carga diferida y con movimiento reducido se desactiva la elevación. Si ambas peticiones fallan, la fila se retira para no dejar un hueco. La fila aparece al final del orden por defecto y se puede mover como cualquier otra sección.

### Efecto de cada opción

| Opción | Valores | Efecto en la interfaz |
|---|---|---|
| Tema | `cinema`, `midnight`, `amoled`, `contrast` | Paleta de acento, superficies y contraste |
| Color de acento | tema o `#rrggbb` | Sustituye el acento del tema en bordes, botones e indicadores; el contraste del texto se calcula solo |
| Tamaño de tarjetas | `small` / `medium` / `large` | Escala ×0.86 / ×1 / ×1.18 sobre el ancho nativo; móvil conserva el nativo |
| Transparencia | 0–100 | Opacidad de superficies y sombras |
| Altura del Hero | 40–90 vh | Altura mínima del hero (limitada al 54 vh en móvil) |
| Transiciones | 0–1000 ms | Duración de hovers y animaciones |
| Intensidad del hover | 0–150 % | Elevación, escala, anillo y sombra de las tarjetas al pasar el cursor; 0 deja solo el cambio de color |
| Hero | on/off | Carrusel de sugerencias al inicio de Inicio (exclusivo de esa vista); un clic abre la ficha mostrada |
| Fuente del Hero | `random`, `latest`, `movies`, `series`, `resume` | Filtra el carrusel de Inicio: Películas/Series por tipo, Últimas incorporaciones solo recientes, Seguir viendo solo progreso |
| Carruseles | on/off | Muestra u oculta las filas de Inicio; el hero conserva su propia fuente y sigue rotando |
| Recomendaciones | on/off | Fila propia del plugin con sugerencias según el historial del usuario; sin historial muestra películas y series aleatorias. Sin barra inferior: flechas laterales como las filas nativas. Su posición se ajusta en "Orden de secciones" |
| Mis medios | on/off | Muestra u oculta la fila de accesos a las bibliotecas; su posición se ajusta en "Orden de secciones" |
| Orden de secciones | lista | Reordena las filas visibles reconocidas, incluidas las envueltas en `.sectionN`; agrupa todas las filas de continuar y deja fijo el Hero. Incluye "Estudios" (si está activa) y "Mis contenidos". Las filas ocultas y las desconocidas no se mueven |
| Elementos por carrusel | 4–20 | Sugerencias del carrusel del hero; las filas nativas conservan su límite |
| Colecciones por estudio | on/off + lista | Fila de logos centrada, por defecto bajo el hero; logo y vídeo curados con fallback al logo del servidor. Los estudios de una misma marca se agrupan en una sola casilla (Disney, Marvel, DC…) y el clic abre todos sus títulos juntos. Lista vacía = Disney, Marvel, DC, Paramount, Netflix; máx. 40 |
| Backdrop de Detalles | on/off | Altura, saturación y degradados de `#itemBackdrop` |
| Metadatos | on/off | Tipografía y espaciado de la información principal |
| Información adicional | on/off | Separadores, etiquetas y enlaces de los bloques técnicos; el panel de sinopsis conserva su estilo base |
| Contenido relacionado | on/off | Títulos y tarjetas de `.detailVerticalSection` |
| Precarga | on/off | Pide la imagen del hero a 1920×1080 en lugar de la de la tarjeta |
| Lazy loading | on/off | Se apoya en la carga diferida nativa de Jellyfin |
| Caché | 1–120 min | TTL del bootstrap en memoria (el guardado del panel la invalida al instante) |
| Movimiento reducido | `auto` / `on` / `off` | Respeta `prefers-reduced-motion` o lo fuerza/desactiva |

## Endpoints

| Endpoint | Acceso | Uso |
|---|---|---|
| `GET /PremiumUI/Diagnostics` | Administrador (elevación) | Estado de integración, contadores y errores recientes |
| `GET /PremiumUI/Bootstrap` | Sesión autenticada | Configuración no administrativa para el frontend |

## Estructura

```
src/Jellyfin.Plugin.PremiumUI/
├── PremiumUiPlugin.cs              Plugin, identidad y página de configuración
├── PluginServiceRegistrator.cs     Registro de servicios e IStartupFilter
├── Configuration/                  Configuración, validador y panel HTML
├── Hosting/                        Startup filter, middleware, índice y assets
├── Api/                            Diagnóstico y bootstrap
├── Diagnostics/                    Estado compartido y buffer de errores
└── Web/                            app.js y app.css generados por Vite (embebidos)

frontend/
├── src/main.ts                     Arranque, bootstrap y montaje por vista
├── src/core/                       DOM, configuración, i18n, router y bootstrap
├── src/home/                       Hero, clasificación y orden de secciones
├── src/details/                    Estilos y opciones de Detalles
├── src/styles/                     Tokens, Inicio y Detalles
├── src/types.ts                    Contrato del bootstrap y de la configuración
└── test/smoke.mjs                  Prueba jsdom del bundle final
```

## Documentación

- `docs/PHASE0-RESEARCH.md`: investigación técnica de Jellyfin 12.4 y arquitectura.
- `docs/PHASE1-REPORT.md`: alcance, decisiones y evidencias de la Fase 1.
- `docs/PHASE2-REPORT.md`: inyección del índice, assets y evidencias de la Fase 2.
- `docs/PHASE3-REPORT.md`: DOM verificado, alcance visual, rendimiento y limitaciones.
- `CHANGELOG.md`: historial de cambios.

## Licencia

GPL-3.0. Ver `LICENSE`.
