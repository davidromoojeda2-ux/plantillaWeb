/**
 * Gestor de consentimiento de cookies de unautonomo.com.
 *
 * Categorías:
 *  - necessary: siempre activas (seguridad, formularios y registro del consentimiento).
 *  - preferences: preferencias de interfaz (por ejemplo, el widget de WhatsApp).
 *  - analytics: medición agregada; desactivada por defecto y sin proveedor activo.
 *
 * La elección se guarda durante 12 meses y puede modificarse desde el pie de página.
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'unautonomo_cookie_consent_v1';
    const LEGACY_KEY = 'cookies_accepted';
    const WHATSAPP_PREF_KEY = 'whatsapp_float_hidden';
    const VERSION = 1;
    const TTL_MS = 365 * 24 * 60 * 60 * 1000;

    const banner = document.getElementById('siteNotice');
    const settings = document.getElementById('sitePreferences');
    const settingsDialog = settings?.querySelector('.site-preferences__dialog');
    const status = document.getElementById('siteNoticeStatus');
    const categoryInputs = Array.from(document.querySelectorAll('[data-preference-category]'));

    let memoryConsent = null;
    let lastFocusedElement = null;
    let previousBodyOverflow = '';
    let keydownHandler = null;
    let showTimer = null;
    let previousHash = '';

    function safeRead() {
        try {
            const raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return null;
            const data = JSON.parse(raw);
            if (!data || data.version !== VERSION) return null;
            if (!data.expiresAt || new Date(data.expiresAt).getTime() < Date.now()) {
                localStorage.removeItem(STORAGE_KEY);
                return null;
            }
            return {
                version: VERSION,
                necessary: true,
                preferences: !!data.preferences,
                analytics: !!data.analytics,
                date: data.date || null,
                expiresAt: data.expiresAt
            };
        } catch (error) {
            return memoryConsent;
        }
    }

    function safeWrite(data) {
        memoryConsent = data;
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
        } catch (error) {
            /* almacenamiento no disponible: se mantiene en memoria durante la sesión */
        }
    }

    function setToggleValues(data) {
        categoryInputs.forEach(input => {
            const category = input.dataset.consentCategory;
            input.checked = category === 'preferences'
                ? !!data?.preferences
                : category === 'analytics' ? !!data?.analytics : false;
        });
    }

    function activateConsentScripts(data) {
        document.querySelectorAll('script[type="text/plain"][data-preference-category]').forEach(script => {
            if (script.dataset.consentLoaded === 'true') return;
            const category = script.dataset.consentCategory;
            const allowed = category === 'necessary' || !!data?.[category];
            if (!allowed) return;

            const active = document.createElement('script');
            for (const attribute of Array.from(script.attributes)) {
                if (['type', 'data-preference-category', 'data-preference-loaded'].includes(attribute.name)) continue;
                active.setAttribute(attribute.name, attribute.value);
            }
            active.text = script.text || '';
            script.dataset.consentLoaded = 'true';
            script.after(active);
        });
    }

    function applyConsent(data) {
        if (!data?.preferences) {
            try { localStorage.removeItem(WHATSAPP_PREF_KEY); } catch (error) { /* noop */ }
        }

        activateConsentScripts(data);
        document.dispatchEvent(new CustomEvent('site-preferences:change', { detail: data }));
    }

    function showBanner() {
        if (!banner) return;
        clearTimeout(showTimer);
        banner.hidden = false;
        document.documentElement.classList.add('site-notice-open');
        showTimer = window.setTimeout(() => banner.classList.add('is-visible'), 30);
    }

    function hideBanner(immediate = false) {
        if (!banner) return;
        clearTimeout(showTimer);
        banner.classList.remove('is-visible');
        document.documentElement.classList.remove('site-notice-open');
        if (immediate) {
            banner.hidden = true;
            return;
        }
        window.setTimeout(() => {
            if (!banner.classList.contains('is-visible')) banner.hidden = true;
        }, 340);
    }

    function setStatus(message) {
        if (!status) return;
        status.textContent = '';
        window.setTimeout(() => { status.textContent = message; }, 10);
    }

    function saveConsent(categories) {
        const data = {
            version: VERSION,
            necessary: true,
            preferences: !!categories?.preferences,
            analytics: !!categories?.analytics,
            date: new Date().toISOString(),
            expiresAt: new Date(Date.now() + TTL_MS).toISOString()
        };

        safeWrite(data);
        setToggleValues(data);
        applyConsent(data);
        if (settings && !settings.hidden) closeSettings(false);
        hideBanner();
        setStatus('Preferencias de cookies guardadas.');

        // Mantiene el foco en un control visible tras cerrar el aviso.
        const visibleTrigger = Array.from(document.querySelectorAll('[data-site-preferences]'))
            .find(trigger => !banner?.contains(trigger));
        visibleTrigger?.focus();

        return data;
    }

    function focusableElements() {
        if (!settingsDialog) return [];
        return Array.from(settingsDialog.querySelectorAll(
            'button:not([disabled]), a[href], input:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter(element => element.offsetParent !== null || element === document.activeElement);
    }

    function wantsSettings() {
        return location.hash === '#preferencias'
            || new URLSearchParams(location.search).get('cookies') === 'config';
    }

    function syncSettingsUrl(open) {
        try {
            const base = location.pathname + location.search;
            if (open) {
                previousHash = location.hash && location.hash !== '#preferencias' ? location.hash : '';
                history.replaceState(null, '', `${base}#preferencias`);
            } else {
                history.replaceState(null, '', `${base}${previousHash || ''}`);
                previousHash = '';
            }
        } catch (error) {
            /* historial no disponible */
        }
    }

    function onDialogKeydown(event) {
        if (event.key === 'Escape') {
            event.preventDefault();
            closeSettings();
            return;
        }
        if (event.key !== 'Tab') return;

        const focusable = focusableElements();
        if (focusable.length === 0) {
            event.preventDefault();
            return;
        }

        const first = focusable[0];
        const last = focusable[focusable.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function openSettings() {
        if (!settings || !settings.hidden) return;

        lastFocusedElement = document.activeElement;
        const data = safeRead();
        setToggleValues(data);

        settings.hidden = false;
        previousBodyOverflow = document.body.style.overflow;
        document.body.style.overflow = 'hidden';
        syncSettingsUrl(true);

        keydownHandler = onDialogKeydown;
        document.addEventListener('keydown', keydownHandler);
        settingsDialog?.focus();
    }

    function closeSettings(restoreFocus = true, syncUrl = true) {
        if (!settings) return;

        settings.hidden = true;
        document.body.style.overflow = previousBodyOverflow;
        if (syncUrl) syncSettingsUrl(false);
        if (keydownHandler) {
            document.removeEventListener('keydown', keydownHandler);
            keydownHandler = null;
        }

        if (restoreFocus && lastFocusedElement && typeof lastFocusedElement.focus === 'function' && lastFocusedElement.isConnected) {
            lastFocusedElement.focus();
        }
        lastFocusedElement = null;
    }

    function currentCategoryValues() {
        const values = { preferences: false, analytics: false };
        categoryInputs.forEach(input => {
            if (input.dataset.consentCategory in values) {
                values[input.dataset.consentCategory] = input.checked;
            }
        });
        return values;
    }

    function bindEvents() {
        document.addEventListener('click', event => {
            const settingsTrigger = event.target.closest('[data-site-preferences]');
            if (settingsTrigger) {
                event.preventDefault();
                openSettings();
                return;
            }

            if (event.target.closest('[data-preferences-accept-all]')) {
                event.preventDefault();
                saveConsent({ preferences: true, analytics: true });
                return;
            }

            if (event.target.closest('[data-preferences-necessary]')) {
                event.preventDefault();
                saveConsent({ preferences: false, analytics: false });
                return;
            }

            if (event.target.closest('[data-preferences-save]')) {
                event.preventDefault();
                saveConsent(currentCategoryValues());
                return;
            }

            if (event.target.closest('[data-preferences-close]')) {
                event.preventDefault();
                closeSettings();
                return;
            }

            if (event.target === settings) {
                closeSettings();
            }
        });
    }

    function init() {
        try { localStorage.removeItem(LEGACY_KEY); } catch (error) { /* noop */ }

        bindEvents();

        const consent = safeRead();
        if (consent) {
            setToggleValues(consent);
            applyConsent(consent);
            hideBanner(true);
        } else {
            showBanner();
        }

        window.addEventListener('hashchange', () => {
            if (wantsSettings()) {
                openSettings();
            } else if (settings && !settings.hidden) {
                closeSettings(true, false);
            }
        });

        if (wantsSettings()) openSettings();
    }

    window.UnacConsent = {
        version: VERSION,
        get: safeRead,
        isAllowed: category => category === 'necessary' || !!(safeRead()?.[category]),
        open: openSettings,
        save: saveConsent
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
