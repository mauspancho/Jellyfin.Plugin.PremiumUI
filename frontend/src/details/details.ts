import type { PremiumUiConfig } from '../types';

/**
 * Activa el estilo de Detalles. Todo el trabajo visual vive en CSS; aquí solo
 * se publican las opciones como atributos de la página.
 */
export function mountDetails(page: HTMLElement, config: PremiumUiConfig): void {
    if (!config.details.enabled) {
        return;
    }

    page.classList.add('pui-details');
    page.dataset.puiBackdrop = config.details.backdrop ? 'on' : 'off';
    page.dataset.puiMetadata = config.details.metadata ? 'on' : 'off';
    page.dataset.puiExtra = config.details.extraInfo ? 'on' : 'off';
    page.dataset.puiRecommendations = config.details.recommendations ? 'on' : 'off';
}
