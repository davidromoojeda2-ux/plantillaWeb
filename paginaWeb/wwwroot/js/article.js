/**
 * Scripts para la Vista Detalle de Artículo
 * - Barra de progreso de lectura interactiva.
 * - Generación dinámica del índice de contenido ("En esta página") con IntersectionObserver.
 * - Copiar enlace con feedback visual tipo Toast.
 */
(function () {
    'use strict';

    // 1. Barra de progreso de lectura
    function initReadingProgress() {
        var bar = document.getElementById('readingProgress');
        if (!bar) return;

        window.addEventListener('scroll', function () {
            var docHeight = document.documentElement.scrollHeight - document.documentElement.clientHeight;
            var scrolled = docHeight > 0 ? (window.scrollY / docHeight) * 100 : 0;
            bar.style.width = scrolled + '%';
        }, { passive: true });
    }

    // 2. Índice Interactivo ("En esta página")
    function initTableOfContents() {
        var body = document.getElementById('articleBody');
        var toc = document.getElementById('tableOfContents');
        if (!body || !toc) return;

        var headings = body.querySelectorAll('h2, h3');
        if (headings.length === 0) {
            toc.innerHTML = '<span class="text-slate-400 italic">Lectura corrida continua</span>';
            return;
        }

        toc.innerHTML = '';
        headings.forEach(function (h, idx) {
            var id = 'section-' + idx;
            h.id = id;

            var link = document.createElement('a');
            link.href = '#' + id;
            link.innerText = h.innerText;
            link.className = 'block py-1 hover:text-[#00296B] transition ' +
                (h.tagName === 'H3' ? 'pl-3 text-slate-500' : 'font-semibold text-slate-700');

            toc.appendChild(link);
        });

        // Resaltar sección activa al hacer scroll
        var allLinks = toc.querySelectorAll('a');
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    allLinks.forEach(function (l) {
                        l.classList.remove('text-[#00296B]', '!font-black');
                    });
                    var activeLink = toc.querySelector('a[href="#' + entry.target.id + '"]');
                    if (activeLink) activeLink.classList.add('text-[#00296B]', '!font-black');
                }
            });
        }, { rootMargin: '-20% 0px -70% 0px' });

        headings.forEach(function (h) { observer.observe(h); });
    }

    // 3. Copiar enlace al portapapeles con Toast
    function initCopyLink() {
        var btn = document.getElementById('copyLinkBtn');
        var toast = document.getElementById('copyToast');
        if (!btn || !toast) return;

        btn.addEventListener('click', async function () {
            var url = window.location.href;
            try {
                await navigator.clipboard.writeText(url);
            } catch (err) {
                var ta = document.createElement('textarea');
                ta.value = url;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.focus();
                ta.select();
                document.execCommand('copy');
                document.body.removeChild(ta);
            }

            toast.classList.remove('opacity-0', 'pointer-events-none');
            toast.classList.add('opacity-100');
            setTimeout(function () {
                toast.classList.remove('opacity-100');
                toast.classList.add('opacity-0', 'pointer-events-none');
            }, 2800);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initReadingProgress();
            initTableOfContents();
            initCopyLink();
        });
    } else {
        initReadingProgress();
        initTableOfContents();
        initCopyLink();
    }
})();
