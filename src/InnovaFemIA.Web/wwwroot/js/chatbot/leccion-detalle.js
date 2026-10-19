const LECCION_DATA = JSON.parse(document.getElementById('leccion-data')?.textContent || '{}');

    let indicePregunta = 0;
    let opcionSeleccionada = null;
    let totalAciertos = 0;
    let yaComprobado = false;

    document.addEventListener('DOMContentLoaded', () => {
        if (!LECCION_DATA || !LECCION_DATA.Preguntas || LECCION_DATA.Preguntas.length === 0) {
            document.getElementById('contenedor-pregunta').innerHTML = `
                <div class="p-8 text-center text-gray-500 italic">
                    Esta lección aún no contiene preguntas registradas.
                </div>
            `;
            return;
        }

        cargarPreguntaActual();
    });

    function cargarPreguntaActual() {
        yaComprobado = false;
        opcionSeleccionada = null;

        const totalPreguntas = LECCION_DATA.Preguntas.length;
        const p = LECCION_DATA.Preguntas[indicePregunta];

        // Actualizar barra de progreso
        const porcentaje = Math.round(((indicePregunta) / totalPreguntas) * 100);
        document.getElementById('progreso-barra').style.width = `${porcentaje}%`;
        document.getElementById('texto-progreso-paso').innerText = `${indicePregunta + 1} / ${totalPreguntas}`;

        // Reset barra de acción
        const btn = document.getElementById('btn-comprobar');
        btn.disabled = true;
        btn.className = 'px-6 py-3 bg-gray-200 text-gray-400 font-black text-xs rounded-2xl transition-all shadow-sm cursor-not-allowed';
        btn.innerText = 'Comprobar';
        btn.onclick = comprobarRespuesta;
        document.getElementById('mensaje-feedback').innerHTML = 'Selecciona una respuesta para continuar';

        // Renderizar explicación previa (si tiene)
        let explicacionHtml = '';
        if (p.ContenidoExplicativo || p.TieneImagenExplicativa) {
            explicacionHtml = `
                <div class="p-4 bg-emerald-50/70 border border-emerald-200/80 rounded-2xl space-y-2">
                    <div class="flex items-center gap-1.5 text-verde font-extrabold text-[11px] uppercase tracking-wider">
                        <span>💡</span>
                        <span>Saberes Agroecológicos:</span>
                    </div>
                    ${p.TieneImagenExplicativa ? `
                        <div class="rounded-xl overflow-hidden shadow-xs border border-emerald-200 max-h-48 bg-white">
                            <img src="/chatbot/preguntas/${p.Id}/imagen" alt="Ilustración explicativa" class="w-full h-full object-cover max-h-48" />
                        </div>
                    ` : ''}
                    ${p.ContenidoExplicativo ? `<p class="text-xs text-verde leading-relaxed">${p.ContenidoExplicativo}</p>` : ''}
                </div>
            `;
        }

        // Renderizar opciones de respuesta
        const opcionesHtml = p.Opciones.map((op, idx) => `
            <button id="opcion-btn-${op.Id}" 
                    type="button" 
                    onclick="seleccionarOpcion(${op.Id})" 
                    class="opcion-card w-full p-4 bg-white hover:bg-emerald-50/40 border-2 border-gray-200 hover:border-emerald-400 rounded-2xl font-bold text-xs text-gray-800 text-left transition-all flex items-center justify-between gap-3 shadow-xs cursor-pointer">
                <div class="flex items-center gap-3">
                    <span id="opcion-letter-${op.Id}" class="w-6 h-6 rounded-full bg-gray-100 text-gray-600 font-extrabold text-[10px] flex items-center justify-center shrink-0 transition-colors">
                        ${String.fromCharCode(65 + idx)}
                    </span>
                    <span class="leading-snug">${op.TextoOpcion}</span>
                </div>
                <div id="opcion-indicator-${op.Id}" class="w-6 h-6 rounded-full border-2 border-gray-300 text-transparent flex items-center justify-center text-xs font-black transition-all shrink-0"></div>
            </button>
        `).join('');

        const preguntaHtml = `
            ${explicacionHtml}

            <div class="space-y-3">
                <h3 class="text-base sm:text-lg font-black text-gray-900 leading-snug">
                    ${p.TextoPregunta}
                </h3>
                <div class="space-y-2.5 pt-1">
                    ${opcionesHtml}
                </div>
            </div>
        `;

        document.getElementById('contenedor-pregunta').innerHTML = preguntaHtml;
    }

    function seleccionarOpcion(opcionId) {
        if (yaComprobado) return;

        opcionSeleccionada = opcionId;

        // Deseleccionar todas las opciones visualmente
        document.querySelectorAll('.opcion-card').forEach(c => {
            c.classList.remove('border-emerald-600', 'bg-emerald-50', 'shadow-md');
            c.classList.add('border-gray-200', 'bg-white');
        });

        // Resetear todos los indicadores circulares y letras
        document.querySelectorAll('[id^="opcion-indicator-"]').forEach(ind => {
            ind.className = 'w-6 h-6 rounded-full border-2 border-gray-300 text-transparent flex items-center justify-center text-xs font-black transition-all shrink-0';
            ind.innerHTML = '';
        });
        document.querySelectorAll('[id^="opcion-letter-"]').forEach(letSpan => {
            letSpan.className = 'w-6 h-6 rounded-full bg-gray-100 text-gray-600 font-extrabold text-[10px] flex items-center justify-center shrink-0 transition-colors';
        });

        // Marcar la opción activa
        const selectedBtn = document.getElementById(`opcion-btn-${opcionId}`);
        if (selectedBtn) {
            selectedBtn.classList.remove('border-gray-200', 'bg-white');
            selectedBtn.classList.add('border-emerald-600', 'bg-emerald-50', 'shadow-md');
        }

        const selectedIndicator = document.getElementById(`opcion-indicator-${opcionId}`);
        if (selectedIndicator) {
            selectedIndicator.className = 'w-6 h-6 rounded-full border-2 border-emerald-600 bg-emerald-600 text-white flex items-center justify-center text-xs font-black transition-all shadow-xs shrink-0';
            selectedIndicator.innerHTML = '✓';
        }

        const selectedLetter = document.getElementById(`opcion-letter-${opcionId}`);
        if (selectedLetter) {
            selectedLetter.className = 'w-6 h-6 rounded-full bg-emerald-200 text-emerald-900 font-extrabold text-[10px] flex items-center justify-center shrink-0 transition-colors';
        }

        // Habilitar botón comprobar
        const btn = document.getElementById('btn-comprobar');
        btn.disabled = false;
        btn.className = 'px-6 py-3 bg-emerald-600 hover:bg-verde text-white font-black text-xs rounded-2xl transition-all shadow-md cursor-pointer';
        document.getElementById('mensaje-feedback').innerHTML = '<span class="text-verde font-bold">¡Opción lista! Haz clic en Comprobar</span>';
    }

    function comprobarRespuesta() {
        if (!opcionSeleccionada || yaComprobado) return;
        yaComprobado = true;

        const p = LECCION_DATA.Preguntas[indicePregunta];
        const opcionElegida = p.Opciones.find(o => o.Id === opcionSeleccionada);
        const opcionCorrecta = p.Opciones.find(o => o.EsCorrecta);

        const esAcierto = opcionElegida && opcionElegida.EsCorrecta;
        if (esAcierto) totalAciertos++;

        // Resaltar visualmente las opciones con verde/rojo y sus checks/cruces
        p.Opciones.forEach(op => {
            const btnEl = document.getElementById(`opcion-btn-${op.Id}`);
            const indEl = document.getElementById(`opcion-indicator-${op.Id}`);
            const letEl = document.getElementById(`opcion-letter-${op.Id}`);

            if (op.EsCorrecta) {
                btnEl.className = 'opcion-card w-full p-4 bg-emerald-100 border-2 border-emerald-500 rounded-2xl font-bold text-xs text-verde text-left flex items-center justify-between gap-3 shadow-md';
                if (indEl) {
                    indEl.className = 'w-6 h-6 rounded-full bg-emerald-600 border-2 border-emerald-600 text-white flex items-center justify-center text-xs font-black shadow-xs shrink-0';
                    indEl.innerHTML = '✓';
                }
                if (letEl) {
                    letEl.className = 'w-6 h-6 rounded-full bg-emerald-300 text-emerald-950 font-extrabold text-[10px] flex items-center justify-center shrink-0';
                }
            } else if (op.Id === opcionSeleccionada && !op.EsCorrecta) {
                btnEl.className = 'opcion-card w-full p-4 bg-rose-100 border-2 border-rose-500 rounded-2xl font-bold text-xs text-rose-950 text-left flex items-center justify-between gap-3 shadow-md';
                if (indEl) {
                    indEl.className = 'w-6 h-6 rounded-full bg-rose-600 border-2 border-rose-600 text-white flex items-center justify-center text-xs font-black shadow-xs shrink-0';
                    indEl.innerHTML = '✕';
                }
                if (letEl) {
                    letEl.className = 'w-6 h-6 rounded-full bg-rose-200 text-rose-950 font-extrabold text-[10px] flex items-center justify-center shrink-0';
                }
            } else {
                if (indEl) {
                    indEl.className = 'w-6 h-6 rounded-full border-2 border-gray-200 text-transparent flex items-center justify-center text-xs font-black shrink-0 opacity-40';
                    indEl.innerHTML = '';
                }
            }
        });

        // Banner de retroalimentación
        const feedbackEl = document.getElementById('mensaje-feedback');
        if (esAcierto) {
            const retro = opcionElegida.Retroalimentacion || '¡Excelente! Respuesta correcta.';
            feedbackEl.innerHTML = `
                <div class="text-verde font-black flex items-center gap-1.5">
                    <span class="text-base">🌟</span>
                    <span>${retro}</span>
                </div>
            `;
        } else {
            const retro = (opcionElegida && opcionElegida.Retroalimentacion) ? opcionElegida.Retroalimentacion : `Respuesta correcta: ${opcionCorrecta ? opcionCorrecta.TextoOpcion : ''}`;
            feedbackEl.innerHTML = `
                <div class="text-rose-800 font-bold flex items-center gap-1.5">
                    <span class="text-base">❌</span>
                    <span>${retro}</span>
                </div>
            `;
        }

        // Cambiar botón a "Continuar"
        const btn = document.getElementById('btn-comprobar');
        btn.innerText = 'Continuar →';
        btn.className = 'px-6 py-3 bg-emerald-600 hover:bg-verde text-white font-black text-xs rounded-2xl transition-all shadow-md cursor-pointer';
        btn.onclick = avanzarPregunta;
    }

    function avanzarPregunta() {
        indicePregunta++;
        const totalPreguntas = LECCION_DATA.Preguntas.length;

        if (indicePregunta < totalPreguntas) {
            cargarPreguntaActual();
        } else {
            finalizarLeccion();
        }
    }

    function finalizarLeccion() {
        document.getElementById('progreso-barra').style.width = '100%';
        document.getElementById('pantalla-juego').classList.add('hidden');
        document.getElementById('pantalla-resumen').classList.remove('hidden');

        const totalPreguntas = LECCION_DATA.Preguntas.length;
        const precision = Math.round((totalAciertos / totalPreguntas) * 100);

        document.getElementById('resumen-aciertos').innerText = `${totalAciertos} / ${totalPreguntas}`;
        document.getElementById('resumen-precision').innerText = `${precision}%`;

        // Guardar progreso en localStorage y notificar al servidor
        guardarProgresoEnLocalStorage(LECCION_DATA.Id);
        notificarProgresoAlServidor(LECCION_DATA.Id, totalAciertos, totalPreguntas, precision);
    }

    function guardarProgresoEnLocalStorage(leccionId) {
        const key = 'innovafemia_lecciones_progreso';
        let progreso = {};
        try {
            progreso = JSON.parse(localStorage.getItem(key) || '{}');
        } catch {
            progreso = {};
        }

        if (!progreso.completadas) progreso.completadas = [];
        if (!progreso.completadas.includes(leccionId)) {
            progreso.completadas.push(leccionId);
        }

        progreso.ultimaFecha = new Date().toISOString();
        localStorage.setItem(key, JSON.stringify(progreso));
    }

    function notificarProgresoAlServidor(leccionId, aciertos, total, precision) {
        try {
            fetch(`/chatbot/api/lecciones/${leccionId}/completar`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify({ aciertos: aciertos, totalPreguntas: total, precision: precision })
            }).then(r => r.json()).then(data => {
                if (data && data.guardadoEnServidor) {
                    console.log('Progreso de lección sincronizado exitosamente con la cuenta de la familia.');
                }
            }).catch(() => {});
        } catch (e) {}
    }