/**
 * InnovaFemIA - Compatibilidad de Sub-Rutas y Proxy Reverso (PathBase)
 * Detecta el prefijo configurado (ej. /innovafemia) y asegura que todas las llamadas
 * fetch() asíncronas utilicen la URL base correcta cuando se despliega detrás de un proxy.
 */
(function () {
    var meta = document.querySelector('meta[name="app-path-base"]');
    var base = meta ? (meta.getAttribute('content') || '').trim() : '';
    if (base && !base.startsWith('/')) {
        base = '/' + base;
    }
    base = base.replace(/\/+$/, '');
    window.__PATH_BASE__ = base;

    if (base) {
        var origFetch = window.fetch;
        window.fetch = function (resource, init) {
            if (typeof resource === 'string') {
                if (resource.startsWith('/') && !resource.startsWith('//') && !resource.startsWith(base + '/') && resource !== base) {
                    resource = base + resource;
                }
            } else if (resource && resource.url) {
                try {
                    var parsed = new URL(resource.url, window.location.origin);
                    if (parsed.origin === window.location.origin && parsed.pathname.startsWith('/') && !parsed.pathname.startsWith(base + '/') && parsed.pathname !== base) {
                        var newUrl = parsed.origin + base + parsed.pathname + parsed.search + parsed.hash;
                        resource = new Request(newUrl, resource);
                    }
                } catch (e) { }
            }
            return origFetch.call(this, resource, init);
        };
    }
})();
