/**
 * CMS Admin Tooltips Engine
 * Sistema accesible de tooltips flotantes para el CMS.
 *
 * Mejoras clave frente a la versión anterior:
 *  - La espera de aparición se cancela si el puntero sale antes de mostrarse
 *    (causa principal de los tooltips "pegados").
 *  - Cualquier pulsación, scroll, cambio de foco o eliminación del disparador
 *    oculta el tooltip de inmediato.
 *  - Ya no se interceptan clics: los botones con [data-tooltip] funcionan
 *    normalmente (antes se bloqueaban los formularios de moderación).
 */
(function () {
    'use strict';

    const SHOW_DELAY_MS = 120;
    const HIDE_DELAY_MS = 60;
    const ARROW_SIZE = 8;
    const OFFSET_PX = 8;

    let tooltipEl = null;
    let tooltipInner = null;
    let tooltipArrow = null;
    let currentTrigger = null;
    let showTimeout = null;
    let hideTimeout = null;
    const ariaDescribedByOriginals = new WeakMap();
    let lastPointerDown = 0;

    function getOrCreateTooltip() {
        if (!tooltipEl) {
            tooltipEl = document.createElement('div');
            tooltipEl.id = 'cms-tooltip';
            tooltipEl.className = 'cms-tooltip';
            tooltipEl.setAttribute('role', 'tooltip');
            tooltipEl.setAttribute('aria-hidden', 'true');

            tooltipInner = document.createElement('div');
            tooltipInner.className = 'cms-tooltip__inner';

            tooltipArrow = document.createElement('div');
            tooltipArrow.className = 'cms-tooltip__arrow';

            tooltipEl.appendChild(tooltipInner);
            tooltipEl.appendChild(tooltipArrow);
            document.body.appendChild(tooltipEl);
        }
        return tooltipEl;
    }

    function getTooltipText(element) {
        if (!element || element.nodeType !== Node.ELEMENT_NODE) return null;

        const dataTooltip = element.getAttribute('data-tooltip');
        if (dataTooltip && dataTooltip.trim()) return dataTooltip.trim();

        const cached = element.getAttribute('data-cms-title');
        if (cached && cached.trim()) return cached.trim();

        // Migración perezosa del title nativo: se elimina en cuanto aparece el
        // puntero para que no convivan el tooltip del navegador y el propio.
        const rawTitle = element.getAttribute('title');
        if (rawTitle && rawTitle.trim()) {
            const clean = rawTitle.trim();
            element.setAttribute('data-cms-title', clean);
            element.removeAttribute('title');
            return clean;
        }

        return null;
    }

    function findTrigger(target) {
        if (!target || target.nodeType !== Node.ELEMENT_NODE) return null;
        return target.closest('[data-tooltip], [data-cms-title], [title]');
    }

    function positionTooltip(trigger) {
        const tooltip = getOrCreateTooltip();
        const triggerRect = trigger.getBoundingClientRect();

        tooltip.className = 'cms-tooltip';

        const explicitVariant = trigger.getAttribute('data-tooltip-variant');
        if (explicitVariant) {
            tooltip.classList.add(`cms-tooltip--${explicitVariant}`);
        } else if (trigger.closest('button[type="submit"]') && trigger.closest('form[action*="Delete"]')) {
            tooltip.classList.add('cms-tooltip--danger');
        } else if (
            trigger.classList.contains('cms-tooltip-icon--amber') ||
            trigger.closest('.border-amber-300') ||
            trigger.classList.contains('bg-amber-100') ||
            trigger.classList.contains('bg-amber-200/80') ||
            (trigger.textContent && trigger.textContent.trim() === 'Hero')
        ) {
            tooltip.classList.add('cms-tooltip--hero');
        }

        const tooltipW = tooltip.offsetWidth || 200;
        const tooltipH = tooltip.offsetHeight || 40;
        const arrowOffset = ARROW_SIZE / 2;

        let finalPos = trigger.getAttribute('data-tooltip-position') || 'top';
        const viewportW = window.innerWidth;
        const viewportH = window.innerHeight;

        if (finalPos === 'top' && triggerRect.top - tooltipH - OFFSET_PX < 8) {
            finalPos = 'bottom';
        } else if (finalPos === 'bottom' && triggerRect.bottom + tooltipH + OFFSET_PX > viewportH - 8) {
            finalPos = 'top';
        }

        if (finalPos === 'left' && triggerRect.left - tooltipW - OFFSET_PX < 8) {
            finalPos = 'right';
        } else if (finalPos === 'right' && triggerRect.right + tooltipW + OFFSET_PX > viewportW - 8) {
            finalPos = 'left';
        }

        tooltip.classList.add(`cms-tooltip--${finalPos}`);

        let left = 0;
        let top = 0;
        let arrowLeft = null;
        let arrowTop = null;

        if (finalPos === 'top') {
            top = triggerRect.top - tooltipH - OFFSET_PX;
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipW / 2);
            arrowLeft = (tooltipW / 2) - arrowOffset;
        } else if (finalPos === 'bottom') {
            top = triggerRect.bottom + OFFSET_PX;
            left = triggerRect.left + (triggerRect.width / 2) - (tooltipW / 2);
            arrowLeft = (tooltipW / 2) - arrowOffset;
        } else if (finalPos === 'left') {
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipH / 2);
            left = triggerRect.left - tooltipW - OFFSET_PX;
            arrowTop = (tooltipH / 2) - arrowOffset;
        } else if (finalPos === 'right') {
            top = triggerRect.top + (triggerRect.height / 2) - (tooltipH / 2);
            left = triggerRect.right + OFFSET_PX;
            arrowTop = (tooltipH / 2) - arrowOffset;
        }

        const margin = 8;
        if (left < margin) {
            const shift = margin - left;
            left = margin;
            if (arrowLeft !== null) arrowLeft -= shift;
        } else if (left + tooltipW > viewportW - margin) {
            const shift = (left + tooltipW) - (viewportW - margin);
            left = viewportW - tooltipW - margin;
            if (arrowLeft !== null) arrowLeft += shift;
        }

        if (arrowLeft !== null) {
            arrowLeft = Math.max(8, Math.min(tooltipW - 16, arrowLeft));
            tooltipArrow.style.left = `${Math.round(arrowLeft)}px`;
            tooltipArrow.style.top = '';
        }
        if (arrowTop !== null) {
            arrowTop = Math.max(6, Math.min(tooltipH - 14, arrowTop));
            tooltipArrow.style.top = `${Math.round(arrowTop)}px`;
            tooltipArrow.style.left = '';
        }

        tooltip.style.left = `${Math.round(left)}px`;
        tooltip.style.top = `${Math.round(top)}px`;
    }

    function clearTimers() {
        clearTimeout(showTimeout);
        clearTimeout(hideTimeout);
        showTimeout = null;
        hideTimeout = null;
    }

    function markDescribedBy(trigger) {
        if (ariaDescribedByOriginals.has(trigger)) return;
        const original = trigger.getAttribute('aria-describedby');
        ariaDescribedByOriginals.set(trigger, original);

        const ids = original ? original.split(/\s+/).filter(Boolean) : [];
        if (!ids.includes('cms-tooltip')) {
            ids.push('cms-tooltip');
            trigger.setAttribute('aria-describedby', ids.join(' '));
        }
    }

    function restoreDescribedBy(trigger) {
        if (!ariaDescribedByOriginals.has(trigger)) return;
        const original = ariaDescribedByOriginals.get(trigger);
        ariaDescribedByOriginals.delete(trigger);

        if (original) trigger.setAttribute('aria-describedby', original);
        else trigger.removeAttribute('aria-describedby');
    }

    function isTriggerActive(trigger) {
        if (!trigger || !trigger.isConnected) return false;
        if (trigger.matches(':hover')) return true;
        return !!document.activeElement && trigger.contains(document.activeElement);
    }

    function startShow(trigger) {
        const text = getTooltipText(trigger);
        clearTimers();
        if (!text) return;

        // Si ya hay otro tooltip visible, se oculta antes de programar el nuevo.
        if (currentTrigger && currentTrigger !== trigger) hideTooltip(true);

        showTimeout = setTimeout(() => {
            showTimeout = null;

            // El puntero pudo salir durante la espera: no mostrar nada.
            if (!isTriggerActive(trigger)) return;

            currentTrigger = trigger;
            const tooltip = getOrCreateTooltip();
            tooltipInner.textContent = text;

            tooltip.style.display = 'block';
            tooltip.style.visibility = 'hidden';
            tooltip.style.opacity = '0';
            positionTooltip(trigger);

            requestAnimationFrame(() => {
                if (currentTrigger !== trigger) return;
                tooltip.setAttribute('aria-hidden', 'false');
                tooltip.classList.add('is-visible');
                tooltip.style.visibility = '';
                tooltip.style.opacity = '';

                markDescribedBy(trigger);
            });
        }, SHOW_DELAY_MS);
    }

    function scheduleHide() {
        clearTimeout(showTimeout);
        showTimeout = null;

        clearTimeout(hideTimeout);
        hideTimeout = setTimeout(() => hideTooltip(), HIDE_DELAY_MS);
    }

    function hideTooltip(immediate = false) {
        clearTimers();

        if (currentTrigger) restoreDescribedBy(currentTrigger);
        currentTrigger = null;

        if (!tooltipEl) return;

        tooltipEl.classList.remove('is-visible');
        tooltipEl.setAttribute('aria-hidden', 'true');
        // Se dejan los estilos en manos de la transición CSS (fundido suave).
        tooltipEl.style.opacity = '';
        tooltipEl.style.visibility = '';

        if (immediate) {
            tooltipEl.style.display = 'none';
        }
    }

    function initEvents() {
        document.addEventListener('pointerover', function (e) {
            const trigger = findTrigger(e.target);
            if (!trigger) return;

            const from = e.relatedTarget instanceof Element ? findTrigger(e.relatedTarget) : null;
            if (from === trigger) return;

            startShow(trigger);
        }, { passive: true });

        document.addEventListener('pointerout', function (e) {
            const trigger = findTrigger(e.target);
            if (!trigger) return;

            const to = e.relatedTarget;
            if (to && trigger.contains(to)) return;

            // Evita ocultar el tooltip del disparador que se acaba de abandonar
            // cuando el puntero entra en otro disparador distinto.
            if (!currentTrigger || currentTrigger === trigger) {
                scheduleHide();
            }
        }, { passive: true });

        document.addEventListener('focusin', function (e) {
            // El foco posterior a un clic no debe reabrir el tooltip: el usuario
            // ya está interactuando con el control.
            if (Date.now() - lastPointerDown < 600) return;
            const trigger = findTrigger(e.target);
            if (trigger) startShow(trigger);
        }, { passive: true });

        document.addEventListener('focusout', function (e) {
            const trigger = findTrigger(e.target);
            if (!trigger) return;
            const to = e.relatedTarget;
            if (to && trigger.contains(to)) return;
            if (!currentTrigger || currentTrigger === trigger) scheduleHide();
        }, { passive: true });

        document.addEventListener('pointerdown', function () {
            lastPointerDown = Date.now();
            hideTooltip(true);
        }, { passive: true, capture: true });

        document.addEventListener('click', function () {
            hideTooltip(true);
        }, { passive: true });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') hideTooltip(true);
        }, { passive: true });

        window.addEventListener('scroll', function () {
            hideTooltip(true);
        }, { passive: true, capture: true });

        window.addEventListener('resize', function () {
            hideTooltip(true);
        }, { passive: true });

        window.addEventListener('blur', function () {
            hideTooltip(true);
        });

        // Si el disparador se elimina del DOM (por ejemplo, al re-renderizar
        // bloques del editor), el tooltip no debe quedarse huérfano.
        if (typeof MutationObserver === 'function') {
            const observer = new MutationObserver(function () {
                if (currentTrigger && !currentTrigger.isConnected) {
                    hideTooltip(true);
                    return;
                }
                if (tooltipEl && tooltipEl.classList.contains('is-visible') && currentTrigger) {
                    if (!isTriggerActive(currentTrigger)) scheduleHide();
                }
            });
            observer.observe(document.body, { childList: true, subtree: true });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initEvents);
    } else {
        initEvents();
    }
})();
