import { create } from '../core/dom';
import { t } from '../core/i18n';
import { classifySection } from './classify';
import type { HomeConfig } from '../types';

const MARKER = 'puiRecommendations';
const SECTION_SELECTOR = 'section.pui-recommendations-section';
const TYPES = 'Movie,Series';

interface RecommendationItem {
    Id: string;
    Name: string;
    Type: string;
    ProductionYear?: number;
    RunTimeTicks?: number;
    ImageTags?: Record<string, string>;
}

interface CacheEntry {
    key: string;
    at: number;
    pending: Promise<RecommendationItem[]>;
}

interface ScrollerState {
    observer: ResizeObserver | null;
}

const scrollers = new WeakMap<HTMLElement, ScrollerState>();

let cache: CacheEntry | null = null;

/**
 * Fila de recomendaciones de Inicio.
 *
 * Usa `Items/Suggestions`, que Jellyfin calcula a partir del historial del
 * usuario; si el usuario es nuevo (o la API no devuelve nada), cae a una
 * selección aleatoria de películas y series de toda la biblioteca. Es una
 * sección propia del plugin: no reemplaza ni modifica filas de Jellyfin.
 */
export function applyRecommendations(container: HTMLElement, home: HomeConfig): void {
    if (!home.enabled || !home.recommendations) {
        removeSections(container);
        return;
    }

    const limit = Math.min(Math.max(home.itemsPerSection || 12, 4), 20);
    const userId = currentUserId();
    const key = `${userId}|${limit}`;

    // La sección se crea antes de pedir los datos para que el orden de
    // secciones la coloque en su sitio desde el primer render.
    container.dataset[MARKER + 'Key'] = key;
    ensureSection(container, home);

    void load(key, limit, userId).then((items) => {
        if (!container.isConnected || container.dataset[MARKER + 'Key'] !== key) {
            return;
        }

        if (items.length === 0) {
            removeSections(container);
            return;
        }

        render(container, items, home);
    }).catch(() => {
        // Si ambas peticiones fallan, la fila se retira sin romper Inicio.
        if (container.dataset[MARKER + 'Key'] === key) {
            removeSections(container);
        }
    });
}

function existingSections(container: HTMLElement): HTMLElement[] {
    return Array.from(container.querySelectorAll<HTMLElement>(SECTION_SELECTOR));
}

/**
 * Retira las filas del plugin y desconecta sus observers para no dejar
 * referencias vivas cuando la configuración cambia o se sale de Inicio.
 */
function removeSections(container: HTMLElement): void {
    for (const section of existingSections(container)) {
        scrollers.get(section)?.observer?.disconnect();
        scrollers.delete(section);
        section.remove();
    }
}

function ensureSection(container: HTMLElement, home: HomeConfig): HTMLElement {
    const existing = existingSections(container)[0];

    if (existing) {
        ensureScroller(existing);
        return existing;
    }

    const section = create('section', 'verticalSection pui-recommendations-section');
    section.dataset[MARKER] = 'on';

    const header = create('div', 'sectionTitleContainer sectionTitleContainer-cards padded-left');
    const title = create('h2', 'sectionTitle sectionTitle-cards');
    title.textContent = t('recommendations');
    header.append(title);

    // Mismos botones que las filas nativas de Películas y Series: sustituyen
    // a la barra de desplazamiento inferior y se colocan en la esquina
    // superior derecha sobre la cabecera.
    const buttons = create('div', 'emby-scrollbuttons padded-right');
    buttons.append(
        buildScrollButton('left', 'chevron_left', t('previous')),
        buildScrollButton('right', 'chevron_right', t('next'))
    );

    const row = create('div', 'pui-rec-row');
    section.append(header, buttons, row);
    insertByOrder(container, section, home);
    ensureScroller(section);

    return section;
}

function buildScrollButton(
    direction: 'left' | 'right',
    icon: string,
    title: string
): HTMLButtonElement {
    const button = create('button', 'emby-scrollbuttons-button');
    button.type = 'button';
    button.dataset.direction = direction;
    button.title = title;
    button.setAttribute('aria-label', title);

    const glyph = create('span', `material-icons ${icon}`);
    glyph.setAttribute('aria-hidden', 'true');
    button.append(glyph);

    return button;
}

/**
 * Conecta los botones con el desplazamiento horizontal de la fila. El estado
 * `disabled` se recalcula en cada scroll y cuando cambia el tamaño de la fila;
 * no hay temporizadores ni sondeos.
 */
function ensureScroller(section: HTMLElement): void {
    if (scrollers.has(section)) {
        updateScrollButtons(section);
        return;
    }

    const bar = section.querySelector<HTMLElement>('.emby-scrollbuttons');
    const row = section.querySelector<HTMLElement>('.pui-rec-row');

    if (!bar || !row) {
        return;
    }

    for (const button of Array.from(bar.querySelectorAll<HTMLButtonElement>('.emby-scrollbuttons-button'))) {
        button.addEventListener('click', () => {
            scrollRow(row, button.dataset.direction === 'left' ? -1 : 1);
        });
    }

    row.addEventListener('scroll', () => updateScrollButtons(section), { passive: true });

    const observer = typeof ResizeObserver === 'function'
        ? new ResizeObserver(() => updateScrollButtons(section))
        : null;
    observer?.observe(row);

    scrollers.set(section, { observer });

    // El ancho real solo se conoce tras el primer cálculo de layout.
    const frame = window.requestAnimationFrame
        ? window.requestAnimationFrame.bind(window)
        : (callback: FrameRequestCallback) => window.setTimeout(() => callback(0), 16);
    frame(() => updateScrollButtons(section));
}

function updateScrollButtons(section: HTMLElement): void {
    const bar = section.querySelector<HTMLElement>('.emby-scrollbuttons');
    const row = section.querySelector<HTMLElement>('.pui-rec-row');

    if (!bar || !row) {
        return;
    }

    const left = bar.querySelector<HTMLButtonElement>('[data-direction="left"]');
    const right = bar.querySelector<HTMLButtonElement>('[data-direction="right"]');
    const scrollWidth = row.scrollWidth;
    const clientWidth = row.clientWidth;
    const overflow = scrollWidth - clientWidth > 4;
    const position = isRtl(row) ? -row.scrollLeft : row.scrollLeft;
    const atStart = position <= 2;
    const atEnd = position + clientWidth >= scrollWidth - 2;

    bar.hidden = !overflow;

    if (left) {
        left.disabled = !overflow || atStart;
    }

    if (right) {
        right.disabled = !overflow || atEnd;
    }
}

function scrollRow(row: HTMLElement, direction: number): void {
    const amount = Math.max(row.clientWidth * 0.8, 220);
    const delta = direction * amount * (isRtl(row) ? -1 : 1);
    const behavior: ScrollBehavior = prefersReducedMotion() ? 'auto' : 'smooth';

    if (typeof row.scrollBy === 'function') {
        row.scrollBy({ left: delta, behavior });
    } else {
        row.scrollLeft += delta;
    }
}

function isRtl(element: HTMLElement): boolean {
    return window.getComputedStyle(element).direction === 'rtl';
}

function prefersReducedMotion(): boolean {
    if (document.documentElement.dataset.puiMotion === 'reduce') {
        return true;
    }

    return typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * Inserta la fila en la posición que marca "Orden de secciones" desde el
 * primer render, sin esperar a que el observer repita el reordenado.
 */
function insertByOrder(container: HTMLElement, section: HTMLElement, home: HomeConfig): void {
    const order = home.sectionOrder ?? [];
    const position = order.indexOf('recommendations');

    if (position < 0) {
        container.append(section);
        return;
    }

    const later = new Set(order.slice(position + 1));
    let anchor: Element | null = null;

    for (const child of Array.from(container.children)) {
        if (child === section || !(child instanceof HTMLElement)) {
            continue;
        }

        const kind = classifySection(child);

        if (kind && later.has(kind)) {
            anchor = child;
            break;
        }
    }

    container.insertBefore(section, anchor);
}

function load(key: string, limit: number, userId: string): Promise<RecommendationItem[]> {
    const ttl = window.PremiumUI?.ttlMs ?? 5 * 60 * 1000;

    if (cache && cache.key === key && Date.now() - cache.at < ttl) {
        return cache.pending;
    }

    const pending = fetchRecommendations(limit, userId);
    cache = { key, at: Date.now(), pending };
    pending.catch(() => {});
    return pending;
}

async function fetchRecommendations(limit: number, userId: string): Promise<RecommendationItem[]> {
    if (userId.length > 0) {
        try {
            const personalized = await requestItems(
                `${itemsUrl()}/Suggestions?userId=${encodeURIComponent(userId)}`
                + `&limit=${limit}`
                + '&type=Movie&type=Series'
                + '&enableTotalRecordCount=false'
            );

            if (personalized.length > 0) {
                return personalized.slice(0, limit);
            }
        } catch {
            // Se intenta el respaldo aleatorio.
        }
    }

    return requestItems(
        `${itemsUrl()}?Recursive=true`
        + `&IncludeItemTypes=${TYPES}`
        + '&SortBy=Random'
        + `&Limit=${limit}`
        + '&Fields=ProductionYear,RunTimeTicks,PrimaryImageAspectRatio'
        + '&EnableImages=true&ImageTypeLimit=1&EnableTotalRecordCount=false'
    );
}

async function requestItems(url: string): Promise<RecommendationItem[]> {
    const response = await window.ApiClient!.getJSON<{ Items?: RecommendationItem[] }>(url);
    const seen = new Set<string>();
    const result: RecommendationItem[] = [];

    for (const item of response.Items ?? []) {
        if (!item.Id || seen.has(item.Id) || item.Type === 'CollectionFolder') {
            continue;
        }

        if (!imageUrl(item)) {
            continue;
        }

        seen.add(item.Id);
        result.push(item);
    }

    return result;
}

function render(container: HTMLElement, items: RecommendationItem[], home: HomeConfig): void {
    const section = ensureSection(container, home);
    let row = section.querySelector<HTMLElement>('.pui-rec-row');

    if (!row) {
        row = create('div', 'pui-rec-row');
        section.append(row);
    }

    ensureScroller(section);

    const seen = new Set<string>();
    let index = 0;

    for (const item of items) {
        const existing = row.querySelector<HTMLElement>(`[data-item-id="${cssEscape(item.Id)}"]`);

        if (existing) {
            seen.add(item.Id);
            continue;
        }

        row.append(buildCard(item));
        seen.add(item.Id);
        index += 1;
    }

    // Retira tarjetas de una lista anterior que ya no se recomiendan.
    for (const card of Array.from(row.querySelectorAll<HTMLElement>('[data-item-id]'))) {
        if (!seen.has(card.dataset.itemId ?? '')) {
            card.remove();
        }
    }

    if (index > 0) {
        section.hidden = false;
    }

    updateScrollButtons(section);
}

function buildCard(item: RecommendationItem): HTMLAnchorElement {
    const link = create('a', 'pui-rec-card') as HTMLAnchorElement;
    const serverId = window.ApiClient?.serverId?.();
    link.href = `#/details?id=${encodeURIComponent(item.Id)}`
        + (serverId ? `&serverId=${encodeURIComponent(serverId)}` : '');
    link.setAttribute('aria-label', item.Name);
    link.dataset.itemId = item.Id;

    const image = create('span', 'pui-rec-image');
    const picture = document.createElement('img');
    picture.src = imageUrl(item) as string;
    picture.loading = 'lazy';
    picture.decoding = 'async';
    picture.alt = '';
    image.append(picture);

    const title = create('span', 'pui-rec-title');
    title.textContent = item.Name;

    const meta = create('span', 'pui-rec-meta');
    meta.textContent = itemMeta(item);

    link.append(image, title, meta);
    return link;
}

function imageUrl(item: RecommendationItem): string | null {
    const tag = item.ImageTags?.Primary
        ? 'Primary'
        : item.ImageTags?.Thumb
            ? 'Thumb'
            : null;

    if (!tag) {
        return null;
    }

    return `${itemsUrl()}/${item.Id}/Images/${tag}?fillWidth=320&fillHeight=480&quality=90`;
}

function itemMeta(item: RecommendationItem): string {
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

function currentUserId(): string {
    try {
        return window.ApiClient?.getCurrentUserId?.() ?? '';
    } catch {
        return '';
    }
}

function itemsUrl(): string {
    return window.ApiClient!.getUrl('Items');
}

function cssEscape(value: string): string {
    const escape = window.CSS?.escape;

    if (typeof escape === 'function') {
        return escape.call(window.CSS, value);
    }

    return value.replace(/["\\]/g, '\\$&');
}
