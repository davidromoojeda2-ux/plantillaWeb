(function () {
    'use strict';

    var endpoint = '/admin/changes';
    var known = null;
    var checking = false;

    window.cmsLiveTouch = function () {
        window.cmsLiveLastTouch = Date.now();
    };

    function isCircuitAlive() {
        return Date.now() - (window.cmsLiveLastTouch || 0) < 60000;
    }

    function changed(previous, current) {
        if (!previous) return false;

        return previous.automation !== current.automation
            || previous.articles !== current.articles
            || previous.categories !== current.categories
            || previous.comments !== current.comments;
    }

    async function checkVersions() {
        if (checking || document.visibilityState !== 'visible') return;
        checking = true;

        try {
            var response = await fetch(endpoint, {
                headers: { 'Accept': 'application/json' },
                cache: 'no-store'
            });

            if (!response.ok) return;

            var versions = await response.json();

            if (changed(known, versions) && !isCircuitAlive()) {
                // El circuito no responde: se recarga para reconciliar el estado.
                window.location.reload();
                return;
            }

            known = versions;
        } catch (_) {
            // Sin conexión: se reintenta en el siguiente ciclo.
        } finally {
            checking = false;
        }
    }

    setTimeout(checkVersions, 8000);
    setInterval(checkVersions, 30000);

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') checkVersions();
    });
})();
