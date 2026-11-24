import type { PremiumUiConfig } from '../types';
import { createHero, type HeroController } from './hero';
import { applySectionVisibility, applyRecentLimit, getHomeSections, orderSections } from './sections';
import { applyRecommendations } from './recommendations';
import { applyStudioSections } from './studios';

interface HomeState {
    tab: HTMLElement;
    sections: HTMLElement;
    hero: HeroController;
    observer: MutationObserver;
    timer: number;
    config: PremiumUiConfig;
    suppress: boolean;
}

const states = new WeakMap<HTMLElement, HomeState>();

/**
 * Monta la mejora de Inicio de forma idempotente. El Observer solo reacciona a
 * cambios reales de la pestaña; no hay sondeos ni temporizadores periódicos.
 */
export function mountHome(page: HTMLElement, config: PremiumUiConfig): void {
    if (!config.home.enabled) {
        return;
    }

    const tab = page.querySelector<HTMLElement>('#homeTab');
    const sections = tab?.querySelector<HTMLElement>('.sections');

    if (!tab || !sections) {
        return;
    }

    let state = states.get(page);

    if (!state) {
        const hero = createHero(tab);
        const created: HomeState = {
            tab,
            sections,
            hero,
            observer: new MutationObserver(() => schedule(page)),
            timer: 0,
            config,
            suppress: false
        };

        // Se observa la pestaña completa: Jellyfin puede reemplazar `.sections`
        // o vaciar `#homeTab` al volver de otra vista.
        created.observer.observe(tab, { childList: true, subtree: true });

        page.classList.add('pui-home');
        tab.classList.add('pui-home-tab');
        states.set(page, created);
        state = created;
    } else {
        state.config = config;
    }

    schedule(page);
}

function schedule(page: HTMLElement): void {
    const state = states.get(page);

    if (!state || state.timer) {
        return;
    }

    state.timer = window.setTimeout(() => {
        state.timer = 0;
        refresh(state);
    }, 120);
}

function refresh(state: HomeState): void {
    if (state.suppress) {
        return;
    }

    state.suppress = true;

    try {
        const tab = state.tab;

        // El hero puede haber sido eliminado por un re-render de React.
        if (!tab.contains(state.hero.element)) {
            tab.insertBefore(state.hero.element, tab.firstChild);
        }

        const sections = tab.querySelector<HTMLElement>('.sections');

        if (!sections) {
            return;
        }

        if (sections !== state.sections) {
            state.observer.disconnect();
            state.observer.observe(tab, { childList: true, subtree: true });
            state.sections = sections;
        }

        applySectionVisibility(sections, state.config.home);
        orderSections(sections, state.config.home.sectionOrder);
        applyRecentLimit(sections, state.config.home);
        state.hero.update(getHomeSections(sections), state.config);
        applyStudioSections(sections, state.config.home);
        applyRecommendations(sections, state.config.home);
    } finally {
        // Las mutaciones propias del reordenado se ignoran en la siguiente pasada.
        window.setTimeout(() => {
            state.suppress = false;
        }, 0);
    }
}
