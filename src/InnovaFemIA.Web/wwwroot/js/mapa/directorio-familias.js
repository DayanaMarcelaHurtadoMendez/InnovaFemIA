/**
 * InnovaFemIA - Directorio de Familias Campesinas
 * Controla la carga y visualización de la ficha técnica de productoras y modal de detalle.
 */
async function verFichaFamilia(familiaId) {
    const modal = document.getElementById('modal-familia-dir');
    if (!modal) return;
    modal.classList.remove('hidden');

    document.getElementById('modal-dir-nombre').innerText = 'Cargando...';
    document.getElementById('modal-dir-stock').innerHTML = '<p class="text-xs text-gray-400 italic">Cargando productos...</p>';

    try {
        const res = await fetch(`/api/mapa/familias/${familiaId}`);
        if (!res.ok) throw new Error('Error al cargar la familia');
        const f = await res.json();

        document.getElementById('modal-dir-nombre').innerText = f.nombre;
        document.getElementById('modal-dir-vereda').innerText = `📍 Vereda ${f.vereda}, ${f.municipio}, Cauca`;
        document.getElementById('modal-dir-contacto').innerText = f.nombreContacto;
        document.getElementById('modal-dir-whatsapp').href = `https://wa.me/57${f.telefonoContacto.replace(/\D/g, '')}`;

        if (f.fotoBase64) {
            document.getElementById('modal-dir-foto-container').innerHTML = `<button onclick="window.abrirVistaPreviaImagen('${f.fotoBase64}')" title="Ampliar foto" class="block w-full h-full cursor-zoom-in hover:opacity-90 transition-opacity"><img src="${f.fotoBase64}" class="w-full h-full object-cover"/></button>`;
        } else {
            document.getElementById('modal-dir-foto-container').innerHTML = '👩‍🌾';
        }

        if (f.descripcion) {
            document.getElementById('modal-dir-descripcion').innerText = f.descripcion;
            document.getElementById('modal-dir-desc-container').classList.remove('hidden');
        } else {
            document.getElementById('modal-dir-desc-container').classList.add('hidden');
        }

        if (f.stockProductos && f.stockProductos.length > 0) {
            const stockHtml = f.stockProductos.map(s => `
                <div class="flex items-center justify-between p-3 rounded-xl border border-gray-100 bg-white hover:bg-gray-50 transition-colors">
                    <div class="flex items-center gap-2.5">
                        <span class="text-xl">${s.icono}</span>
                        <div>
                            <span class="font-semibold text-xs text-gray-900 block">${s.nombreCultivo}</span>
                            <span class="text-[10px] text-gray-400">${s.notas || 'Producto de finca agroecológica'}</span>
                        </div>
                    </div>
                    <div class="text-right">
                        <span class="font-bold text-[11px] ${s.disponible ? 'text-verde bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200' : 'text-gray-400'} inline-block">
                            ${s.disponible ? '✓ Cosecha Disponible' : '✕ Agotado'}
                        </span>
                    </div>
                </div>
            `).join('');
            document.getElementById('modal-dir-stock').innerHTML = stockHtml;
        } else {
            document.getElementById('modal-dir-stock').innerHTML = '<p class="text-xs text-gray-400 italic">No hay productos registrados en stock actualmente.</p>';
        }
    } catch (e) {
        console.error(e);
        document.getElementById('modal-dir-nombre').innerText = 'Error al cargar';
    }
}

// Controladores de cierre de modal
const cerrarModalDir = () => {
    const modal = document.getElementById('modal-familia-dir');
    if (modal) modal.classList.add('hidden');
};

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('btn-cerrar-modal-dir')?.addEventListener('click', cerrarModalDir);
    document.getElementById('btn-cerrar-modal-dir-footer')?.addEventListener('click', cerrarModalDir);
    document.getElementById('modal-familia-dir')?.addEventListener('click', function(e) {
        if (e.target === this) cerrarModalDir();
    });
});

// Vista previa de fotos estilo Lightbox profesional
window.abrirVistaPreviaImagen = function(url) {
    if (!url) return;
    const div = document.createElement('div');
    div.className = 'fixed inset-0 z-[9999] bg-gray-900/95 backdrop-blur-sm flex items-center justify-center p-4 animate-fade-in cursor-zoom-out';
    div.onclick = () => div.remove();
    div.innerHTML = `
        <img src="${url}" class="max-w-full max-h-[90vh] rounded-xl shadow-2xl object-contain" onclick="event.stopPropagation()" />
        <button class="absolute top-6 right-6 text-white text-3xl font-bold bg-gray-800/60 hover:bg-gray-700 w-12 h-12 rounded-full flex items-center justify-center transition-colors" onclick="this.parentElement.remove()">✕</button>
    `;
    document.body.appendChild(div);
};
