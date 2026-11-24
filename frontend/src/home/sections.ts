import type { HomeConfig } from '../types';
import { classifySection, sectionFlags, type SectionKind } from './classify';

const HIDDEN_CLASS = 'pui-section-hidden';
const TRIMMED_CLASS = 'pui-card-trimmed';
const DEFAULT_RECENT_LIMIT = 16;
const MIN_RECENT_LIMIT = 4;
const MAX_RECENT_LIMIT = 30;

/**
 * Reordena las filas movibles de Inicio según el orden del panel.
 *
 * Solo participan las filas reconocidas y visibles: las ocultas (por Jellyfin
 * o por los checks "Carruseles") y las desconocidas conservan su posición, de
 * modo que un cambio de orden no provoca saltos invisibles. Varias filas del
 * mismo tipo (p. ej. "Continuar viendo" y "A continuación") se mueven juntas y
 * en su orden actual.
 *
 * Jellyfin envuelve algunas filas en `<div class="sectionN">`; el envoltorio
 * es la unidad que se mueve (no la `.verticalSection` interior), porque es el
 * hijo directo de `.sections`.
 */
export function orderSections(container: HTMLElement, order: string[]): void {
    const units = sectionUnits(container);

    if (units.length < 2) {
        return;
    }

    const slots = units.filter((element) => !isFixed(element) && classifySection(element) !== null);

    if (slots.length < 2) {
        return;
    }

    const groups = new Map<SectionKind, HTMLElement[]>();

    for (const element of slots) {
        const kind = classifySection(element) as SectionKind;
        const list = groups.get(kind) ?? [];
        list.push(element);
        groups.set(kind, list);
    }

    const sequence: HTMLElement[] = [];

    for (const key of order) {
        const list = groups.get(key as SectionKind);

        if (list) {
            sequence.push(...list);
            groups.delete(key as SectionKind);
        }
    }

    for (const list of groups.values()) {
        sequence.push(...list);
    }

    if (sequence.length !== slots.length || sequence.every((element, index) => element === slots[index])) {
        return;
    }

    const placeholders = slots.map((element) => {
        const marker = document.createComment('premiumui');
        element.parentElement?.insertBefore(marker, element);
        return marker;
    });

    sequence.forEach((element, index) => {
        placeholders[index].parentElement?.insertBefore(element, placeholders[index]);
    });

    for (const marker of placeholders) {
        marker.remove();
    }
}

/**
 * Unidades reordenables: hijos directos de `.sections` que son una fila con
 * título o que envuelven una `.verticalSection` (caso `<div class="sectionN">`).
 */
function sectionUnits(container: HTMLElement): HTMLElement[] {
    return Array.from(container.children).filter((element): element is HTMLElement => {
        if (!(element instanceof HTMLElement)) {
            return false;
        }

        return element.classList.contains('verticalSection')
            || element.dataset.puiStudio === 'on'
            || element.querySelector('.verticalSection, .sectionTitle') !== null;
    });
}

/**
 * Las filas ocultas y las que el plugin no reconoce no se mueven: si
 * participaran, un cambio de orden podría trasladar una fila invisible y
 * parecer que el panel no hace nada.
 */
function isFixed(element: HTMLElement): boolean {
    const nodes = [element, ...Array.from(element.children).filter(
        (child): child is HTMLElement => child instanceof HTMLElement && child.classList.contains('verticalSection')
    )];

    return nodes.some((node) => node.hidden
        || node.classList.contains('hide')
        || node.classList.contains(HIDDEN_CLASS));
}

export function getHomeSections(container: HTMLElement): HTMLElement[] {
    // Jellyfin envuelve algunas secciones en `<div class="sectionN">`; hay que
    // incluirlas o el hero no encuentra las tarjetas de Películas/Series.
    return Array.from(container.querySelectorAll<HTMLElement>('.verticalSection'))
        .filter((element) => !element.parentElement?.closest('.verticalSection'));
}

/**
 * Muestra u oculta las filas nativas según los checks "Carruseles" del panel.
 * Se usa una clase (no `hidden` ni eliminación del DOM) para que la acción sea
 * reversible y para que el hero siga pudiendo leer las tarjetas ya cargadas.
 */
export function applySectionVisibility(container: HTMLElement, home: HomeConfig): void {
    for (const section of getHomeSections(container)) {
        const flags = sectionFlags(section);
        const hidden = (flags.media && home.mediaLibraries === false)
            || (flags.continue && !home.continueWatching)
            || (flags.recent && !home.recentlyAdded)
            || (flags.movies && !home.movies)
            || (flags.series && !home.series)
            || (flags.recommendations && !home.recommendations);

        section.classList.toggle(HIDDEN_CLASS, hidden);
    }
}

/**
 * Recorta las filas nativas de "Agregado recientemente" al número elegido.
 *
 * Jellyfin pide siempre 16 tarjetas; `latestLimit.ts` eleva la petición a 30 y
 * aquí se oculta el excedente con una clase, sin borrar nodos: el cambio se
 * aplica al instante y es reversible al subir el número desde el panel.
 */
export function applyRecentLimit(container: HTMLElement, home: HomeConfig): void {
    const value = home.recentlyAddedCount;
    const limit = typeof value === 'number' && Number.isFinite(value)
        ? Math.min(MAX_RECENT_LIMIT, Math.max(MIN_RECENT_LIMIT, Math.round(value)))
        : DEFAULT_RECENT_LIMIT;

    for (const section of getHomeSections(container)) {
        if (!sectionFlags(section).recent) {
            continue;
        }

        const slider = section.querySelector<HTMLElement>('.scrollSlider, .itemsContainer');

        if (!slider) {
            continue;
        }

        const direct = Array.from(slider.children).filter(
            (child): child is HTMLElement => child instanceof HTMLElement && child.classList.contains('card')
        );
        const cards = direct.length > 0
            ? direct
            : Array.from(slider.querySelectorAll<HTMLElement>('.card'));

        cards.forEach((card, index) => {
            card.classList.toggle(TRIMMED_CLASS, index >= limit);
        });
    }
}
