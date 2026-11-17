/**
 * Generación automática de slugs para los formularios del CMS.
 *
 * El slug se sincroniza con el título cada vez que el usuario lo modifica.
 * Si el usuario edita manualmente el slug, se respeta hasta el siguiente
 * cambio en el título (momento en el que vuelve a generarse).
 */
(function () {
    'use strict';

    function slugify(value) {
        return String(value || '')
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/-{2,}/g, '-')
            .replace(/^-+|-+$/g, '');
    }

    function init() {
        const title = document.getElementById('titleInput');
        const slug = document.getElementById('slugInput');
        if (!title || !slug) return;

        const resizeTitle = () => {
            if (title.tagName !== 'TEXTAREA') return;
            title.style.height = 'auto';
            if (title.scrollHeight > 0) {
                title.style.height = `${title.scrollHeight}px`;
            }
        };

        const update = () => {
            const next = slugify(title.value);
            if (slug.value !== next) slug.value = next;
            resizeTitle();
        };

        title.addEventListener('input', update);

        if (title.tagName === 'TEXTAREA') {
            title.addEventListener('keydown', event => {
                if (event.key === 'Enter') event.preventDefault();
            });
            window.addEventListener('resize', resizeTitle);
            requestAnimationFrame(resizeTitle);
        }

        // Nunca se sobrescribe un slug ya guardado al cargar la página.
        if (!slug.value.trim()) update();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
