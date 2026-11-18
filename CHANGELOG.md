# Changelog

Todas las versiones siguen `MAJOR.MINOR.PATCH.BUILD`, el formato de Jellyfin.

## 1.0.0.0 — Fase 1

### Añadido

- Plugin `Jellyfin Premium UI` para Jellyfin 12.4.0 (`net10.0`, `targetAbi 12.4.0.0`).
- Configuración persistente con esquema versionado: General, Diseño, Inicio, Detalles y Rendimiento.
- Validador que normaliza rangos numéricos, listas permitidas y orden de secciones (incluye deduplicación).
- Panel de administración completo en Dashboard → Plugins → Jellyfin Premium UI, con restauración de valores y guardado vía API nativa.
- `IStartupFilter` registrado por `IPluginServiceRegistrator`, con gate de compatibilidad por versión major.
- Estado de integración compartido y endpoint `GET /PremiumUI/Diagnostics` restringido a administradores.
- Pruebas unitarias de configuración, serialización XML e identidad/empaquetado del plugin (31 pruebas).
- Script `build/package.sh` y `build/meta.json` para generar el ZIP instalable.

### Corregido

- El orden de secciones se duplicaba al recargar la configuración: `XmlSerializer` agrega elementos a la lista ya inicializada. La lista ahora nace vacía y el validador aplica el orden canónico.

## 1.0.0.0 — Fase 2

### Añadido

- `WebIntegrationMiddleware` registrado por `IStartupFilter`, ejecutado antes de la compresión y de los archivos estáticos de Jellyfin.
- Inyección del índice en memoria: `index.html` no se modifica en disco. Incluye `ETag`, `304`, `HEAD`, `301` de `/web` a `/web/` y respeto de `BaseUrl`.
- Catálogo de assets embebidos con nombres hasheados, Brotli/Gzip precalculados, caché inmutable y `404` controlado.
- Endpoint `GET /PremiumUI/Bootstrap` autenticado con la configuración no administrativa.
- Frontend *stub* 0.2.0: marca `data-premiumui`, obtiene el bootstrap y emite `premiumui:ready`; no altera la interfaz.
- Diagnóstico ampliado con contadores de índice, inyecciones, assets y bootstrap, más el número de assets embebidos.
- Documento `docs/PHASE2-REPORT.md` con alcance, comportamiento HTTP y evidencias.
- Pruebas de middleware, compresión, catálogo y proveedor de índice (69 en total).

### Notas

- El middleware no se registra en servidores que no sean major 12.
- Si el plugin está deshabilitado, o Inicio y Detalles están apagados, la interfaz se sirve sin inyección.

## 1.0.0.0 — Fase 3

### Añadido

- Frontend propio en TypeScript con Vite (sin React/Angular/Vue/jQuery), empaquetado en `app.js` y `app.css` y embebido en el ensamblado.
- Inicio: hero cinematográfico construido desde las tarjetas nativas, sin peticiones JSON adicionales; reutiliza la imagen de la tarjeta a 1920×1080 cuando la precarga está activa.
- Bibliotecas (0.9.0, retirado en 0.10.0): el hero llegó a aparecer también al entrar directamente en Películas (`#/movies`) y Series (`#/tv`), reutilizando las tarjetas de la cuadrícula nativa. A partir de 0.10.0 el hero vive únicamente en Inicio y las bibliotecas conservan solo el hover premium de las tarjetas.
- Inicio: reordenado conservador de secciones reconocidas por título localizado (es/en/pt/fr/de/it) y por tipo de tarjeta; las secciones desconocidas no se mueven.
- Inicio: refinado de tarjetas, títulos de sección y carruseles, con tamaño configurable en escritorio (escala ×0.86 / 1 / 1.18 sobre el ancho nativo).
- Detalles: backdrop, póster, cabecera, metadatos, botones y bloques de información con estilo propio, activables por separado.
- Temas `cinema`, `midnight`, `amoled` y `contrast`; transparencia, altura del hero y transiciones como variables CSS.
- Política de movimiento reducido (`auto`/`on`/`off`) y respeto de `prefers-reduced-motion`.
- Escucha de navegación en fase de captura y parcheo de `pushState`/`replaceState`, `popstate` y `hashchange`, sin sondeos.
- Bootstrap cacheado en memoria con TTL (`CacheMinutes`, 1–120 min) y `Cache-Control: private, max-age` alineado.
- `FrontendVersion` visible en el panel de diagnóstico.
- Prueba de humo con `jsdom` sobre el bundle final: orden de secciones, hero, imagen en alta resolución y atributos de Detalles.
- `npm run typecheck` y `npm run test:smoke`; `build/package.sh` compila el frontend y verifica los tres embebidos.
- Documento `docs/PHASE3-REPORT.md`.
- Color de acento configurable en Diseño: selector de color con opción "Personalizado" y vuelta al color del tema. Se aplica a bordes, botones, indicadores, títulos y al hero, con contraste de texto calculado automáticamente.
- Inicio: encabezados de sección con barra de acento y tipografía mayor, botones de desplazamiento circulares con hover de acento, anillo de acento al enfocar tarjetas, barra de progreso en el color del tema y títulos de tarjeta que se iluminan al pasar el cursor. Los efectos de hover se limitan a punteros finos y se desactivan con movimiento reducido.
- Inicio (0.6.1): la fila nativa recorta en vertical (`overflow-y: hidden`) y se comía la elevación y la sombra del hover. Se reserva espacio dentro del propio scroller (compensado con margen negativo) y el efecto ahora eleva la tarjeta 9 px con un anillo de acento de 3 px y una sombra amplia, sin recortes.
- Inicio (0.6.1): los botones de desplazamiento pasan a 2,8 em, con borde e icono en el color de acento y fondo propio. Al pasar el cursor se rellenan por completo con el acento, crecen un 12 % y añaden halo; los deshabilitados se distinguen claramente.
- Diseño (0.7.0): nueva opción "Intensidad del hover" (0–150 %, por defecto 100 %). Ajusta la elevación, la escala, el grosor del anillo y la sombra de las tarjetas al pasar el cursor, además del crecimiento de los botones de desplazamiento. A 0 % se conserva el cambio de color como única señal.
- Detalles (0.8.0): póster con esquinas de 18 px y anillo suave de acento, título con subrayado de acento, metadatos en chips y estrella/presentación en acento, botones con radio de 14 px, separación uniforme, hover de acento y anillo de foco, y panel de sinopsis con más contraste y altura de línea.
- Detalles (0.8.0): géneros y etiquetas pasan a chips con hover; las filas relacionadas usan el mismo hover que Inicio (elevación, anillo, sombra y botones de desplazamiento) y reservan espacio para que el efecto no se recorte. La "Intensidad del hover" también se aplica aquí.
- Detalles (0.8.0): "Información adicional" ahora controla los bloques técnicos, etiquetas y enlaces; el panel de sinopsis mantiene su estilo base aunque la opción esté desactivada.
- Inicio (0.8.1): los checks "Carruseles" no ocultaban ninguna fila y, al desmarcar "Recién agregadas", el hero se quedaba con una sola sugerencia (A continuación) y dejaba de rotar. Ahora cada check muestra u oculta sus filas de Inicio y el hero mantiene su propia fuente, de modo que sigue funcionando como carrusel. El panel lo explica con una nota.
- Guardado (0.8.2): al guardar el panel, la copia en memoria seguía vigente hasta el TTL ("Caché", 10 min), por lo que al volver a Inicio desde el Dashboard se veía la configuración anterior salvo que se forzara la recarga. Ahora el panel invalida esa copia (`window.PremiumUI.invalidate()`) y pide un bootstrap nuevo (`refresh(true)`), de modo que los cambios se aplican al volver a Inicio o Detalles sin `Ctrl+Shift+R`. La navegación normal sigue usando la caché con TTL para no repetir peticiones.
- El hero ahora es un carrusel de sugerencias: rota automáticamente cada 9 s entre las tarjetas de las secciones habilitadas, con flechas, indicadores, swipe horizontal en móvil y pausa al pasar el cursor o enfocar un control.
- Un clic en cualquier zona del hero (o `Enter`/`Espacio` con el hero enfocado) abre la ficha del elemento mostrado; las flechas del teclado cambian de sugerencia.
- La rotación se detiene con movimiento reducido y cuando la pestaña está en segundo plano; precarga la imagen siguiente para evitar parpadeos.

### Corregido

- El bootstrap se serializaba en PascalCase (`Design`, `Home`…) porque Jellyfin 12.4 ya no aplica la política camelCase por defecto; el frontend esperaba camelCase y abortaba el montaje. Los DTO del bootstrap ahora declaran `JsonPropertyName` explícito y el frontend valida el contrato antes de usarlo.
- La petición del bootstrap incluye `?v=<versión>` para no reutilizar una respuesta cacheada tras actualizar el plugin.
- El arranque captura y registra errores de montaje por vista, de modo que un fallo en Detalles no bloquea Inicio.
- El hero no encontraba tarjetas porque Jellyfin 12.4 envuelve algunas secciones en `<div class="sectionN">`; ahora se consideran todas las `.verticalSection` anidadas, no solo las hijas directas de `.sections`.
- El tamaño de tarjeta ya no impone píxeles fijos: se replica la escala `vw` nativa de Jellyfin multiplicada por `--pui-card-scale` (Mediano = tamaño nativo, sin reducción).
- El estilo de Detalles se perdía al abrir otra ficha desde las recomendaciones: Jellyfin recrea la página y el plugin aplicaba las clases al elemento anterior. Ahora el CSS se ancla a atributos de `<html>` (`data-pui-details`, `data-pui-backdrop`, `data-pui-metadata`, `data-pui-extra`, `data-pui-recommendations`, `data-pui-home`), que sobreviven a la recreación de la vista.
- El hero se reinserta si React lo elimina y el observer se vuelve a enlazar cuando `.sections` es reemplazado.
- El bootstrap se podía reutilizar desde la caché HTTP del navegador (`private, max-age=600`): tras guardar el panel, la interfaz seguía aplicando la configuración anterior durante varios minutos. Ahora se sirve `private, no-store` y la URL incluye la versión del frontend.
- El carrusel del hero ignoraba "Elementos por carrusel" y limitaba a cuatro tarjetas por sección. Ahora usa ese valor (4–20), reparte las sugerencias entre las secciones habilitadas y rota el orden en cada carga para no mostrar siempre las mismas.
- El hero solo tomaba una sección por tipo: si Inicio mostraba "Reciente en Películas" y "Reciente en Series" (sin secciones "Películas"/"Series"), se quedaba con una sola y no llenaba el carrusel. Ahora reparte una tarjeta por sección y turno, priorizando las que coinciden con la fuente configurada.
- El hero se separa del borde superior y de los laterales con márgenes adaptativos y esquinas redondeadas en los cuatro lados.
- El hero lleva un borde de 2 px con el color de acento del tema para que se distinga del fondo sin depender solo del contraste de la imagen.
- El contenido destacado no filtraba: cualquier opción mostraba una mezcla de todas las secciones. Ahora "Películas" y "Series" filtran por tipo de tarjeta, "Últimas incorporaciones" usa solo secciones recientes y "Seguir viendo" solo la sección de progreso; "Aleatorio" mantiene la mezcla entre secciones habilitadas.
- Con "Series" seleccionado el hero podía desaparecer: algunas filas no exponen `data-type` en sus tarjetas y el filtro las descartaba. Ahora también se reconoce la sección por su título y, si no hay tipos, se usa la fila completa. Si una fuente no tiene contenido se registra un aviso en la consola en lugar de fallar en silencio.

### Notas

- Estrategia *Enhance*: no se reemplaza el DOM ni se replica la lógica de reproducción o navegación de Jellyfin.
- `ItemsPerSection` no recorta tarjetas para no romper el scroller nativo.
- Pendiente la validación visual en el servidor real (Fase 3.1).

## 0.10.0 — Fase 3 (refinamiento)

### Añadido

- Inicio (0.10.0): colecciones por estudio. Una fila de tiles apaisados muestra únicamente el logo de cada estudio configurado (Disney, Marvel, DC, Paramount y Netflix por defecto; lista configurable, máx. 12). El clic abre la ficha nativa del estudio, que Jellyfin 12.4 ya renderiza como lista de su contenido. Los nombres ambiguos (p. ej. "Marvel") se resuelven con una búsqueda por `SearchTerm` y, solo si hay varios candidatos, un contador por candidato (`StudioIds=…&Limit=0`) elige el estudio más poblado; los logos se piden desde `Primary` o `Thumb`. Resultados en caché con el TTL configurado; la sección se retira al desactivar la opción.
- Inicio (0.10.0): nueva sección "Colecciones por estudio" en el panel (toggle + lista de nombres separados por comas; si se deja vacía se usan los cinco por defecto).

### Corregido

- Bibliotecas (0.10.0): el hero ya no aparece en las bibliotecas de Películas (`#/movies`) y Series (`#/tv`); vive únicamente en Inicio. Las bibliotecas se limitan a marcarse con `data-pui-library` para que el CSS aplique el mismo hover premium de Inicio a las tarjetas de la cuadrícula, sin JS recurrente ni observers.
- El validador normaliza la lista de estudios: recorta espacios, deduplica sin distinguir mayúsculas, trunca a 60 caracteres y limita a 12; una lista vacía con la opción activa usa los cinco estudios por defecto (el inicializado evita el bug del `XmlSerializer` que duplica listas al recargar).
- El bootstrap expone `studioSections` (vacío si la opción está desactivada) y las pruebas de contrato y de validador lo cubren (82 pruebas).


## 0.11.0 — Fase 3 (orden y centrado)

### Añadido

- Inicio (0.11.0): "Estudios" pasa a ser una sección más en "Orden de secciones" del panel (identificador `estudios`). El backend la incluye en el orden canónico y el validador la acepta en configuraciones guardadas; las configuraciones antiguas la reciben al final automáticamente. La fila se reordena junto al resto de secciones reconocidas.
- Inicio (0.11.0): `classifySection` reconoce la sección de estudios por su marcador `data-pui-studio`, de modo que el reordenado existente la coloca en el hueco correspondiente.

### Corregido

- Inicio (0.11.0): la fila de logos de estudios quedaba alineada a la izquierda; ahora los tiles se centran horizontalmente en la pantalla (`justify-content: center`), con el mismo comportamiento en móvil.
- Las pruebas de validador y del ciclo del plugin se actualizaron para el nuevo orden canónico (82 pruebas).

## 0.12.0 — Fase 3 (lista directa y orden)

### Añadido

- Inicio (0.12.0): al hacer clic en el logo de un estudio se abre directamente su lista completa de películas (`#/list?type=Movie&studioId=…&serverId=…`, la misma vista a la que apunta el botón "Más" nativo), con ordenación, filtros y "Reproducir todo". Antes se abría la ficha del estudio, que solo muestra una fila y un "Más".
- Inicio (0.12.0): "Estudios" pasa a estar justo debajo del hero en el orden canónico. El validador migra las configuraciones del esquema anterior (1 → 2) colocando `estudios` inmediatamente después de `hero`; a partir de ahí el orden elegido en el panel se respeta.

### Corregido

- El orden por defecto (`SectionIds`) ahora es `hero, estudios, continue, recent, movies, series, recommendations`, y las secciones ausentes se siguen añadiendo al final al normalizar.
- El panel documenta que el clic abre la lista completa de películas y que la fila aparece por defecto bajo el hero.
- `ApiClientLike` declara `serverId()` para construir la ruta de la lista.
- Pruebas: 84 en verde, incluidas dos nuevas de migración de esquema (esquema 1 → estudios bajo el hero; esquema actual → se respeta el orden del usuario).

## 0.13.0 — Fase 3 (orden de secciones real)

### Corregido

- "Orden de secciones" ahora mueve de verdad todas las filas reconocidas de Inicio. Jellyfin envuelve algunas filas en `<div class="sectionN">` (por ejemplo "Agregado recientemente…"); antes el plugin solo reordenaba las `.verticalSection` que eran hijas directas de `.sections` y esas filas quedaban ancladas, de modo que cambiar el orden en el panel apenas se notaba. El envoltorio pasa a ser la unidad que se mueve.
- "A continuación" se reconoce como sección de continuar y se agrupa con "Continuar viendo"/"Continuar escuchando"/"Continuar leyendo", por lo que respeta el hueco de `continue` en el orden.
- Las filas ocultas (por Jellyfin o por los checks del panel) y las que el plugin no reconoce siguen sin moverse, para que un cambio de orden no traslade filas invisibles; el Hero permanece siempre al principio.
- El aviso del panel explica el comportamiento (filas visibles, agrupación de "continuar" y Hero fijo).
- Pruebas: `smoke` incluye ahora una fila envuelta en `.sectionN` y verifica que se reordena con las demás.

## 0.14.0 — Fase 3 ("Mis medios" configurable)

### Añadido

- Inicio: "Mis medios" (la fila nativa de accesos a las bibliotecas) pasa a ser una sección reconocida del orden. Aparece como "Mis medios" en "Orden de secciones", por defecto justo debajo de los estudios, y se puede mover con las flechas como cualquier otra fila.
- Inicio: nuevo check "Mis medios" en la sección Inicio del panel. Al desmarcarlo la fila desaparece (clase `pui-section-hidden`) y el espacio lo ocupa la siguiente sección; al volver a marcarlo reaparece sin recargar.
- Migración de esquema 2 → 3: las configuraciones existentes reciben `media` justo después de `estudios`, de modo que tras actualizar los logos quedan por encima de las bibliotecas sin tocar nada.

### Corregido

- El panel envía de vuelta la versión de esquema que recibió. Antes mandaba siempre `SchemaVersion: 1`, así que cada guardado volvía a ejecutar las migraciones y podía pisar el orden elegido por el usuario.
- Bootstrap: nuevo campo `home.mediaLibraries` (`MediaLibraries` en la configuración), validado y con valor por defecto `true`.
- Pruebas: 87 en verde, incluidas migración 2 → 3, orden personalizado que se conserva al migrar y el valor por defecto de `MediaLibraries`; `smoke` cubre el reordenado de "Mis medios" y su ocultación/restauración en caliente.

## 0.15.0 — Fase 3 (arte curado de estudios)

### Añadido

- Inicio: los tiles de estudio usan el arte curado de Jellyfin-MonWUI-Plugin (misma licencia GPL-3.0): logo WebP y bucle de vídeo MP4 para Disney, Walt Disney Pictures, Marvel Studios, DC, Paramount Pictures, Netflix, Pixar, Universal, Warner Bros. Pictures, Columbia Pictures, DreamWorks Animation y Lucasfilm.
- El vídeo se carga solo al primer hover o foco, se reproduce en bucle silenciado y se pausa al salir. Con `prefers-reduced-motion` o la política de movimiento reducido del panel no se crea el elemento.
- Los estudios sin arte incluido conservan el logo del servidor; si el WebP curado fallara en tiempo de ejecución, el `img` cae al logo nativo.
- Los assets se sirven bajo `/web/premiumui/studios/<archivo>?v=<versión>` desde el propio ensamblado; no hay peticiones a GitHub ni dependencia de red externa.
- `THIRD-PARTY-NOTICES.md` con la atribución del arte (incluido en el ZIP).

### Cambiado

- `AssetCatalog`: admite `image/webp` y `video/mp4`, nombres lógicos estables y omite la recompresión de imágenes/vídeos ya comprimidos.
- El middleware responde a `Range` con `206 Partial Content` (identidad) para que los navegadores reproduzcan los MP4 sin descargar el archivo completo; `ByteRange` valida los rangos y las pruebas cubren los casos límite (102 pruebas en verde).
- Pruebas: 87 en verde; el catálogo verifica los tipos MIME y la ausencia de Brotli para el arte. `smoke` cubre el logo curado de Marvel/Netflix y el fallback al logo del servidor de un estudio sin arte.

## 0.16.0 — Fase 3 (selección de estudios desde el servidor)

### Añadido

- Panel: "Colecciones por estudio" ya no es un campo de texto. El plugin consulta todos los estudios del servidor (`GET /PremiumUI/Studios`, compacto y solo para administradores) y los muestra como casillas con buscador, contador de seleccionados y botón "Usar sugeridos". Los nombres guardados que no coinciden literalmente se mantienen marcados y se resuelven por nombre (los alias de Disney, Marvel, etc. siguen funcionando).
- La lista admite hasta 40 estudios seleccionados.

### Cambiado

- Inicio: la sección de estudios se crea en el acto como primer hijo de `.sections`, de modo que queda justo debajo del hero desde el primer render. Antes se creaba cuando terminaban de resolverse los logos y "Mis contenidos" ocupaba ese lugar durante un instante.
- Si ningún estudio configurado se resuelve, la fila vacía se retira para no dejar un hueco.

## 0.17.0 — Fase 3 (fuentes del hero y "Mis contenidos")

### Añadido

- Hero: "Aleatorio" pide a la API películas y series de toda la biblioteca (`SortBy=Random`), no solo las tarjetas de las filas de recientes ya cargadas. "Películas" y "Series" hacen lo mismo limitadas a su tipo y muestran la duración/año reales.
- El hero sintetiza tarjetas internas (sin tocar el DOM de Jellyfin) con póster/backdrop, título, metadatos y enlace a la ficha; si la API falla, usa las tarjetas de Inicio como respaldo.
- La etiqueta superior del hero usa "Películas"/"Series" según el tipo cuando la fuente es aleatoria.

### Cambiado

- Panel: "Mis medios" pasa a llamarse "Mis contenidos", que es el título real de la fila en español de España. El clasificador reconoce ahora "Mis contenidos", "Mis medios" y "My Media", por lo que la fila se puede mover y ocultar en todas las variantes.
- Panel: se elimina "Continuar viendo" de "Contenido destacado"; las configuraciones guardadas con `resume` pasan a "Aleatorio" al normalizar.
- "Últimas incorporaciones" mantiene el comportamiento anterior: solo recientes.

## 0.18.0 — Fase 3 (limpieza del panel)

### Cambiado

- Panel: se retira el interruptor "Recomendaciones" de Carruseles y la entrada del mismo nombre en "Orden de secciones". Jellyfin 12.4 no genera una fila de recomendaciones en Inicio, así que el control no tenía efecto. La recomendación de contenido relacionado en la ficha de Detalles se mantiene con su propio check.


## 0.19.0 — Fase 3 (recomendaciones en Inicio)

### Añadido

- Inicio: nueva fila **Recomendaciones**, propia del plugin, controlada por el check "Recomendaciones" del panel y por su entrada en "Orden de secciones" (por defecto al final). Se crea en su posición desde el primer render, antes de que lleguen los datos, igual que la fila de estudios.
- Personalización: consulta `Items/Suggestions` con el `userId` de la sesión para películas y series. Si el usuario es nuevo, no tiene historial o la API no devuelve nada, cae a `Items?SortBy=Random&IncludeItemTypes=Movie,Series` de toda la biblioteca.
- Las tarjetas muestran póster (o `Thumb` si no hay póster), título, año y duración, con enlace a la ficha del título y el mismo hover con acento que el resto de Inicio. Se respeta "Movimiento reducido".
- Caché en memoria con TTL (`performance.cacheMinutes`) por usuario y número de elementos; sin polling, las imágenes usan `loading="lazy"`.
- Pruebas: `smoke` cubre las sugerencias personalizadas, el fallback aleatorio y los enlaces de las tarjetas.

### Cambiado

- Panel: vuelve el interruptor "Recomendaciones" en Carruseles y su entrada en el orden. La recomendación de contenido relacionado en la ficha de Detalles mantiene su propio check independiente.

### Notas

- La fila se retira si ambas peticiones fallan, para no dejar un hueco vacío en Inicio.

## 0.20.0 — Fase 3 (estudios agrupados por marca)

### Añadido

- Panel: los estudios que son la misma marca se muestran como una sola casilla. "Disney" reúne Disney Channel, DisneyToon Studios y todas las variantes de Walt Disney; "Marvel", "DC", "Warner Bros.", "Sony Pictures", "20th Century Fox", "HBO", "BBC", "Nickelodeon", "Cartoon Network", etc. agrupan sus derivados. El número junto al nombre indica cuántos estudios incluye y el buscador también encuentra el grupo por el nombre de cualquiera de sus miembros.
- Inicio: un tile de grupo abre una capa propia del plugin con **todos** los títulos de **todos** sus estudios a la vez (`Items?StudioIds=…&StudioIds=…`), con rejilla de pósters, orden por título/año/añadidos, paginación de 60 en 60 y enlaces a la ficha nativa.
- Agrupación en el servidor (`StudioGrouper`): combina marcas conocidas y nombres que solo difieren en sufijos corporativos ("Working Title" / "Working Title Films"); evita falsos positivos como "Nippon Columbia" dentro de Sony o "Atomic Cartoons" dentro de Cartoon Network.
- El bootstrap incluye `studioCollections` ya resueltas (id, nombre, estudios y logo de respaldo); el frontend no busca estudios por nombre ni hace peticiones extra para pintar los logos.
- Pruebas: 114 en verde, incluidas las reglas de agrupación, la resolución de alias y los casos que no deben agruparse; `smoke` cubre el tile agrupado, la petición con varios `StudioIds` y el cierre de la capa.

### Cambiado

- Panel: "Usar sugeridos" marca los grupos canónicos; una configuración guardada con "Walt Disney" pasa a mostrarse como "Disney" sin duplicar la casilla.
- Si el grupo tiene un solo estudio, el tile mantiene la lista nativa de Jellyfin; la capa solo se abre cuando hay varios.

## 0.21.0 — Flechas laterales en Recomendaciones

### Añadido

- La fila de recomendaciones de Inicio usa los mismos botones laterales que las filas nativas de Películas y Series: dos círculos con `chevron_left` y `chevron_right` en la esquina superior derecha, con las clases nativas `.emby-scrollbuttons` / `.emby-scrollbuttons-button` y el mismo estilo de acento del plugin.
- Los botones desplazan la fila un 80 % del ancho visible (mínimo 220 px), respetan `prefers-reduced-motion` y se deshabilitan en los extremos. El estado se recalcula al hacer scroll y al cambiar el tamaño de la fila mediante `ResizeObserver`; no hay temporizadores ni sondeos.
- Si la fila no desborda (pocos elementos o pantalla muy ancha), la barra de botones se oculta para no dejar controles inútiles.

### Cambiado

- Se elimina la barra de desplazamiento inferior de `.pui-rec-row` (`scrollbar-width: none` y `::-webkit-scrollbar`), también en Firefox; el desplazamiento nativo sigue disponible con rueda, trackpad, teclado y arrastre táctil.
- El `ResizeObserver` y los listeners de scroll se desconectan al retirar la sección, igual que el resto de observers del plugin.
- Pruebas: `smoke` comprueba las clases e iconos de los botones, el desplazamiento en ambos sentidos, el estado `disabled` al inicio y al final y la ocultación cuando no hay desbordamiento.

## 0.22.0 — Estudios estables y colecciones completas

### Corregido

- Los tiles de estudio ya no tiemblan al pasar el cursor. La fila se reconstruía en cada pasada del observer de Inicio (`row.replaceChildren`), lo que reiniciaba la transición CSS una y otra vez. Ahora solo se insertan, retiran o reordenan los tiles que cambian; si la configuración no cambió, el DOM queda intacto.
- La elevación del hover se aplica a una superficie interior (`.pui-studio-surface`) mientras el enlace conserva un área sensible fija. Así el borde del tile no se mueve bajo el cursor ni se produce el bucle hover/leave que también hacía fallar algunos clics.
- Las colecciones agrupadas incluyen películas **y series** (`IncludeItemTypes=Movie,Series`), igual que la lista nativa de un estudio. Netflix pasa de 4 a 27 títulos y Apple de 3 a 11 en el servidor de referencia.

### Cambiado

- Las pruebas `smoke` comprueban que un refresh sin cambios reutiliza los mismos nodos (sin temblor), que la imagen y el vídeo viven dentro de la superficie estable y que la colección pide películas y series.
- Documentación del panel y README: la colección habla de "títulos", no solo de películas.

## 0.23.0 — Instalación desde el catálogo de Jellyfin

### Añadido

- `manifest.json` en la raíz del repositorio: permite añadir el plugin como repositorio en **Dashboard → Complementos → Repositorios** e instalarlo o actualizarlo desde el catálogo, sin copiar archivos a mano.
- `build/manifest.py` genera ese manifest a partir de `build/meta.json` y del ZIP de `dist/`, calculando el **MD5** que Jellyfin valida antes de extraer el paquete. Conserva las versiones anteriores y deduce la etiqueta de la release (`0.23.0.0` → `v0.23.0`).
- `build/package.sh` ejecuta el script al terminar, de modo que el manifest y el ZIP nunca se desincronizan; `--no-manifest` permite omitirlo.

### Cambiado

- La versión del plugin pasa a seguir la del proyecto con cuatro componentes (`0.23.0.0`), en lugar de quedarse fija en `1.0.0.0`. Así el catálogo detecta las actualizaciones y las releases coinciden con lo instalado.
- `build/meta.json` actualiza `owner` a `mauspancho`, activa `autoUpdate` y describe el plugin para el catálogo.
- El README explica la instalación por catálogo, la manual y el flujo de publicación de una versión nueva, incluido el aviso de no mantener dos carpetas del mismo `guid`.

## 0.24.0 — Temas, transparencia y opciones que sí se aplican

### Corregido

- Los temas no cambiaban nada perceptible: solo variaban el acento y dos colores de superficie, y el acento personalizado tapaba incluso eso. Ahora cada tema define una paleta completa (acento RGB, superficies, color de sombra, bordes, radios, sombra y halo del hero) y se aplica de verdad en hero, tiles de estudio, tarjetas, capa de colección y Detalles.
- La transparencia era un valor muerto: `--pui-transparency` se publicaba pero ninguna regla lo consumía. Ahora se convierte en el alfa de `--pui-surface`, `--pui-surface-strong` y `--pui-scrim` (0 % opaco, 100 % cristal).
- "Lazy loading" no tenía efecto: las imágenes de estudio siempre se cargaban con `loading="lazy"`. Ahora la opción decide entre `lazy` y `eager` en los logos de estudio y en los pósters de la colección.
- El halo del hero y el `--pui-accent-rgb` siguen al color de acento personalizado cuando está activo, en vez de quedarse con el color del tema.
- La etiqueta de versión del panel aparecía fija como `v1.0.0.0`; ahora se rellena con el diagnóstico y queda oculta hasta tener el dato real.

### Añadido

- Tres temas nuevos: **Noir**, **Aurora** y **Ember** (siete en total).
- Vista previa en vivo en Diseño: reproduce el hero y las superficies del tema elegido con el acento y la transparencia actuales, y muestra una descripción de cada tema.
- Ayudas en el panel que explican qué controla realmente cada opción (elementos del hero y recomendaciones frente a carruseles nativos, precarga, carga diferida y caché).

### Cambiado

- "Elementos por carrusel" pasa a llamarse "Elementos en hero y recomendaciones", que es lo que el plugin controla; los carruseles nativos mantienen el tamaño que decide Jellyfin.
- Las pruebas `smoke` comprueban el acento RGB y el halo, la transparencia publicada, el atributo de carga diferida y la reutilización de nodos; las pruebas .NET cubren los siete temas canónicos.

## 0.24.1 — Detalles, precarga y carga diferida en caliente

### Corregido

- El interruptor **Detalles** no apagaba nada: las reglas de backdrop, metadatos, información adicional y secciones relacionadas se aplicaban por su propio atributo aunque la sección estuviera desactivada. Ahora todas exigen `data-pui-details='on'`, así que apagar Detalles devuelve la página nativa de Jellyfin.
- La **precarga** del hero no se respetaba: se preparaba siempre la diapositiva siguiente y cambiar la opción no actualizaba la imagen actual. Ahora la precarga decide la resolución del hero y si se prepara la siguiente diapositiva, y el cambio se aplica sin esperar a que rote.
- Cambiar **Carga diferida** no surtía efecto hasta recargar: los tiles de estudio se reutilizaban con el `loading` anterior. La opción entra en la clave del tile, de modo que se reconstruye como `lazy` o `eager` al guardar.

### Añadido

- La vista previa de Diseño avisa cuando hay un acento personalizado activo y explica que ese color tapa el acento del tema.
