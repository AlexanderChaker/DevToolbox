export function scrollToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

export function isScrolledToBottom(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        const threshold = 30; // pixels from bottom to consider "scrolled to bottom"
        return element.scrollHeight - element.clientHeight - element.scrollTop <= threshold;
    }
    return false;
}

export function initializeScrollTracking(elementId, dotNetRef) {
    const element = document.getElementById(elementId);
    if (element) {
        element.addEventListener('scroll', () => {
            const isAtBottom = element.scrollHeight - element.clientHeight - element.scrollTop <= 30;
            dotNetRef.invokeMethodAsync('OnScrollPositionChanged', isAtBottom);
        });
    }
}
