/**
 * Reveal — efecto de aparición al hacer scroll (equivalente a un hook `useInView`).
 *
 * Uso: añade el atributo `data-reveal` a cualquier elemento.
 *  - Se activa UNA sola vez por elemento (se deja de observar tras mostrarse).
 *  - Los elementos que entran en el viewport en el mismo lote (p. ej. tarjetas de una
 *    misma fila) reciben `--reveal-index` para el retraso escalonado (80ms × índice).
 *  - La animación (opacity + translateY) y prefers-reduced-motion se resuelven en CSS.
 *  - Sin IntersectionObserver, todo se muestra de inmediato (mejora progresiva).
 *
 * API pública mínima: window.Reveal.observe(root?) para contenido inyectado más tarde.
 */
(function () {
    'use strict';

    var SELECTOR = '[data-reveal]';
    var VISIBLE_CLASS = 'is-visible';
    var MAX_STAGGER_STEPS = 6; // evita retrasos excesivos en lotes grandes

    function showAll(elements) {
        elements.forEach(function (el) { el.classList.add(VISIBLE_CLASS); });
    }

    function byDocumentOrder(a, b) {
        return a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING ? -1 : 1;
    }

    var observer = 'IntersectionObserver' in window
        ? new IntersectionObserver(function (entries, obs) {
            var batch = entries
                .filter(function (entry) { return entry.isIntersecting; })
                .map(function (entry) { return entry.target; })
                .sort(byDocumentOrder);

            batch.forEach(function (el, index) {
                el.style.setProperty('--reveal-index', String(Math.min(index, MAX_STAGGER_STEPS)));
                el.classList.add(VISIBLE_CLASS);
                obs.unobserve(el); // una sola vez
            });
        }, { rootMargin: '0px 0px -8% 0px', threshold: 0.12 })
        : null;

    function observe(root) {
        var elements = Array.prototype.slice.call(
            (root || document).querySelectorAll(SELECTOR + ':not(.' + VISIBLE_CLASS + ')'));

        if (!observer) {
            showAll(elements);
            return;
        }
        elements.forEach(function (el) { observer.observe(el); });
    }

    // Limpia el índice tras la transición para que no afecte a futuras transiciones.
    document.addEventListener('transitionend', function (event) {
        var el = event.target;
        if (el instanceof Element && el.matches(SELECTOR + '.' + VISIBLE_CLASS) && event.propertyName === 'opacity') {
            el.style.removeProperty('--reveal-index');
        }
    });

    window.Reveal = { observe: observe };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { observe(); });
    } else {
        observe();
    }
})();
