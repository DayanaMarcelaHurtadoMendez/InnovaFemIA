function abrirModalCrearRegla() {
    const form = document.getElementById('form-regla');
    document.getElementById('modal-regla-title').innerText = 'Agregar Práctica Lunar';
    if (form && form.dataset.actionCrear) {
        form.action = form.dataset.actionCrear;
    }
    document.getElementById('regla-id').value = 0;
    document.getElementById('regla-practica').value = '';
    document.getElementById('regla-recomendacion').value = '';
    document.getElementById('regla-cultivo').value = '';
    document.getElementById('modal-regla-form').classList.remove('hidden');
}

function abrirModalEditarRegla(r) {
    const form = document.getElementById('form-regla');
    document.getElementById('modal-regla-title').innerText = 'Editar Práctica Lunar';
    if (form && form.dataset.actionEditar) {
        form.action = form.dataset.actionEditar;
    }
    document.getElementById('regla-id').value = r.Id;
    document.getElementById('regla-fase').value = r.FaseLunar;
    document.getElementById('regla-practica').value = r.Practica;
    document.getElementById('regla-recomendacion').value = r.Recomendacion;
    document.getElementById('regla-confianza').value = r.NivelConfianza;
    document.getElementById('regla-cultivo').value = r.CultivoId || '';
    document.getElementById('modal-regla-form').classList.remove('hidden');
}

function cerrarModalReglaForm() {
    document.getElementById('modal-regla-form').classList.add('hidden');
}

function confirmarEliminarRegla(id, nombre) {
    document.getElementById('regla-id-eliminar').value = id;
    document.getElementById('regla-nombre-eliminar').innerText = nombre;
    document.getElementById('modal-eliminar-regla').classList.remove('hidden');
}

function cerrarModalEliminarRegla() {
    document.getElementById('modal-eliminar-regla').classList.add('hidden');
}
