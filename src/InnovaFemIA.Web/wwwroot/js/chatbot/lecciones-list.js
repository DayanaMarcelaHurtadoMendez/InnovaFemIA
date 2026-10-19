/**
 * InnovaFemIA - Modo Lección (Trivias Didácticas)
 * Controla la carga y visualización del progreso del usuario en las lecciones agroecológicas.
 */
document.addEventListener('DOMContentLoaded', () => {
    cargarProgreso();
});

async function cargarProgreso() {
    const key = 'innovafemia_lecciones_progreso';
    let progreso = {};
    try {
        progreso = JSON.parse(localStorage.getItem(key) || '{}');
    } catch {
        progreso = {};
    }

    let completadas = new Set(progreso.completadas || []);

    // Consultar progreso en servidor si la familia tiene sesión iniciada
    try {
        const resp = await fetch('/chatbot/api/lecciones/progreso', { credentials: 'include' });
        if (resp.ok) {
            const data = await resp.json();
            if (data.isAuthenticated && Array.isArray(data.completadas)) {
                data.completadas.forEach(id => completadas.add(id));
                // Guardar fusión en localStorage
                progreso.completadas = Array.from(completadas);
                progreso.ultimaFecha = new Date().toISOString();
                localStorage.setItem(key, JSON.stringify(progreso));
            }
        }
    } catch (e) {
        console.log('Modo offline / local para lecciones.');
    }

    const completadasArr = Array.from(completadas);
    const cards = document.querySelectorAll('.leccion-card');
    let completadasCount = 0;

    cards.forEach((card) => {
        const id = parseInt(card.getAttribute('data-leccion-id'));
        const badge = document.getElementById(`badge-estado-${id}`);
        const btn = document.getElementById(`btn-accion-${id}`);

        if (completadasArr.includes(id)) {
            completadasCount++;
            if (badge) {
                badge.className = 'px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-emerald-100 text-verde border border-emerald-300';
                badge.innerText = '✅ Completada';
            }

            if (btn) {
                btn.className = 'w-full sm:w-auto px-5 py-3 bg-amber-50 hover:bg-amber-100 text-amber-900 font-extrabold text-xs rounded-2xl border border-amber-200 transition-all flex items-center justify-center gap-2';
                btn.innerHTML = '<span>🔄</span> <span>Repasar Lección</span>';
            }
        } else {
            if (badge) {
                badge.className = 'px-2.5 py-0.5 rounded-full text-[10px] font-extrabold bg-amber-100 text-amber-900 border border-amber-200';
                badge.innerText = '⏳ Disponible';
            }
        }
    });

    const statEl = document.getElementById('stat-completadas');
    if (statEl) statEl.innerText = `${completadasCount} / ${cards.length}`;

    if (cards.length > 0 && completadasCount >= cards.length) {
        const banner = document.getElementById('banner-todas-completadas');
        if (banner) banner.classList.remove('hidden');
    }
}
