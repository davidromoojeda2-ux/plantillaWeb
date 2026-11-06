/**
 * JavaScript Global del Sitio — Portal Noticias
 * - Inicialización de clase JS en el documento.
 * - Banner de consentimiento de cookies.
 * - Comportamiento del Navbar global (efecto scroll, menú desplegable, drawer móvil).
 */
(function () {
    'use strict';

    // 1. Marca soporte de JavaScript en el elemento raíz
    document.documentElement.classList.add('js');

    // 2. Banner de Cookies
    function initCookieBanner() {
        var banner = document.getElementById('cookieBanner');
        var acceptBtn = document.getElementById('acceptCookiesBtn');

        if (!localStorage.getItem('cookies_accepted')) {
            banner?.classList.remove('hidden');
        }

        acceptBtn?.addEventListener('click', function () {
            localStorage.setItem('cookies_accepted', 'true');
            banner?.classList.add('hidden');
        });
    }

    // 3. Navbar Global
    function initNavbar() {
        var nav = document.querySelector('[data-nav]');
        if (!nav) return;

        var toggle = nav.querySelector('#nav-toggle');
        var dropdowns = Array.prototype.slice.call(nav.querySelectorAll('[data-dropdown]'));
        var SCROLL_THRESHOLD = 8;

        // Estado con sombra al hacer scroll
        var ticking = false;
        function updateScrolled() {
            nav.classList.toggle('is-scrolled', window.scrollY > SCROLL_THRESHOLD);
            ticking = false;
        }

        window.addEventListener('scroll', function () {
            if (!ticking) {
                ticking = true;
                window.requestAnimationFrame(updateScrolled);
            }
        }, { passive: true });
        updateScrolled();

        // Control del menú móvil
        function setMenu(open) {
            nav.classList.toggle('is-open', open);
            if (toggle) {
                toggle.setAttribute('aria-expanded', String(open));
                toggle.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
            }
        }

        if (toggle) {
            toggle.addEventListener('click', function () {
                setMenu(!nav.classList.contains('is-open'));
            });
        }

        // Control de menús desplegables
        function setDropdown(dropdown, open) {
            dropdown.classList.toggle('is-open', open);
            var btn = dropdown.querySelector('[aria-expanded]');
            if (btn) btn.setAttribute('aria-expanded', String(open));
        }

        function closeDropdowns(except) {
            dropdowns.forEach(function (d) {
                if (d !== except) setDropdown(d, false);
            });
        }

        dropdowns.forEach(function (dropdown) {
            var btn = dropdown.querySelector('button');
            if (!btn) return;
            btn.addEventListener('click', function (event) {
                event.stopPropagation();
                var willOpen = !dropdown.classList.contains('is-open');
                closeDropdowns(dropdown);
                setDropdown(dropdown, willOpen);
            });
        });

        // Cierre al hacer clic fuera
        document.addEventListener('click', function (event) {
            if (!nav.contains(event.target)) {
                closeDropdowns();
                setMenu(false);
            } else if (!event.target.closest('[data-dropdown]')) {
                closeDropdowns();
            }
        });

        // Cierre con tecla Escape
        document.addEventListener('keydown', function (event) {
            if (event.key !== 'Escape') return;
            var openDropdown = dropdowns.filter(function (d) { return d.classList.contains('is-open'); })[0];
            if (openDropdown) {
                setDropdown(openDropdown, false);
                var btn = openDropdown.querySelector('button');
                if (btn) btn.focus();
            } else if (nav.classList.contains('is-open')) {
                setMenu(false);
                if (toggle) toggle.focus();
            }
        });

        // Cierre y actualización al pulsar anclas internas
        nav.querySelectorAll('a[href^="#"]').forEach(function (link) {
            link.addEventListener('click', function () {
                closeDropdowns();
                setMenu(false);
                nav.querySelectorAll('.site-nav__link[aria-current]').forEach(function (el) {
                    el.removeAttribute('aria-current');
                });
                link.setAttribute('aria-current', 'page');
            });
        });

        // Reset al cambiar a pantalla de escritorio
        var desktop = window.matchMedia('(min-width: 900px)');
        var onChange = function (mq) { if (mq.matches) setMenu(false); };
        if (desktop.addEventListener) desktop.addEventListener('change', onChange);
        else if (desktop.addListener) desktop.addListener(onChange);
    }

    // 4. Modal público de grupos de WhatsApp
    function initWhatsAppGroupsModal() {
        var modal = document.getElementById('whatsAppGroupsPublicModal');
        var openButtons = Array.prototype.slice.call(document.querySelectorAll('[data-open-whatsapp-groups]'));
        if (!modal || openButtons.length === 0) return;

        var lastTrigger = null;

        function open(event) {
            lastTrigger = event && event.currentTarget ? event.currentTarget : openButtons[0];
            modal.classList.remove('hidden');
            modal.classList.add('flex');
            document.body.classList.add('overflow-hidden');
            var closeBtn = modal.querySelector('[data-close-whatsapp-public]');
            if (closeBtn) closeBtn.focus();
        }

        function close() {
            modal.classList.add('hidden');
            modal.classList.remove('flex');
            document.body.classList.remove('overflow-hidden');
            if (lastTrigger) lastTrigger.focus();
        }

        openButtons.forEach(function (btn) {
            btn.addEventListener('click', open);
        });
        modal.querySelectorAll('[data-close-whatsapp-public]').forEach(function (btn) {
            btn.addEventListener('click', close);
        });
        modal.addEventListener('click', function (event) {
            if (event.target === modal) close();
        });
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && !modal.classList.contains('hidden')) close();
        });
    }

    // 5. Plegar/desplegar el botón flotante de WhatsApp
    function initWhatsAppFloatToggle() {
        var widget = document.getElementById('whatsAppFloat');
        var toggle = document.getElementById('btnToggleWhatsAppFloat');
        var fab = document.getElementById('btnWhatsAppFloat');
        if (!widget || !toggle || !fab) return;

        var storageKey = 'whatsapp_float_hidden';
        var chevron = toggle.querySelector('svg');

        function setCollapsed(collapsed, persist) {
            fab.style.display = collapsed ? 'none' : '';
            toggle.setAttribute('aria-expanded', String(!collapsed));
            toggle.setAttribute('aria-label', collapsed ? 'Mostrar botón de WhatsApp' : 'Ocultar botón de WhatsApp');
            toggle.setAttribute('title', collapsed ? 'Mostrar WhatsApp' : 'Ocultar WhatsApp');
            if (chevron) chevron.classList.toggle('rotate-180', collapsed);

            if (persist) {
                try {
                    if (collapsed) localStorage.setItem(storageKey, 'true');
                    else localStorage.removeItem(storageKey);
                } catch (error) { /* almacenamiento no disponible */ }
            }
        }

        var hidden = false;
        try { hidden = localStorage.getItem(storageKey) === 'true'; } catch (error) { }
        setCollapsed(hidden, false);

        toggle.addEventListener('click', function () {
            setCollapsed(fab.style.display !== 'none', true);
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            initCookieBanner();
            initNavbar();
            initWhatsAppGroupsModal();
            initWhatsAppFloatToggle();
        });
    } else {
        initCookieBanner();
        initNavbar();
        initWhatsAppGroupsModal();
        initWhatsAppFloatToggle();
    }
})();
