import './styles/base.css';
import './styles/home.css';
import './styles/details.css';
import './styles/library.css';

import { ensureBootstrap } from './core/bootstrap';
import { applyDesign } from './core/config';
import { onNavigate } from './core/router';
import { mountDetails } from './details/details';
import { mountHome } from './home/home';
import { installLatestLimitHook } from './home/latestLimit';
import { mountLibrary } from './library/library';
import type { PremiumUiConfig, PremiumUiGlobal } from './types';

// Se instala de inmediato: Jellyfin pide `/Items/Latest` con un límite fijo y
// el parche debe existir antes de que se monte Inicio.
installLatestLimitHook();

const VERSION = '0.30.0';
const LIBRARY_PAGES = ['moviesPage', 'tvshowsPage'];

let current: PremiumUiConfig | null = null;
let booting = false;
let pendingForce = false;
let mountedHome = false;
let mountedDetails = false;
let mountedLibrary = false;

function run(): void {
    if (!current?.enabled || !current.supported) {
        return;
    }

    const home = document.querySelector<HTMLElement>('#indexPage.homePage');
    const details = document.querySelector<HTMLElement>('#itemDetailPage');

    if (home && current.home.enabled) {
        try {
            mountHome(home, current);

            if (!mountedHome) {
                mountedHome = true;
                console.info('[PremiumUI] Inicio mejorado');
            }
        } catch (error) {
            console.error('[PremiumUI] no se pudo montar Inicio', error);
        }
    }

    if (details && current.details.enabled) {
        try {
            mountDetails(details, current);

            if (!mountedDetails) {
                mountedDetails = true;
                console.info('[PremiumUI] Detalles mejorados');
            }
        } catch (error) {
            console.error('[PremiumUI] no se pudo montar Detalles', error);
        }
    }

    for (const id of LIBRARY_PAGES) {
        const library = document.getElementById(id);

        if (!library) {
            continue;
        }

        try {
            mountLibrary(library, current);

            if (!mountedLibrary && current.home.enabled) {
                mountedLibrary = true;
                console.info('[PremiumUI] Bibliotecas mejoradas');
            }
        } catch (error) {
            console.error('[PremiumUI] no se pudo montar la biblioteca', error);
        }
    }
}

async function boot(force = false): Promise<void> {
    if (booting) {
        // Si ya hay una petición en curso, se repite al terminar para que un
        // guardado del panel nunca se pierda en una carrera.
        pendingForce = pendingForce || force;
        return;
    }

    booting = true;

    try {
        const config = await ensureBootstrap(force);

        if (!config) {
            return;
        }

        current = config;

        if (!config.enabled || !config.supported) {
            document.documentElement.dataset.puiState = 'disabled';
            document.documentElement.dataset.puiHome = 'off';
            document.documentElement.dataset.puiDetails = 'off';
            return;
        }

        document.documentElement.dataset.puiState = 'active';
        applyDesign(config);
        run();
        document.dispatchEvent(new CustomEvent('premiumui:ready', { detail: config }));
    } catch (error) {
        console.error('[PremiumUI] error durante el arranque', error);
    } finally {
        booting = false;

        if (pendingForce) {
            pendingForce = false;
            void boot(true);
        }
    }
}

const shared: PremiumUiGlobal = window.PremiumUI ?? {
    version: VERSION,
    config: null,
    ready: false,
    fetchedAt: 0,
    ttlMs: 5 * 60 * 1000,
    refresh: (force?: boolean) => boot(force),
    invalidate() {
        // `fetchedAt = 0` basta: `ensureBootstrap` considera caducada la copia
        // en memoria y la siguiente navegación usa la configuración guardada.
        shared.fetchedAt = 0;
        shared.ready = false;
    }
};

shared.version = VERSION;
shared.refresh = (force?: boolean) => boot(force);
shared.invalidate = () => {
    shared.fetchedAt = 0;
    shared.ready = false;
};
window.PremiumUI = shared;

// Permite que el CSS de los tokens se aplique desde el primer momento.
document.documentElement.dataset.premiumui = 'active';

onNavigate(() => {
    void boot();
});

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => void boot(), { once: true });
} else {
    void boot();
}
