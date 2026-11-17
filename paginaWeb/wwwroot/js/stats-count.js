/**
 * Animación de incremento para las métricas del catálogo.
 * El HTML ya contiene el valor final (accesible sin JS); la animación solo
 * se ejecuta la primera vez que la sección entra en pantalla.
 */
(function () {
    'use strict';

    const counters = Array.from(document.querySelectorAll('[data-count-to]'));
    if (counters.length === 0) return;

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const numberFormat = new Intl.NumberFormat('es-ES');
    const DURATION = 1200;

    function easeOutCubic(t) {
        return 1 - Math.pow(1 - t, 3);
    }

    function animate(counter) {
        if (counter.dataset.countDone === 'true') return;
        counter.dataset.countDone = 'true';

        const target = Number(counter.dataset.countTo);
        if (!Number.isFinite(target)) return;

        if (prefersReducedMotion) {
            counter.textContent = numberFormat.format(target);
            return;
        }

        const start = performance.now();
        counter.textContent = numberFormat.format(0);

        function frame(now) {
            const progress = Math.min(1, (now - start) / DURATION);
            const value = Math.round(target * easeOutCubic(progress));
            counter.textContent = numberFormat.format(value);

            if (progress < 1) {
                requestAnimationFrame(frame);
            } else {
                counter.textContent = numberFormat.format(target);
            }
        }

        requestAnimationFrame(frame);
    }

    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver((entries, currentObserver) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                animate(entry.target);
                currentObserver.unobserve(entry.target);
            });
        }, { threshold: 0.35 });

        counters.forEach(counter => observer.observe(counter));
    } else {
        counters.forEach(animate);
    }
})();
