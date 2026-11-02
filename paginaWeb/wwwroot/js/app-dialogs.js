(function () {
    'use strict';

    var dialog, backdrop, icon, titleEl, messageEl;
    var inputWrap, inputEl, inputError;
    var cancelBtn, acceptBtn;

    var resolver = null;
    var currentMode = 'confirm';
    var requireValue = true;
    var previousFocus = null;

    var ICONS = {
        primary: '<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>',
        danger: '<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>',
        success: '<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>',
        warning: '<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>',
        prompt: '<svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" aria-hidden="true"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>'
    };

    var ICON_BASE = 'flex h-11 w-11 flex-shrink-0 items-center justify-center rounded-xl text-white';
    var ACCEPT_BASE = 'inline-flex items-center justify-center rounded-xl px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2';

    function cache() {
        dialog = document.getElementById('appDialog');
        if (!dialog) return false;

        backdrop = dialog.querySelector('[data-dialog-backdrop]');
        icon = document.getElementById('appDialogIcon');
        titleEl = document.getElementById('appDialogTitle');
        messageEl = document.getElementById('appDialogMessage');
        inputWrap = document.getElementById('appDialogInputWrap');
        inputEl = document.getElementById('appDialogInput');
        inputError = document.getElementById('appDialogInputError');
        cancelBtn = document.getElementById('appDialogCancel');
        acceptBtn = document.getElementById('appDialogAccept');
        return true;
    }

    function iconClass(type) {
        switch (type) {
            case 'danger': return ICON_BASE + ' bg-red-600';
            case 'success': return ICON_BASE + ' bg-green-600';
            case 'warning': return ICON_BASE + ' bg-amber-500';
            default: return ICON_BASE + ' bg-[#00296B]';
        }
    }

    function acceptClass(type) {
        switch (type) {
            case 'danger': return ACCEPT_BASE + ' bg-red-600 hover:bg-red-700 focus:ring-red-500';
            case 'success': return ACCEPT_BASE + ' bg-green-600 hover:bg-green-700 focus:ring-green-500';
            case 'warning': return ACCEPT_BASE + ' bg-amber-500 hover:bg-amber-600 focus:ring-amber-400';
            default: return ACCEPT_BASE + ' bg-[#00296B] hover:bg-[#003F88] focus:ring-[#00509D]';
        }
    }

    function clearInputError() {
        if (!inputError) return;
        inputError.textContent = '';
        inputError.classList.add('hidden');
    }

    function close(result) {
        if (!dialog) return;

        dialog.classList.add('hidden');
        document.body.classList.remove('overflow-hidden');

        var done = resolver;
        resolver = null;

        if (previousFocus && typeof previousFocus.focus === 'function') {
            try { previousFocus.focus(); } catch (e) { /* ignore */ }
        }
        previousFocus = null;

        if (done) done(result);
    }

    function accept() {
        if (!dialog || dialog.classList.contains('hidden')) return;

        if (currentMode === 'prompt') {
            var value = inputEl.value;
            if (requireValue && !value.trim()) {
                inputError.textContent = 'Escribe un valor para continuar.';
                inputError.classList.remove('hidden');
                inputEl.focus();
                return;
            }
            close(value);
            return;
        }

        close(currentMode === 'alert' ? undefined : true);
    }

    function cancel() {
        if (!dialog || dialog.classList.contains('hidden')) return;
        close(currentMode === 'prompt' ? null : false);
    }

    function open(options) {
        if (!dialog && !cache()) {
            return Promise.resolve(options && options.mode === 'prompt' ? null : false);
        }

        options = options || {};
        if (resolver) {
            var pending = resolver;
            resolver = null;
            pending(options.mode === 'prompt' ? null : false);
        }

        currentMode = options.mode || 'confirm';
        requireValue = options.requireValue !== false;
        var type = options.type || (currentMode === 'prompt' ? 'prompt' : 'primary');

        titleEl.textContent = options.title
            || (currentMode === 'alert' ? 'Aviso' : currentMode === 'prompt' ? 'Introducir valor' : 'Confirmar');
        messageEl.textContent = options.message || '';
        messageEl.classList.toggle('hidden', !options.message);

        acceptBtn.textContent = options.acceptLabel || (currentMode === 'alert' ? 'Entendido' : 'Aceptar');
        cancelBtn.textContent = options.cancelLabel || 'Cancelar';
        cancelBtn.style.display = currentMode === 'alert' ? 'none' : 'inline-flex';

        icon.className = iconClass(type);
        icon.innerHTML = ICONS[type] || ICONS.primary;
        acceptBtn.className = acceptClass(type);

        inputWrap.classList.toggle('hidden', currentMode !== 'prompt');
        if (currentMode === 'prompt') {
            inputEl.value = options.value || '';
            inputEl.placeholder = options.placeholder || '';
            clearInputError();
        }

        previousFocus = document.activeElement;
        dialog.classList.remove('hidden');
        document.body.classList.add('overflow-hidden');

        return new Promise(function (resolve) {
            resolver = resolve;

            window.setTimeout(function () {
                if (dialog.classList.contains('hidden')) return;
                if (currentMode === 'prompt') {
                    inputEl.focus();
                    inputEl.select();
                } else {
                    acceptBtn.focus();
                }
            }, 40);
        });
    }

    window.appConfirm = function (message, options) {
        options = options || {};
        return open({
            mode: 'confirm',
            type: options.type || 'primary',
            title: options.title || 'Confirmar',
            message: message,
            acceptLabel: options.acceptLabel,
            cancelLabel: options.cancelLabel
        });
    };

    window.appAlert = function (message, options) {
        options = options || {};
        return open({
            mode: 'alert',
            type: options.type || 'primary',
            title: options.title || 'Aviso',
            message: message,
            acceptLabel: options.acceptLabel
        });
    };

    window.appPrompt = function (message, defaultValue, options) {
        options = options || {};
        return open({
            mode: 'prompt',
            type: options.type || 'prompt',
            title: options.title || 'Introducir valor',
            message: message,
            value: defaultValue,
            placeholder: options.placeholder,
            acceptLabel: options.acceptLabel || 'Aceptar',
            requireValue: options.requireValue !== false
        });
    };

    document.addEventListener('keydown', function (e) {
        if (!dialog || dialog.classList.contains('hidden')) return;

        if (e.key === 'Escape') {
            e.preventDefault();
            cancel();
        } else if (e.key === 'Enter' && currentMode !== 'prompt') {
            e.preventDefault();
            accept();
        }
    });

    function spinnerHtml() {
        return '<svg class="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path></svg> ';
    }

    // Confirmación global para formularios con data-confirm (incluye estado de carga).
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;

        if (form.dataset.confirmApproved === 'true') {
            delete form.dataset.confirmApproved;
            return;
        }

        var message = form.getAttribute('data-confirm');
        if (!message) return;

        e.preventDefault();

        var submitter = e.submitter || form.querySelector('button[type="submit"]');
        var loading = form.getAttribute('data-loading');

        window.appConfirm(message, {
            type: form.getAttribute('data-confirm-type') || 'primary',
            title: form.getAttribute('data-confirm-title') || 'Confirmar',
            acceptLabel: form.getAttribute('data-confirm-accept') || 'Aceptar'
        }).then(function (ok) {
            if (!ok) return;

            form.dataset.confirmApproved = 'true';

            if (form.requestSubmit) {
                form.requestSubmit(submitter);
            } else {
                form.submit();
            }
            delete form.dataset.confirmApproved;

            if (loading && submitter) {
                submitter.disabled = true;
                submitter.classList.add('opacity-70', 'cursor-not-allowed');
                submitter.innerHTML = spinnerHtml() + loading;
            }
        });
    });

    document.addEventListener('DOMContentLoaded', function () {
        if (!cache()) return;

        backdrop.addEventListener('click', cancel);
        cancelBtn.addEventListener('click', cancel);
        acceptBtn.addEventListener('click', accept);
        inputEl.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                accept();
            }
        });
        inputEl.addEventListener('input', clearInputError);
    });
})();
