import assert from 'node:assert/strict';
import { pathToFileURL } from 'node:url';

import { JSDOM } from 'jsdom';

const config = {
    version: '0.10.0',
    serverVersion: '12.4.0.0',
    supported: true,
    enabled: true,
    design: {
        theme: 'cinema',
        transparency: 55,
        heroHeight: 62,
        cardSize: 'large',
        transitionMs: 180,
        hoverIntensity: 125,
        accentColor: '#7aa2ff',
        shadowColor: '#000000',
        backgroundColor: '',
        backgroundGradient: false,
        backgroundGradientDirection: 'bottom',
        backgroundGradientColor: ''
    },
    home: {
        enabled: true,
        heroEnabled: true,
        heroSource: 'latest',
        continueWatching: true,
        recentlyAdded: true,
        movies: true,
        series: true,
        recommendations: true,
        mediaLibraries: true,
        sectionOrder: ['hero', 'continue', 'estudios', 'recent', 'movies', 'series', 'recommendations'],
        itemsPerSection: 12,
        recommendationsCount: 12,
        recentlyAddedCount: 16,
        studioSections: ['Marvel', 'Netflix', 'Cine Random'],
        studioCollections: [
            {
                key: 'brand:marvel',
                name: 'Marvel',
                studioIds: ['studio-marvel-comics', 'studio-marvel-entertainment', 'studio-marvel-studios'],
                imageItemId: 'studio-marvel-studios',
                imageType: 'Thumb'
            },
            {
                key: 'brand:netflix',
                name: 'Netflix',
                studioIds: ['studio-netflix'],
                imageItemId: null,
                imageType: null
            },
            {
                key: 'studio:studio-random',
                name: 'Cine Random',
                studioIds: ['studio-random'],
                imageItemId: 'studio-random',
                imageType: 'Thumb'
            }
        ]
    },
    details: {
        enabled: true,
        backdrop: true,
        metadata: true,
        extraInfo: true,
        recommendations: true
    },
    performance: {
        preload: true,
        lazyLoading: true,
        cacheMinutes: 10,
        reducedMotion: 'off'
    }
};

const dom = new JSDOM(
    `<!doctype html><html><head></head><body>
        <div id="indexPage" class="page homePage">
            <div id="homeTab" class="tabContent pageTabContent">
                <div class="sections">
                    <div class="verticalSection section0">
                        <h2 class="sectionTitle">Películas</h2>
                        <div class="itemsContainer">
                            <div class="card" data-id="movie-1" data-type="Movie">
                                <div class="cardImageContainer" data-src="/Items/movie-1/Images/Primary?fillWidth=400&amp;fillHeight=225"></div>
                                <div class="cardText"><div class="cardText-first">Película Uno</div></div>
                            </div>
                        </div>
                    </div>
                    <div class="verticalSection section1">
                        <h2 class="sectionTitle">Mi contenido</h2>
                    </div>
                    <div class="section6">
                        <div class="verticalSection">
                            <h2 class="sectionTitle">Añadidos recientemente</h2>
                            <div class="itemsContainer">
                                <div class="card" data-id="recent-1" data-type="Movie">
                                    <div class="cardImageContainer" data-src="/Items/recent-1/Images/Primary?fillWidth=400&amp;fillHeight=225"></div>
                                    <div class="cardText"><div class="cardText-first">Reciente Uno</div></div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="section7">
                        <div class="verticalSection">
                            <h2 class="sectionTitle">Reciente en Series</h2>
                            <div class="itemsContainer">
                                <div class="card" data-id="recent-series-1" data-type="Series">
                                    <div class="cardImageContainer" data-src="/Items/recent-series-1/Images/Primary?fillWidth=400&amp;fillHeight=225"></div>
                                    <div class="cardText"><div class="cardText-first">Serie Reciente</div></div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="verticalSection section3">
                        <h2 class="sectionTitle">Continuar viendo</h2>
                        <div class="itemsContainer">
                            <div class="card" data-id="resume-1" data-type="Episode">
                                <div class="cardImageContainer" data-src="/Items/resume-1/Images/Thumb?fillWidth=400&amp;fillHeight=225" aria-label="Episodio Uno"></div>
                                <div class="cardText"><div class="cardText-first">Episodio Uno</div></div>
                                <div class="cardOverlayContainer itemAction" data-action="resume">
                                    <button class="cardOverlayFab-primary" data-action="resume" title="Reanudar"></button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div id="moviesPage" class="page mainAnimatedPage libraryPage" data-role="page" data-title="Películas">
            <div class="padded-bottom-page">
                <div class="itemsContainer padded-left padded-right vertical-wrap">
                    <div class="card portraitCard" data-id="lib-movie-1" data-type="Movie">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardContent">
                                    <div class="cardImageContainer coveredImage">
                                        <img src="/Items/lib-movie-1/Images/Primary?fillHeight=150&amp;fillWidth=200&amp;quality=96&amp;tag=abc">
                                    </div>
                                </div>
                            </div>
                            <div class="cardText">
                                <div class="cardText-first">Película de Biblioteca</div>
                                <div class="cardText-secondary">2019</div>
                            </div>
                        </div>
                        <div class="cardOverlayContainer itemAction" data-action="link">
                            <a href="#/details?id=lib-movie-1&amp;serverId=abc" aria-label="Película de Biblioteca" class="cardImageContainer"></a>
                            <button class="cardOverlayFab-primary" data-action="play" title="Reproducir"></button>
                        </div>
                    </div>
                    <div class="card portraitCard" data-id="lib-movie-2" data-type="Movie">
                        <div class="cardBox cardBox-bottompadded">
                            <div class="cardScalable">
                                <div class="cardContent">
                                    <div class="cardImageContainer coveredImage">
                                        <img src="/Items/lib-movie-2/Images/Primary?fillHeight=150&amp;fillWidth=200&amp;quality=96&amp;tag=def">
                                    </div>
                                </div>
                            </div>
                            <div class="cardText">
                                <div class="cardText-first">Segunda de Biblioteca</div>
                                <div class="cardText-secondary">2021</div>
                            </div>
                        </div>
                        <div class="cardOverlayContainer itemAction" data-action="link">
                            <a href="#/details?id=lib-movie-2&amp;serverId=abc" aria-label="Segunda de Biblioteca" class="cardImageContainer"></a>
                            <button class="cardOverlayFab-primary" data-action="play" title="Reproducir"></button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div id="itemDetailPage" class="page libraryPage itemDetailPage">
            <div id="itemBackdrop" class="itemBackdrop"></div>
        </div>
    </body></html>`,
    { url: 'http://localhost:8096/web/', pretendToBeVisual: true }
);

const { window } = dom;

const navigations = [];
window.Dashboard = {
    navigate: (url) => navigations.push(url)
};

// Cada colección llega resuelta en el bootstrap; la capa de la colección pide
// los títulos con todos los StudioIds del grupo a la vez.
const studioRequests = [];
const studioUrls = [];
const overlayTitles = {
    'studio-marvel-comics,studio-marvel-entertainment,studio-marvel-studios': [
        { Id: 'movie-iron', Name: 'Iron Man', Type: 'Movie', ProductionYear: 2008, RunTimeTicks: 75600000000, ImageTags: { Primary: 'p-iron' } },
        { Id: 'movie-thor', Name: 'Thor', Type: 'Movie', ProductionYear: 2011, RunTimeTicks: 69000000000, ImageTags: { Primary: 'p-thor' } },
        { Id: 'show-loki', Name: 'Loki', Type: 'Series', ProductionYear: 2021, ImageTags: { Primary: 'p-loki' } },
        { Id: 'movie-sin-imagen', Name: 'Sin imagen', Type: 'Movie', ProductionYear: 2012, ImageTags: {} }
    ]
};

let suggestionsEmpty = false;
let suggestionRequests = [];

window.ApiClient = {
    // Cada petición devuelve una copia: así la prueba distingue la caché en
    // memoria de una configuración realmente recibida del servidor.
    getJSON: async (url) => {
        const text = String(url);

        if (text.includes('/Items/Suggestions')) {
            suggestionRequests.push(text);
            return suggestionsEmpty
                ? { Items: [] }
                : {
                    Items: [
                        {
                            Id: 'rec-movie',
                            Name: 'Recomendada',
                            Type: 'Movie',
                            ProductionYear: 2020,
                            RunTimeTicks: 63000000000,
                            ImageTags: { Primary: 'p-rec-movie' }
                        },
                        {
                            Id: 'rec-series',
                            Name: 'Serie recomendada',
                            Type: 'Series',
                            ProductionYear: 2018,
                            ImageTags: { Primary: 'p-rec-series' }
                        }
                    ]
                };
        }

        if (text.includes('StudioIds=')) {
            const ids = [...text.matchAll(/StudioIds=([^&]+)/g)]
                .map((match) => decodeURIComponent(match[1]));
            const key = ids.join(',');
            studioRequests.push(key);
            studioUrls.push(text);
            return JSON.parse(JSON.stringify({
                Items: overlayTitles[key] ?? [],
                TotalRecordCount: (overlayTitles[key] ?? []).length
            }));
        }

        if (text.includes('SortBy=Random')) {
            const items = [
                {
                    Id: 'random-movie',
                    Name: 'Película aleatoria',
                    Type: 'Movie',
                    ProductionYear: 2021,
                    RunTimeTicks: 54000000000,
                    // Solo póster: el hero debe caer a Primary si no hay backdrop.
                    ImageTags: { Primary: 'p-random-movie' },
                    BackdropImageTags: []
                },
                {
                    Id: 'random-series',
                    Name: 'Serie aleatoria',
                    Type: 'Series',
                    ProductionYear: 2019,
                    ImageTags: { Primary: 'p-random-series' },
                    BackdropImageTags: ['b-random-series']
                }
            ];
            const onlyMovies = text.includes('IncludeItemTypes=Movie&');
            const onlySeries = text.includes('IncludeItemTypes=Series&');
            return JSON.parse(JSON.stringify({
                Items: onlyMovies
                    ? items.filter((item) => item.Type === 'Movie')
                    : onlySeries
                        ? items.filter((item) => item.Type === 'Series')
                        : items
            }));
        }

        return JSON.parse(JSON.stringify(config));
    },
    getUrl: (path) => new URL('/' + String(path).replace(/^\/+/, ''), window.location.origin).toString(),
    getCurrentUserId: () => 'user-1',
    serverId: () => 'abc'
};

for (const key of [
    'window',
    'document',
    'history',
    'location',
    'HTMLElement',
    'MutationObserver',
    'CustomEvent',
    'Event',
    'requestAnimationFrame'
]) {
    Object.defineProperty(globalThis, key, {
        value: key === 'requestAnimationFrame' ? window.requestAnimationFrame.bind(window) : window[key],
        configurable: true,
        writable: true
    });
}

const failures = [];
process.on('unhandledRejection', (reason) => failures.push(reason));

await import(pathToFileURL(new URL('../../src/Jellyfin.Plugin.PremiumUI/Web/app.js', import.meta.url).pathname).href);
await new Promise((resolve) => setTimeout(resolve, 500));

const document = window.document;
const titles = [...document.querySelectorAll('#homeTab .sections > .verticalSection > .sectionTitle')]
    .map((element) => element.textContent);

assert.deepEqual(
    titles,
    ['Continuar viendo', 'Mi contenido', 'Películas'],
    'las secciones conocidas se reordenan y las desconocidas no se mueven'
);

// Las filas que Jellyfin envuelve en `<div class="sectionN">` también
// participan en el orden: el envoltorio es la unidad que se mueve.
const unitTitles = [...document.querySelector('#homeTab .sections').children]
    .map((element) => element.querySelector('.sectionTitle')?.textContent)
    .filter(Boolean);
assert.deepEqual(
    unitTitles,
    ['Continuar viendo', 'Mi contenido', 'Añadidos recientemente', 'Reciente en Series', 'Películas', 'Recomendaciones'],
    'las filas envueltas en .sectionN siguen el orden configurado'
);

const hero = document.querySelector('#homeTab > .pui-hero');
assert.ok(hero, 'el hero debe existir');
assert.equal(hero.hidden, false, 'el hero debe mostrarse');
assert.equal(hero.querySelector('.pui-hero-title')?.textContent, 'Reciente Uno');
assert.equal(hero.querySelector('.pui-hero-kicker')?.textContent, 'Añadidos recientemente');
assert.match(
    hero.querySelector('.pui-hero-image')?.style.backgroundImage ?? '',
    /fillWidth=1920/,
    'la imagen del hero debe pedirse en alta resolución'
);

const dots = hero.querySelectorAll('.pui-hero-dot');
assert.equal(dots.length, 2, 'el hero solo debe usar las secciones de Últimas incorporaciones');
assert.equal(hero.querySelector('.pui-hero-next')?.hidden, false, 'el carrusel debe mostrar flechas con varias sugerencias');

hero.querySelector('.pui-hero-title')?.dispatchEvent(new window.MouseEvent('click', { bubbles: true }));
assert.match(
    navigations.at(-1) ?? '',
    /details\?id=recent-1/,
    'un clic en el hero debe abrir la ficha del elemento mostrado'
);

hero.querySelector('.pui-hero-next')?.dispatchEvent(new window.MouseEvent('click', { bubbles: true }));
assert.equal(
    hero.querySelector('.pui-hero-title')?.textContent,
    'Serie Reciente',
    'la flecha siguiente debe pasar a la otra sección de recientes'
);
assert.equal(document.documentElement.dataset.puiTheme, 'cinema');
assert.equal(document.documentElement.dataset.puiCards, 'large');
assert.equal(document.documentElement.style.getPropertyValue('--pui-card-scale'), '1.18');
assert.equal(document.documentElement.style.getPropertyValue('--pui-hover'), '1.25');
assert.equal(document.documentElement.dataset.puiMotion, 'full');
assert.equal(document.documentElement.style.getPropertyValue('--pui-accent'), '#7aa2ff');
assert.equal(document.documentElement.style.getPropertyValue('--pui-accent-contrast'), '#0b0b0b');
assert.equal(document.documentElement.style.getPropertyValue('--pui-accent-rgb'), '122, 162, 255');
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-card-shadow-rgb'),
    '0, 0, 0',
    'el color de la sombra se publica como canales RGB'
);
assert.match(
    document.documentElement.style.getPropertyValue('--pui-glow'),
    /^rgba\(122, 162, 255, 0\.22\)$/,
    'el halo del hero sigue al acento personalizado'
);
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-transparency'),
    '0.55',
    'la transparencia del panel se publica como alfa de las superficies'
);
assert.equal(
    document.documentElement.dataset.puiLazy,
    'on',
    'la carga diferida activa imágenes lazy en los estudios'
);

// Secciones de colección por estudio: aparecen tras resolver los nombres
// contra la API (el nombre ambiguo "Marvel" se resuelve por conteo).
await new Promise((resolve) => setTimeout(resolve, 300));

const studioRow = document.querySelector('#homeTab .sections .pui-studio-section .pui-studio-tile-row');
assert.ok(studioRow, 'el bloque de estudios debe existir en Inicio');

const studioTiles = [...studioRow.querySelectorAll('a.pui-studio-tile')];
assert.equal(studioTiles.length, 3, 'deben crearse un tile por estudio configurado');

// La sección de estudios se coloca según el "Orden de secciones" del panel:
// entre "Continuar viendo" (continue) y "Películas" (movies).
const studioSection = studioRow.closest('.pui-studio-section');
const byTitle = (text) => [...document.querySelectorAll('#homeTab .sections > .verticalSection')]
    .find((element) => element.querySelector('.sectionTitle')?.textContent === text);
const continueSection = byTitle('Continuar viendo');
const moviesSection = byTitle('Películas');
assert.ok(continueSection && moviesSection, 'deben existir las secciones nativas');
const positions = [...document.querySelector('#homeTab .sections').children];
assert.ok(
    positions.indexOf(continueSection) < positions.indexOf(studioSection)
        && positions.indexOf(studioSection) < positions.indexOf(moviesSection),
    'la sección de estudios debe seguir el orden configurado'
);
assert.equal(studioTiles[0].dataset.studioKey, 'brand:marvel');
assert.equal(studioTiles[0].dataset.studioId, 'studio-marvel-comics');
assert.ok(studioTiles[0].querySelector('.pui-studio-surface'), 'el tile eleva una superficie interior, no el enlace');
assert.ok(
    studioTiles[0].querySelector('.pui-studio-surface > img'),
    'la imagen vive dentro de la superficie estable'
);
assert.ok(
    studioTiles[0].querySelector('.pui-studio-surface > video'),
    'el vídeo también vive dentro de la superficie'
);
assert.match(
    studioTiles[0].querySelector('img')?.src ?? '',
    /\/web\/premiumui\/studios\/marvel-studios\.webp/,
    'Marvel usa el logo curado del plugin'
);
assert.equal(
    studioTiles[0].querySelector('img')?.getAttribute('loading'),
    'lazy',
    'con "Carga diferida" activa las imágenes de estudio no bloquean el render'
);
assert.equal(studioTiles[0].dataset.puiArtwork, 'marvel-studios');
assert.equal(studioTiles[1].dataset.studioKey, 'brand:netflix');
assert.match(
    studioTiles[1].getAttribute('href'),
    /#\/list\?type=Movie&studioId=studio-netflix&serverId=abc/,
    'un estudio único conserva la lista nativa'
);
assert.match(
    studioTiles[1].querySelector('img')?.src ?? '',
    /\/web\/premiumui\/studios\/netflix\.webp/,
    'Netflix usa el logo curado del plugin'
);
assert.match(
    studioTiles[2].querySelector('img')?.src ?? '',
    /Items\/studio-random\/Images\/Thumb/,
    'un estudio sin arte curado conserva el logo del servidor'
);

// Un grupo con varios estudios abre la colección completa del plugin y pide
// todos los StudioIds a la vez.
const click = new window.MouseEvent('click', { bubbles: true, cancelable: true, button: 0 });
assert.equal(studioTiles[0].dispatchEvent(click), false, 'el clic del grupo se intercepta');
await new Promise((resolve) => setTimeout(resolve, 60));

const overlay = document.querySelector('.pui-studio-overlay');
assert.ok(overlay, 'la colección de Marvel debe abrirse en una capa propia');
assert.equal(
    studioRequests.at(-1),
    'studio-marvel-comics,studio-marvel-entertainment,studio-marvel-studios',
    'la capa pide todos los estudios del grupo'
);
assert.match(
    studioUrls.at(-1) ?? '',
    /IncludeItemTypes=Movie%2CSeries/,
    'la colección incluye películas y series, igual que la lista nativa del estudio'
);
assert.equal(overlay.querySelector('.pui-overlay-title')?.textContent, 'Marvel');
assert.equal(
    overlay.querySelectorAll('.pui-overlay-card').length,
    3,
    'se pinta una tarjeta por película o serie con imagen y se omite la que no tiene póster'
);
assert.match(
    overlay.querySelector('.pui-overlay-card')?.getAttribute('href') ?? '',
    /#\/details\?id=movie-iron&serverId=abc/,
    'la tarjeta abre la ficha nativa'
);

overlay.querySelector('.pui-overlay-close')?.click();
assert.equal(document.querySelector('.pui-studio-overlay'), null, 'el botón de cerrar retira la capa');

// Un refresh sin cambios de configuración no debe reemplazar los tiles: el
// reemplazo reiniciaba la transición de hover (temblor) y podía perder clics.
const stableTile = studioTiles[0];
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 350));
assert.equal(
    document.querySelector('.pui-studio-tile'),
    stableTile,
    'los tiles se reutilizan si la configuración no cambió'
);

// Recomendaciones: fila propia del plugin con sugerencias del usuario.
const recommendations = document.querySelector('#homeTab .pui-recommendations-section');
assert.ok(recommendations, 'la fila de recomendaciones debe existir en Inicio');
assert.equal(
    recommendations.querySelector('.sectionTitle')?.textContent,
    'Recomendaciones',
    'la fila debe llevar su título'
);

const recCards = [...recommendations.querySelectorAll('a.pui-rec-card')];
assert.equal(recCards.length, 2, 'debe pintar una tarjeta por sugerencia');
assert.match(
    recCards[0].getAttribute('href') ?? '',
    /#\/details\?id=rec-movie&serverId=abc/,
    'el clic abre la ficha del elemento recomendado'
);
assert.match(
    recCards[0].querySelector('img')?.src ?? '',
    /Items\/rec-movie\/Images\/Primary/,
    'la tarjeta usa el póster del elemento'
);
assert.equal(recCards[0].querySelector('.pui-rec-title')?.textContent, 'Recomendada');

// La fila no tiene barra inferior: usa los mismos botones laterales que las
// filas nativas de Películas y Series.
const recRow = recommendations.querySelector('.pui-rec-row');
const recButtons = recommendations.querySelector('.emby-scrollbuttons');
const recLeft = recButtons?.querySelector('[data-direction="left"]');
const recRight = recButtons?.querySelector('[data-direction="right"]');

assert.ok(recRow, 'la fila de recomendaciones debe existir');
assert.equal(
    recRow.classList.contains('padded-left') && recRow.classList.contains('padded-right'),
    true,
    'la fila usa los mismos márgenes (3.3 % + safe-area) que las secciones nativas'
);
assert.ok(recButtons, 'la fila debe incluir la barra de botones nativa');
assert.ok(recLeft && recRight, 'la barra debe tener los botones izquierdo y derecho');
assert.equal(recLeft.classList.contains('emby-scrollbuttons-button'), true);
assert.equal(recLeft.querySelector('.material-icons')?.classList.contains('chevron_left'), true);
assert.equal(recRight.querySelector('.material-icons')?.classList.contains('chevron_right'), true);

// Desplazamiento simulado: el estado disabled se recalcula en cada scroll.
let recScrollLeft = 0;
Object.defineProperty(recRow, 'clientWidth', { configurable: true, value: 600 });
Object.defineProperty(recRow, 'scrollWidth', { configurable: true, value: 1800 });
Object.defineProperty(recRow, 'scrollLeft', {
    configurable: true,
    get: () => recScrollLeft,
    set: (value) => {
        recScrollLeft = value;
    }
});

const recScrollCalls = [];
recRow.scrollBy = (options) => {
    recScrollCalls.push(options);
    recScrollLeft = Math.max(0, Math.min(1200, recScrollLeft + options.left));
    recRow.dispatchEvent(new window.Event('scroll'));
};

recRow.dispatchEvent(new window.Event('scroll'));
assert.equal(recButtons.hidden, false, 'con contenido desbordado se muestran las flechas');
assert.equal(recLeft.disabled, true, 'al inicio la flecha izquierda está deshabilitada');
assert.equal(recRight.disabled, false, 'al inicio la flecha derecha está disponible');

recRight.click();
assert.equal(recScrollCalls.length, 1, 'la flecha derecha desplaza la fila');
assert.ok(recScrollCalls[0].left > 0, 'la flecha derecha avanza hacia el final');
assert.equal(recLeft.disabled, false, 'tras avanzar la flecha izquierda se habilita');

recScrollLeft = 1200;
recRow.dispatchEvent(new window.Event('scroll'));
assert.equal(recRight.disabled, true, 'al final la flecha derecha queda deshabilitada');
recLeft.click();
assert.ok(recScrollCalls[1].left < 0, 'la flecha izquierda retrocede');

// Sin desbordamiento (pocos elementos) la barra se oculta por completo.
recScrollLeft = 0;
Object.defineProperty(recRow, 'scrollWidth', { configurable: true, value: 600 });
recRow.dispatchEvent(new window.Event('scroll'));
assert.equal(recButtons.hidden, true, 'sin desbordamiento no se muestran flechas');

// Las recomendaciones tienen su propio número: no dependen del hero.
suggestionsEmpty = true;
suggestionRequests = [];
config.home.recommendationsCount = 6;
config.home.itemsPerSection = 20;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.ok(suggestionRequests.length > 0, 'debe pedir sugerencias al servidor');
assert.ok(
    suggestionRequests.every((url) => url.includes('limit=6') || url.includes('Limit=6')),
    'las recomendaciones usan su propio número aunque el hero tenga otro'
);

const fallbackTitles = [...document.querySelectorAll('#homeTab .pui-recommendations-section .pui-rec-title')]
    .map((element) => element.textContent);
assert.deepEqual(
    fallbackTitles.sort(),
    ['Película aleatoria', 'Serie aleatoria'],
    'sin historial deben mostrarse películas y series aleatorias'
);
suggestionsEmpty = false;
config.home.itemsPerSection = 12;
config.home.recommendationsCount = 12;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

// "Mis contenidos" (es-ES) es una fila nativa reconocida: se puede colocar bajo los
// estudios y se puede ocultar desde el panel.
const sectionsContainer = document.querySelector('#homeTab .sections');
const mediaSection = document.createElement('div');
mediaSection.className = 'verticalSection section-mine';
mediaSection.innerHTML = '<h2 class="sectionTitle">Mis contenidos</h2>'
    + '<div class="itemsContainer"><div class="card" data-id="lib-1" data-type="CollectionFolder"></div></div>';
sectionsContainer.insertBefore(mediaSection, sectionsContainer.firstChild);

config.home.sectionOrder = ['hero', 'estudios', 'media', 'continue', 'recent', 'movies', 'series', 'recommendations'];
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

const currentOrder = [...sectionsContainer.children];
const indexOf = (text) => currentOrder.findIndex(
    (element) => element.querySelector('.sectionTitle')?.textContent === text
);
const studioIndex = currentOrder.findIndex((element) => element.classList.contains('pui-studio-section'));
assert.ok(studioIndex >= 0, 'la sección de estudios sigue presente');
assert.ok(
    studioIndex < indexOf('Mis contenidos') && indexOf('Mis contenidos') < indexOf('Continuar viendo'),
    '"Mis contenidos" se coloca entre Estudios y Continuar viendo'
);

config.home.mediaLibraries = false;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

assert.ok(
    mediaSection.classList.contains('pui-section-hidden'),
    'desmarcar "Mis contenidos" oculta la fila de bibliotecas'
);

config.home.mediaLibraries = true;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

assert.ok(
    !mediaSection.classList.contains('pui-section-hidden'),
    'volver a marcar "Mis contenidos" la muestra de nuevo'
);

mediaSection.remove();
config.home.sectionOrder = ['hero', 'continue', 'estudios', 'recent', 'movies', 'series', 'recommendations'];
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

// Los cambios de configuración se reflejan sin recargar la página.
const allCollections = config.home.studioCollections;
config.home.studioCollections = allCollections.filter((collection) => collection.key === 'brand:netflix');
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

const updatedRow = document.querySelector('#homeTab .sections .pui-studio-tile-row');
assert.equal(updatedRow?.querySelectorAll('a.pui-studio-tile').length, 1, 'los tiles retirados de la configuración desaparecen');
assert.equal(updatedRow?.querySelector('a.pui-studio-tile')?.dataset.studioKey, 'brand:netflix');

// Desactivar los estudios retira la sección completa.
config.home.studioCollections = [];
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

assert.equal(
    document.querySelector('#homeTab .pui-studio-section'),
    null,
    'al desactivar los estudios no queda la sección'
);

config.home.studioCollections = allCollections;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 300));

// Bibliotecas de Películas/Series: el hero vive solo en Inicio; la biblioteca
// solo se marca para que el CSS aplique el hover premium.
const libraryPage = document.querySelector('#moviesPage');
assert.equal(libraryPage.dataset.puiLibrary, 'movies', 'la biblioteca debe marcarse para el estilo premium');
assert.equal(libraryPage.querySelector('.pui-hero'), null, 'la biblioteca no debe recibir hero');

// El TTL protege la navegación normal: refresh() sin invalidar no vuelve a pedir.
config.design.accentColor = '#000000';
await window.PremiumUI.refresh();
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-accent'),
    '#7aa2ff',
    'refresh() sin invalidar debe respetar la caché en memoria'
);

// Guardado desde el panel: invalidate() + refresh() aplica lo nuevo sin recargar.
config.design.accentColor = '#22c55e';
config.design.shadowColor = '#112233';
config.design.backgroundColor = '#101820';
config.home.recentlyAdded = false;
config.home.movies = false;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.ok(window.PremiumUI.fetchedAt > 0, 'invalidate() debe forzar un bootstrap nuevo');
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-accent'),
    '#22c55e',
    'el acento guardado debe aplicarse sin Ctrl+Shift+R'
);
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-card-shadow-rgb'),
    '17, 34, 51',
    'el color de la sombra guardado debe aplicarse sin Ctrl+Shift+R'
);
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-app-bg'),
    '#101820',
    'el color de fondo guardado debe aplicarse sin Ctrl+Shift+R'
);
assert.equal(
    document.documentElement.dataset.puiBg,
    'on',
    'el fondo personalizado debe activarse en el documento'
);

// Degradado del fondo: color propio, dirección lineal, radial, variante
// automática y vuelta al color plano.
config.design.backgroundGradient = true;
config.design.backgroundGradientDirection = 'top-left';
config.design.backgroundGradientColor = '#22d3ee';
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(
    document.documentElement.dataset.puiBgGradient,
    'on',
    'el degradado debe activarse con data-pui-bg-gradient'
);
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-app-bg-image'),
    'linear-gradient(to top left, #101820, #22d3ee)',
    'el color del degradado debe ser el segundo extremo de la imagen'
);

config.design.backgroundGradientDirection = 'radial-top';
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(
    document.documentElement.style.getPropertyValue('--pui-app-bg-image'),
    'radial-gradient(circle at 50% 0%, #101820, #22d3ee)',
    'las direcciones radiales deben publicarse en --pui-app-bg-image'
);

config.design.backgroundGradientColor = '';
config.design.backgroundGradientDirection = 'bottom';
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.match(
    document.documentElement.style.getPropertyValue('--pui-app-bg-image'),
    /^linear-gradient\(to bottom, #101820, #[0-9a-f]{6}\)$/,
    'sin color de degradado debe derivarse una variante del color de fondo'
);

config.design.backgroundGradient = false;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(
    document.documentElement.dataset.puiBgGradient,
    'off',
    'sin degradado se retira data-pui-bg-gradient'
);
assert.equal(
    document.documentElement.style.getPropertyValue('--pui-app-bg-image'),
    '',
    'sin degradado se retira --pui-app-bg-image'
);

// Los checks "Carruseles" ocultan filas nativas sin romper el hero.
const hiddenRows = [...document.querySelectorAll('#homeTab .pui-section-hidden')]
    .map((element) => element.querySelector('.sectionTitle')?.textContent);
assert.deepEqual(
    hiddenRows.sort(),
    ['Añadidos recientemente', 'Películas', 'Reciente en Series'],
    'los checks deben ocultar las filas de Inicio correspondientes'
);
assert.equal(hero.hidden, false, 'el hero debe seguir visible con filas ocultas');
assert.equal(hero.querySelector('.pui-hero-next')?.hidden, false, 'el hero debe seguir siendo un carrusel');
assert.equal(hero.querySelectorAll('.pui-hero-dot').length, 2, 'el hero conserva sus sugerencias');

const details = document.querySelector('#itemDetailPage');
assert.ok(details.classList.contains('pui-details'), 'Detalles debe recibir la clase');
assert.equal(details.dataset.puiBackdrop, 'on');
assert.equal(document.documentElement.dataset.puiDetails, 'on');
assert.equal(document.documentElement.dataset.puiBackdrop, 'on');
assert.equal(document.documentElement.dataset.puiRecommendations, 'on');
assert.equal(details.dataset.puiRecommendations, 'on');

// "Aleatorio" pide a la API contenido de toda la biblioteca (películas y
// series), no solo las filas de recientes ya cargadas en Inicio.
config.home.heroSource = 'random';
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

const randomTitles = [...document.querySelectorAll('#homeTab .pui-hero-dot')].length;
assert.equal(randomTitles, 2, 'el hero aleatorio debe usar los elementos de la API');
const randomTitle = hero.querySelector('.pui-hero-title')?.textContent ?? '';
assert.ok(
    ['Película aleatoria', 'Serie aleatoria'].includes(randomTitle),
    'el hero aleatorio debe mostrar una película o serie de toda la biblioteca'
);
assert.match(
    hero.querySelector('.pui-hero-image.is-active')?.style.backgroundImage ?? '',
    /Items\/(random-movie|random-series)\/Images\/(Primary|Backdrop)\//,
    'la imagen del hero aleatorio debe venir de la API'
);

// "Películas" se limita a películas de toda la biblioteca.
config.home.heroSource = 'movies';
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(
    hero.querySelector('.pui-hero-title')?.textContent,
    'Película aleatoria',
    'la fuente Películas solo debe traer películas'
);
assert.equal(hero.querySelector('.pui-hero-kicker')?.textContent, 'Películas');
assert.equal(
    hero.querySelectorAll('.pui-hero-dot').length,
    1,
    'con una sola película no debe haber carrusel'
);

// Desactivar el hero lo oculta en Inicio; las bibliotecas siguen marcadas
// (el hover premium depende de Inicio, no del hero).
config.home.heroEnabled = false;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(document.querySelector('#homeTab > .pui-hero')?.hidden, true, 'el hero de Inicio se oculta');
assert.equal(document.querySelector('#moviesPage .pui-hero'), null, 'las bibliotecas nunca tienen hero');
assert.equal(document.querySelector('#moviesPage').dataset.puiLibrary, 'movies', 'la biblioteca sigue marcada');

// Número configurable de tarjetas en "Agregado recientemente": se piden hasta
// 30 al servidor y el excedente se oculta sin recargar la vista.
function addRecentCards(title, count) {
    const section = [...document.querySelectorAll('#homeTab .verticalSection')]
        .find((element) => element.querySelector('.sectionTitle')?.textContent === title);
    const container = section?.querySelector('.itemsContainer');

    if (!container) {
        return null;
    }

    for (let index = 0; index < count; index += 1) {
        const card = document.createElement('div');
        card.className = 'card';
        card.dataset.id = `extra-${title}-${index}`;
        card.innerHTML = '<div class="cardImageContainer"></div>';
        container.appendChild(card);
    }

    return container;
}

const movieRecent = addRecentCards('Añadidos recientemente', 9);
const seriesRecent = addRecentCards('Reciente en Series', 8);

assert.ok(movieRecent && seriesRecent, 'deben existir las filas nativas de recién agregados');
assert.equal(movieRecent.querySelectorAll('.card').length, 10);
assert.equal(seriesRecent.querySelectorAll('.card').length, 9);

config.home.recentlyAdded = true;
config.home.movies = true;
config.home.recentlyAddedCount = 6;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

const visibleCards = (container) => [...container.querySelectorAll('.card')]
    .filter((card) => !card.classList.contains('pui-card-trimmed'));

assert.equal(visibleCards(movieRecent).length, 6, 'la fila de películas respeta el número configurado');
assert.equal(visibleCards(seriesRecent).length, 6, 'la fila de series respeta el número configurado');

config.home.recentlyAddedCount = 20;
window.PremiumUI.invalidate();
await window.PremiumUI.refresh();
await new Promise((resolve) => setTimeout(resolve, 400));

assert.equal(visibleCards(movieRecent).length, 10, 'al subir el número se recuperan las tarjetas ocultas');
assert.equal(visibleCards(seriesRecent).length, 9, 'al subir el número se recuperan las tarjetas ocultas');

assert.deepEqual(failures, [], 'no debe haber rechazos sin manejar');

window.close();
console.log('smoke ok');
