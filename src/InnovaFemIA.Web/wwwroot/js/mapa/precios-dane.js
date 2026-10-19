/**
 * InnovaFemIA - Indicadores Mayoristas DANE (SIPSA)
 * Manejo de filtros reactivos por grupo de alimento, término de búsqueda y mercado de destino.
 */
let grupoActivo = '';

function seleccionarGrupoReactivo(grupo) {
    grupoActivo = (grupo || '').trim().toLowerCase();

    // Actualizar estilos de los botones de pestañas
    document.querySelectorAll('.btn-filtro-grupo').forEach(btn => {
        const btnGrupo = (btn.getAttribute('data-grupo') || '').trim().toLowerCase();
        if (btnGrupo === grupoActivo) {
            btn.className = 'btn-filtro-grupo px-4 py-2 rounded-xl text-xs sm:text-sm font-extrabold whitespace-nowrap transition-all bg-verde text-white shadow-md shadow-verde/20';
        } else {
            btn.className = 'btn-filtro-grupo px-4 py-2 rounded-xl text-xs sm:text-sm font-extrabold whitespace-nowrap transition-all bg-gray-100 text-gray-700 hover:bg-gray-200';
        }
    });

    aplicarFiltrosReactivos();
}

function aplicarFiltrosReactivos() {
    const textoBusqueda = (document.getElementById('filtro-busqueda')?.value || '').trim().toLowerCase();
    const mercadoSeleccionado = (document.getElementById('filtro-mercado')?.value || '').trim().toLowerCase();

    const filas = document.querySelectorAll('.fila-cotizacion');
    let visibles = 0;

    filas.forEach(fila => {
        const grupo = fila.getAttribute('data-grupo') || '';
        const producto = fila.getAttribute('data-producto') || '';
        const mercado = fila.getAttribute('data-mercado') || '';

        const coincideGrupo = !grupoActivo || grupo === grupoActivo;
        const coincideBusqueda = !textoBusqueda || producto.includes(textoBusqueda) || mercado.includes(textoBusqueda) || grupo.includes(textoBusqueda);
        const coincideMercado = !mercadoSeleccionado || mercado === mercadoSeleccionado;

        if (coincideGrupo && coincideBusqueda && coincideMercado) {
            fila.style.display = '';
            visibles++;
        } else {
            fila.style.display = 'none';
        }
    });

    // Actualizar contador
    const contadorEl = document.getElementById('contador-cotizaciones');
    if (contadorEl) {
        contadorEl.innerText = visibles;
    }

    // Mostrar / ocultar estado de sin resultados
    const sinResultadosEl = document.getElementById('sin-resultados-filtro');
    const tablaContenedor = document.getElementById('contenedor-tabla-sipsa');
    if (sinResultadosEl && tablaContenedor) {
        if (visibles === 0) {
            sinResultadosEl.classList.remove('hidden');
            tablaContenedor.classList.add('hidden');
        } else {
            sinResultadosEl.classList.add('hidden');
            tablaContenedor.classList.remove('hidden');
        }
    }

    // Control del botón limpiar
    const btnLimpiar = document.getElementById('btn-limpiar-filtros');
    if (btnLimpiar) {
        if (grupoActivo || textoBusqueda || mercadoSeleccionado) {
            btnLimpiar.classList.remove('hidden');
        } else {
            btnLimpiar.classList.add('hidden');
        }
    }
}

function limpiarFiltrosReactivos() {
    const inputBusqueda = document.getElementById('filtro-busqueda');
    const selectMercado = document.getElementById('filtro-mercado');
    if (inputBusqueda) inputBusqueda.value = '';
    if (selectMercado) selectMercado.value = '';
    seleccionarGrupoReactivo('');
}
