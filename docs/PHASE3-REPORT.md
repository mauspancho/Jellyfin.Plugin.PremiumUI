# Fase 3 — Interfaz de Inicio y Detalles

Fecha: 2026-11-16
Objetivo: Jellyfin 12.4.0 (`net10.0`, `targetAbi 12.4.0.0`)
Frontend: `0.8.0` (Vite 7 + TypeScript 5.9, sin frameworks)

## Estrategia

Se eligió **Enhance**, no *Replace*: la interfaz de Jellyfin sigue siendo la
responsable de cargar datos, reproducir, navegar y recordar el estado. El plugin
solo añade una capa visual y un hero construido con las tarjetas que Jellyfin ya
cargó.

Ventajas:

- Cero peticiones de API nuevas para el hero: reutiliza `data-src` de la primera
  tarjeta disponible y pide la misma imagen a mayor resolución.
- Cero duplicación de lógica de reproducción: los botones del hero hacen clic en
  los controles nativos de la tarjeta.
- Reversible: si se quita el script, no queda ningún nodo propio que limpiar más
  allá del hero (y el CSS deja de aplicar porque todo cuelga de `data-premiumui`).

## DOM verificado en jellyfin-web 12.4.0

Inicio (ruta legacy dentro del router React):

```
#indexPage.homePage
└── #homeTab.pageTabContent
    └── .sections
        └── .verticalSection.sectionN
            ├── .sectionTitleContainer > h2.sectionTitle
            └── emby-scroller / .itemsContainer
                └── .card[data-id][data-type]
                    ├── .cardImageContainer[data-src]
                    ├── .cardText > .cardText-first / .cardText-secondary
                    └── .cardOverlayContainer > button[data-action]
```

Detalles (ruta legacy):

```
#itemDetailPage.itemDetailPage
├── #itemBackdrop.itemBackdrop
└── .detailPageWrapperContainer
    ├── .detailPagePrimaryContainer
    │   ├── .detailImageContainer > .card
    │   ├── .detailRibbon > .nameContainer, .itemMiscInfo, .mainDetailButtons
    └── .detailPageSecondaryContainer > .detailSection / .detailVerticalSection
```

Hechos que condicionan la implementación:

- `viewshow`/`pageshow` se despachan sobre el elemento de la página y no siempre
  burbujean; se escuchan en fase de captura y además se parchea
  `history.pushState`/`replaceState`.
- Las secciones se rellenan de forma asíncrona y se reemplazan por `innerHTML`,
  por eso el orden y el hero se recalculan con `MutationObserver` (debounce 120 ms).
- Las tarjetas de Inicio son `div` sin `href`; la acción vive en los botones
  `data-action`. Para "Más información" se usa `Dashboard.navigate('details…')`.

## Funcionalidad

### Inicio

| Opción | Efecto |
|---|---|
| Activar Inicio | Monta hero + estilos; si está apagado no se toca el DOM |
| Hero | Construye tarjeta destacada con título, sección, metadatos y botones |
| Fuente del hero | `random`, `latest` (recientes), `resume` (continuar), `movies`, `series` |
| Carruseles | Muestran u ocultan las filas de Inicio; el hero mantiene su propia fuente y no se queda sin rotación |
| Orden de secciones | Reordena solo secciones reconocidas; las desconocidas no se mueven |
| Elementos por sección | Se mantiene el límite nativo de Jellyfin (documentado abajo) |

Reconocimiento de secciones: títulos normalizados en varios idiomas (es, en, pt,
fr, de, it) y, como respaldo, `data-type` de las tarjetas (`Movie`/`Series`).

### Detalles

| Opción | Efecto |
|---|---|
| Activar Detalles | Añade `pui-details` y atributos de opciones a `#itemDetailPage` |
| Backdrop | Altura, saturación y degradados sobre `#itemBackdrop` |
| Metadatos | Tipografía y espaciado de `.itemMiscInfo` y etiquetas |
| Información adicional | Separadores, etiquetas y enlaces de los bloques técnicos en acento |
| Contenido relacionado | Título con barra de acento, tarjetas con hover de Inicio y botones de desplazamiento en `.detailVerticalSection` |

### Diseño global

- Temas: `cinema` (oro), `midnight` (azul), `amoled` (negro puro), `contrast`.
- Tamaño de tarjeta: 168 / 208 / 248 px (solo escritorio; móvil conserva el nativo).
- Transparencia, altura del hero y duración de transición se publican como
  variables CSS (`--pui-*`).
- Movimiento reducido: `auto` respeta `prefers-reduced-motion`, `on` lo fuerza,
  `off` lo desactiva.

## Rendimiento

- Bundle: **10.4 KB** JS + **10.2 KB** CSS; Brotli ≈ 3.7 KB + 2.5 KB.
- Sin `innerHTML` con contenido no confiable, sin `eval`, sin `new Function`.
- Un único `MutationObserver` por página de Inicio, con debounce.
- Bootstrap cacheado en memoria con TTL (`CacheMinutes`, 1–120 min) y cabecera
  HTTP `private, max-age` alineada con esa TTL.
- El hero reutiliza la imagen de la tarjeta (sin peticiones JSON nuevas).
- Navegación resuelta con `pushState`/`replaceState`, `popstate`, `hashchange` y
  eventos de vista en captura; sin sondeos.

## Accesibilidad

- Hero con `role="region"`, `aria-label` del elemento y botones reales.
- Iconos decorativos con `aria-hidden="true"`.
- Foco visible en los botones del hero.
- `prefers-reduced-motion` y política configurable.
- El contenido nativo (títulos, botones, enlaces) no se sustituye.

## Pruebas

- `82` pruebas unitarias de backend en verde, incluidas las del contrato camelCase
  del bootstrap, la de `studioSections` y las del normalizador de estudios.
- `npm run typecheck` sin errores.
- `npm run test:smoke` con `jsdom`: valida orden de secciones (incluida una
  sección desconocida que no se mueve), hero (solo en Inicio), imagen en alta
  resolución, tokens de diseño, atributos de Detalles, marcado de bibliotecas
  (`data-pui-library`, sin hero) y las secciones de estudio (resolución por
  conteo del nombre ambiguo, logo Thumb, enlace a la ficha del estudio y
  retirada al cambiar la configuración).

## Correcciones tras la primera validación

- El bootstrap se serializaba en PascalCase (Jellyfin 12.4 no aplica camelCase por defecto) y el frontend no podía montar la interfaz. Ahora los DTO declaran `JsonPropertyName` camelCase, el frontend valida el contrato y la URL incluye `?v=<versión>` para evitar caché obsoleta.
- El hero no encontraba tarjetas: varias secciones (Películas, Series, Recientes) se renderizan dentro de `<div class="sectionN">`, no como hijas directas de `.sections`. La selección ahora recorre todas las `.verticalSection` anidadas y descarta las que están dentro de un contenedor `.hide`.
- El tamaño de tarjeta imponía anchos fijos en píxeles y reducía las tarjetas nativas. Ahora replica los anchos `vw` de Jellyfin por breakpoint y los multiplica por `--pui-card-scale`; el tamaño Mediano equivale al nativo.

## Correcciones de persistencia

- El estilo de Detalles se ancla a atributos de `<html>` (`data-pui-details`, `data-pui-backdrop`, `data-pui-metadata`, `data-pui-extra`, `data-pui-recommendations`) y el de Inicio a `data-pui-home`. Jellyfin recrea `#itemDetailPage` al navegar entre fichas; anclar los selectores al documento evita depender de clases aplicadas por JS sobre el elemento que se destruye.
- El hero se reinserta si React lo elimina del DOM y el `MutationObserver` se vuelve a enlazar cuando `.sections` es reemplazado.
- El CSS de Inicio dejó de depender de `#homeTab.pui-home-tab`: usa `html[data-pui-home='on'] #homeTab`.

## Carrusel del hero

- El hero reúne hasta `Elementos por carrusel` (4–20) tarjetas de todas las secciones habilitadas y las deduplica por `data-id`.
- Rota cada 9 s con transición de opacidad; conserva el elemento actual cuando el DOM se vuelve a renderizar.
- Se detiene con `prefers-reduced-motion` (o la política del panel), con la pestaña oculta y mientras el puntero o el foco están sobre el hero.
- Clic en cualquier zona, `Enter` o `Espacio` abre `details?id=…` mediante la navegación nativa (`Dashboard.navigate`); las flechas ←/→ y el swipe cambian de sugerencia.
- El número de sugerencias sigue "Elementos por carrusel" (4–20) y el orden se rota en cada carga de la página para ofrecer variedad.
- "Contenido destacado" filtra de verdad: Películas/Series por `data-type` y por título de sección, Últimas incorporaciones por secciones recientes y Seguir viendo por la sección de progreso. Si una fila no expone `data-type`, se usa completa para no dejar el hero vacío. Si la fuente elegida no tiene contenido, el hero se oculta y se registra un aviso en la consola.
- Se usan todas las secciones habilitadas, repartiendo una tarjeta por sección y turno: cubre casos sin sección "Películas" donde solo existen "Reciente en Películas" y "Reciente en Series".
- El hero tiene margen superior y lateral adaptativo, esquinas redondeadas en los cuatro lados y un borde de 2 px con el color de acento del tema.
- El color de acento se puede personalizar desde Diseño (`#rrggbb`); vacío usa el del tema. El backend normaliza el valor y el frontend calcula el contraste del texto, además de la variante translúcida para fondos.
- `GET /PremiumUI/Bootstrap` se sirve con `Cache-Control: private, no-store`: el navegador no puede reutilizar una configuración anterior después de guardar el panel. La caché útil sigue en memoria del plugin.
- El guardado del panel invalida esa caché en memoria: `window.PremiumUI.invalidate()` marca `fetchedAt = 0` y `refresh(true)` pide un bootstrap nuevo. Así la navegación interna de la SPA (Dashboard → Inicio) aplica los cambios sin `Ctrl+Shift+R`, mientras que la navegación normal mantiene el TTL configurado en "Caché" para no repetir peticiones.

## Bibliotecas de Películas y Series (0.9.0, revertido en 0.10.0)

- La versión 0.9.0 añadió el hero a `#moviesPage` y `#tvshowsPage`; a partir de
  0.10.0 el hero vive únicamente en Inicio (pedido del usuario).
- Las bibliotecas se limitan a marcarse con `data-pui-library` (movies/series)
  al navegar y tras cada guardado del panel; `library.ts` ya no usa observers ni
  pide datos, y `library.css` quedó vacío.
- El hover premium de las tarjetas se conserva gracias al selector compartido
  `html[data-pui-home='on'] :is(#homeTab, [data-pui-library])` en `home.css`.

## Colecciones por estudio (0.10.0)

- Nueva sección al final de Inicio con una fila de tiles apaisados (~2.2:1) que
  muestran solo el logo de cada estudio; el clic lleva a `#/details?id=<studioId>`,
  la ficha nativa del estudio que Jellyfin 12.4 renderiza como lista de su
  contenido (verificado con captura).
- Resolución de nombres: `GET /Items?IncludeItemTypes=Studio&Recursive=true&…`
  con `SearchTerm` por estudio; solo si hay ambigüedad se cuenta el contenido de
  cada candidato (`StudioIds=<id>&Limit=0`, máx. 6) y gana el más poblado.
  Logos desde `Primary` y, si no existe, `Thumb` (verificado: `Primary` de
  "Marvel Studios" responde 404 y `Thumb` 200). Sin logo se muestra el nombre.
- `home/studios.ts` es idempotente: conserva tiles vigentes, retira los que dejan
  de estar configurados, marca la sección con `data-pui-studio` y descarta
  respuestas de configuraciones anteriores con un token en el contenedor.
  Caché en memoria con el TTL del panel; sin sondeos.
- Backend: `HomeOptions.StudiosEnabled` + `HomeOptions.Studios` (lista vacía por
  defecto para evitar el bug del `XmlSerializer` que duplica listas
  inicializadas; el validador rellena Disney/Marvel/DC/Paramount/Netflix cuando
  está activa y vacía), normalización con trim/deduplicación/truncado a 60
  caracteres y tope de 12 estudios, y `HomeBootstrapDto.studioSections` (vacío
  si está desactivado).
- Panel: sección "Colecciones por estudio" con toggle y lista de nombres
  separados por comas; el hint documenta los valores por defecto.
- Orden (0.11.0–0.12.0): "Estudios" es una sección más del orden configurable
  (`estudios` en `SectionIds`, `SECTION_LABELS` y `DEFAULT_ORDER`); el frontend
  la reconoce por su marcador `data-pui-studio` en `classifySection` y el
  reordenado existente la coloca en su hueco. El orden canónico es
  `hero, estudios, continue, recent, movies, series, recommendations` y la
  migración de esquema 1 → 2 coloca `estudios` justo debajo de `hero`.
- Centrado (0.11.0): `.pui-studio-tile-row` usa `justify-content: center`, de
  modo que los logos se centran en la pantalla también en móvil.

- `#moviesPage` y `#tvshowsPage` reciben el mismo controlador de hero que Inicio (`library/library.ts`), insertado antes de la cuadrícula.
- El hero usa siempre las tarjetas de la biblioteca (`HeroOptions.ignoreSource`), independientemente de "Contenido destacado", que sigue filtrando solo Inicio.
- Jellyfin 12.4 pinta las tarjetas de biblioteca con un `<img>` en vez de `data-src`; `imageSource()` acepta ambas formas y `highResolution()` vuelve a pedir la imagen a 1920×1080.
- Las tarjetas de la cuadrícula comparten el hover de Inicio mediante `html[data-pui-home='on'] :is(#homeTab, [data-pui-library])`, con la misma `--pui-hover`.
- Un `MutationObserver` con debounce de 160 ms sigue la paginación, el cambio de orden y los re-render de React; ignora las mutaciones internas del propio hero y se desconecta al desactivar "Habilitar Hero".

## Estudios agrupados por marca

- `StudioGrouper` (servidor) recibe los 1.733 estudios del servidor y los une
  por dos reglas: base idéntica tras quitar sufijos corporativos ("Working
  Title" / "Working Title Films") y marcas conocidas con sus derivados
  ("Disney Channel" + "Walt Disney Pictures" → Disney). Las reglas de marca
  usan palabra completa o prefijo compacto para no arrastrar falsos positivos
  ("Nippon Columbia" no entra en Sony/Columbia; "Atomic Cartoons" no entra en
  Cartoon Network; "Applecross" no entra en Apple).
- `GET /PremiumUI/Studios` devuelve `items` (todos los estudios) y `groups`
  (key, nombre y miembros); el panel pinta una casilla por grupo con el número
  de estudios y permite buscar por cualquiera de sus miembros. Al marcar un
  grupo guarda su nombre canónico, de modo que una configuración antigua con
  "Walt Disney" se muestra como "Disney" sin duplicar.
- El bootstrap añade `home.studioCollections`: grupos ya resueltos con sus
  `studioIds`, más `imageItemId`/`imageType` del logo de respaldo. El frontend
  deja de buscar estudios por nombre para pintar los tiles.
- El tile de un grupo abre una capa propia (`pui-studio-overlay`):
  `Items?StudioIds=…&StudioIds=…&IncludeItemTypes=Movie` con orden por
  título/año/recientes, rejilla de pósters con carga diferida, paginación de
  60 y enlaces a la ficha nativa. Se cierra con Escape, con el botón, al pulsar
  el fondo o al cambiar de ruta. Los grupos de un solo estudio mantienen la
  lista nativa de Jellyfin.
- Pruebas nuevas de agrupación, resolución de alias y falsos positivos; `smoke`
  comprueba el tile agrupado, la petición con varios `StudioIds`, el render de
  la rejilla y el cierre de la capa.

## Recomendaciones de Inicio

- El check "Recomendaciones" del panel vuelve a tener efecto: `home.recommendations`
  crea una `section.pui-recommendations-section` propia del plugin, marcada con
  `data-pui-recommendations` para que `classifySection` la reconozca y el
  reordenado la trate como una fila más (`recommendations` en `SectionIds`).
- La sección se inserta en su posición de "Orden de secciones" antes de pedir
  los datos, igual que la fila de estudios; así no salta de sitio al llegar la
  respuesta. Si se desmarca el check, la sección se retira del DOM.
- Datos: `Items/Suggestions?userId=…&limit=…&type=Movie&type=Series`, que
  Jellyfin calcula a partir del historial del usuario. Si devuelve una lista
  vacía o falla, se usa `Items?Recursive=true&IncludeItemTypes=Movie,Series&SortBy=Random`,
  por lo que un usuario nuevo ve aleatorios de toda la biblioteca.
- Caché en memoria con clave `userId|limit` y el TTL de `performance.cacheMinutes`;
  el guardado del panel la invalida con el resto del bootstrap. Sin polling.
- Tarjetas propias (`a.pui-rec-card`): póster `Primary` o `Thumb`, título, año y
  duración, enlace a `#/details?id=…&serverId=…`, `loading="lazy"` y el mismo
  hover con acento y la política de movimiento reducido del resto de Inicio.

## Estabilidad de los tiles de estudio

- El observer de Inicio vuelve a llamar a `applyStudioSections` tras cualquier
  mutación; la versión anterior hacía `row.replaceChildren(fragment)` en cada
  pasada, así que los nodos se retiraban y reinsertaban cada ~120 ms. Eso
  reiniciaba la transición de hover (temblor) y podía perder el clic si el
  elemento cambiaba entre `pointerdown` y `pointerup`.
- `render` ahora calcula la lista deseada, retira solo los tiles sobrantes y
  mueve únicamente los que están fuera de su posición. Sin cambios de
  configuración no hay ninguna mutación y los nodos se conservan.
- La elevación del hover vive en `.pui-studio-surface`; el enlace mantiene su
  caja fija, por lo que el borde inferior no se escapa del cursor ni se produce
  el bucle hover/leave.
- La capa de colección pide `Movie,Series` para igualar la lista nativa de un
  estudio (Netflix 27 títulos y Apple 11 en el servidor de referencia, frente a
  4 y 3 si solo se pedían películas).
- Validado en Chromium sobre la página real: el `top` de la superficie se
  estabiliza tras la transición (sin oscilaciones) y los 9 tiles abren su capa
  o su lista nativa.

## Flechas laterales de Recomendaciones

- `.pui-rec-row` oculta la barra de desplazamiento inferior (`scrollbar-width:
  none` y `::-webkit-scrollbar`) sin quitar el scroll nativo.
- La cabecera de la sección incluye `.emby-scrollbuttons` con dos
  `.emby-scrollbuttons-button` (`data-direction="left|right"`, iconos
  `chevron_left`/`chevron_right`), las mismas clases que usan Películas y
  Series, por lo que heredan el estilo de acento del plugin.
- Cada botón desplaza un 80 % del ancho visible (mínimo 220 px) con
  `scrollBy({ behavior: 'smooth' })`, o salto instantáneo con movimiento
  reducido. El estado `disabled` se recalcula en `scroll` y con
  `ResizeObserver`; sin desbordamiento la barra se oculta.
- Al retirar la sección se desconecta el observer. `smoke` verifica botones,
  iconos, desplazamiento, extremos y ocultación.

## Estilo de las filas de Inicio

- Encabezados con barra de acento, tamaño fluido y enlace "más" con chevrón animado.
- Botones de desplazamiento nativos convertidos en círculos con superficie translúcida y hover de acento; se ocultan en táctil.
- Tarjetas: elevación y anillo de acento al pasar el cursor, anillo de foco accesible, barra de progreso con el color del tema y título que adopta el acento.
- Los efectos de `hover` usan `@media (hover: hover)` y respetan `prefers-reduced-motion` mediante los tokens del tema.
- Las filas nativas recortan verticalmente (`overflow-y: hidden`), así que la elevación y la sombra del hover se perdían. El contenedor reserva 1,15 rem arriba y abajo (compensados con margen negativo) para que la tarjeta se eleve 9 px con anillo de acento de 3 px y sombra amplia sin recortes.
- Los botones de desplazamiento se ven siempre: 2,8 em, borde e icono en acento, fondo translúcido y halo. En hover se rellenan con el acento, escalan un 12 % y muestran un halo; los deshabilitados quedan atenuados.
- Diseño incluye "Intensidad del hover" (0–150 %, por defecto 100 %), que se publica como `--pui-hover` y multiplica la elevación, la escala, el anillo y la sombra de las tarjetas, y el crecimiento de los botones de desplazamiento. A 0 % solo queda el cambio de color como feedback.

## Estilo de Detalles

- Póster con esquinas de 18 px, anillo suave de acento y sombra profunda; el título principal lleva un subrayado de acento.
- Metadatos con chips para textos (`mediaInfoText`), estrella de valoración y hora de finalización en el color de acento.
- Botones principales con radio de 14 px, separación uniforme y hover de acento; Play/Reanudar se rellenan con el acento y los secundarios muestran anillo de foco accesible.
- Panel de sinopsis con más contraste, altura de línea y etiquetas de bloque en acento. "Información adicional" controla los bloques técnicos, etiquetas y enlaces.
- Filas relacionadas con el mismo tratamiento que Inicio: encabezado con barra de acento, tarjetas con elevación/anillo/sombra según "Intensidad del hover", botones de desplazamiento circulares y espacio reservado para evitar recortes.

## Limitaciones conocidas

- `ItemsPerSection` define el tamaño del carrusel del hero, no las filas nativas: Jellyfin decide cuántas tarjetas pide en cada sección y recortarlas rompería su scroller.
- `LazyLoading` y `Preload` se aplican de forma conservadora: la carga diferida
  sigue siendo la de Jellyfin y `Preload` controla la resolución del hero.
- El reconocimiento de secciones es heurístico por título; si un idioma no está
  cubierto, el hero usa cualquier sección con imágenes y el orden no se altera.
- Falta validación visual en el servidor real tras el reinicio (Fase 3.1).
- La fila de estudios añade peticiones de resolución (una por nombre y, solo en
  nombres ambiguos, un contador por candidato); quedan en caché con el TTL, así
  que la navegación normal no repite las peticiones.

## Próximos pasos

1. Reiniciar Jellyfin y revisar Inicio/Detalles en escritorio y móvil.
2. Validar visualmente: hero solo en Inicio, hover en bibliotecas, fila de
   estudios con logos y ficha nativa del estudio al hacer clic.
3. Ajustar alturas, tamaños y contraste según el tema real del usuario.
4. Medir LCP/CLS antes y después con las DevTools.
5. Añadir pruebas de regresión visual si se estabiliza el DOM.

### Orden de secciones (0.13.0)

Jellyfin crea algunas filas como `<div class="sectionN"><div class="verticalSection">…`.
El reordenado anterior solo consideraba las `.verticalSection` hijas directas de
`.sections`, así que esas filas quedaban ancladas y mover "Estudios" en el panel
no cambiaba la vista. Ahora la unidad que se mueve es el hijo directo de
`.sections` (la fila o su envoltorio), se clasifica por título/tipo y se coloca
en los huecos de las filas reconocidas y visibles; las ocultas y desconocidas
conservan su posición y el Hero sigue siempre al principio. Verificado con tres
órdenes distintos en el navegador real (Estudios segundo, tercero y último).

### "Mis medios" (0.14.0)

La fila nativa de accesos a bibliotecas se clasifica por título ("Mis medios",
"My Media", "Meine Medien"…) y entra en el orden configurable como `media`.
El validador migra el esquema 2 → 3 colocando `media` justo después de
`estudios`, para que los logos queden por encima de las bibliotecas sin que el
usuario tenga que reordenar nada. El check "Mis medios" del panel la oculta con
`pui-section-hidden` (reversible, sin polling). Verificado en el navegador real:
con la fila visible el orden es Estudios → Mis medios → A continuación →
Recientes; al desmarcarla queda Estudios → A continuación → Recientes.

### Arte curado de estudios (0.15.0)

Los tiles dejan de depender del logo del servidor cuando existe arte propio.
`frontend/public/studios` contiene 12 logos WebP y 12 bucles MP4 de
Jellyfin-MonWUI-Plugin (GPL-3.0); Vite los copia a `Web/studios` y el
ensamblado los embebe. `AssetCatalog` los publica por nombre lógico estable
(`studios/disney.webp`, servido como `studios/disney.webp` bajo el prefijo del
plugin) con ETag y `?v=` para invalidar caché entre versiones; las imágenes y
vídeos no se recomprimen al arrancar. El frontend resuelve alias de nombres
(configurado y nombre real del estudio) y conserva el logo nativo como
fallback. El MP4 se reproduce en bucle al hover mediante `pointerenter`/
`pointerleave`, con `preload="none"` y sin crear el elemento si el movimiento
está reducido. Verificado en Chromium: 5/5 logos cargan (`naturalWidth > 0`),
el hover asigna `src`, reproduce (`paused=false`, `readyState=4`) y al salir
pausa y vuelve al póster.

### Selección de estudios (0.16.0)

El panel deja de pedir una lista de nombres escrita a mano. `GET
/PremiumUI/Studios` (solo administradores) devuelve un DTO compacto con los
estudios que Jellyfin tiene indexados —1.733 en la biblioteca de prueba— y el
panel los pinta como casillas con buscador, contador y botón "Usar sugeridos".
Los nombres guardados que no existen literalmente se mantienen seleccionados y
se marcan como "se resuelve por nombre", porque los alias de Disney/Marvel y la
búsqueda por nombre del frontend siguen cubriéndolos. El límite pasa de 12 a
40. La sección de estudios se inserta como primer hijo de `.sections` antes de
resolver los logos, así que queda bajo el hero desde el primer render y no
deja hueco si ningún nombre se resuelve.
