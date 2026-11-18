import { lazyImagesEnabled } from '../core/config';
import { create } from '../core/dom';
import type { HomeConfig, StudioCollection } from '../types';
import { resolveStudioArtwork } from './studioAssets';
import { openStudioList } from './studioList';

const MARKER = 'puiStudio';
const SECTION_SELECTOR = 'section.verticalSection.pui-studio-section';

interface TileState {
    token: string;
    element: HTMLAnchorElement;
}

/**
 * Secciones de colección por estudio en Inicio. El servidor resuelve los
 * nombres configurados a grupos (Disney, Walt Disney, Disney Channel…), así que
 * cada tile representa una marca completa: al pulsarlo se abre la lista de
 * películas de todos sus estudios, no solo de una de sus variantes.
 */
export function applyStudioSections(container: HTMLElement, home: HomeConfig): void {
    const collections = (home.studioCollections ?? [])
        .filter((collection) => collection && collection.name && collection.studioIds?.length > 0);
    const token = collections
        .map((collection) => `${collection.key}|${collection.studioIds.join(',')}`)
        .join(';');

    // El token permite descartar una respuesta de configuración anterior.
    container.dataset[MARKER + 'Key'] = token;

    if (!home.enabled || collections.length === 0) {
        for (const section of existingSections(container)) {
            section.remove();
        }

        return;
    }

    const section = ensureSection(container);
    render(section, collections, token);
}

function existingSections(container: HTMLElement): HTMLElement[] {
    return Array.from(container.querySelectorAll<HTMLElement>(SECTION_SELECTOR));
}

/**
 * Devuelve la sección de estudios, creándola al principio de `.sections`
 * (es decir, inmediatamente después del hero) si todavía no existe.
 */
function ensureSection(container: HTMLElement): HTMLElement {
    const existing = existingSections(container)[0];

    if (existing) {
        return existing;
    }

    const section = create('section', 'verticalSection pui-studio-section');
    section.dataset.puiStudio = 'on';

    const row = document.createElement('div');
    row.className = 'pui-studio-tile-row';
    section.append(row);

    container.insertBefore(section, container.firstChild);
    return section;
}

function render(section: HTMLElement, collections: StudioCollection[], token: string): void {
    let row = section.querySelector<HTMLElement>('.pui-studio-tile-row');

    if (!row) {
        row = document.createElement('div');
        row.className = 'pui-studio-tile-row';
        section.append(row);
    }

    const previous = new Map<string, TileState>();

    for (const tile of Array.from(row.querySelectorAll<HTMLAnchorElement>('.pui-studio-tile'))) {
        const key = tile.dataset.studioKey ?? '';

        if (key.length > 0) {
            previous.set(key, { token: tile.dataset.token ?? '', element: tile });
        }
    }

    const desired: HTMLAnchorElement[] = [];

    for (const collection of collections) {
        const existing = previous.get(collection.key);
        const tileToken = `${token}|${collection.name}|${collection.imageItemId ?? ''}`;

        desired.push(existing && existing.token === tileToken
            ? existing.element
            : buildTile(collection, tileToken));
    }

    // Solo se toca el DOM cuando hay un cambio real. Reemplazar la fila en
    // cada pasada del observer reiniciaba las transiciones CSS (el tile
    // temblaba) y podía perder el clic si el elemento se movía entre
    // `pointerdown` y `pointerup`.
    for (const tile of Array.from(row.querySelectorAll<HTMLAnchorElement>('.pui-studio-tile'))) {
        if (!desired.includes(tile)) {
            tile.remove();
        }
    }

    desired.forEach((tile, index) => {
        if (row.children[index] !== tile) {
            row.insertBefore(tile, row.children[index] ?? null);
        }
    });
}

function buildTile(collection: StudioCollection, token: string): HTMLAnchorElement {
    const ids = collection.studioIds;
    const link = document.createElement('a');
    link.className = 'pui-studio-tile itemAction';
    link.dataset.studioKey = collection.key;
    link.dataset.token = token;
    link.dataset.studioId = ids[0];

    // La lista nativa sigue siendo el enlace de respaldo (clic con modificador
    // o apertura en otra pestaña); con varios estudios se abre la colección
    // completa del plugin.
    const serverId = window.ApiClient?.serverId?.();
    link.href = `#/list?type=Movie&studioId=${encodeURIComponent(ids[0])}`
        + (serverId ? `&serverId=${encodeURIComponent(serverId)}` : '');
    link.setAttribute('aria-label', collection.name);

    if (ids.length > 1) {
        link.addEventListener('click', (event) => {
            if (event.defaultPrevented
                || event.button !== 0
                || event.metaKey
                || event.ctrlKey
                || event.shiftKey
                || event.altKey) {
                return;
            }

            event.preventDefault();
            openStudioList(collection.name, ids);
        });
    }

    // El enlace queda quieto y solo se eleva la superficie interior: así el
    // cursor nunca sale del área sensible durante el hover y el tile no
    // vibra al acercarse a los bordes.
    const surface = create('span', 'pui-studio-surface');

    const artwork = resolveStudioArtwork(collection.name);
    const fallback = fallbackImage(collection);
    const imageUrl = artwork?.logo ?? fallback;

    if (artwork) {
        link.dataset.puiArtwork = artwork.slug;
        attachVideo(link, surface, artwork.video);
    }

    if (imageUrl) {
        const image = document.createElement('img');
        image.src = imageUrl;
        image.setAttribute('loading', lazyImagesEnabled() ? 'lazy' : 'eager');
        image.decoding = 'async';
        image.alt = '';

        if (artwork && fallback) {
            // Si el asset curado faltara, se usa el logo del servidor.
            image.addEventListener('error', () => {
                image.src = fallback;
            }, { once: true });
        }

        surface.append(image);
    } else {
        const label = document.createElement('span');
        label.className = 'pui-studio-name';
        label.textContent = collection.name;
        surface.append(label);
    }

    link.append(surface);
    return link;
}

/**
 * Logo nativo del estudio que representa la colección. El bootstrap ya trae
 * el id y el tipo de imagen, así que no hace falta buscar el estudio.
 */
function fallbackImage(collection: StudioCollection): string | null {
    if (!collection.imageItemId || !collection.imageType) {
        return null;
    }

    return `${window.ApiClient!.getUrl('Items')}/${collection.imageItemId}/Images/${collection.imageType}?fillWidth=480&quality=90`;
}

/**
 * Reproduce el bucle del estudio solo mientras el tile tiene el cursor o el
 * foco. El `src` se asigna en el primer hover para no descargar 12 vídeos al
 * cargar Inicio; con movimiento reducido no se crea el elemento.
 */
function attachVideo(link: HTMLAnchorElement, surface: HTMLElement, videoUrl: string | null): void {
    if (!videoUrl || shouldReduceMotion() || !canHover()) {
        return;
    }

    const video = document.createElement('video');
    video.className = 'pui-studio-video';
    video.muted = true;
    video.loop = true;
    video.playsInline = true;
    video.preload = 'none';
    video.setAttribute('aria-hidden', 'true');

    const start = (): void => {
        if (!video.src) {
            video.src = videoUrl;
        }

        try {
            const playback = video.play();

            if (playback && typeof playback.then === 'function') {
                void playback.catch(() => {});
            }
        } catch {
            // Sin reproducción automática el tile conserva el logo estático.
        }
    };

    const stop = (): void => {
        video.pause();
        link.classList.remove('pui-studio-playing');

        if (video.readyState > 0) {
            video.currentTime = 0;
        }
    };

    video.addEventListener('playing', () => link.classList.add('pui-studio-playing'));
    link.addEventListener('pointerenter', start);
    link.addEventListener('pointerleave', stop);
    link.addEventListener('focus', start);
    link.addEventListener('blur', stop);

    // El vídeo va debajo del logo, dentro de la superficie que se eleva.
    surface.prepend(video);
}

function shouldReduceMotion(): boolean {
    if (document.documentElement.dataset.puiMotion === 'reduce') {
        return true;
    }

    return typeof window.matchMedia === 'function'
        && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

function canHover(): boolean {
    return typeof window.matchMedia !== 'function'
        || window.matchMedia('(hover: hover)').matches;
}
