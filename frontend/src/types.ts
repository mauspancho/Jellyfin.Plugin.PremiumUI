export interface DesignConfig {
    theme: string;
    transparency: number;
    heroHeight: number;
    cardSize: string;
    transitionMs: number;
    hoverIntensity: number;
    accentColor: string;
    shadowColor: string;
}

export interface StudioCollection {
    key: string;
    name: string;
    studioIds: string[];
    imageItemId?: string | null;
    imageType?: string | null;
}

export interface HomeConfig {
    enabled: boolean;
    heroEnabled: boolean;
    heroSource: string;
    continueWatching: boolean;
    recentlyAdded: boolean;
    movies: boolean;
    series: boolean;
    recommendations: boolean;
    mediaLibraries: boolean;
    sectionOrder: string[];
    itemsPerSection: number;
    recommendationsCount?: number;
    studioSections: string[];
    studioCollections?: StudioCollection[];
}

export interface StudioDto {
    Id: string;
    Name: string;
    ImageTags?: Record<string, string>;
}

export interface DetailsConfig {
    enabled: boolean;
    backdrop: boolean;
    metadata: boolean;
    extraInfo: boolean;
    recommendations: boolean;
}

export interface PerformanceConfig {
    preload: boolean;
    lazyLoading: boolean;
    cacheMinutes: number;
    reducedMotion: string;
}

export interface PremiumUiConfig {
    version: string;
    serverVersion: string;
    supported: boolean;
    enabled: boolean;
    design: DesignConfig;
    home: HomeConfig;
    details: DetailsConfig;
    performance: PerformanceConfig;
}

export interface ApiClientLike {
    getJSON<T>(url: string): Promise<T>;
    getUrl(path: string): string;
    getCurrentUserId?(): string;
    serverId(): string;
}

export interface PremiumUiGlobal {
    version: string;
    config: PremiumUiConfig | null;
    ready: boolean;
    fetchedAt: number;
    ttlMs: number;
    /** Vuelve a pedir el bootstrap (`force` ignora el TTL). */
    refresh: (force?: boolean) => Promise<void>;
    /** Marca la configuración en memoria como caducada para el próximo refresco. */
    invalidate: () => void;
}
