export function byId<T extends HTMLElement>(id: string, root: ParentNode = document): T | null {
    return root.querySelector<T>('#' + id);
}

export function create<K extends keyof HTMLElementTagNameMap>(
    tag: K,
    className?: string,
    text?: string
): HTMLElementTagNameMap[K] {
    const element = document.createElement(tag);

    if (className) {
        element.className = className;
    }

    if (text !== undefined) {
        element.textContent = text;
    }

    return element;
}

export function empty(element: Element | null): void {
    if (element) {
        element.replaceChildren();
    }
}
