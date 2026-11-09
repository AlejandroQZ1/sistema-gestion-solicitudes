(function () {
    const nombres = [
        'mes', 'año', 'horas', 'monto', 'iva', 'total',
        'observaciones', 'porcentaje de cobro'
    ];

    function texto(valor) {
        return String(valor === undefined || valor === null ? '' : valor).trim();
    }

    function numero(valor) {
        if (typeof valor === 'number') return valor;
        const escrito = texto(valor).replace(',', '.');
        return escrito !== '' ? Number(escrito) : NaN;
    }

    function redondear(valor) {
        return Math.round((valor + Number.EPSILON) * 100) / 100;
    }

    function mensaje(titulo, detalle) {
        const ventana = document.getElementById('siges-modal');
        if (!ventana || !window.bootstrap) {
            window.alert(titulo + '\n\n' + detalle);
            return;
        }
        ventana.querySelector('.modal-title').textContent = titulo;
        ventana.querySelector('.modal-body').textContent = detalle;
        window.bootstrap.Modal.getOrCreateInstance(ventana).show();
    }

    function leerLineas(hoja) {
        const filas = XLSX.utils.sheet_to_json(hoja, { header: 1, defval: '', raw: true });
        if (filas.length < 2) throw new Error('El Excel debe tener encabezados y al menos una línea.');

        const encabezados = filas[0].map(valor => texto(valor).toLowerCase());
        if (nombres.some((nombre, indice) => encabezados[indice] !== nombre)) {
            throw new Error('Las columnas deben estar en este orden: Mes, Año, Horas, Monto, IVA, Total, Observaciones y Porcentaje de cobro.');
        }

        const lineas = [];
        for (let indice = 1; indice < filas.length; indice++) {
            const fila = filas[indice];
            if (fila.every(valor => texto(valor) === '')) continue;

            const [mes, anio, horas, monto, iva, total, observaciones, porcentaje] = fila;
            const datos = [mes, anio, horas, monto, iva, total, observaciones, porcentaje];
            if (datos.some(valor => texto(valor) === '')) {
                throw new Error('Fila ' + (indice + 1) + ': todos los datos son requeridos.');
            }

            const m = numero(mes);
            const a = numero(anio);
            const h = numero(horas);
            const valor = numero(monto);
            const impuesto = numero(iva);
            const suma = numero(total);
            const cobro = numero(porcentaje);
            const valido = Number.isInteger(m) && m >= 1 && m <= 12
                && Number.isInteger(a) && a >= 2000 && a <= 2100
                && Number.isFinite(h) && h > 0
                && Number.isFinite(valor) && valor > 0
                && Number.isFinite(impuesto) && Number.isFinite(suma)
                && Number.isFinite(cobro) && cobro > 0 && cobro <= 100;

            if (!valido) throw new Error('Fila ' + (indice + 1) + ': revise los valores numéricos.');

            const ivaEsperado = redondear(valor * 0.13);
            if (Math.abs(impuesto - ivaEsperado) > 0.01 || Math.abs(suma - redondear(valor + ivaEsperado)) > 0.01) {
                throw new Error('Fila ' + (indice + 1) + ': el IVA o el total no coincide con el monto.');
            }

            lineas.push({
                mes: m, anio: a, horas: h, monto: valor,
                observaciones: texto(observaciones), porcentaje: cobro
            });
        }

        if (lineas.length === 0) throw new Error('El Excel no contiene líneas de propuesta.');
        return lineas;
    }

    const paginaCarga = document.getElementById('siges-archivo-excel');

    if (paginaCarga) {
        const retorno = sessionStorage.getItem('siges_excel_retorno');
        const cancelar = document.getElementById('siges-cancelar-excel');
        cancelar.href = retorno || window.location.href;

        document.getElementById('siges-confirmar-excel').addEventListener('click', async function () {
            if (!retorno || !paginaCarga.files.length || !window.XLSX) {
                document.getElementById('siges-error-excel').textContent =
                    'Seleccione un archivo Excel desde el formulario de participación.';
                return;
            }

            try {
                const datos = await paginaCarga.files[0].arrayBuffer();
                const libro = XLSX.read(datos);
                const lineas = leerLineas(libro.Sheets[libro.SheetNames[0]]);
                sessionStorage.setItem('siges_excel_lineas', JSON.stringify(lineas));
                window.location.assign(retorno);
            } catch (error) {
                document.getElementById('siges-error-excel').textContent =
                    error.message || 'Revise el archivo e intente de nuevo.';
            }
        });
    } else {
        document.addEventListener('siges:cargar-excel', function () {
            if (!window.sigesPaginaExcel) {
                mensaje('Carga no disponible', 'No se encontró la página Cargar Excel.');
                return;
            }

            const formulario = document.getElementById('siges-form-ext4');
            const campos = {};
            for (const campo of new FormData(formulario).entries()) {
                if (!campo[0].startsWith('siges_lineas[') && campo[0] !== 'siges_nonce') {
                    campos[campo[0]] = campo[1];
                }
            }
            sessionStorage.setItem('siges_excel_campos', JSON.stringify(campos));
            sessionStorage.setItem('siges_excel_retorno', window.location.href);
            window.location.assign(window.sigesPaginaExcel);
        });

        const camposGuardados = sessionStorage.getItem('siges_excel_campos');
        const lineasGuardadas = sessionStorage.getItem('siges_excel_lineas');
        if (camposGuardados) {
            const formulario = document.getElementById('siges-form-ext4');
            const campos = JSON.parse(camposGuardados);
            for (const [nombre, valor] of Object.entries(campos)) {
                const campo = formulario.elements.namedItem(nombre);
                if (campo) campo.value = valor;
            }
            sessionStorage.removeItem('siges_excel_campos');
        }
        if (lineasGuardadas && window.SigesPropuesta) {
            window.SigesPropuesta.agregarLineas(JSON.parse(lineasGuardadas), true);
            sessionStorage.removeItem('siges_excel_lineas');
            mensaje('Excel cargado', 'Revise las líneas antes de confirmar la propuesta.');
        }
    }
})();
