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
    window.AppPathBase = base;

    // Helper centralizado de endpoints para evitar cadenas mágicas en scripts
    window.AppEndpoints = {
        base: base,
        url: function (path) {
            if (!path) return base || '/';
            if (!path.startsWith('/')) path = '/' + path;
            return base ? base + path : path;
        },
        chatbot: {
            cultivos: function () { return window.AppEndpoints.url('/chatbot/api/cultivos'); },
            temasPorCultivo: function (cultivoId) { return window.AppEndpoints.url('/chatbot/api/temas-por-cultivo?cultivoId=' + encodeURIComponent(cultivoId)); },
            contenidos: function (cultivoId, tema) { return window.AppEndpoints.url('/chatbot/api/contenidos?cultivoId=' + encodeURIComponent(cultivoId) + '&tema=' + encodeURIComponent(tema)); },
            recursoArchivo: function (id) { return window.AppEndpoints.url('/chatbot/recursos/' + encodeURIComponent(id) + '/archivo'); },
            lecciones: function () { return window.AppEndpoints.url('/chatbot/lecciones'); },
            leccionDetalle: function (id) { return window.AppEndpoints.url('/chatbot/lecciones/' + encodeURIComponent(id)); },
            preguntaImagen: function (id) { return window.AppEndpoints.url('/chatbot/preguntas/' + encodeURIComponent(id) + '/imagen'); },
            completarLeccion: function (id) { return window.AppEndpoints.url('/chatbot/api/lecciones/' + encodeURIComponent(id) + '/completar'); },
            progresoLecciones: function () { return window.AppEndpoints.url('/chatbot/api/lecciones/progreso'); }
        },
        mapa: {
            lunar: function () { return window.AppEndpoints.url('/api/mapa/lunar'); },
            lunarView: function () { return window.AppEndpoints.url('/mapa/lunar'); },
            directorio: function () { return window.AppEndpoints.url('/mapa/directorio'); },
            mapa: function () { return window.AppEndpoints.url('/mapa/mapa'); }
        },
        admin: {
            contenidoArchivo: function (id) { return window.AppEndpoints.url('/admin/chatbot/contenidos/' + encodeURIComponent(id) + '/archivo'); }
        }
    };

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
