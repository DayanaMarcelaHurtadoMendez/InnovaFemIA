function previsualizarFotoFamilia(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            document.getElementById('preview-foto-box').innerHTML = `<img src="${e.target.result}" class="w-full h-full object-cover"/>`;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

function abrirModalCrearFamilia() {
    const form = document.getElementById('form-familia');
    document.getElementById('modal-title').innerText = 'Agregar Nueva Familia';
    if (form && form.dataset.actionCrear) {
        form.action = form.dataset.actionCrear;
    }
    document.getElementById('input-id').value = 0;
    document.getElementById('input-nombre').value = '';
    document.getElementById('input-descripcion').value = '';
    document.getElementById('input-vereda').value = '';
    document.getElementById('input-contacto').value = '';
    document.getElementById('input-telefono').value = '';
    document.getElementById('input-latitud').value = '2.8051';
    document.getElementById('input-longitud').value = '-76.5512';
    document.getElementById('input-foto').value = '';
    document.getElementById('preview-foto-box').innerHTML = '👩‍🌾';
    document.getElementById('div-activo').classList.add('hidden');
    document.querySelectorAll('.chk-cultivo').forEach(c => c.checked = false);
    document.getElementById('modal-familia-form').classList.remove('hidden');
}

function abrirModalEditarFamilia(f) {
    const form = document.getElementById('form-familia');
    document.getElementById('modal-title').innerText = 'Editar Familia: ' + f.Nombre;
    if (form && form.dataset.actionEditar) {
        form.action = form.dataset.actionEditar;
    }
    document.getElementById('input-id').value = f.Id;
    document.getElementById('input-nombre').value = f.Nombre;
    document.getElementById('input-descripcion').value = f.Descripcion || '';
    document.getElementById('input-vereda').value = f.Vereda;
    document.getElementById('input-municipio').value = f.Municipio;
    document.getElementById('input-contacto').value = f.NombreContacto;
    document.getElementById('input-telefono').value = f.TelefonoContacto;
    document.getElementById('input-latitud').value = f.Latitud;
    document.getElementById('input-longitud').value = f.Longitud;
    document.getElementById('input-foto').value = '';
    
    if (f.FotoBase64) {
        document.getElementById('preview-foto-box').innerHTML = `<img src="${f.FotoBase64}" class="w-full h-full object-cover"/>`;
    } else {
        document.getElementById('preview-foto-box').innerHTML = '👩‍🌾';
    }

    document.getElementById('div-activo').classList.remove('hidden');
    document.getElementById('input-activo').checked = f.Activo;

    document.querySelectorAll('.chk-cultivo').forEach(c => {
        c.checked = f.CultivosIds && f.CultivosIds.includes(parseInt(c.value));
    });

    document.getElementById('modal-familia-form').classList.remove('hidden');
}

function cerrarModalFamiliaForm() {
    document.getElementById('modal-familia-form').classList.add('hidden');
}

function confirmarEliminarFamilia(id, nombre) {
    document.getElementById('id-eliminar').value = id;
    document.getElementById('nombre-eliminar').innerText = nombre;
    document.getElementById('modal-eliminar').classList.remove('hidden');
}

function cerrarModalEliminar() {
    document.getElementById('modal-eliminar').classList.add('hidden');
}
