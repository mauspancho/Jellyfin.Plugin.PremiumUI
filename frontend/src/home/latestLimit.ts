/**
 * Jellyfin pide a `/Items/Latest` un límite fijo de 16 tarjetas. Para poder
 * mostrar hasta 30 sin recargar la vista, se reescribe el parámetro `limit` de
 * esas peticiones y el excedente se oculta en el DOM (`applyRecentLimit`).
 *
 * El parche se instala al cargar el script, antes de que Jellyfin monte Inicio.
 * Se apoya en `XMLHttpRequest` porque es el transporte que usa el SDK de
 * Jellyfin Web 12.4 para esta llamada; `fetch` no interviene.
 */

const REQUESTED_LIMIT = 30;
const PATCH_FLAG = '__premiumUiLatestLimit__';

type PatchedWindow = Window & { [PATCH_FLAG]?: boolean };

function rewrite(url: string): string {
    if (!/\/Items\/Latest(?:[/?]|$)/.test(url)) {
        return url;
    }

    if (/([?&]limit=)\d+/i.test(url)) {
        return url.replace(/([?&]limit=)\d+/i, `$1${REQUESTED_LIMIT}`);
    }

    return `${url}${url.includes('?') ? '&' : '?'}limit=${REQUESTED_LIMIT}`;
}

/**
 * Instala el parche una sola vez. Es idempotente para que los refrescos de
 * configuración no acumulen capas de `open`.
 */
export function installLatestLimitHook(): void {
    if (typeof window === 'undefined' || typeof XMLHttpRequest === 'undefined') {
        return;
    }

    const target = window as PatchedWindow;

    if (target[PATCH_FLAG]) {
        return;
    }

    const prototype = XMLHttpRequest.prototype;
    const original = prototype.open;

    prototype.open = function open(
        this: XMLHttpRequest,
        method: string,
        url: string | URL,
        ...rest: unknown[]
    ): void {
        const next = typeof url === 'string' ? rewrite(url) : url;
        return (original as (...args: unknown[]) => void).call(this, method, next, ...rest);
    } as typeof prototype.open;

    target[PATCH_FLAG] = true;
}
