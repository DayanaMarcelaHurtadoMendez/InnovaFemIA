/**
 * InnovaFemIA - Gestión de Actores (Panel Admin)
 * Control de modales para creación, edición y eliminación de actores territoriales.
 */
function abrirModalCrearActor() {
    var form = document.getElementById('form-actor');
    document.getElementById('modal-actor-title').innerText = 'Agregar Nuevo Actor';
    if (form && form.dataset.actionCrear) form.action = form.dataset.actionCrear;
    document.getElementById('actor-id').value = 0;
    document.getElementById('actor-nombre').value = '';
    document.getElementById('actor-descripcion').value = '';
    document.getElementById('actor-contacto').value = '';
    document.getElementById('actor-latitud').value = '2.8145';
    document.getElementById('actor-longitud').value = '-76.5360';
    document.getElementById('actor-div-activo').classList.add('hidden');
    document.getElementById('modal-actor-form').classList.remove('hidden');
}

function abrirModalEditarActor(a) {
    var form = document.getElementById('form-actor');
    document.getElementById('modal-actor-title').innerText = 'Editar Actor: ' + (a.Nombre || a.nombre);
    if (form && form.dataset.actionEditar) form.action = form.dataset.actionEditar;
    document.getElementById('actor-id').value = a.Id || a.id;
    document.getElementById('actor-tipo').value = a.TipoActor || a.tipoActor;
    document.getElementById('actor-nombre').value = a.Nombre || a.nombre;
    document.getElementById('actor-descripcion').value = a.Descripcion || a.descripcion || '';
    document.getElementById('actor-contacto').value = a.DatosContacto || a.datosContacto || '';
    document.getElementById('actor-latitud').value = a.Latitud || a.latitud;
    document.getElementById('actor-longitud').value = a.Longitud || a.longitud;
    document.getElementById('actor-div-activo').classList.remove('hidden');
    document.getElementById('actor-activo').checked = (a.Activo !== undefined ? a.Activo : a.activo);
    document.getElementById('modal-actor-form').classList.remove('hidden');
}

function cerrarModalActorForm() {
    document.getElementById('modal-actor-form').classList.add('hidden');
}

function confirmarEliminarActor(id, nombre) {
    document.getElementById('actor-id-eliminar').value = id;
    document.getElementById('actor-nombre-eliminar').innerText = nombre;
    document.getElementById('modal-eliminar-actor').classList.remove('hidden');
}

function cerrarModalEliminarActor() {
    document.getElementById('modal-eliminar-actor').classList.add('hidden');
}
