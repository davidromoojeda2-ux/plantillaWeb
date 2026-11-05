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
        // �ndice de escritorio (sidebar) + �ndice plegable para m�viles/tablets.
        var tocs = Array.prototype.filter.call(
            [document.getElementById('tableOfContents'), document.getElementById('tableOfContentsMobile')],
            Boolean);
        if (!body || tocs.length === 0) return;

        var headings = body.querySelectorAll('h2, h3');
        if (headings.length === 0) {
            tocs.forEach(function (toc) {
                toc.innerHTML = '<span class="block px-3 text-blue-100 italic text-sm">Lectura corrida continua</span>';
            });
            var mobileWrapper = document.getElementById('mobileTocWrapper');
            if (mobileWrapper) mobileWrapper.classList.add('hidden');
            return;
        }

        // Clases pensadas para fondo azul marino (#00296B): alto contraste.
        var activeClasses = ['bg-white/10', 'text-[#FDC500]!', 'border-[#FDC500]!'];
        var baseClass = 'block border-l-2 border-transparent rounded-r-md py-2 pr-3 leading-snug transition-colors hover:bg-white/5 hover:text-[#FDC500] focus-visible:outline-2 focus-visible:outline-[#FDC500] ';

        headings.forEach(function (h, idx) {
            // El renderizador de bloques genera ids deterministas; se conservan
            // para que los enlaces profundos y la tabla de contenido coincidan.
            if (!h.id) {
                h.id = 'seccion-' + idx;
            }
        });

        tocs.forEach(function (toc) {
            toc.innerHTML = '';
            headings.forEach(function (h) {
                var link = document.createElement('a');
                link.href = '#' + h.id;
                link.innerText = h.innerText;
                link.className = baseClass +
                    (h.tagName === 'H3' ? 'pl-6 text-sm text-blue-100' : 'pl-3 text-[15px] font-semibold text-white');
                link.addEventListener('click', function () {
                    var details = toc.closest('details');
                    if (details) details.open = false;
                });
                toc.appendChild(link);
            });
        });

        // Resaltar secci�n activa al hacer scroll
        var allLinks = document.querySelectorAll('#tableOfContents a, #tableOfContentsMobile a');
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    allLinks.forEach(function (l) {
                        activeClasses.forEach(function (c) { l.classList.remove(c); });
                        l.removeAttribute('aria-current');
                    });
                    tocs.forEach(function (toc) {
                        var activeLink = toc.querySelector('a[href="#' + CSS.escape(entry.target.id) + '"]');
                        if (activeLink) {
                            activeClasses.forEach(function (c) { activeLink.classList.add(c); });
                            activeLink.setAttribute('aria-current', 'true');
                        }
                    });
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

    // 4. Tablas responsivas automáticas
    function initResponsiveTables() {
        var body = document.getElementById('articleBody');
        if (!body) return;

        var tables = body.querySelectorAll('table');
        tables.forEach(function (table) {
            if (!table.parentElement.classList.contains('table-responsive-wrapper')) {
                var wrapper = document.createElement('div');
                wrapper.className = 'table-responsive-wrapper';
                table.parentNode.insertBefore(wrapper, table);
                wrapper.appendChild(table);
            }
        });
    }

    // 5. Botón de copiado en bloques de código
    function initCodeCopy() {
        var body = document.getElementById('articleBody');
        if (!body) return;

        var pres = body.querySelectorAll('pre');
        pres.forEach(function (pre) {
            var wrapper = pre.closest('.article-code-wrapper');
            var btn = wrapper ? wrapper.querySelector('[data-copy-code]') : null;
            var copyIcon = '<svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z"/></svg><span>Copiar</span>';

            if (!btn) {
                btn = document.createElement('button');
                btn.type = 'button';
                btn.setAttribute('data-copy-code', '');
                btn.className = 'absolute top-2.5 right-2.5 px-2.5 py-1 text-[11px] font-semibold text-slate-300 hover:text-white bg-slate-800/80 hover:bg-slate-700/80 rounded-md border border-slate-700 transition flex items-center gap-1 shadow-sm';
                btn.innerHTML = copyIcon;
                pre.classList.add('relative', 'group');
                pre.appendChild(btn);
            }

            btn.addEventListener('click', async function () {
                var code = pre.querySelector('code')?.innerText || pre.innerText;
                try {
                    await navigator.clipboard.writeText(code);
                    btn.innerHTML = '<svg class="w-3.5 h-3.5 text-emerald-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg><span class="text-emerald-400 font-bold">¡Copiado!</span>';
                    setTimeout(function () {
                        btn.innerHTML = copyIcon;
                    }, 2000);
                } catch (e) {
                    console.error('Error al copiar código', e);
                }
            });
        });
    }

    // 6. Navegación por teclado entre artículos (Flecha izquierda / Flecha derecha)
    function initKeyboardNav() {
        document.addEventListener('keydown', function (e) {
            var tag = (e.target && e.target.tagName) ? e.target.tagName.toUpperCase() : '';
            if (tag === 'INPUT' || tag === 'TEXTAREA' || (e.target && e.target.isContentEditable)) {
                return;
            }
            if (e.key === 'ArrowLeft') {
                var prevLink = document.getElementById('nav-prev-article') || document.getElementById('nav-prev-quick');
                if (prevLink && prevLink.href) {
                    window.location.href = prevLink.href;
                }
            } else if (e.key === 'ArrowRight') {
                var nextLink = document.getElementById('nav-next-article') || document.getElementById('nav-next-quick');
                if (nextLink && nextLink.href) {
                    window.location.href = nextLink.href;
                }
            }
        });
    }

    function initAll() {
        initReadingProgress();
        initTableOfContents();
        initCopyLink();
        initResponsiveTables();
        initCodeCopy();
        initKeyboardNav();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }
})();

