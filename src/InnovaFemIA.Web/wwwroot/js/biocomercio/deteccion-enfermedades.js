// ─── Detección de Enfermedades en Café — InnovaFemIA ──────────────────────────
// Manejador cliente de captura de cámara, carga de archivos y renderizado canvas

let currentImage = null;
let currentDetections = [];
let showBoundingBoxes = true;

document.addEventListener('DOMContentLoaded', () => {
    inicializarEventos();
});

function inicializarEventos() {
    const inputCamara = document.getElementById('input-camara');
    const inputArchivo = document.getElementById('input-archivo');
    const btnLimpiar = document.getElementById('btn-limpiar-muestra');
    const btnToggleCajas = document.getElementById('btn-toggle-cajas');

    if (inputCamara) {
        inputCamara.addEventListener('change', (e) => procesarArchivoSeleccionado(e.target.files[0]));
    }

    if (inputArchivo) {
        inputArchivo.addEventListener('change', (e) => procesarArchivoSeleccionado(e.target.files[0]));
    }

    if (btnLimpiar) {
        btnLimpiar.addEventListener('click', limpiarVisor);
    }

    if (btnToggleCajas) {
        btnToggleCajas.addEventListener('click', alternarCajas);
    }



    // Redibujar cajas si cambia el tamaño de la ventana
    window.addEventListener('resize', () => {
        if (currentImage && currentDetections.length > 0) {
            dibujarCajasEnCanvas();
        }
    });
}

function procesarArchivoSeleccionado(file) {
    if (!file) return;

    if (!file.type.startsWith('image/')) {
        alert('Por favor selecciona un archivo de imagen válido (JPG, PNG o WEBP).');
        return;
    }

    const reader = new FileReader();
    reader.onload = function (e) {
        const rawDataUrl = e.target.result;
        optimizarYAnalizar(rawDataUrl, file.name);
    };
    reader.readAsDataURL(file);
}

function optimizarYAnalizar(dataUrl, nombreArchivo) {
    const img = new Image();
    img.onload = function () {
        // Redimensionar si es muy pesada (max 1280px en el lado mayor)
        const maxDim = 1280;
        let w = img.width;
        let h = img.height;

        if (w > maxDim || h > maxDim) {
            if (w > h) {
                h = Math.round((h * maxDim) / w);
                w = maxDim;
            } else {
                w = Math.round((w * maxDim) / h);
                h = maxDim;
            }
        }

        const offCanvas = document.createElement('canvas');
        offCanvas.width = w;
        offCanvas.height = h;
        const ctx = offCanvas.getContext('2d');
        ctx.drawImage(img, 0, 0, w, h);

        const optimizedDataUrl = offCanvas.toDataURL('image/jpeg', 0.88);
        mostrarImagenEnVisor(optimizedDataUrl, nombreArchivo);
        enviarAnalisisApi(optimizedDataUrl, nombreArchivo);
    };
    img.src = dataUrl;
}

function mostrarImagenEnVisor(dataUrl, nombreArchivo) {
    const imgEl = document.getElementById('img-muestra');
    const placeholder = document.getElementById('placeholder-visor');
    const controles = document.getElementById('controles-visor');
    const labelEstado = document.getElementById('label-estado-visor');
    const canvas = document.getElementById('canvas-overlay');

    currentImage = new Image();
    currentImage.onload = function () {
        imgEl.src = dataUrl;
        imgEl.classList.remove('hidden');
        placeholder.classList.add('hidden');
        controles.classList.remove('hidden');
        canvas.classList.remove('hidden');
        if (labelEstado) labelEstado.innerText = nombreArchivo || 'Muestra cargada';
    };
    currentImage.src = dataUrl;
}

async function enviarAnalisisApi(base64Data, nombreArchivo) {
    const loader = document.getElementById('loader-analisis');
    if (loader) loader.classList.remove('hidden');

    const basePath = (window.__PATH_BASE__ || window.AppPathBase || '').replace(/\/$/, '');
    const apiUrl = `${basePath}/biocomercio/api/detectar-enfermedad`;

    try {
        const response = await fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                imagenBase64: base64Data,
                nombreArchivo: nombreArchivo
            })
        });

        if (!response.ok) {
            throw new Error(`Error en el servidor (${response.status})`);
        }

        const data = await response.json();
        renderizarResultado(data);
    } catch (err) {
        console.error('Error al analizar la imagen:', err);
        alert('Ocurrió un inconveniente al comunicarse con el servicio de análisis fitosanitario. Por favor verifica tu conexión y vuelve a intentar.');
    } finally {
        if (loader) loader.classList.add('hidden');
    }
}

function renderizarResultado(resultado) {
    if (!resultado || !resultado.exito) {
        alert(resultado?.mensaje || 'No se pudo procesar el análisis de la imagen.');
        return;
    }

    const panelVacio = document.getElementById('panel-resultado-vacio');
    const panelActivo = document.getElementById('panel-resultado-activo');
    const headerDiag = document.getElementById('header-diagnostico');
    const badgeSemaforo = document.getElementById('badge-semaforo');
    const labelConfianza = document.getElementById('label-confianza');
    const tituloDiag = document.getElementById('titulo-diagnostico');
    const subCientifico = document.getElementById('subtitulo-cientifico');
    const textoSintomas = document.getElementById('texto-sintomas');
    const seccionCajas = document.getElementById('seccion-cajas-detalle');
    const listaCajas = document.getElementById('lista-cajas');

    panelVacio.classList.add('hidden');
    panelActivo.classList.remove('hidden');

    // Configurar color de semáforo según estado
    headerDiag.className = 'p-6 text-white transition-colors ' +
        (resultado.estadoSalud === 'Critico' ? 'bg-rose-700' :
         resultado.estadoSalud === 'Alerta' ? 'bg-amber-600' : 'bg-emerald-700');

    badgeSemaforo.innerText = resultado.estadoSalud === 'Critico' ? '⚠️ Nivel Crítico' :
                              resultado.estadoSalud === 'Alerta' ? '⚡ Nivel de Alerta' : '✅ Hoja Sana';

    labelConfianza.innerText = `${resultado.confianza.toFixed(1)}% de certeza`;
    tituloDiag.innerText = resultado.diagnostico;
    subCientifico.innerText = resultado.nombreCientifico || 'Coffea arabica';
    textoSintomas.innerText = resultado.descripcionSintomas;

    // Detecciones en el visor
    currentDetections = resultado.detecciones || [];
    if (currentDetections.length > 0) {
        seccionCajas.classList.remove('hidden');
        listaCajas.innerHTML = '';
        currentDetections.forEach((c, idx) => {
            const item = document.createElement('div');
            item.className = 'flex items-center justify-between text-xs bg-gray-50 px-3 py-1.5 rounded-lg border border-gray-100';
            item.innerHTML = `<span class="font-bold text-gray-800">#${idx + 1} ${c.clase}</span> <span class="font-mono text-[11px] text-gray-500">${c.confianza}%</span>`;
            listaCajas.appendChild(item);
        });
    } else {
        seccionCajas.classList.add('hidden');
    }

    // Dibujar en el canvas
    dibujarCajasEnCanvas();
}

function dibujarCajasEnCanvas() {
    const canvas = document.getElementById('canvas-overlay');
    const img = document.getElementById('img-muestra');
    if (!canvas || !img || !currentImage) return;

    // Ajustar resolución del canvas a las dimensiones de renderizado de la imagen
    const rect = img.getBoundingClientRect();
    const container = document.getElementById('contenedor-visor').getBoundingClientRect();

    canvas.width = rect.width;
    canvas.height = rect.height;
    canvas.style.left = `${img.offsetLeft}px`;
    canvas.style.top = `${img.offsetTop}px`;
    canvas.style.width = `${rect.width}px`;
    canvas.style.height = `${rect.height}px`;

    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    if (!showBoundingBoxes || currentDetections.length === 0) return;

    // Escala entre la imagen natural y el tamaño mostrado
    const scaleX = rect.width / (currentImage.naturalWidth || rect.width);
    const scaleY = rect.height / (currentImage.naturalHeight || rect.height);

    currentDetections.forEach((d, i) => {
        // En Roboflow x, y son el centro de la caja
        const boxW = d.ancho * scaleX;
        const boxH = d.alto * scaleY;
        const boxX = (d.x * scaleX) - (boxW / 2);
        const boxY = (d.y * scaleY) - (boxH / 2);

        const color = d.clase.toLowerCase().includes('roya') ? '#ef4444' :
                      d.clase.toLowerCase().includes('minador') ? '#f59e0b' : '#10b981';

        // Recuadro exterior
        ctx.strokeStyle = color;
        ctx.lineWidth = 3;
        ctx.strokeRect(boxX, boxY, boxW, boxH);

        // Fondo semitransparente
        ctx.fillStyle = color + '25';
        ctx.fillRect(boxX, boxY, boxW, boxH);

        // Etiqueta con clase y porcentaje
        const label = `${d.clase} (${d.confianza}%)`;
        ctx.font = 'bold 11px Segoe UI, sans-serif';
        const textWidth = ctx.measureText(label).width;

        ctx.fillStyle = color;
        ctx.fillRect(boxX, Math.max(0, boxY - 20), textWidth + 10, 20);

        ctx.fillStyle = '#ffffff';
        ctx.fillText(label, boxX + 5, Math.max(14, boxY - 5));
    });
}

function alternarCajas() {
    showBoundingBoxes = !showBoundingBoxes;
    dibujarCajasEnCanvas();
    const btn = document.getElementById('btn-toggle-cajas');
    if (btn) {
        btn.innerText = showBoundingBoxes ? '👁️ Ocultar recuadros de detección' : '👁️ Mostrar recuadros de detección';
    }
}

function limpiarVisor() {
    currentImage = null;
    currentDetections = [];
    const imgEl = document.getElementById('img-muestra');
    const placeholder = document.getElementById('placeholder-visor');
    const controles = document.getElementById('controles-visor');
    const canvas = document.getElementById('canvas-overlay');
    const labelEstado = document.getElementById('label-estado-visor');
    const panelVacio = document.getElementById('panel-resultado-vacio');
    const panelActivo = document.getElementById('panel-resultado-activo');

    imgEl.classList.add('hidden');
    imgEl.src = '';
    placeholder.classList.remove('hidden');
    controles.classList.add('hidden');
    canvas.classList.add('hidden');
    if (labelEstado) labelEstado.innerText = 'Sin imagen cargada';

    panelVacio.classList.remove('hidden');
    panelActivo.classList.add('hidden');

    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}


