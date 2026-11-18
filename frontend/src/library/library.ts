import type { PremiumUiConfig } from '../types';

/**
 * Bibliotecas de Películas y Series.
 * El hero solo se muestra en Inicio; en las bibliotecas el plugin se limita a
 * marcar la página con `data-pui-library` para que el CSS de tarjetas de
 * Inicio (home.css) aplique el hover premium. Sin JS recurrente ni observers.
 */

const LIBRARY_KINDS: Record<string, string> = {
    moviesPage: 'movies',
    tvshowsPage: 'series'
};

const marked = new WeakMap<HTMLElement, string>();

/**
 * Marca o desmarca la página según la configuración vigente. Se llama en cada
 * navegación y tras cada guardado del panel.
 */
export function mountLibrary(page: HTMLElement, config: PremiumUiConfig): void {
    const kind = LIBRARY_KINDS[page.id];

    if (!kind) {
        return;
    }

    const active = config.enabled && config.supported && config.home.enabled;
    const current = marked.get(page);

    if (active && current !== kind) {
        page.dataset.puiLibrary = kind;
        marked.set(page, kind);
    } else if (!active && current) {
        delete page.dataset.puiLibrary;
        marked.delete(page);
    }
}
