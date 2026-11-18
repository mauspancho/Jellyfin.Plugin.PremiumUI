/**
 * Arte curado de los estudios: logo en WebP y bucle de vídeo MP4.
 *
 * Los archivos viven en `Web/studios` (embebidos en el plugin y servidos bajo
 * `/web/premiumui/studios/<archivo>`), así que no dependen de la imagen que
 * tenga el servidor para el estudio ni de peticiones externas. Si el nombre
 * configurado no coincide con ninguno de los alias, la sección usa el logo
 * nativo de Jellyfin como hasta ahora.
 *
 * Los recursos provienen de Jellyfin-MonWUI-Plugin
 * (https://github.com/G-grbz/Jellyfin-MonWUI-Plugin), GPL-3.0, la misma
 * licencia que este plugin.
 */
export interface StudioArtwork {
    slug: string;
    logo: string;
    video: string | null;
}

interface AssetEntry {
    slug: string;
    aliases: string[];
}

const PUBLIC_DIR = 'web/premiumui/studios/';

const ENTRIES: AssetEntry[] = [
    {
        slug: 'disney',
        aliases: ['disney', 'disneytoon studios', 'disneytoon', 'disney channel', 'disney television animation']
    },
    {
        slug: 'walt-disney-pictures',
        aliases: ['walt disney pictures', 'walt disney animation studios', 'walt disney animation', 'walt disney']
    },
    {
        slug: 'marvel-studios',
        aliases: ['marvel studios', 'marvel', 'marvel entertainment', 'marvel comics', 'marvel television']
    },
    {
        slug: 'dc',
        aliases: ['dc', 'dc comics', 'dc entertainment', 'dc studios']
    },
    {
        slug: 'paramount-pictures',
        aliases: ['paramount pictures', 'paramount', 'paramount players']
    },
    {
        slug: 'netflix',
        aliases: ['netflix', 'netflix studios']
    },
    {
        slug: 'pixar',
        aliases: ['pixar', 'pixar animation studios']
    },
    {
        slug: 'universal',
        aliases: ['universal pictures', 'universal studios', 'universal', 'universal animation studios']
    },
    {
        slug: 'warner-bros-pictures',
        aliases: ['warner bros pictures', 'warner bros', 'warner brothers', 'warner bros entertainment', 'warner']
    },
    {
        slug: 'columbia-pictures',
        aliases: ['columbia pictures', 'columbia', 'sony pictures', 'sony pictures entertainment']
    },
    {
        slug: 'dreamworks-animation',
        aliases: ['dreamworks animation', 'dreamworks', 'dreamworks pictures']
    },
    {
        slug: 'lucasfilm-ltd',
        aliases: ['lucasfilm', 'lucasfilm ltd', 'lucasfilm ltd.']
    }
];

/**
 * Resuelve el arte por los nombres disponibles (el configurado y el nombre
 * real del estudio en el servidor). Gana el primer alias que coincida.
 */
export function resolveStudioArtwork(...names: Array<string | null | undefined>): StudioArtwork | null {
    const normalized = names
        .map((name) => (name ? normalize(name) : ''))
        .filter((name) => name.length > 0);

    if (normalized.length === 0) {
        return null;
    }

    for (const entry of ENTRIES) {
        if (entry.aliases.some((alias) => normalized.includes(normalize(alias)))) {
            return {
                slug: entry.slug,
                logo: assetUrl(`${entry.slug}.webp`),
                video: assetUrl(`${entry.slug}.mp4`)
            };
        }
    }

    return null;
}

function assetUrl(file: string): string {
    const version = window.PremiumUI?.version ?? '0';
    const path = `${PUBLIC_DIR}${file}?v=${encodeURIComponent(version)}`;
    return window.ApiClient?.getUrl(path) ?? `/${path}`;
}

function normalize(value: string): string {
    return value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, ' ')
        .replace(/\s+/g, ' ')
        .trim();
}
