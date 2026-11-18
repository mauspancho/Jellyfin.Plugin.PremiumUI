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
    root.dataset.puiLazy = config.performance.lazyLoading === false ? 'off' : 'on';

    applyAccent(root, design.accentColor);
    applyShadowColor(root, design.shadowColor);
    applyBackgroundColor(
        root,
        design.backgroundColor,
        design.backgroundGradient === true,
        design.backgroundGradientDirection
    );
}

/**
 * Aplica el color de acento elegido en el panel. Si está vacío o no es un hex
 * válido, se eliminan los estilos en línea y manda el color del tema.
 */
function applyAccent(root: HTMLElement, value: string | undefined): void {
    const color = parseHex(value);

    if (!color) {
        root.style.removeProperty('--pui-accent');
        root.style.removeProperty('--pui-accent-rgb');
        root.style.removeProperty('--pui-accent-soft');
        root.style.removeProperty('--pui-accent-contrast');
        root.style.removeProperty('--pui-glow');
        return;
    }

    root.style.setProperty('--pui-accent', color.hex);
    root.style.setProperty('--pui-accent-rgb', `${color.r}, ${color.g}, ${color.b}`);
    root.style.setProperty('--pui-accent-soft', `rgba(${color.r}, ${color.g}, ${color.b}, 0.16)`);
    root.style.setProperty('--pui-accent-contrast', color.contrast);
    // El brillo del hero y de los halos sigue al acento personalizado.
    root.style.setProperty('--pui-glow', `rgba(${color.r}, ${color.g}, ${color.b}, 0.22)`);
}

/**
 * Aplica el color de la sombra de las tarjetas elegido en el panel. Si está
 * vacío o no es un hex válido, se elimina el estilo en línea y manda el color
 * del tema (negro en todos los temas actuales).
 */
function applyShadowColor(root: HTMLElement, value: string | undefined): void {
    const color = parseHex(value);

    if (!color) {
        root.style.removeProperty('--pui-card-shadow-rgb');
        return;
    }

    root.style.setProperty('--pui-card-shadow-rgb', `${color.r}, ${color.g}, ${color.b}`);
}

/**
 * Aplica el color de fondo elegido en el panel. Si está vacío o no es un hex
 * válido, se elimina el estilo en línea y manda el fondo del tema de Jellyfin.
 * El contraste del texto se calcula igual que en el acento para que un fondo
 * claro no deje el texto nativo blanco sobre blanco.
 *
 * Con el degradado activo se publica `--pui-app-bg-image`: parte del color
 * elegido y termina en una variante mezclada con el color de contraste (más
 * clara sobre fondos oscuros, más oscura sobre fondos claros), así el fondo
 * sigue siendo legible y el texto conserva el contraste del color base.
 */
function applyBackgroundColor(
    root: HTMLElement,
    value: string | undefined,
    gradient: boolean,
    direction: string | undefined
): void {
    const color = parseHex(value);

    if (!color) {
        root.style.removeProperty('--pui-app-bg');
        root.style.removeProperty('--pui-app-fg');
        root.style.removeProperty('--pui-app-bg-image');
        root.dataset.puiBg = 'off';
        root.dataset.puiBgGradient = 'off';
        return;
    }

    root.style.setProperty('--pui-app-bg', color.hex);
    root.style.setProperty('--pui-app-fg', color.contrast);
    root.dataset.puiBg = 'on';

    if (!gradient) {
        root.style.removeProperty('--pui-app-bg-image');
        root.dataset.puiBgGradient = 'off';
        return;
    }

    const shade = mixHex(color, color.contrast === '#ffffff' ? WHITE : NEAR_BLACK, 0.18);
    root.style.setProperty('--pui-app-bg-image', gradientImage(direction, shade, color.hex));
    root.dataset.puiBgGradient = 'on';
}

const WHITE = { r: 255, g: 255, b: 255 };
const NEAR_BLACK = { r: 11, g: 11, b: 11 };

const LINEAR_GRADIENTS: Record<string, string> = {
    bottom: 'to bottom',
    top: 'to top',
    right: 'to right',
    left: 'to left',
    'bottom-right': 'to bottom right',
    'bottom-left': 'to bottom left',
    'top-right': 'to top right',
    'top-left': 'to top left'
};

const RADIAL_GRADIENTS: Record<string, string> = {
    radial: '50% 50%',
    'radial-top': '50% 0%',
    'radial-bottom': '50% 100%'
};

/**
 * Construye la imagen CSS del fondo a partir de la dirección elegida. Las
 * direcciones lineales usan las palabras clave de CSS y las radiales colocan
 * el foco en el centro, el borde superior o el inferior. Cualquier valor
 * desconocido cae en el degradado vertical.
 */
function gradientImage(direction: string | undefined, shade: string, base: string): string {
    const key = (direction ?? '').trim().toLowerCase();
    const radial = RADIAL_GRADIENTS[key];

    if (radial) {
        return `radial-gradient(circle at ${radial}, ${shade}, ${base})`;
    }

    return `linear-gradient(${LINEAR_GRADIENTS[key] ?? LINEAR_GRADIENTS.bottom}, ${shade}, ${base})`;
}

function mixHex(color: AccentColor, target: Rgb, ratio: number): string {
    const mix = (from: number, to: number): number => Math.round(from + (to - from) * ratio);
    const channel = (value: number): string => value.toString(16).padStart(2, '0');

    return `#${channel(mix(color.r, target.r))}${channel(mix(color.g, target.g))}${channel(mix(color.b, target.b))}`;
}

interface AccentColor {
    hex: string;
    r: number;
    g: number;
    b: number;
    contrast: string;
}

interface Rgb {
    r: number;
    g: number;
    b: number;
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


/**
 * La opción "Carga diferida" del panel decide si las imágenes de estudio se
 * piden al entrar en pantalla o de inmediato.
 */
export function lazyImagesEnabled(): boolean {
    return document.documentElement.dataset.puiLazy !== 'off';
}
