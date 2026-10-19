const EMOJIS_DISPONIBLES = [
    // Granos, Legumbres y Semillas
    { e: '🫘', n: 'frijol caraota poroto legumbre alubia habichuela' },
    { e: '🫛', n: 'guisante arveja arvejas vaina haba' },
    { e: '🌽', n: 'maiz choclo elote mazorca' },
    { e: '☕', n: 'cafe cafeto grano cafezal' },
    { e: '🌾', n: 'arroz trigo cebada espiga avena ajonjoli centeno' },
    { e: '🥜', n: 'mani cacahuate fruto seco' },
    { e: '🌰', n: 'castana nuez avellana' },
    { e: '🌻', n: 'girasol pipa semilla' },

    // Frutas Tropicales, Cítricos y Silvestres
    { e: '🥑', n: 'aguacate palta hass' },
    { e: '🍌', n: 'platano banano guineo harton dominico' },
    { e: '🍈', n: 'papaya guayaba guanabana melon lulo maracuya chirimoya' },
    { e: '🥭', n: 'mango tommy hilacha' },
    { e: '🍍', n: 'pina anana oro miel' },
    { e: '🍊', n: 'naranja mandarina citrico valencia' },
    { e: '🍋', n: 'limon tahiti pajarito mandariacitrico' },
    { e: '🍋‍🟩', n: 'limon verde lima' },
    { e: '🍓', n: 'fresa frutilla mora silvestre' },
    { e: '🫐', n: 'mora arandano agraz zarzamora' },
    { e: '🍇', n: 'uva vid racimo isabella' },
    { e: '🍉', n: 'sandia patilla' },
    { e: '🍎', n: 'manzana roja' },
    { e: '🍏', n: 'manzana verde' },
    { e: '🍐', n: 'pera agua' },
    { e: '🍑', n: 'durazno melocoton melocotonero' },
    { e: '🍒', n: 'cereza guinda capulin' },
    { e: '🥥', n: 'coco palmera agua de coco' },
    { e: '🥝', n: 'kiwi' },

    // Hortalizas, Tubérculos, Raíces y Aliños
    { e: '🥔', n: 'papa patata criolla pastusa capira' },
    { e: '🍠', n: 'yuca batata camote boniato tuberculo' },
    { e: '🍅', n: 'tomate chonto milano cheryjitomate' },
    { e: '🥕', n: 'zanahoria hortaliza' },
    { e: '🧅', n: 'cebolla cabezona junca rama hortaliza' },
    { e: '🧄', n: 'ajo alino diente de ajo' },
    { e: '🫑', n: 'pimenton pimiento morron paprika' },
    { e: '🌶️', n: 'aji chile picante habanero jalepeno tabasco' },
    { e: '🥒', n: 'pepino cohombro' },
    { e: '🥬', n: 'lechuga repollo acelga espinaca verdura col' },
    { e: '🥦', n: 'brocoli coliflor hortaliza' },
    { e: '🍆', n: 'berenjena verdura' },
    { e: '🎃', n: 'calabaza ahuyama zapallo calabacin' },
    { e: '🫚', n: 'jengibre curcuma raiz medicinal' },
    { e: '🍄', n: 'hongo champinon seta orellana' },
    { e: '🫒', n: 'olivo aceituna aceituno' },

    // Plantas, Cañas, Aromáticas y Agroecología
    { e: '🌱', n: 'brote semilla siembra plantula germinado almasigo' },
    { e: '🌿', n: 'hierba yuca aromatica cilantro perejil menta ruda tomillo albahaca' },
    { e: '🎋', n: 'cana cana panelera azucar guadua bambu' },
    { e: '🪵', n: 'madera lena arbol maderable eucalipto roble' },
    { e: '🍃', n: 'hoja hoja de coca te tabaco hoja verde abono verde' },
    { e: '🪴', n: 'planta maceta vivero' },
    { e: '🌴', n: 'palma chontaduro platanera cocotero' },
    { e: '🌳', n: 'arbol bosque sombrio guamo carboncillo cedro' },
    { e: '🌲', n: 'pino cipres arbol' },
    { e: '🌵', n: 'cactus nopal pitahaya' },
    { e: '☘️', n: 'trebol pasto forraje pastizal' },
    { e: '🍀', n: 'trebol cuatro hojas bendicion' },

    // Flores, Apicultura y Polinizadores
    { e: '🌺', n: 'flor hibisco orquidea floracion' },
    { e: '🌸', n: 'flor cafezal cerezo flor' },
    { e: '🌼', n: 'manzanilla flor silvestre calendula arnica' },
    { e: '🪻', n: 'lavanda aromatica flor morada' },
    { e: '🌹', n: 'rosa flor ornamental' },
    { e: '🌷', n: 'tulipan flor' },
    { e: '🪷', n: 'loto lirio flor acuatica' },
    { e: '🐝', n: 'abeja apicultura polinizador colmena enjambre' },
    { e: '🍯', n: 'miel polen panal melipona jalea' },
    { e: '🦋', n: 'mariposa polinizador bioindicador' },
    { e: '🐞', n: 'mariquita control biologico predador' },
    { e: '🐛', n: 'gusano oruga larva plaga' },
    { e: '🪱', n: 'lombriz lombricultura humus abono organico compost' },

    // Campo, Clima, Herramientas y Saberes
    { e: '👩‍🌾', n: 'campesina agricultora mujer rural mujer productora' },
    { e: '👨‍🌾', n: 'campesino agricultor jornalero productor' },
    { e: '🧑‍🌾', n: 'agricultor joven rural' },
    { e: '🧺', n: 'cosecha canasto canasta canastilla recoleccion' },
    { e: '🚜', n: 'tractor maquinaria arado transporte chiva' },
    { e: '✂️', n: 'poda tijera mantenimiento plateo' },
    { e: '🧪', n: 'abono biofertilizante caldo microbiologia' },
    { e: '💧', n: 'agua riego gota humedad aspersor goteo' },
    { e: '🌧️', n: 'lluvia invierno precipitacion aguas' },
    { e: '☀️', n: 'sol verano secado soleado clima' },
    { e: '🌤️', n: 'nubes tiempo templado' },
    { e: '🌘', n: 'luna fase lunar menguante creciente calendario lunar' },
    { e: '🌕', n: 'luna llena plenilunio siembra' },
    { e: '⛰️', n: 'montana cordillera loma pendiente microclima caldono' }
];

function renderEmojis(lista) {
    const grid = document.getElementById('emoji-grid');
    if (!grid) return;
    grid.innerHTML = '';
    lista.forEach(item => {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'hover:scale-125 transition-transform p-1 rounded-lg hover:bg-emerald-100';
        btn.innerText = item.e;
        btn.onclick = () => seleccionarEmoji(item.e);
        grid.appendChild(btn);
    });
}

function toggleEmojiPicker() {
    const container = document.getElementById('emoji-picker-container');
    if (!container) return;
    container.classList.toggle('hidden');
    if (!container.classList.contains('hidden')) {
        renderEmojis(EMOJIS_DISPONIBLES);
    }
}

function filtrarEmojis() {
    const searchEl = document.getElementById('emoji-search');
    if (!searchEl) return;
    const q = searchEl.value.toLowerCase().trim();
    if (!q) {
        renderEmojis(EMOJIS_DISPONIBLES);
        return;
    }
    const filtrados = EMOJIS_DISPONIBLES.filter(x => x.n.includes(q) || x.e.includes(q));
    renderEmojis(filtrados);
}

function seleccionarEmoji(emoji) {
    const inputEl = document.getElementById('cultivo-icono');
    const previewEl = document.getElementById('cultivo-icono-preview');
    const container = document.getElementById('emoji-picker-container');
    if (inputEl) inputEl.value = emoji;
    if (previewEl) previewEl.innerText = emoji;
    if (container) container.classList.add('hidden');
}

function abrirModalCrearCultivo() {
    const form = document.getElementById('form-cultivo');
    document.getElementById('modal-cultivo-title').innerText = 'Registrar Nuevo Cultivo';
    if (form && form.dataset.actionCrear) {
        form.action = form.dataset.actionCrear;
    }
    document.getElementById('cultivo-id').value = 0;
    seleccionarEmoji('🌱');
    document.getElementById('cultivo-nombre').value = '';
    document.getElementById('cultivo-descripcion').value = '';
    document.getElementById('div-activo-cultivo').classList.add('hidden');
    document.getElementById('modal-cultivo-form').classList.remove('hidden');
}

function abrirModalEditarCultivo(c) {
    const form = document.getElementById('form-cultivo');
    document.getElementById('modal-cultivo-title').innerText = 'Editar Cultivo';
    if (form && form.dataset.actionEditar) {
        form.action = form.dataset.actionEditar;
    }
    document.getElementById('cultivo-id').value = c.Id;
    seleccionarEmoji(c.Icono || '🌱');
    document.getElementById('cultivo-nombre').value = c.Nombre;
    document.getElementById('cultivo-descripcion').value = c.Descripcion || '';
    document.getElementById('cultivo-activo').checked = c.Activo;
    document.getElementById('div-activo-cultivo').classList.remove('hidden');
    document.getElementById('modal-cultivo-form').classList.remove('hidden');
}

function cerrarModalCultivoForm() {
    document.getElementById('modal-cultivo-form').classList.add('hidden');
}

function confirmarEliminarCultivo(id, nombre) {
    document.getElementById('cultivo-id-eliminar').value = id;
    document.getElementById('cultivo-nombre-eliminar').innerText = nombre;
    document.getElementById('modal-eliminar-cultivo').classList.remove('hidden');
}

function cerrarModalEliminarCultivo() {
    document.getElementById('modal-eliminar-cultivo').classList.add('hidden');
}

// Inicializar emojis
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => renderEmojis(EMOJIS_DISPONIBLES));
} else {
    renderEmojis(EMOJIS_DISPONIBLES);
}
