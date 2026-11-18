import type { ApiClientLike, PremiumUiGlobal } from './types';

export {};

declare global {
    interface Window {
        PremiumUI?: PremiumUiGlobal;
        ApiClient?: ApiClientLike;
        Dashboard?: {
            navigate(url: string): unknown;
        };
    }
}
