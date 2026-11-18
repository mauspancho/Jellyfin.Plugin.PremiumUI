type Key = 'play' | 'more' | 'previous' | 'next' | 'suggestions' | 'slide' | 'movies' | 'series' | 'recommendations'
    | 'close' | 'loadMore' | 'loading' | 'titles' | 'noTitles' | 'loadError' | 'sortBy' | 'sortTitle' | 'sortYear' | 'sortAdded';

const STRINGS: Record<string, Record<Key, string>> = {
    es: {
        play: 'Reproducir',
        more: 'Más información',
        previous: 'Anterior',
        next: 'Siguiente',
        suggestions: 'Sugerencias',
        slide: 'Sugerencia {n}',
        movies: 'Películas',
        series: 'Series',
        recommendations: 'Recomendaciones',
        close: 'Cerrar',
        loadMore: 'Cargar más',
        loading: 'Cargando…',
        titles: 'títulos',
        noTitles: 'No hay títulos con imagen',
        loadError: 'No se pudieron cargar los títulos',
        sortBy: 'Ordenar por',
        sortTitle: 'Título (A-Z)',
        sortYear: 'Año (nuevos primero)',
        sortAdded: 'Añadidos recientemente'
    },
    en: {
        play: 'Play',
        more: 'More info',
        previous: 'Previous',
        next: 'Next',
        suggestions: 'Suggestions',
        slide: 'Suggestion {n}',
        movies: 'Movies',
        series: 'Series',
        recommendations: 'Recommendations',
        close: 'Close',
        loadMore: 'Load more',
        loading: 'Loading…',
        titles: 'titles',
        noTitles: 'No titles with artwork',
        loadError: 'Could not load titles',
        sortBy: 'Sort by',
        sortTitle: 'Title (A-Z)',
        sortYear: 'Year (newest first)',
        sortAdded: 'Recently added'
    },
    pt: {
        play: 'Reproduzir',
        more: 'Mais informações',
        previous: 'Anterior',
        next: 'Próximo',
        suggestions: 'Sugestões',
        slide: 'Sugestão {n}',
        movies: 'Filmes',
        series: 'Séries',
        recommendations: 'Recomendações',
        close: 'Fechar',
        loadMore: 'Carregar mais',
        loading: 'Carregando…',
        titles: 'títulos',
        noTitles: 'Nenhum título com imagem',
        loadError: 'Não foi possível carregar os títulos',
        sortBy: 'Ordenar por',
        sortTitle: 'Título (A-Z)',
        sortYear: 'Ano (mais novos primeiro)',
        sortAdded: 'Adicionados recentemente'
    },
    fr: {
        play: 'Lire',
        more: "Plus d'infos",
        previous: 'Précédent',
        next: 'Suivant',
        suggestions: 'Suggestions',
        slide: 'Suggestion {n}',
        movies: 'Films',
        series: 'Séries',
        recommendations: 'Recommandations',
        close: 'Fermer',
        loadMore: 'Charger plus',
        loading: 'Chargement…',
        titles: 'titres',
        noTitles: 'Aucun titre avec visuel',
        loadError: 'Impossible de charger les titres',
        sortBy: 'Trier par',
        sortTitle: 'Titre (A-Z)',
        sortYear: 'Année (plus récents)',
        sortAdded: 'Ajouts récents'
    },
    de: {
        play: 'Abspielen',
        more: 'Mehr Infos',
        previous: 'Zurück',
        next: 'Weiter',
        suggestions: 'Vorschläge',
        slide: 'Vorschlag {n}',
        movies: 'Filme',
        series: 'Serien',
        recommendations: 'Empfehlungen',
        close: 'Schließen',
        loadMore: 'Mehr laden',
        loading: 'Wird geladen…',
        titles: 'Titel',
        noTitles: 'Keine Titel mit Bild',
        loadError: 'Titel konnten nicht geladen werden',
        sortBy: 'Sortieren nach',
        sortTitle: 'Titel (A-Z)',
        sortYear: 'Jahr (neueste zuerst)',
        sortAdded: 'Kürzlich hinzugefügt'
    },
    it: {
        play: 'Riproduci',
        more: 'Più informazioni',
        previous: 'Precedente',
        next: 'Successivo',
        suggestions: 'Suggerimenti',
        slide: 'Suggerimento {n}',
        movies: 'Film',
        series: 'Serie',
        recommendations: 'Consigliati',
        close: 'Chiudi',
        loadMore: 'Carica altri',
        loading: 'Caricamento…',
        titles: 'titoli',
        noTitles: 'Nessun titolo con immagine',
        loadError: 'Impossibile caricare i titoli',
        sortBy: 'Ordina per',
        sortTitle: 'Titolo (A-Z)',
        sortYear: 'Anno (prima i più recenti)',
        sortAdded: 'Aggiunti di recente'
    }
};

const FALLBACK = STRINGS.en;

/** Textos mínimos para los controles del hero, sin dependencias externas. */
export function t(key: Key, n?: number): string {
    const languages = navigator.languages?.length ? navigator.languages : [navigator.language];
    const language = (document.documentElement.lang || languages[0] || 'en').slice(0, 2).toLowerCase();
    const table = STRINGS[language] ?? FALLBACK;

    // `FALLBACK` no es undefined porque la tabla de inglés siempre existe.
    const text = (table ?? FALLBACK)[key];
    return n === undefined ? text : text.replace('{n}', String(n));
}
