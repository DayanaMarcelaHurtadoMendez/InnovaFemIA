const dbTiempos = JSON.parse(document.getElementById('tiempos-data')?.textContent || '[]');
    const mesesNombres = ['enero', 'febrero', 'marzo', 'abril', 'mayo', 'junio', 'julio', 'agosto', 'septiembre', 'octubre', 'noviembre', 'diciembre'];

    // ─── Pestañas ────────────────────────────────────────────────────────────
    function mostrarCalculadora(id, btnElement) {
        document.querySelectorAll('.calc-section').forEach(el => el.classList.add('hidden'));
        document.getElementById('calc-' + id).classList.remove('hidden');

        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.classList.remove('bg-emerald-600', 'text-white', 'shadow-md');
            btn.classList.add('bg-gray-100', 'text-gray-700');
        });
        
        btnElement.classList.remove('bg-gray-100', 'text-gray-700');
        btnElement.classList.add('bg-emerald-600', 'text-white', 'shadow-md');
    }

    // ─── Utilidad: Formato de Moneda Colombiano con separador de miles ────────
    function formatearCOP(numero) {
        return '$' + Math.round(numero).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    }

    // ─── Calculadora 1: Cosecha y Actividades ────────────────────────────────
    function actualizarActividades() {
        const cultivo = document.getElementById('cultivoCosecha').value;
        const selectAct = document.getElementById('actividadCosecha');
        selectAct.innerHTML = '<option value="">Selecciona una labor...</option>';
        
        if (!cultivo) return;
        
        const acts = dbTiempos.filter(t => t.NombreCultivo === cultivo);
        acts.forEach(a => {
            const opt = document.createElement('option');
            opt.value = a.Actividad;
            opt.textContent = a.Actividad;
            selectAct.appendChild(opt);
        });
    }

    function calcularCosecha(e) {
        e.preventDefault();
        const cultivo = document.getElementById('cultivoCosecha').value;
        const actividad = document.getElementById('actividadCosecha').value;
        const fechaStr = document.getElementById('fechaCosecha').value;
        
        if (!cultivo || !actividad || !fechaStr) {
            alert('Por favor completa todos los campos requeridos.');
            return;
        }

        const objTiempo = dbTiempos.find(t => t.NombreCultivo === cultivo && t.Actividad === actividad);
        if (!objTiempo) {
            alert('No se encontraron tiempos de referencia para esta labor.');
            return;
        }

        // Parsear fecha YYYY-MM-DD
        const [y, m, d] = fechaStr.split('-').map(Number);
        const fechaIngresada = new Date(y, m - 1, d);

        // Fecha en formato DD/MM/YYYY
        const diaStr = String(d).padStart(2, '0');
        const mesNumStr = String(m).padStart(2, '0');
        const fechaFormateadaPdf = `${diaStr}/${mesNumStr}/${y}`;

        // Calcular fecha estimada sumando meses
        const fechaCosecha = new Date(fechaIngresada);
        fechaCosecha.setMonth(fechaCosecha.getMonth() + objTiempo.MesesHastaCosecha);
        
        const strMes = mesesNombres[fechaCosecha.getMonth()];
        const strAnio = fechaCosecha.getFullYear();

        // 1. Meses hasta la cosecha
        document.getElementById('lblMesesHastaCosecha').innerText = `${objTiempo.MesesHastaCosecha} meses`;
        document.getElementById('lblRangoCosechaTexto').innerText = objTiempo.RangoCosechaTexto || '';

        // 2. Fecha estimada de cosecha
        document.getElementById('lblFechaEstimadaCosecha').innerText = `${strMes} de ${strAnio}`;

        // 3. Próxima actividad recomendada
        document.getElementById('lblProximaActividad').innerText = objTiempo.ProximaActividad;
        document.getElementById('lblFuenteBadge').innerText = `Fuente: ${objTiempo.Fuente} (${objTiempo.Confianza})`;

        // 4. Mensaje / Resumen de orientación
        let txtMensaje = objTiempo.DescripcionTemplate
            .replace('{fecha}', fechaFormateadaPdf)
            .replace('{mes}', strMes)
            .replace('{año}', strAnio);
        document.getElementById('textoCosechaMensaje').innerText = txtMensaje;

        // 5. Recomendación técnica según Cenicafé
        const recBox = document.getElementById('cajaRecomendacionClimatica');
        const lblMomento = document.getElementById('lblMomentoRecomendado');
        const lblCuando = document.getElementById('lblCuandoCae');
        const lblPorQue = document.getElementById('lblPorQue');
        const lblMensajeApp = document.getElementById('textoRecomendacionClimatica');

        const cenicafeData = obtenerDatosCenicafe(cultivo, actividad, m);

        if (cenicafeData) {
            recBox.classList.remove('hidden');
            lblMomento.innerText = cenicafeData.momento;
            lblCuando.innerText = cenicafeData.cuando;
            lblPorQue.innerText = cenicafeData.porque;
            lblMensajeApp.innerText = `"${cenicafeData.mensajeApp}"`;
        } else {
            recBox.classList.add('hidden');
        }

        document.getElementById('resultadoCosechaPlaceholder').classList.add('hidden');
        document.getElementById('resultadoCosecha').classList.remove('hidden');
    }

    function obtenerDatosCenicafe(cultivo, actividad, mes) {
        if (cultivo !== 'Café') {
            return null;
        }

        if (actividad === 'Abonado') {
            if (mes === 3 || mes === 4) {
                return {
                    momento: 'Inicio de lluvias del primer semestre',
                    cuando: 'Marzo – abril',
                    porque: 'Hay humedad para disolver el fertilizante y la planta se prepara para la mitaca',
                    mensajeApp: 'Es buena época para abonar: empiezan las lluvias y la planta aprovecha mejor los nutrientes.'
                };
            } else if (mes === 7 || mes === 8) {
                return {
                    momento: 'Antes de la cosecha principal',
                    cuando: 'Julio – agosto (2 meses antes de la maduración)',
                    porque: 'El fruto en sus últimos meses demanda muchos nutrientes',
                    mensajeApp: 'Abonar ahora ayuda al llenado del grano de la cosecha principal.'
                };
            } else if (mes === 9 || mes === 10) {
                return {
                    momento: 'Inicio de lluvias del segundo semestre',
                    cuando: 'Septiembre – octubre',
                    porque: 'Segundo período húmedo del año, asegura absorción',
                    mensajeApp: 'Buen momento para abonar: inician las lluvias del segundo semestre.'
                };
            } else if (mes === 11 || mes === 12 || mes === 5 || mes === 6) {
                return {
                    momento: 'Después de cada cosecha',
                    cuando: 'Tras recolección principal y tras la mitaca',
                    porque: 'Reponer los nutrientes que el árbol gastó produciendo',
                    mensajeApp: 'Después de cosechar conviene abonar para que la planta se recupere.'
                };
            } else {
                return {
                    momento: 'Período seco',
                    cuando: 'Enero – febrero',
                    porque: 'Abonar en seca desperdicia el fertilizante. La humedad es la que disuelve el fertilizante y permite que la planta lo absorba.',
                    mensajeApp: 'En época seca se desperdicia el fertilizante. Es mejor programar el abonado con el inicio de lluvias de marzo – abril.'
                };
            }
        } else if (actividad === 'Siembra') {
            return {
                momento: 'Al inicio de un período de lluvias',
                cuando: 'Marzo–abril o septiembre–octubre',
                porque: 'La planta joven necesita humedad constante para pegar bien y no marchitarse',
                mensajeApp: 'Se recomienda sembrar al empezar las lluvias, para que la matica tenga agua mientras se establece.'
            };
        } else if (actividad === 'Poda') {
            return {
                momento: 'Después de la cosecha principal',
                cuando: 'Diciembre – enero (al terminar de recoger)',
                porque: 'El árbol está descargado de fruto; se recupera mejor antes de las próximas lluvias',
                mensajeApp: 'La poda o zoca se hace mejor al terminar la cosecha, cuando el árbol está descansado.'
            };
        }

        return null;
    }

    // ─── Calculadora 2: Rentabilidad del Cultivo ─────────────────────────────
    document.querySelectorAll('.dinero-input').forEach(input => {
        input.addEventListener('input', function() {
            let val = this.value.replace(/\D/g, '');
            if (val === '') {
                this.value = '';
                return;
            }
            this.value = val.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
        });
    });

    function getMonto(id) {
        const el = document.getElementById(id);
        if (!el) return 0;
        const valStr = el.value.replace(/\./g, '').trim();
        const v = parseFloat(valStr);
        return isNaN(v) ? 0 : v;
    }

    function calcularRentabilidad(e) {
        e.preventDefault();
        const cultivo = document.getElementById('cultivoRentabilidad').value || 'café';
        const ingresoVenta = getMonto('ingresoVenta');
        
        if (ingresoVenta <= 0) {
            alert('Por favor ingresa el valor de ingreso por la venta.');
            return;
        }

        const gastoSiembra = getMonto('gastoSiembra');
        const gastoAbonado = getMonto('gastoAbonado');
        const gastoPoda = getMonto('gastoPoda');
        const gastoPlagas = getMonto('gastoPlagas');
        const gastoOtros = getMonto('gastoOtros');

        const gastoTotal = gastoSiembra + gastoAbonado + gastoPoda + gastoPlagas + gastoOtros;
        const balance = ingresoVenta - gastoTotal;

        const container = document.getElementById('resultadoRentabilidad');
        const titulo = document.getElementById('rentabilidadTitulo');
        const lblMensaje = document.getElementById('lblRentabilidadMensajeSencillo');
        const lblGastos = document.getElementById('lblGastosTotal');
        const lblBalance = document.getElementById('lblBalanceMonto');

        container.classList.remove('hidden', 'bg-emerald-600', 'bg-rose-600', 'bg-gray-700');

        lblGastos.innerText = formatearCOP(gastoTotal);
        lblBalance.innerText = (balance > 0 ? '+' : '') + formatearCOP(balance);

        const cultivoNombreLower = cultivo.toLowerCase();

        if (balance > 0) {
            container.classList.add('bg-emerald-600');
            titulo.innerText = 'Ganaste';
            lblMensaje.innerText = `En tu cultivo de ${cultivoNombreLower} gastaste ${formatearCOP(gastoTotal)} y recibiste ${formatearCOP(ingresoVenta)}. Te quedó una ganancia de ${formatearCOP(balance)}.`;
        } else if (balance < 0) {
            container.classList.add('bg-rose-600');
            titulo.innerText = 'Perdiste';
            lblMensaje.innerText = `En tu cultivo de ${cultivoNombreLower} gastaste ${formatearCOP(gastoTotal)} y recibiste ${formatearCOP(ingresoVenta)}. Tuviste una pérdida de ${formatearCOP(Math.abs(balance))}.`;
        } else {
            container.classList.add('bg-gray-700');
            titulo.innerText = 'Quedaste igual';
            lblMensaje.innerText = `En tu cultivo de ${cultivoNombreLower} gastaste ${formatearCOP(gastoTotal)} y recibiste ${formatearCOP(ingresoVenta)}. Quedaste igual.`;
        }
    }

    // ─── Calculadora 3: Conversión de Café ───────────────────────────────────
    function calcularConversion(e) {
        e.preventDefault();
        const cereza = parseFloat(document.getElementById('kilosCereza').value);
        if (isNaN(cereza) || cereza <= 0) {
            alert('Por favor ingresa los kilos de café en cereza cosechados.');
            return;
        }

        const pergaminoSeco = cereza / 5.0;
        const cafeVerde = pergaminoSeco * 0.8;

        document.getElementById('lblPergamino').innerText = Number.isInteger(pergaminoSeco) ? pergaminoSeco : pergaminoSeco.toFixed(1);
        document.getElementById('lblVerde').innerText = Number.isInteger(cafeVerde) ? cafeVerde : cafeVerde.toFixed(1);

        document.getElementById('lblComoSeObtuvoPergamino').innerText = `${cereza} ÷ 5`;
        document.getElementById('lblComoSeObtuvoVerde').innerText = `${Number.isInteger(pergaminoSeco) ? pergaminoSeco : pergaminoSeco.toFixed(1)} × 0,8`;

        document.getElementById('resultadoConversionPlaceholder').classList.add('hidden');
        document.getElementById('resultadoConversion').classList.remove('hidden');
    }