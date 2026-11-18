import type { PremiumUiConfig } from '../types';

const DEFAULT_TTL_MS = 5 * 60 * 1000;
const MIN_TTL_MS = 30 * 1000;
const MAX_TTL_MS = 60 * 60 * 1000;

/**
 * Obtiene la configuración no administrativa una sola vez por ventana de TTL.
 * Nunca lanza: si falla, la interfaz sigue intacta.
 */
export async function ensureBootstrap(force = false): Promise<PremiumUiConfig | null> {
    const api = window.ApiClient;
    const shared = window.PremiumUI;

    if (!shared) {
        return null;
    }

    if (!api || typeof api.getJSON !== 'function') {
        return shared.config;
    }

    const fresh = shared.config !== null && Date.now() - shared.fetchedAt < shared.ttlMs;
    if (!force && fresh) {
        return shared.config;
    }

    try {
        // El parámetro de versión evita reutilizar una respuesta cacheada por el
        // navegador tras actualizar el plugin.
        const url = `${api.getUrl('PremiumUI/Bootstrap')}?v=${encodeURIComponent(shared.version)}`;
        const config = await api.getJSON<PremiumUiConfig>(url);

        if (!isUsable(config)) {
            console.warn('[PremiumUI] bootstrap con formato inesperado', config);
            return shared.config;
        }

        shared.config = config;
        shared.fetchedAt = Date.now();
        shared.ready = true;
        shared.ttlMs = resolveTtl(config);
        return config;
    } catch (error) {
        console.debug('[PremiumUI] bootstrap no disponible todavía', error);
        return shared.config;
    }
}

function isUsable(config: PremiumUiConfig | null | undefined): config is PremiumUiConfig {
    return Boolean(
        config
        && config.design
        && config.home
        && config.details
        && config.performance
        && Array.isArray(config.home.sectionOrder)
    );
}

function resolveTtl(config: PremiumUiConfig): number {
    const minutes = config.performance?.cacheMinutes ?? 5;
    const ttl = minutes * 60 * 1000;

    try {
        return Math.min(Math.max(ttl, MIN_TTL_MS), MAX_TTL_MS);
    } catch {
        return DEFAULT_TTL_MS;
    }
}
