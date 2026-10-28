/**
 * CMS Admin Tooltips Engine
 * Sistema accesible, fluido y universal de tooltips flotantes para el CMS.
 * Soporta [data-tooltip] y reemplaza de forma elegante el atributo [title] nativo del navegador.
 */
(function () {
    'use strict';

    let tooltipEl = null;
    let tooltipInner = null;
    let tooltipArrow = null;
    let currentTrigger = null;
    let showTimeout = null;
    const SHOW_DELAY_MS = 60;
    const ARROW_SIZE = 8;
    const OFFSET_PX = 8;

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

        if (element.hasAttribute('data-tooltip')) {
            const val = element.getAttribute('data-tooltip');
            return val && val.trim() ? val.trim() : null;
        }

        if (element.hasAttribute('data-cms-title')) {
            return element.getAttribute('data-cms-title');
        }

        if (element.hasAttribute('title')) {
            const rawTitle = element.getAttribute('title');
            if (rawTitle && rawTitle.trim()) {
                const clean = rawTitle.trim();
                element.setAttribute('data-cms-title', clean);
                element.removeAttribute('title');
                return clean;
            }
        }

        return null;
    }

    function positionTooltip(trigger) {
        const tooltip = getOrCreateTooltip();
        const triggerRect = trigger.getBoundingClientRect();

        // Limpiar clases de posicionamiento previas manteniendo la base
        tooltip.className = 'cms-tooltip';

        // Aplicar variante si corresponde
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

        // Auto-flip vertical si se saldría de la pantalla
        if (finalPos === 'top' && triggerRect.top - tooltipH - OFFSET_PX < 8) {
            finalPos = 'bottom';
        } else if (finalPos === 'bottom' && triggerRect.bottom + tooltipH + OFFSET_PX > viewportH - 8) {
            finalPos = 'top';
        }

        // Auto-flip horizontal si se saldría de la pantalla
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

        // Limitar dentro de los bordes horizontales de la ventana
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

        // Ajustar flecha para que no desborde la burbuja
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

    function showTooltip(trigger) {
        const text = getTooltipText(trigger);
        if (!text) return;

        clearTimeout(showTimeout);
        showTimeout = setTimeout(() => {
            currentTrigger = trigger;
            const tooltip = getOrCreateTooltip();
            tooltipInner.textContent = text;

            tooltip.style.display = 'block';
            tooltip.style.visibility = 'hidden';
            tooltip.style.opacity = '0';

            positionTooltip(trigger);

            requestAnimationFrame(() => {
                if (currentTrigger === trigger) {
                    tooltip.setAttribute('aria-hidden', 'false');
                    tooltip.classList.add('is-visible');
                    tooltip.style.visibility = 'visible';
                    tooltip.style.opacity = '1';
                }
            });
        }, SHOW_DELAY_MS);
    }

    function hideTooltip() {
        clearTimeout(showTimeout);
        if (!tooltipEl) {
            currentTrigger = null;
            return;
        }
        tooltipEl.classList.remove('is-visible');
        tooltipEl.setAttribute('aria-hidden', 'true');
        tooltipEl.style.opacity = '0';
        tooltipEl.style.visibility = 'hidden';
        currentTrigger = null;
    }

    function initEvents() {
        document.addEventListener('mouseover', function (e) {
            const trigger = e.target.closest('[data-tooltip], [title], [data-cms-title]');
            if (trigger) {
                showTooltip(trigger);
            }
        }, { passive: true });

        document.addEventListener('mouseout', function (e) {
            const trigger = e.target.closest('[data-tooltip], [title], [data-cms-title]');
            if (trigger && trigger === currentTrigger) {
                if (e.relatedTarget && trigger.contains(e.relatedTarget)) {
                    return;
                }
                hideTooltip();
            }
        }, { passive: true });

        document.addEventListener('focusin', function (e) {
            const trigger = e.target.closest('[data-tooltip], [title], [data-cms-title]');
            if (trigger) {
                showTooltip(trigger);
            }
        }, { passive: true });

        document.addEventListener('focusout', function () {
            hideTooltip();
        }, { passive: true });

        document.addEventListener('click', function (e) {
            const trigger = e.target.closest('.cms-tooltip-icon, [data-tooltip]');
            if (trigger && (trigger.classList.contains('cms-tooltip-icon') || trigger.tagName === 'BUTTON')) {
                // Prevenir que un click en el icono active labels externos
                e.preventDefault();
                e.stopPropagation();
                if (currentTrigger === trigger && tooltipEl && tooltipEl.classList.contains('is-visible')) {
                    hideTooltip();
                } else {
                    showTooltip(trigger);
                }
                return;
            }
            hideTooltip();
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') hideTooltip();
        }, { passive: true });

        window.addEventListener('scroll', hideTooltip, { passive: true, capture: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initEvents);
    } else {
        initEvents();
    }
})();
