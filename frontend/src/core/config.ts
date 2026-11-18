import type { PremiumUiConfig } from '../types';

const CARD_SCALES: Record<string, number> = {
    small: 0.86,
    medium: 1,
    large: 1.18
};

/**
 * Publica las decisiones de diseño como atributos y variables CSS.
 * No toca la configuración de Jellyfin ni escribe en el DOM de contenido.
 */
export function applyDesign(config: PremiumUiConfig): void {
    const root = document.documentElement;
    const design = config.design;

    root.dataset.puiTheme = design.theme;
    root.dataset.puiCards = design.cardSize;
    root.dataset.puiMotion = shouldReduceMotion(config) ? 'reduce' : 'full';
    root.dataset.puiHome = config.home.enabled ? 'on' : 'off';
    root.dataset.puiDetails = config.details.enabled ? 'on' : 'off';
    root.dataset.puiBackdrop = config.details.backdrop ? 'on' : 'off';
    root.dataset.puiMetadata = config.details.metadata ? 'on' : 'off';
    root.dataset.puiExtra = config.details.extraInfo ? 'on' : 'off';
    root.dataset.puiRecommendations = config.details.recommendations ? 'on' : 'off';
    root.style.setProperty('--pui-hero-height', `${clamp(design.heroHeight, 40, 100)}vh`);
    root.style.setProperty('--pui-transparency', String(clamp(design.transparency, 0, 100) / 100));
    root.style.setProperty('--pui-transition', `${clamp(design.transitionMs, 0, 1000)}ms`);
    root.style.setProperty('--pui-card-scale', String(CARD_SCALES[design.cardSize] ?? 1));
    root.style.setProperty('--pui-hover', String(clamp(design.hoverIntensity ?? 100, 0, 150) / 100));

    applyAccent(root, design.accentColor);
}

/**
 * Aplica el color de acento elegido en el panel. Si está vacío o no es un hex
 * válido, se eliminan los estilos en línea y manda el color del tema.
 */
function applyAccent(root: HTMLElement, value: string | undefined): void {
    const color = parseHex(value);

    if (!color) {
        root.style.removeProperty('--pui-accent');
        root.style.removeProperty('--pui-accent-soft');
        root.style.removeProperty('--pui-accent-contrast');
        return;
    }

    root.style.setProperty('--pui-accent', color.hex);
    root.style.setProperty('--pui-accent-soft', `rgba(${color.r}, ${color.g}, ${color.b}, 0.16)`);
    root.style.setProperty('--pui-accent-contrast', color.contrast);
}

interface AccentColor {
    hex: string;
    r: number;
    g: number;
    b: number;
    contrast: string;
}

function parseHex(value: string | undefined): AccentColor | null {
    const hex = (value ?? '').trim().replace(/^#/, '');

    if (!/^[0-9a-f]{6}$/i.test(hex)) {
        return null;
    }

    const r = parseInt(hex.slice(0, 2), 16);
    const g = parseInt(hex.slice(2, 4), 16);
    const b = parseInt(hex.slice(4, 6), 16);
    const luminance = srgbLuminance(r, g, b);
    const white = 1.05 / (luminance + 0.05);
    const black = (luminance + 0.05) / 0.05;

    return {
        hex: `#${hex.toLowerCase()}`,
        r,
        g,
        b,
        contrast: white > black ? '#ffffff' : '#0b0b0b'
    };
}

function srgbLuminance(r: number, g: number, b: number): number {
    const channel = (value: number): number => {
        const normalized = value / 255;
        return normalized <= 0.03928 ? normalized / 12.92 : ((normalized + 0.055) / 1.055) ** 2.4;
    };

    return 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
}

export function shouldReduceMotion(config: PremiumUiConfig | null): boolean {
    const policy = config?.performance.reducedMotion ?? 'auto';

    if (policy === 'on') {
        return true;
    }

    if (policy === 'off') {
        return false;
    }

    return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}

export function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(Number.isFinite(value) ? value : min, min), max);
}
