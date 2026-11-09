(function () {
    'use strict';

    if (window.cmsModals) return;

    window.cmsModals = {
        /**
         * Devuelve el token antiforgery del ámbito indicado (o del documento).
         */
        token: function (scope) {
            var input = (scope || document).querySelector('input[name="__RequestVerificationToken"]');
            return input ? input.value : '';
        },

        /**
         * Escapa texto para insertarlo de forma segura en HTML.
         */
        escapeHtml: function (text) {
            if (text === null || text === undefined) return '';
            return String(text)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#039;');
        },

        /**
         * Muestra una notificación usando el toast global del panel.
         */
        toast: function (message, type) {
            if (typeof window.showToast === 'function') {
                window.showToast(message, type || 'success');
            }
        }
    };
})();
