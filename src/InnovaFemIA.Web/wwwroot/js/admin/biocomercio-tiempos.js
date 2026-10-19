/**
 * InnovaFemIA - Configuración de Tiempos de Cosecha (Panel Admin)
 * Control de modales para creación, edición y eliminación de tiempos agronómicos de referencia.
 */
function abrirModalCrearTiempo() {
    var form = document.getElementById('form-tiempo');
    document.getElementById('modal-tiempo-title').innerText = 'Configurar Tiempo de Cosecha';
    if (form && form.dataset.actionCrear) form.action = form.dataset.actionCrear;
    document.getElementById('tiempo-id').value = 0;
    document.getElementById('tiempo-meses').value = 8;
    document.getElementById('tiempo-template').value = 'Tu cultivo estará listo para cosecha en aproximadamente {meses} meses.';
    document.getElementById('tiempo-notas').value = '';
    document.getElementById('modal-tiempo-form').classList.remove('hidden');
}

function abrirModalEditarTiempo(t) {
    var form = document.getElementById('form-tiempo');
    document.getElementById('modal-tiempo-title').innerText = 'Editar Tiempo de Cosecha';
    if (form && form.dataset.actionEditar) form.action = form.dataset.actionEditar;
    document.getElementById('tiempo-id').value = t.Id || t.id;
    document.getElementById('tiempo-cultivo').value = t.CultivoId || t.cultivoId;
    document.getElementById('tiempo-actividad').value = t.Actividad || t.actividad;
    document.getElementById('tiempo-meses').value = t.MesesHastaCosecha || t.mesesHastaCosecha;
    document.getElementById('tiempo-template').value = t.DescripcionTemplate || t.descripcionTemplate || '';
    document.getElementById('tiempo-notas').value = t.Notas || t.notas || '';
    document.getElementById('modal-tiempo-form').classList.remove('hidden');
}

function cerrarModalTiempoForm() {
    document.getElementById('modal-tiempo-form').classList.add('hidden');
}

function confirmarEliminarTiempo(id) {
    document.getElementById('tiempo-id-eliminar').value = id;
    document.getElementById('modal-eliminar-tiempo').classList.remove('hidden');
}

function cerrarModalEliminarTiempo() {
    document.getElementById('modal-eliminar-tiempo').classList.add('hidden');
}
