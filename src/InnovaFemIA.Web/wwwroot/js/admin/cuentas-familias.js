function abrirModalCrearCuenta() {
    document.getElementById('modal-crear-cuenta').classList.remove('hidden');
}

function cerrarModalCrearCuenta() {
    document.getElementById('modal-crear-cuenta').classList.add('hidden');
}

function abrirModalEditarCuenta(c) {
    document.getElementById('edit-cuenta-id').value = c.Id;
    document.getElementById('edit-cuenta-usuario').value = c.NombreUsuario;
    document.getElementById('edit-cuenta-activo').checked = c.Activo;
    document.getElementById('modal-editar-cuenta').classList.remove('hidden');
}

function cerrarModalEditarCuenta() {
    document.getElementById('modal-editar-cuenta').classList.add('hidden');
}

function confirmarEliminarCuenta(id, usuario) {
    document.getElementById('cuenta-id-eliminar').value = id;
    document.getElementById('cuenta-nombre-eliminar').innerText = usuario;
    document.getElementById('modal-eliminar-cuenta').classList.remove('hidden');
}

function cerrarModalEliminarCuenta() {
    document.getElementById('modal-eliminar-cuenta').classList.add('hidden');
}
