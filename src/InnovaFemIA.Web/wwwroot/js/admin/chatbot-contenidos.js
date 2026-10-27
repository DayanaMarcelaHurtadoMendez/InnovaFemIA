function abrirModalCrearContenido() {
        document.getElementById('crear-archivo').value = '';
        document.getElementById('crear-url').value = '';
        toggleMutuamenteExcluyente('crear');
        document.getElementById('modal-contenido-form').classList.remove('hidden');
    }
    function cerrarModalContenidoForm() {
        document.getElementById('modal-contenido-form').classList.add('hidden');
    }

    function toggleMutuamenteExcluyente(prefix) {
        const fileInput = document.getElementById(`${prefix}-archivo`);
        const urlInput = document.getElementById(`${prefix}-url`);
        const fileContainer = document.getElementById(`${prefix}-file-container`);
        const urlContainer = document.getElementById(`${prefix}-url-container`);
        const urlMsg = document.getElementById(`${prefix}-url-msg`);
        const fileMsg = document.getElementById(`${prefix}-file-msg`);

        // Si hay archivo cargado
        if (fileInput.files.length > 0) {
            urlInput.disabled = true;
            urlContainer.classList.add('opacity-40');
            urlMsg.classList.remove('hidden');
            urlMsg.innerText = "Deshabilitado porque has seleccionado subir un archivo. Borra el archivo si quieres usar un enlace.";
            urlMsg.classList.add('text-rose-500', 'font-bold');
        } else {
            urlInput.disabled = false;
            urlContainer.classList.remove('opacity-40');
            urlMsg.classList.add('hidden');
            urlMsg.classList.remove('text-rose-500', 'font-bold');
        }

        // Si hay URL escrita (y no hay archivo que ya haya bloqueado la URL)
        if (urlInput.value.trim().length > 0 && fileInput.files.length === 0) {
            fileInput.disabled = true;
            fileContainer.classList.add('opacity-40');
            fileMsg.innerText = "Deshabilitado porque has ingresado un enlace web. Borra el enlace si quieres subir un archivo.";
            fileMsg.classList.add('text-rose-500', 'font-bold');
        } else {
            fileInput.disabled = false;
            fileContainer.classList.remove('opacity-40');
            if (prefix === 'crear') {
                fileMsg.innerText = "El sistema detectará automáticamente el tipo de recurso.";
            } else {
                fileMsg.innerText = "Si subes un archivo, el enlace web será ignorado.";
            }
            fileMsg.classList.remove('text-rose-500', 'font-bold');
        }
    }

    function abrirModalEditarContenido(recurso) {
        document.getElementById('edit-id').value = recurso.Id;
        document.getElementById('edit-cultivoId').value = recurso.CultivoId;
        
        // Mapeo Tema
        let temaVal = 'Siembra';
        if (recurso.Tema === 1 || recurso.Tema === 'Siembra' || recurso.Tema === 'CuandoSembrar') temaVal = 'Siembra';
        else if (recurso.Tema === 2 || recurso.Tema === 'Cosecha' || recurso.Tema === 'CuandoCosechar') temaVal = 'Cosecha';
        else if (recurso.Tema === 3 || recurso.Tema === 'AbonadoNutricion' || recurso.Tema === 'Abonado') temaVal = 'AbonadoNutricion';
        else if (recurso.Tema === 4 || recurso.Tema === 'PlagasEnfermedades') temaVal = 'PlagasEnfermedades';
        else if (recurso.Tema === 5 || recurso.Tema === 'PodasManejo' || recurso.Tema === 'PodaMantenimiento' || recurso.Tema === 'ManejoAgroecologico') temaVal = 'PodasManejo';
        else if (recurso.Tema === 6 || recurso.Tema === 'Riego' || recurso.Tema === 'RiegoDrenaje') temaVal = 'Riego';
        else if (recurso.Tema === 7 || recurso.Tema === 'PostcosechaAlmacenamiento' || recurso.Tema === 'PostcosechaManejo') temaVal = 'PostcosechaAlmacenamiento';
        else if (recurso.Tema === 8 || recurso.Tema === 'Comercializacion' || recurso.Tema === 'ComercializacionCalidad') temaVal = 'Comercializacion';
        else temaVal = recurso.Tema;
        document.getElementById('edit-tema').value = temaVal;

        document.getElementById('edit-titulo').value = recurso.Titulo || '';
        document.getElementById('editar-url').value = recurso.Url || '';
        document.getElementById('edit-orden').value = recurso.Orden || 1;
        document.getElementById('edit-activo').checked = recurso.Activo;
        document.getElementById('edit-nombrefuente').value = recurso.NombreFuente || '';
        document.getElementById('edit-enlacefuente').value = recurso.EnlaceFuente || '';

        document.getElementById('editar-archivo').value = '';
        toggleMutuamenteExcluyente('editar');

        // Mostrar información del contenido actual
        const detalleEl = document.getElementById('edit-info-actual-detalle');
        if (recurso.TieneArchivoEnBd) {
            const tamMb = (recurso.TamanoBytes / (1024 * 1024)).toFixed(1);
            detalleEl.innerHTML = `📦 Archivo almacenado: <span>${recurso.ArchivoNombre || 'recurso'}</span> (${tamMb} MB) — <span class="text-xs text-gray-500 font-normal">Se conservará este archivo a menos que subas uno nuevo a continuación.</span>`;
        } else if (recurso.Url) {
            detalleEl.innerHTML = `🔗 Enlace Web actual: <a href="${recurso.Url}" target="_blank" class="underline">${recurso.Url}</a>`;
        } else {
            detalleEl.innerHTML = `⚠️ Sin archivo ni enlace registrado.`;
        }

        document.getElementById('modal-contenido-editar-form').classList.remove('hidden');
    }

    function cerrarModalEditarContenidoForm() {
        document.getElementById('modal-contenido-editar-form').classList.add('hidden');
    }

    function confirmarEliminarContenido(id, titulo) {
        document.getElementById('contenido-id-eliminar').value = id;
        document.getElementById('contenido-nombre-eliminar').innerText = titulo;
        document.getElementById('modal-eliminar-contenido').classList.remove('hidden');
    }
    function cerrarModalEliminarContenido() {
        document.getElementById('modal-eliminar-contenido').classList.add('hidden');
    }

    function abrirVistaPrevia(recurso) {
        const titleEl = document.getElementById('preview-title');
        const iconEl = document.getElementById('preview-icon');
        const filenameEl = document.getElementById('preview-filename');
        const directLinkEl = document.getElementById('preview-direct-link');
        const container = document.getElementById('preview-content-container');

        titleEl.innerText = recurso.Titulo;
        container.innerHTML = '';

        let fileUrl = '';
        if (recurso.TieneArchivoEnBd) {
            fileUrl = (window.AppEndpoints && window.AppEndpoints.admin)
                ? window.AppEndpoints.admin.contenidoArchivo(recurso.Id)
                : `${(window.__PATH_BASE__ || '').replace(/\/+$/, '')}/admin/chatbot/contenidos/${recurso.Id}/archivo`;
            filenameEl.innerText = `Archivo: ${recurso.ArchivoNombre || 'recurso'} (${(recurso.TamanoBytes / (1024 * 1024)).toFixed(1)} MB)`;
        } else {
            fileUrl = recurso.Url;
            filenameEl.innerText = `Enlace: ${recurso.Url}`;
        }
        directLinkEl.href = fileUrl;

        // Tipo Recurso: 1 = Video, 2 = Infografía, 3 = Documento
        if (recurso.TipoRecurso === 1 || (recurso.ArchivoMimeType && recurso.ArchivoMimeType.includes('video'))) {
            iconEl.innerText = '🎬';
            if (recurso.TieneArchivoEnBd || (recurso.Url && (recurso.Url.endsWith('.mp4') || recurso.Url.endsWith('.webm')))) {
                container.innerHTML = `<video controls autoplay class="max-h-[65vh] w-full max-w-3xl rounded-2xl shadow-2xl bg-black"><source src="${fileUrl}" type="${recurso.ArchivoMimeType || 'video/mp4'}">Tu navegador no soporta reproducción de video.</video>`;
            } else if (recurso.Url && (recurso.Url.includes('youtube.com') || recurso.Url.includes('youtu.be'))) {
                let embedUrl = recurso.Url;
                if (recurso.Url.includes('watch?v=')) {
                    embedUrl = recurso.Url.replace('watch?v=', 'embed/');
                } else if (recurso.Url.includes('youtu.be/')) {
                    embedUrl = recurso.Url.replace('youtu.be/', 'www.youtube.com/embed/');
                }
                container.innerHTML = `<iframe src="${embedUrl}" class="w-full h-[60vh] max-w-3xl rounded-2xl shadow-2xl border-0" allowfullscreen></iframe>`;
            } else {
                container.innerHTML = `<iframe src="${fileUrl}" class="w-full h-[60vh] max-w-3xl rounded-2xl shadow-2xl border-0"></iframe>`;
            }
        } else if (recurso.TipoRecurso === 2 || (recurso.ArchivoMimeType && recurso.ArchivoMimeType.includes('image'))) {
            iconEl.innerText = '🖼️';
            container.innerHTML = `<img src="${fileUrl}" alt="${recurso.Titulo}" class="max-h-[65vh] max-w-full object-contain rounded-2xl shadow-2xl border border-gray-800" />`;
        } else {
            iconEl.innerText = '📄';
            container.innerHTML = `<iframe src="${fileUrl}" class="w-full h-[65vh] max-w-4xl rounded-2xl shadow-2xl border-0 bg-white"></iframe>`;
        }

        document.getElementById('modal-vista-previa').classList.remove('hidden');
    }

    function cerrarVistaPrevia() {
        const container = document.getElementById('preview-content-container');
        container.innerHTML = ''; // Detiene el audio/video al cerrar
        document.getElementById('modal-vista-previa').classList.add('hidden');
    }