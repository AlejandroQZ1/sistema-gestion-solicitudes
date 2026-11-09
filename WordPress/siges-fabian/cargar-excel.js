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

    const archivo = document.createElement('input');
    archivo.type = 'file';
    archivo.accept = '.xlsx,.xls';
    archivo.hidden = true;
    document.body.appendChild(archivo);

    document.addEventListener('siges:cargar-excel', function () {
        archivo.value = '';
        archivo.click();
    });

    archivo.addEventListener('change', async function () {
        if (!archivo.files.length) return;
        if (!window.XLSX || !window.SigesPropuesta) {
            mensaje('Carga no disponible', 'No se pudo preparar la lectura del Excel.');
            return;
        }

        try {
            const datos = await archivo.files[0].arrayBuffer();
            const libro = XLSX.read(datos);
            const hoja = libro.Sheets[libro.SheetNames[0]];
            const lineas = leerLineas(hoja);
            window.SigesPropuesta.agregarLineas(lineas, true);
            mensaje('Excel cargado', 'Se cargaron ' + lineas.length + ' líneas. Revise los datos antes de confirmar la propuesta.');
        } catch (error) {
            mensaje('No se pudo cargar el Excel', error.message || 'Revise el archivo e intente de nuevo.');
        }
    });
})();
