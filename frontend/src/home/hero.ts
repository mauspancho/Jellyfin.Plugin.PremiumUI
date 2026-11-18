import { create } from '../core/dom';
import { t } from '../core/i18n';
import { shouldReduceMotion } from '../core/config';
import type { HomeConfig, PremiumUiConfig } from '../types';
import { classifySection, normalizeTitle } from './classify';

export interface HeroController {
    readonly element: HTMLElement;
    update(sections: HTMLElement[], config: PremiumUiConfig, options?: HeroOptions): void;
    destroy(): void;
}

export interface HeroOptions {
    /** Ignora "Contenido destacado": la biblioteca usa sus propias tarjetas. */
    ignoreSource?: boolean;
    /** Texto superior del hero cuando no hay `.sectionTitle` (bibliotecas). */
    label?: string;
}

interface HeroSource {
    card: HTMLElement;
    section: HTMLElement;
    label: string;
}

const DEFAULT_ITEMS = 12;
const MAX_ITEMS = 20;
const ROTATE_MS = 9000;
const SWIPE_MIN = 48;

/**
 * Hero cinematográfico construido a partir de las tarjetas que Jellyfin ya
 * cargó. No hace peticiones de API propias ni clona el DOM de Jellyfin.
 *
 * Es un carrusel de sugerencias: rota entre varias tarjetas de las secciones
 * habilitadas y cualquier clic (o Enter) fuera de sus controles abre la ficha
 * del elemento mostrado.
 */
export function createHero(container: HTMLElement): HeroController {
    const seed = Math.random();
    const element = create('section', 'pui-hero');
    element.hidden = true;
    element.setAttribute('aria-hidden', 'true');
    element.setAttribute('role', 'region');
    element.setAttribute('tabindex', '0');

    const layers = [
        create('div', 'pui-hero-image'),
        create('div', 'pui-hero-image')
    ];
    const slides = create('div', 'pui-hero-slides');
    slides.setAttribute('aria-hidden', 'true');
    slides.append(...layers);

    const shade = create('div', 'pui-hero-shade');
    shade.setAttribute('aria-hidden', 'true');

    const body = create('div', 'pui-hero-body');
    const kicker = create('p', 'pui-hero-kicker');
    const title = create('h2', 'pui-hero-title');
    const meta = create('p', 'pui-hero-meta');

    const actions = create('div', 'pui-hero-actions');

    const play = create('button', 'pui-hero-button pui-hero-play');
    play.type = 'button';
    const playIcon = create('span', 'material-icons', 'play_arrow');
    playIcon.setAttribute('aria-hidden', 'true');
    const playLabel = create('span', 'pui-hero-label', t('play'));
    play.append(playIcon, playLabel);

    const more = create('button', 'pui-hero-button pui-hero-more');
    more.type = 'button';
    const moreIcon = create('span', 'material-icons', 'info');
    moreIcon.setAttribute('aria-hidden', 'true');
    const moreLabel = create('span', 'pui-hero-label', t('more'));
    more.append(moreIcon, moreLabel);

    actions.append(play, more);
    body.append(kicker, title, meta, actions);

    const prev = create('button', 'pui-hero-nav pui-hero-prev');
    prev.type = 'button';
    prev.hidden = true;
    prev.setAttribute('aria-label', t('previous'));
    const prevIcon = create('span', 'material-icons', 'chevron_left');
    prevIcon.setAttribute('aria-hidden', 'true');
    prev.append(prevIcon);

    const next = create('button', 'pui-hero-nav pui-hero-next');
    next.type = 'button';
    next.hidden = true;
    next.setAttribute('aria-label', t('next'));
    const nextIcon = create('span', 'material-icons', 'chevron_right');
    nextIcon.setAttribute('aria-hidden', 'true');
    next.append(nextIcon);

    const dots = create('div', 'pui-hero-dots');
    dots.hidden = true;

    element.append(slides, shade, body, prev, next, dots);

    container.insertBefore(element, container.firstChild);

    let sources: HeroSource[] = [];
    let index = 0;
    let listKey = '';
    let renderedKey = '';
    let activeLayer = 0;
    let timer = 0;
    let paused = false;
    let reduceMotion = false;
    let preload = true;
    let warnedSource = '';
    let touchStart: { x: number; y: number } | null = null;

    // Fuentes de biblioteca completa ("Aleatorio", "Películas", "Series"): se
    // piden a la API y se cachean hasta que cambian la fuente o el presupuesto.
    let lastSections: HTMLElement[] = [];
    let lastConfig: PremiumUiConfig | null = null;
    let lastOptions: HeroOptions | undefined;
    let apiSources: HeroSource[] = [];
    let apiKey = '';
    let apiToken = 0;

    const current = (): HeroSource | null => sources[index] ?? null;

    const openDetails = (): void => {
        const card = current()?.card;

        if (!card) {
            return;
        }

        const link = card.querySelector<HTMLAnchorElement>('a[href]');
        const id = card.dataset.id;
        const serverId = card.dataset.serverid;

        if (id && window.Dashboard?.navigate) {
            const query = 'details?id=' + encodeURIComponent(id)
                + (serverId ? '&serverId=' + encodeURIComponent(serverId) : '');
            window.Dashboard.navigate(query);
            return;
        }

        if (link) {
            link.click();
            return;
        }

        card.querySelector<HTMLElement>('.cardImageContainer')?.click();
    };

    play.addEventListener('click', () => {
        const card = current()?.card;

        if (!card) {
            return;
        }

        const action = card.querySelector<HTMLElement>('.cardOverlayFab-primary')
            ?? card.querySelector<HTMLElement>('[data-action="resume"], [data-action="play"]');

        if (action) {
            action.click();
        } else {
            openDetails();
        }
    });

    more.addEventListener('click', openDetails);

    // Clic en cualquier zona del hero (salvo sus propios controles) abre la ficha.
    element.addEventListener('click', (event) => {
        const target = event.target as HTMLElement | null;

        if (target && typeof target.closest === 'function' && target.closest('button, a')) {
            return;
        }

        openDetails();
    });

    element.addEventListener('keydown', (event) => {
        if (event.target !== element) {
            return;
        }

        if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault();
            openDetails();
        } else if (event.key === 'ArrowRight') {
            event.preventDefault();
            goTo(index + 1);
        } else if (event.key === 'ArrowLeft') {
            event.preventDefault();
            goTo(index - 1);
        }
    });

    prev.addEventListener('click', () => goTo(index - 1));
    next.addEventListener('click', () => goTo(index + 1));

    element.addEventListener('pointerenter', () => setPaused(true));
    element.addEventListener('pointerleave', () => setPaused(false));
    element.addEventListener('focusin', () => setPaused(true));
    element.addEventListener('focusout', (event) => {
        const related = event.relatedTarget as Node | null;

        if (!related || !element.contains(related)) {
            setPaused(false);
        }
    });

    // Swipe horizontal en móvil, sin bloquear el desplazamiento vertical.
    element.addEventListener('touchstart', (event) => {
        const touch = event.touches[0];

        touchStart = touch ? { x: touch.clientX, y: touch.clientY } : null;
    }, { passive: true });

    element.addEventListener('touchend', (event) => {
        const start = touchStart;
        const touch = event.changedTouches[0];

        touchStart = null;

        if (!start || !touch || sources.length < 2) {
            return;
        }

        const dx = touch.clientX - start.x;
        const dy = touch.clientY - start.y;

        if (Math.abs(dx) < SWIPE_MIN || Math.abs(dx) < Math.abs(dy) * 1.5) {
            return;
        }

        goTo(dx < 0 ? index + 1 : index - 1);
    }, { passive: true });

    const onVisibility = (): void => schedule();
    document.addEventListener('visibilitychange', onVisibility);

    function setPaused(value: boolean): void {
        paused = value;
        schedule();
    }

    function goTo(target: number): void {
        if (sources.length === 0) {
            return;
        }

        index = ((target % sources.length) + sources.length) % sources.length;
        render();
        schedule();
    }

    function schedule(): void {
        if (timer) {
            window.clearTimeout(timer);
            timer = 0;
        }

        if (reduceMotion || paused || document.hidden || sources.length < 2) {
            return;
        }

        timer = window.setTimeout(() => {
            timer = 0;
            goTo(index + 1);
        }, ROTATE_MS);
    }

    function showImage(url: string, immediate: boolean): void {
        if (immediate) {
            layers[activeLayer]?.classList.remove('is-active');
            layers[1 - activeLayer]?.classList.remove('is-active');
            activeLayer = 0;
            const layer = layers[0];

            if (layer) {
                layer.style.backgroundImage = `url("${escapeCssUrl(url)}")`;
                layer.classList.add('is-active');
            }

            return;
        }

        const incoming = layers[1 - activeLayer];
        const outgoing = layers[activeLayer];

        if (!incoming || !outgoing) {
            return;
        }

        incoming.style.backgroundImage = `url("${escapeCssUrl(url)}")`;
        incoming.classList.add('is-active');
        outgoing.classList.remove('is-active');
        activeLayer = 1 - activeLayer;
    }

    function render(): void {
        const item = current();

        if (!item) {
            return;
        }

        const cardImage = item.card.querySelector<HTMLElement>('.cardImageContainer');
        const name = item.card.querySelector('.cardText-first')?.textContent?.trim()
            || cardImage?.getAttribute('aria-label')?.trim()
            || '';
        const sectionTitle = item.label;
        // La precarga decide si el hero pide una imagen en alta resolución o
        // reutiliza la de la tarjeta: entra en la clave para que el cambio se
        // aplique sin esperar a que rote la diapositiva.
        const key = `${item.card.dataset.id ?? ''}|${sectionTitle}|${eventKey(item.card)}|${preload ? 'hi' : 'lo'}`;

        syncDots();

        if (key === renderedKey) {
            return;
        }

        const first = renderedKey === '';
        renderedKey = key;

        element.hidden = false;
        element.removeAttribute('aria-hidden');
        element.setAttribute('aria-label', name ? `${t('suggestions')}: ${name}` : 'Premium UI');

        const sourceImage = cardImage ? imageSource(cardImage) : null;

        if (sourceImage) {
            const heroImage = preload ? highResolution(sourceImage) : sourceImage;
            showImage(heroImage, first);
        }

        title.textContent = name;
        title.hidden = name.length === 0;

        kicker.textContent = sectionTitle;
        kicker.hidden = sectionTitle.length === 0;

        const secondary = item.card.querySelector('.cardText-secondary')?.textContent?.trim() ?? '';
        meta.textContent = secondary;
        meta.hidden = secondary.length === 0;

        const playTitle = item.card
            .querySelector('[data-action="play"], [data-action="resume"]')
            ?.getAttribute('title')?.trim();
        playLabel.textContent = playTitle || t('play');

        if (sourceImage && preload) {
            preloadNext();
        }
    }

    function preloadNext(): void {
        if (sources.length < 2) {
            return;
        }

        const upcoming = sources[(index + 1) % sources.length];
        const cardImage = upcoming?.card.querySelector<HTMLElement>('.cardImageContainer');
        const source = cardImage ? imageSource(cardImage) : null;

        if (source) {
            try {
                const image = new window.Image();
                image.src = highResolution(source);
            } catch {
                // Precarga opcional: si el entorno no la soporta, el carrusel sigue.
            }
        }
    }

    function syncDots(): void {
        const multiple = sources.length > 1;
        prev.hidden = !multiple;
        next.hidden = !multiple;
        dots.hidden = !multiple;

        const buttons = dots.children;

        for (let i = 0; i < buttons.length; i += 1) {
            const button = buttons[i] as HTMLElement;
            const active = i === index;
            button.classList.toggle('is-active', active);

            if (active) {
                button.setAttribute('aria-current', 'true');
            } else {
                button.removeAttribute('aria-current');
            }
        }
    }

    function rebuildDots(): void {
        dots.replaceChildren();

        for (let i = 0; i < sources.length; i += 1) {
            const dot = create('button', 'pui-hero-dot');
            dot.type = 'button';
            dot.setAttribute('aria-label', t('slide', i + 1));
            dot.addEventListener('click', () => goTo(i));
            dots.append(dot);
        }
    }

    function hide(): void {
        element.hidden = true;
        element.setAttribute('aria-hidden', 'true');
        renderedKey = '';
        sources = [];
        listKey = '';
        schedule();
    }

    function update(sections: HTMLElement[], config: PremiumUiConfig, options?: HeroOptions): void {
        lastSections = sections;
        lastConfig = config;
        lastOptions = options;

        if (!config.home.heroEnabled) {
            hide();
            return;
        }

        const source = config.home.heroSource;
        const budget = heroBudget(config.home);
        const usesApi = !options?.ignoreSource
            && (source === 'random' || source === 'movies' || source === 'series')
            && Boolean(window.ApiClient);
        let found: HeroSource[] = [];

        if (usesApi) {
            const key = `${source}|${budget}|${window.ApiClient?.serverId?.() ?? ''}`;

            if (key !== apiKey) {
                apiKey = key;
                apiSources = [];
                const token = ++apiToken;

                void fetchHeroItems(source, budget).then((items) => {
                    if (token !== apiToken) {
                        return;
                    }

                    apiSources = items;
                    update(lastSections, lastConfig ?? config, lastOptions);
                }).catch(() => {
                    if (token === apiToken) {
                        apiSources = [];
                        update(lastSections, lastConfig ?? config, lastOptions);
                    }
                });
            }

            // Mientras llega la respuesta (o si la API falla) se usa lo que ya
            // está cargado en Inicio, para no dejar el hero vacío.
            found = apiSources.length > 0
                ? apiSources
                : collectSources(sections, config.home, seed, options);
        } else {
            apiKey = '';
            apiSources = [];
            found = collectSources(sections, config.home, seed, options);
        }

        if (found.length === 0) {
            if (warnedSource !== config.home.heroSource) {
                warnedSource = config.home.heroSource;
                console.info('[PremiumUI] El hero no tiene contenido para la fuente', config.home.heroSource);
            }

            hide();
            return;
        }

        reduceMotion = shouldReduceMotion(config);
        preload = config.performance.preload;

        const nextKey = found.map((source) => source.card.dataset.id ?? '').join('|');

        if (nextKey !== listKey) {
            const currentId = current()?.card.dataset.id;
            sources = found;
            listKey = nextKey;

            const keep = currentId
                ? sources.findIndex((source) => source.card.dataset.id === currentId)
                : -1;

            index = keep >= 0 ? keep : 0;
            renderedKey = '';
            rebuildDots();
        }

        element.hidden = false;
        render();
        schedule();
    }

    return {
        element,
        update,

        destroy(): void {
            if (timer) {
                window.clearTimeout(timer);
                timer = 0;
            }

            document.removeEventListener('visibilitychange', onVisibility);
            element.remove();
            sources = [];
            listKey = '';
            renderedKey = '';
        }
    };
}

function collectSources(
    sections: HTMLElement[],
    home: HomeConfig,
    seed: number,
    options?: HeroOptions
): HeroSource[] {
    // El panel "Elementos por carrusel" define cuántas sugerencias ofrece el hero.
    const budget = Math.min(Math.max(home.itemsPerSection || DEFAULT_ITEMS, 4), MAX_ITEMS);
    const source = home.heroSource;

    // El contenido destacado manda: si se elige Películas, Series, Últimas
    // incorporaciones o Seguir viendo, solo se usan secciones y tarjetas que
    // correspondan. "Aleatorio" reparte entre todas las secciones habilitadas.
    // En una biblioteca (Películas/Series) se usan sus propias tarjetas.
    const candidates = sections
        .filter((section) => options?.ignoreSource || matchesSource(section, source))
        .map((section) => ({
            section,
            label: options?.label?.trim() || sectionTitleOf(section),
            cards: rotate(
                filterCards(cardsIn(section), options?.ignoreSource ? '' : source),
                seed + hashString(sectionTitleOf(section))
            )
        }))
        .filter((entry) => entry.cards.length > 0);

    const ordered = source === 'random'
        ? stableShuffle(candidates, seed)
        : candidates;

    const result: HeroSource[] = [];
    const seen = new Set<string>();
    let added = true;

    while (result.length < budget && added) {
        added = false;

        for (const entry of ordered) {
            const card = entry.cards.shift();

            if (!card) {
                continue;
            }

            added = true;
            const id = card.dataset.id;

            if (!id || seen.has(id)) {
                continue;
            }

            seen.add(id);
            result.push({ card, section: entry.section, label: entry.label });

            if (result.length >= budget) {
                break;
            }
        }
    }

    return result;
}

function matchesSource(section: HTMLElement, source: string): boolean {
    // Los checks "Carruseles" ahora controlan las filas de Inicio. El hero
    // mantiene su propia fuente ("Contenido destacado") y sigue usando las
    // tarjetas ya cargadas aunque su fila esté oculta, para no quedarse con una
    // sola sugerencia cuando se desmarca un carrusel.
    if (source === 'random') {
        return true;
    }

    const kind = classifySection(section);
    const title = normalizeTitle(sectionTitleOf(section));
    const types = cardTypes(section);

    switch (source) {
        case 'latest':
            return kind === 'recent';
        case 'resume':
            return kind === 'continue';
        case 'movies':
            return kind === 'movies'
                || types.includes('Movie')
                || title.includes('pelicula')
                || title.includes('movie');
        case 'series':
            return kind === 'series'
                || types.includes('Series')
                || title.includes('serie');
        default:
            return true;
    }
}

function filterCards(cards: HTMLElement[], source: string): HTMLElement[] {
    if (source !== 'movies' && source !== 'series') {
        return cards;
    }

    const type = source === 'movies' ? 'Movie' : 'Series';
    const typed = cards.filter((card) => card.dataset.type === type);

    // Algunas filas de Jellyfin no exponen `data-type` en sus tarjetas. En ese
    // caso se usa la sección completa en lugar de dejar el hero vacío.
    return typed.length > 0 ? typed : cards;
}

function cardTypes(section: HTMLElement): string[] {
    const types = new Set<string>();

    for (const card of section.querySelectorAll<HTMLElement>('.card[data-type]')) {
        const type = card.dataset.type;

        if (type) {
            types.add(type);
        }
    }

    return [...types];
}

function sectionTitleOf(section: HTMLElement): string {
    return section.querySelector('.sectionTitle')?.textContent?.trim() ?? '';
}

function cardsIn(section: HTMLElement): HTMLElement[] {
    if (section.closest('.hide')) {
        return [];
    }

    return Array.from(section.querySelectorAll<HTMLElement>('.card[data-id]')).filter((card) => {
        if (card.classList.contains('hide') || card.closest('.hide') || card.dataset.type === 'CollectionFolder') {
            return false;
        }

        const image = card.querySelector<HTMLElement>('.cardImageContainer');
        return image !== null && imageSource(image) !== null;
    });
}

function eventKey(card: HTMLElement): string {
    return card.querySelector<HTMLAnchorElement>('a[href]')?.getAttribute('href') ?? '';
}

function imageSource(element: HTMLElement): string | null {
    const dataSrc = element.getAttribute('data-src');

    if (dataSrc) {
        return dataSrc;
    }

    // Las tarjetas de las bibliotecas de Jellyfin 12.4 pintan un `<img>` en
    // lugar de usar `data-src`; el hero reutiliza esa misma imagen.
    const image = element.querySelector<HTMLImageElement>('img[src], img[data-src]');
    const src = image?.getAttribute('data-src') || image?.getAttribute('src') || image?.currentSrc;

    if (src && !src.startsWith('data:')) {
        return src;
    }

    const match = /url\(["']?(.+?)["']?\)/.exec(element.style.backgroundImage);
    return match?.[1] ?? null;
}

/**
 * Reutiliza la misma imagen de la tarjeta, ya en caché, pidiendo una resolución
 * mayor para que el hero no se vea pixelado.
 */
function highResolution(source: string): string {
    try {
        const url = new URL(source, window.location.href);

        if (url.pathname.includes('/Images/')) {
            url.searchParams.delete('maxWidth');
            url.searchParams.delete('maxHeight');
            url.searchParams.set('fillWidth', '1920');
            url.searchParams.set('fillHeight', '1080');
            url.searchParams.set('quality', '90');
        }

        return url.href;
    } catch {
        return source;
    }
}

function escapeCssUrl(url: string): string {
    return url.replace(/["\\\n\r]/g, '\\$&');
}

/** Rota el orden sin perderlo: cada visita empieza por un elemento distinto. */
function rotate<T>(values: T[], seed: number): T[] {
    if (values.length < 2) {
        return values;
    }

    const offset = Math.floor(pseudoRandom(seed) * values.length);
    return [...values.slice(offset), ...values.slice(0, offset)];
}

function hashString(value: string): number {
    let hash = 0;

    for (let index = 0; index < value.length; index += 1) {
        hash = (hash * 31 + value.charCodeAt(index)) % 100000;
    }

    return hash;
}

function stableShuffle<T>(values: T[], seed: number): T[] {
    return values
        .map((value, index) => ({ value, rank: pseudoRandom(seed + index * 0.6180339887) }))
        .sort((a, b) => a.rank - b.rank)
        .map((entry) => entry.value);
}

function pseudoRandom(value: number): number {
    const x = Math.sin(value * 12.9898) * 43758.5453;
    return x - Math.floor(x);
}

function heroBudget(home: HomeConfig): number {
    return Math.min(Math.max(home.itemsPerSection || DEFAULT_ITEMS, 4), MAX_ITEMS);
}

interface ApiItem {
    Id: string;
    Name: string;
    Type: string;
    ProductionYear?: number;
    RunTimeTicks?: number;
    ImageTags?: Record<string, string>;
    BackdropImageTags?: string[];
}

/**
 * "Aleatorio" combina películas y series de toda la biblioteca; "Películas" y
 * "Series" se limitan a su tipo. "Últimas incorporaciones" sigue usando las
 * filas de recientes que Jellyfin ya cargó en Inicio.
 */
async function fetchHeroItems(source: string, budget: number): Promise<HeroSource[]> {
    const types = source === 'movies'
        ? 'Movie'
        : source === 'series'
            ? 'Series'
            : 'Movie,Series';
    const url = `${itemsUrl()}?Recursive=true`
        + `&IncludeItemTypes=${types}`
        + '&SortBy=Random'
        + `&Limit=${budget}`
        + '&Fields=ProductionYear,RunTimeTicks,PrimaryImageAspectRatio'
        + '&EnableImages=true&ImageTypeLimit=1&EnableTotalRecordCount=false';
    const response = await window.ApiClient!.getJSON<{ Items?: ApiItem[] }>(url);

    const result: HeroSource[] = [];

    for (const item of response.Items ?? []) {
        const hero = toHeroSource(item, source);
        if (hero) {
            result.push(hero);
        }
    }

    return result;
}

function toHeroSource(item: ApiItem, source: string): HeroSource | null {
    const image = itemImageUrl(item);

    if (!image) {
        return null;
    }

    const card = document.createElement('div');
    card.className = 'card';
    card.dataset.id = item.Id;
    card.dataset.type = item.Type;

    const serverId = window.ApiClient?.serverId?.();

    if (serverId) {
        card.dataset.serverid = serverId;
    }

    const imageContainer = document.createElement('div');
    imageContainer.className = 'cardImageContainer';
    imageContainer.dataset.src = image;
    imageContainer.setAttribute('aria-label', item.Name);
    card.append(imageContainer);

    const name = document.createElement('div');
    name.className = 'cardText cardText-first';
    name.textContent = item.Name;
    card.append(name);

    const meta = itemMeta(item);

    if (meta) {
        const secondary = document.createElement('div');
        secondary.className = 'cardText cardText-secondary';
        secondary.textContent = meta;
        card.append(secondary);
    }

    const label = source === 'movies'
        ? t('movies')
        : source === 'series'
            ? t('series')
            : item.Type === 'Series' ? t('series') : t('movies');

    return { card, section: card, label };
}

function itemsUrl(): string {
    return window.ApiClient!.getUrl('Items');
}

function itemImageUrl(item: ApiItem): string | null {
    const base = `${itemsUrl()}/${item.Id}/Images/`;
    const path = item.BackdropImageTags && item.BackdropImageTags.length > 0
        ? 'Backdrop/0'
        : item.ImageTags?.Primary
            ? 'Primary/0'
            : item.ImageTags?.Thumb
                ? 'Thumb/0'
                : null;

    if (!path) {
        return null;
    }

    return `${base}${path}?fillWidth=1920&fillHeight=1080&quality=90`;
}

function itemMeta(item: ApiItem): string {
    const parts: string[] = [];

    if (item.ProductionYear) {
        parts.push(String(item.ProductionYear));
    }

    if (item.Type !== 'Series' && item.RunTimeTicks) {
        const minutes = Math.round(item.RunTimeTicks / 600000000);

        if (minutes >= 60) {
            const hours = Math.floor(minutes / 60);
            const rest = minutes % 60;
            parts.push(rest > 0 ? `${hours} h ${rest} min` : `${hours} h`);
        } else if (minutes > 0) {
            parts.push(`${minutes} min`);
        }
    }

    return parts.join(' · ');
}
