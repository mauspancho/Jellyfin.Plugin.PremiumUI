type Listener = () => void;

const listeners = new Set<Listener>();
let frame = 0;
let patched = false;
let watched = false;

/**
 * Notifica cambios de vista. Jellyfin despacha `viewshow`/`pageshow` sobre el
 * elemento de la página (sin burbujeo), así que se escuchan en fase de captura
 * y además se parchea el historial para cubrir la navegación interna.
 */
export function onNavigate(listener: Listener): () => void {
    listeners.add(listener);
    ensurePatched();
    schedule();
    return () => listeners.delete(listener);
}

function ensurePatched(): void {
    if (!patched) {
        patched = true;

        const push = history.pushState.bind(history);
        const replace = history.replaceState.bind(history);

        history.pushState = function (data: unknown, unused: string, url?: string | URL | null): void {
            push(data, unused, url);
            schedule();
        };

        history.replaceState = function (data: unknown, unused: string, url?: string | URL | null): void {
            replace(data, unused, url);
            schedule();
        };

        for (const eventName of ['popstate', 'hashchange', 'viewshow', 'pageshow', 'viewhide', 'viewbeforehide']) {
            document.addEventListener(eventName, schedule, true);
        }
    }

    if (!watched) {
        watched = true;

        const root = document.querySelector('.mainAnimatedPages') ?? document.body;
        const observer = new MutationObserver(schedule);
        observer.observe(root, { childList: true, subtree: root === document.body });
    }
}

function schedule(): void {
    if (frame) {
        return;
    }

    frame = requestAnimationFrame(() => {
        frame = 0;

        for (const listener of listeners) {
            try {
                listener();
            } catch (error) {
                console.debug('[PremiumUI] error en el listener de navegación', error);
            }
        }

        // Segunda comprobación tras el pintado, por si la vista se monta tarde.
        frame = requestAnimationFrame(() => {
            frame = 0;
            for (const listener of listeners) {
                try {
                    listener();
                } catch {
                    // El error ya se registró en la primera pasada.
                }
            }
        });
    });
}
