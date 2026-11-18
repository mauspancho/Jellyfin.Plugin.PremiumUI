import { create } from '../core/dom';
import { t } from '../core/i18n';

const PAGE_SIZE = 60;

interface MediaItem {
    Id: string;
    Name: string;
    Type: string;
    ProductionYear?: number;
    RunTimeTicks?: number;
    ImageTags?: Record<string, string>;
}

interface MediaResponse {
    Items?: MediaItem[];
    TotalRecordCount?: number;
}

interface SortOption {
    value: string;
    order: string;
    label: string;
}

let active: OverlayState | null = null;

interface OverlayState {
    root: HTMLElement;
    grid: HTMLElement;
    status: HTMLElement;
    more: HTMLButtonElement;
    select: HTMLSelectElement;
    close: () => void;
}

/**
 * Lista completa de una colección agrupada (por ejemplo, todos los estudios de
 * Disney). Es una capa propia del plugin sobre la vista actual: consulta la API
 * con todos los `StudioIds` del grupo y abre la ficha nativa al pulsar una
 * tarjeta, sin modificar la lista ni la reproducción de Jellyfin.
 */
export function openStudioList(name: string, studioIds: string[]): void {
    closeStudioList();

    if (studioIds.length === 0) {
        return;
    }

    const previousFocus = document.activeElement instanceof HTMLElement
        ? document.activeElement
        : null;
    const previousOverflow = document.body.style.overflow;
    const root = create('div', 'pui-studio-overlay');
    root.dataset.puiOverlay = 'studio';
    root.setAttribute('role', 'dialog');
    root.setAttribute('aria-modal', 'true');
    root.setAttribute('aria-label', name);

    const header = create('div', 'pui-overlay-head');
    const title = create('h2', 'pui-overlay-title', name);
    const status = create('span', 'pui-overlay-status', t('loading'));
    status.setAttribute('role', 'status');

    const sort = create('label', 'pui-overlay-sort');
    const sortLabel = create('span', 'pui-overlay-sort-label', t('sortBy'));
    const select = document.createElement('select');
    select.className = 'pui-overlay-select';
    select.setAttribute('aria-label', t('sortBy'));

    for (const option of sortOptions()) {
        const item = document.createElement('option');
        item.value = option.value;
        item.textContent = option.label;
        select.append(item);
    }

    sort.append(sortLabel, select);

    const close = create('button', 'pui-overlay-close');
    close.type = 'button';
    close.setAttribute('aria-label', t('close'));
    close.title = t('close');
    close.append(create('span', 'material-icons', 'close'));

    header.append(title, status, sort, close);

    const body = create('div', 'pui-overlay-body');
    const grid = create('div', 'pui-overlay-grid');
    const more = create('button', 'pui-overlay-more');
    more.type = 'button';
    more.textContent = t('loadMore');
    more.hidden = true;

    body.append(grid, more);
    root.append(header, body);
    document.body.append(root);
    document.body.style.overflow = 'hidden';

    const state: OverlayState = {
        root,
        grid,
        status,
        more,
        select,
        close: () => closeStudioList()
    };
    active = state;

    const seen = new Set<string>();
    let start = 0;
    let total = 0;
    let loading = false;
    let sortValue = select.value;

    const setStatus = (message: string): void => {
        status.textContent = message;
    };

    const load = async (reset: boolean): Promise<void> => {
        if (loading) {
            return;
        }

        loading = true;
        select.disabled = true;
        more.disabled = true;
        setStatus(t('loading'));

        if (reset) {
            start = 0;
            total = 0;
            seen.clear();
            grid.replaceChildren();
        }

        try {
            const response = await window.ApiClient!.getJSON<MediaResponse>(
                buildUrl(studioIds, sortValue, start)
            );
            const items = response.Items ?? [];
            total = response.TotalRecordCount ?? start + items.length;
            start += items.length;

            const fragment = document.createDocumentFragment();
            let added = 0;

            for (const item of items) {
                if (!item.Id || seen.has(item.Id) || !posterUrl(item)) {
                    continue;
                }

                seen.add(item.Id);
                fragment.append(buildCard(item));
                added += 1;
            }

            grid.append(fragment);
            more.hidden = items.length < PAGE_SIZE || start >= total;

            if (seen.size === 0) {
                setStatus(t('noTitles'));
            } else {
                setStatus(`${start} / ${total} ${t('titles')}`);
            }

            if (added === 0 && !more.hidden) {
                // La página solo tenía elementos sin imagen: se pide la
                // siguiente para no dejar un hueco.
                loading = false;
                select.disabled = false;
                more.disabled = false;
                await load(false);
                return;
            }
        } catch {
            setStatus(t('loadError'));
        } finally {
            loading = false;
            select.disabled = false;
            more.disabled = false;
        }
    };

    close.addEventListener('click', () => closeStudioList());

    root.addEventListener('click', (event) => {
        if (event.target === root) {
            closeStudioList();
        }
    });

    select.addEventListener('change', () => {
        sortValue = select.value;
        void load(true);
    });

    more.addEventListener('click', () => {
        void load(false);
    });

    const onKey = (event: KeyboardEvent): void => {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeStudioList();
            return;
        }

        if (event.key === 'Tab') {
            trapFocus(event, root);
        }
    };

    const onHashChange = (): void => closeStudioList();

    document.addEventListener('keydown', onKey);
    window.addEventListener('hashchange', onHashChange);

    state.close = () => {
        document.removeEventListener('keydown', onKey);
        window.removeEventListener('hashchange', onHashChange);
        document.body.style.overflow = previousOverflow;
        root.remove();
        active = null;
        previousFocus?.focus();
    };

    close.focus();
    void load(true);
}

function closeStudioList(): void {
    active?.close();
}

function sortOptions(): SortOption[] {
    return [
        { value: 'SortName', order: 'Ascending', label: t('sortTitle') },
        { value: 'ProductionYear', order: 'Descending', label: t('sortYear') },
        { value: 'DateCreated', order: 'Descending', label: t('sortAdded') }
    ];
}

function buildUrl(studioIds: string[], sortBy: string, start: number): string {
    const order = sortOptions().find((option) => option.value === sortBy)?.order ?? 'Ascending';
    const params = new URLSearchParams();
    params.set('Recursive', 'true');
    params.set('IncludeItemTypes', 'Movie,Series');
    params.set('SortBy', sortBy);
    params.set('SortOrder', order);
    params.set('Fields', 'ProductionYear,RunTimeTicks,PrimaryImageAspectRatio');
    params.set('EnableImages', 'true');
    params.set('ImageTypeLimit', '1');
    params.set('EnableTotalRecordCount', 'true');
    params.set('StartIndex', String(start));
    params.set('Limit', String(PAGE_SIZE));

    for (const id of studioIds) {
        params.append('StudioIds', id);
    }

    return `${window.ApiClient!.getUrl('Items')}?${params.toString()}`;
}

function buildCard(item: MediaItem): HTMLAnchorElement {
    const link = create('a', 'pui-overlay-card') as HTMLAnchorElement;
    const serverId = window.ApiClient?.serverId?.();
    link.href = `#/details?id=${encodeURIComponent(item.Id)}`
        + (serverId ? `&serverId=${encodeURIComponent(serverId)}` : '');
    link.setAttribute('aria-label', item.Name);
    link.dataset.itemId = item.Id;

    const poster = create('span', 'pui-overlay-poster');
    const image = document.createElement('img');
    image.src = posterUrl(item) ?? '';
    image.loading = 'lazy';
    image.decoding = 'async';
    image.alt = '';
    poster.append(image);

    const title = create('span', 'pui-overlay-name', item.Name);
    const meta = create('span', 'pui-overlay-meta', itemMeta(item));

    link.append(poster, title, meta);
    return link;
}

function posterUrl(item: MediaItem): string | null {
    const tag = item.ImageTags?.Primary
        ? 'Primary'
        : item.ImageTags?.Thumb
            ? 'Thumb'
            : null;

    if (!tag) {
        return null;
    }

    return `${window.ApiClient!.getUrl('Items')}/${item.Id}/Images/${tag}?fillWidth=320&fillHeight=480&quality=90`;
}

function itemMeta(item: MediaItem): string {
    const parts: string[] = [];

    if (item.ProductionYear) {
        parts.push(String(item.ProductionYear));
    }

    if (item.RunTimeTicks) {
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

function trapFocus(event: KeyboardEvent, root: HTMLElement): void {
    const focusable = Array.from(root.querySelectorAll<HTMLElement>(
        'a[href], button:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
    )).filter((element) => element.offsetParent !== null);

    if (focusable.length === 0) {
        return;
    }

    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const current = document.activeElement;

    if (event.shiftKey && current === first) {
        event.preventDefault();
        last.focus();
    } else if (!event.shiftKey && current === last) {
        event.preventDefault();
        first.focus();
    }
}
