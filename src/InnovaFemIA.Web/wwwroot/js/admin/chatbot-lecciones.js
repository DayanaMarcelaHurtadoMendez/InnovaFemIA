// ─── Despliegue de Acordeón ──────────────────────────────────────────────
function togglePreguntas(leccionId) {
    const container = document.getElementById(`preguntas-container-${leccionId}`);
    const icon = document.getElementById(`toggle-icon-${leccionId}`);
    if (container.classList.contains('hidden')) {
        container.classList.remove('hidden');
        icon.innerText = '▲';
    } else {
        container.classList.add('hidden');
        icon.innerText = '▼';
    }
}

// ─── Modales de Crear / Editar Lección ───────────────────────────────────
function abrirModalCrearLeccion(siguienteNumero) {
    document.getElementById('input-numero-leccion').value = siguienteNumero || 1;
    document.getElementById('modal-crear-leccion').classList.remove('hidden');
}
function cerrarModalCrearLeccion() {
    document.getElementById('modal-crear-leccion').classList.add('hidden');
}

function abrirModalEditarLeccion(id, numeroLeccion, titulo, descripcion) {
    document.getElementById('edit-leccion-id').value = id;
    document.getElementById('edit-leccion-numero').value = numeroLeccion;
    document.getElementById('edit-leccion-titulo').value = titulo;
    document.getElementById('edit-leccion-descripcion').value = descripcion || '';
    document.getElementById('modal-editar-leccion').classList.remove('hidden');
}
function cerrarModalEditarLeccion() {
    document.getElementById('modal-editar-leccion').classList.add('hidden');
}

function confirmarEliminarLeccion(id, titulo) {
    document.getElementById('leccion-id-eliminar').value = id;
    document.getElementById('leccion-nombre-eliminar').innerText = titulo;
    document.getElementById('modal-eliminar-leccion').classList.remove('hidden');
}
function cerrarModalEliminarLeccion() {
    document.getElementById('modal-eliminar-leccion').classList.add('hidden');
}

function confirmarEliminarPregunta(id, texto) {
    document.getElementById('pregunta-id-eliminar').value = id;
    document.getElementById('pregunta-nombre-eliminar').innerText = texto;
    document.getElementById('modal-eliminar-pregunta').classList.remove('hidden');
}
function cerrarModalEliminarPregunta() {
    document.getElementById('modal-eliminar-pregunta').classList.add('hidden');
}

// ─── Generador Dinámico de Opciones: AGREGAR PREGUNTA ───────────────────
let contadorOpciones = 0;

function abrirModalAgregarPregunta(leccionId, tituloLeccion, ordenSugerido) {
    document.getElementById('pregunta-leccion-id').value = leccionId;
    document.getElementById('label-leccion-destino').innerText = `Lección: ${tituloLeccion}`;
    document.getElementById('input-orden-pregunta').value = ordenSugerido || 1;
    document.getElementById('input-texto-pregunta').value = '';
    document.getElementById('banner-error-pregunta').classList.add('hidden');

    // Limpiar y crear 3 opciones por defecto
    const contenedor = document.getElementById('contenedor-opciones-dinamicas');
    contenedor.innerHTML = '';
    contadorOpciones = 0;

    agregarNuevaOpcionUI('', true, '');  // Opción 1 (correcta por defecto)
    agregarNuevaOpcionUI('', false, ''); // Opción 2
    agregarNuevaOpcionUI('', false, ''); // Opción 3

    document.getElementById('modal-agregar-pregunta').classList.remove('hidden');
}

function cerrarModalAgregarPregunta() {
    document.getElementById('modal-agregar-pregunta').classList.add('hidden');
}

function agregarNuevaOpcionUI(texto = '', esCorrecta = false, retro = '') {
    contadorOpciones++;
    const index = contadorOpciones - 1;
    const contenedor = document.getElementById('contenedor-opciones-dinamicas');

    const card = document.createElement('div');
    card.id = `opcion-card-dinamica-${index}`;
    card.className = `p-4 rounded-2xl border transition-all space-y-2 ${esCorrecta ? 'bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40' : 'bg-gray-50 border-gray-200'}`;

    card.innerHTML = `
        <div class="flex items-center justify-between gap-2">
            <label class="flex items-center gap-2 cursor-pointer">
                <input type="radio" name="opcionCorrectaIndex" value="${index}" ${esCorrecta ? 'checked' : ''} onchange="actualizarEstiloOpcionCorrecta()" class="w-4 h-4 text-emerald-600 focus:ring-emerald-500 cursor-pointer" />
                <span class="text-xs font-black ${esCorrecta ? 'text-verde' : 'text-gray-700'}">Opción #${contadorOpciones} ${esCorrecta ? '(Respuesta Correcta ✓)' : ''}</span>
            </label>
            <button type="button" onclick="eliminarOpcionUI(${index})" class="text-[11px] text-rose-600 hover:text-rose-800 font-bold px-2 py-0.5 rounded-lg hover:bg-rose-50 transition-colors">
                ✕ Quitar
            </button>
        </div>
        <input type="text" name="opcionTexto" required value="${escaparHtml(texto)}" placeholder="Texto de la opción #${contadorOpciones}..." class="w-full px-3 py-2 rounded-xl border border-gray-200 bg-white text-xs font-medium focus:ring-2 focus:ring-emerald-500 outline-none" />
        <input type="text" name="opcionRetro" value="${escaparHtml(retro)}" placeholder="Retroalimentación didáctica al seleccionar esta opción (ej: ¡Excelente! o Recuerda que...)..." class="w-full px-3 py-1.5 rounded-xl border border-gray-200 bg-white text-[11px] text-gray-600 focus:ring-2 focus:ring-emerald-500 outline-none" />
    `;

    contenedor.appendChild(card);
    actualizarEstiloOpcionCorrecta();
}

function eliminarOpcionUI(index) {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas');
    const cards = contenedor.children;

    if (cards.length <= 2) {
        alert('Cada pregunta debe tener al menos 2 opciones de respuesta.');
        return;
    }

    const el = document.getElementById(`opcion-card-dinamica-${index}`);
    if (el) el.remove();
    renumerarOpciones();
}

function renumerarOpciones() {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas');
    const cards = contenedor.children;
    contadorOpciones = 0;
    let huboChecked = false;

    Array.from(cards).forEach((card, idx) => {
        contadorOpciones = idx + 1;
        card.id = `opcion-card-dinamica-${idx}`;

        const radio = card.querySelector('input[type="radio"]');
        const labelSpan = card.querySelector('label span');
        const deleteBtn = card.querySelector('button');

        radio.value = idx;
        deleteBtn.setAttribute('onclick', `eliminarOpcionUI(${idx})`);

        if (radio.checked) {
            huboChecked = true;
            labelSpan.innerText = `Opción #${contadorOpciones} (Respuesta Correcta ✓)`;
            labelSpan.className = 'text-xs font-black text-verde';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40';
        } else {
            labelSpan.innerText = `Opción #${contadorOpciones}`;
            labelSpan.className = 'text-xs font-black text-gray-700';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-gray-50 border-gray-200';
        }
    });

    if (!huboChecked && cards.length > 0) {
        const firstRadio = cards[0].querySelector('input[type="radio"]');
        if (firstRadio) {
            firstRadio.checked = true;
            actualizarEstiloOpcionCorrecta();
        }
    }
}

function actualizarEstiloOpcionCorrecta() {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas');
    const cards = contenedor.children;

    Array.from(cards).forEach((card, idx) => {
        const radio = card.querySelector('input[type="radio"]');
        const labelSpan = card.querySelector('label span');
        const num = idx + 1;

        if (radio && radio.checked) {
            labelSpan.innerText = `Opción #${num} (Respuesta Correcta ✓)`;
            labelSpan.className = 'text-xs font-black text-verde';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40';
        } else if (labelSpan) {
            labelSpan.innerText = `Opción #${num}`;
            labelSpan.className = 'text-xs font-black text-gray-700';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-gray-50 border-gray-200';
        }
    });
}

function validarFormularioPregunta(e) {
    const banner = document.getElementById('banner-error-pregunta');
    banner.classList.add('hidden');

    const contenedor = document.getElementById('contenedor-opciones-dinamicas');
    const cards = contenedor.children;

    if (cards.length < 2) {
        e.preventDefault();
        banner.innerText = 'Debes agregar al menos 2 opciones de respuesta.';
        banner.classList.remove('hidden');
        return false;
    }

    let radioChecked = false;
    let opcionesVacias = false;

    Array.from(cards).forEach(card => {
        const radio = card.querySelector('input[type="radio"]');
        if (radio && radio.checked) radioChecked = true;

        const textInput = card.querySelector('input[name="opcionTexto"]');
        if (!textInput || !textInput.value.trim()) opcionesVacias = true;
    });

    if (!radioChecked) {
        e.preventDefault();
        banner.innerText = 'Debes marcar una opción como respuesta correcta.';
        banner.classList.remove('hidden');
        return false;
    }

    if (opcionesVacias) {
        e.preventDefault();
        banner.innerText = 'Todas las opciones agregadas deben tener texto.';
        banner.classList.remove('hidden');
        return false;
    }

    return true;
}

// ─── Generador Dinámico de Opciones: EDITAR PREGUNTA ────────────────────
let contadorOpcionesEdit = 0;

function cargarYEditarPregunta(preguntaId) {
    const btn = document.getElementById(`btn-data-pregunta-${preguntaId}`);
    if (!btn) return;
    try {
        const data = JSON.parse(btn.dataset.pregunta);
        abrirModalEditarPregunta(data);
    } catch (err) {
        console.error('Error al parsear datos de pregunta', err);
    }
}

function abrirModalEditarPregunta(p) {
    document.getElementById('edit-pregunta-id').value = p.Id || p.id;
    document.getElementById('edit-pregunta-texto').value = p.TextoPregunta || p.textoPregunta || '';
    document.getElementById('edit-pregunta-orden').value = p.Orden || p.orden || 1;
    document.getElementById('edit-pregunta-explicacion').value = p.ContenidoExplicativo || p.contenidoExplicativo || '';
    document.getElementById('banner-error-pregunta-edit').classList.add('hidden');

    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');
    contenedor.innerHTML = '';
    contadorOpcionesEdit = 0;

    const opciones = p.Opciones || p.opciones || [];
    if (opciones.length > 0) {
        opciones.forEach(op => {
            const txt = op.TextoOpcion || op.textoOpcion || '';
            const esCorr = (op.EsCorrecta !== undefined) ? op.EsCorrecta : (op.esCorrecta || false);
            const retro = op.Retroalimentacion || op.retroalimentacion || '';
            agregarNuevaOpcionEditUI(txt, esCorr, retro);
        });
    } else {
        agregarNuevaOpcionEditUI('', true, '');
        agregarNuevaOpcionEditUI('', false, '');
    }

    document.getElementById('modal-editar-pregunta').classList.remove('hidden');
}

function cerrarModalEditarPregunta() {
    document.getElementById('modal-editar-pregunta').classList.add('hidden');
}

function agregarNuevaOpcionEditUI(texto = '', esCorrecta = false, retro = '') {
    contadorOpcionesEdit++;
    const index = contadorOpcionesEdit - 1;
    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');

    const card = document.createElement('div');
    card.id = `opcion-card-dinamica-edit-${index}`;
    card.className = `p-4 rounded-2xl border transition-all space-y-2 ${esCorrecta ? 'bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40' : 'bg-gray-50 border-gray-200'}`;

    card.innerHTML = `
        <div class="flex items-center justify-between gap-2">
            <label class="flex items-center gap-2 cursor-pointer">
                <input type="radio" name="opcionCorrectaIndex" value="${index}" ${esCorrecta ? 'checked' : ''} onchange="actualizarEstiloOpcionCorrectaEdit()" class="w-4 h-4 text-emerald-600 focus:ring-emerald-500 cursor-pointer" />
                <span class="text-xs font-black ${esCorrecta ? 'text-verde' : 'text-gray-700'}">Opción #${contadorOpcionesEdit} ${esCorrecta ? '(Respuesta Correcta ✓)' : ''}</span>
            </label>
            <button type="button" onclick="eliminarOpcionEditUI(${index})" class="text-[11px] text-rose-600 hover:text-rose-800 font-bold px-2 py-0.5 rounded-lg hover:bg-rose-50 transition-colors">
                ✕ Quitar
            </button>
        </div>
        <input type="text" name="opcionTexto" required value="${escaparHtml(texto)}" placeholder="Texto de la opción #${contadorOpcionesEdit}..." class="w-full px-3 py-2 rounded-xl border border-gray-200 bg-white text-xs font-medium focus:ring-2 focus:ring-amber-500 outline-none" />
        <input type="text" name="opcionRetro" value="${escaparHtml(retro)}" placeholder="Retroalimentación didáctica al seleccionar esta opción..." class="w-full px-3 py-1.5 rounded-xl border border-gray-200 bg-white text-[11px] text-gray-600 focus:ring-2 focus:ring-amber-500 outline-none" />
    `;

    contenedor.appendChild(card);
    actualizarEstiloOpcionCorrectaEdit();
}

function eliminarOpcionEditUI(index) {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');
    const cards = contenedor.children;

    if (cards.length <= 2) {
        alert('Cada pregunta debe tener al menos 2 opciones de respuesta.');
        return;
    }

    const el = document.getElementById(`opcion-card-dinamica-edit-${index}`);
    if (el) el.remove();
    renumerarOpcionesEdit();
}

function renumerarOpcionesEdit() {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');
    const cards = contenedor.children;
    contadorOpcionesEdit = 0;
    let huboChecked = false;

    Array.from(cards).forEach((card, idx) => {
        contadorOpcionesEdit = idx + 1;
        card.id = `opcion-card-dinamica-edit-${idx}`;

        const radio = card.querySelector('input[type="radio"]');
        const labelSpan = card.querySelector('label span');
        const deleteBtn = card.querySelector('button');

        radio.value = idx;
        deleteBtn.setAttribute('onclick', `eliminarOpcionEditUI(${idx})`);

        if (radio.checked) {
            huboChecked = true;
            labelSpan.innerText = `Opción #${contadorOpcionesEdit} (Respuesta Correcta ✓)`;
            labelSpan.className = 'text-xs font-black text-verde';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40';
        } else {
            labelSpan.innerText = `Opción #${contadorOpcionesEdit}`;
            labelSpan.className = 'text-xs font-black text-gray-700';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-gray-50 border-gray-200';
        }
    });

    if (!huboChecked && cards.length > 0) {
        const firstRadio = cards[0].querySelector('input[type="radio"]');
        if (firstRadio) {
            firstRadio.checked = true;
            actualizarEstiloOpcionCorrectaEdit();
        }
    }
}

function actualizarEstiloOpcionCorrectaEdit() {
    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');
    const cards = contenedor.children;

    Array.from(cards).forEach((card, idx) => {
        const radio = card.querySelector('input[type="radio"]');
        const labelSpan = card.querySelector('label span');
        const num = idx + 1;

        if (radio && radio.checked) {
            labelSpan.innerText = `Opción #${num} (Respuesta Correcta ✓)`;
            labelSpan.className = 'text-xs font-black text-verde';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-emerald-50/70 border-emerald-300 ring-1 ring-emerald-400/40';
        } else if (labelSpan) {
            labelSpan.innerText = `Opción #${num}`;
            labelSpan.className = 'text-xs font-black text-gray-700';
            card.className = 'p-4 rounded-2xl border transition-all space-y-2 bg-gray-50 border-gray-200';
        }
    });
}

function validarFormularioPreguntaEdit(e) {
    const banner = document.getElementById('banner-error-pregunta-edit');
    banner.classList.add('hidden');

    const contenedor = document.getElementById('contenedor-opciones-dinamicas-edit');
    const cards = contenedor.children;

    if (cards.length < 2) {
        e.preventDefault();
        banner.innerText = 'Debes agregar al menos 2 opciones de respuesta.';
        banner.classList.remove('hidden');
        return false;
    }

    let radioChecked = false;
    let opcionesVacias = false;

    Array.from(cards).forEach(card => {
        const radio = card.querySelector('input[type="radio"]');
        if (radio && radio.checked) radioChecked = true;

        const textInput = card.querySelector('input[name="opcionTexto"]');
        if (!textInput || !textInput.value.trim()) opcionesVacias = true;
    });

    if (!radioChecked) {
        e.preventDefault();
        banner.innerText = 'Debes marcar una opción como respuesta correcta.';
        banner.classList.remove('hidden');
        return false;
    }

    if (opcionesVacias) {
        e.preventDefault();
        banner.innerText = 'Todas las opciones agregadas deben tener texto.';
        banner.classList.remove('hidden');
        return false;
    }

    return true;
}

// ─── Utilidad para sanitizar strings en inputs ──────────────────────────
function escaparHtml(texto) {
    if (!texto) return '';
    return String(texto)
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}
