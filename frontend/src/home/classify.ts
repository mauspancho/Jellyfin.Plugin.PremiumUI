export type SectionKind = 'media' | 'continue' | 'recent' | 'movies' | 'series' | 'recommendations' | 'estudios';

const TITLE_HINTS: Record<SectionKind, string[]> = {
    estudios: [],
    media: [
        'mis medios',
        'mis contenidos',
        'mi media',
        'my media',
        'mes medias',
        'meine medien',
        'minhas midias',
        'i miei media'
    ],
    continue: [
        'continuar viendo',
        'continuar escuchando',
        'continuar leyendo',
        'a continuacion',
        'continue watching',
        'continue listening',
        'continue reading',
        'next up',
        'continuar assistindo',
        'reprendre',
        'weiterschauen',
        'continua a guardare'
    ],
    recent: [
        'recient',
        'latest from',
        'recemment',
        'kurzlich',
        'recente',
        'aggiunti di recente'
    ],
    recommendations: [
        'recomend',
        'recommended',
        'recommand',
        'empfohlen',
        'consigliat',
        'sugerid',
        'porque viste',
        'because you watched'
    ],
    movies: ['pelicula', 'movies', 'filme', 'films', 'film'],
    series: ['series', 'serien', 'serie']
};

export interface SectionFlags {
    media: boolean;
    continue: boolean;
    recent: boolean;
    movies: boolean;
    series: boolean;
    recommendations: boolean;
}

export function normalizeTitle(value: string): string {
    return value
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase()
        .replace(/\s+/g, ' ')
        .trim();
}

/**
 * Indica a qué interruptores del panel responde una fila. Una misma fila puede
 * ser "reciente" y de "Películas" a la vez (por ejemplo, "Agregado
 * recientemente en Películas"), de modo que ambos checks la controlan.
 */
export function sectionFlags(section: HTMLElement): SectionFlags {
    const title = normalizeTitle(section.querySelector('.sectionTitle')?.textContent ?? '');
    const types = new Set<string>();

    for (const card of section.querySelectorAll<HTMLElement>('.card[data-type]')) {
        const type = card.dataset.type;

        if (type) {
            types.add(type);
        }
    }

    const matches = (kind: SectionKind): boolean =>
        title.length > 0 && TITLE_HINTS[kind].some((hint) => title.includes(hint));
    const only = (type: string): boolean => types.size > 0 && [...types].every((value) => value === type);

    return {
        media: matches('media'),
        continue: matches('continue'),
        recent: matches('recent'),
        movies: matches('movies') || only('Movie'),
        series: matches('series') || only('Series'),
        recommendations: matches('recommendations')
    };
}

/**
 * Identifica una sección de Inicio por su título localizado y, como respaldo,
 * por el tipo de sus tarjetas. Nunca asume un idioma concreto.
 */
export function classifySection(section: HTMLElement): SectionKind | null {
    // Las secciones propias del plugin se reconocen por sus marcadores.
    if (section.dataset.puiStudio === 'on') {
        return 'estudios';
    }

    if (section.dataset.puiRecommendations === 'on') {
        return 'recommendations';
    }

    const title = normalizeTitle(section.querySelector('.sectionTitle')?.textContent ?? '');

    if (title.length > 0) {
        for (const kind of Object.keys(TITLE_HINTS) as SectionKind[]) {
            if (TITLE_HINTS[kind].some((hint) => title.includes(hint))) {
                return kind;
            }
        }
    }

    const types = new Set<string>();
    for (const card of section.querySelectorAll<HTMLElement>('.card[data-type]')) {
        const type = card.dataset.type;

        if (type) {
            types.add(type);
        }
    }

    if (types.size > 0 && [...types].every((type) => type === 'Movie')) {
        return 'movies';
    }

    if (types.size > 0 && [...types].every((type) => type === 'Series')) {
        return 'series';
    }

    return null;
}
