(function () {
    const nombres = ['mes', 'año', 'horas', 'monto', 'iva', 'total', 'observaciones', 'porcentaje de cobro'];

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
        const ventana = document.getElementById('siges-fabian-modal') || document.getElementById('siges-modal');
        if (!ventana || !window.bootstrap) {
            window.alert(titulo + '\n\n' + detalle);
            return;
        }
        ventana.querySelector('.modal-title').textContent = titulo;
        ventana.querySelector('.modal-body').textContent = detalle;
        window.bootstrap.Modal.getOrCreateInstance(ventana).show();
    }

    // Las columnas se ubican por su encabezado para admitir cualquier orden en el archivo.
    function leerLineas(hoja) {
        const filas = XLSX.utils.sheet_to_json(hoja, { header: 1, defval: '', raw: true });
        if (filas.length < 2) throw new Error('El Excel debe tener encabezados y al menos una línea.');
        const encabezados = filas[0].map(valor => texto(valor).toLowerCase());
        const posiciones = nombres.map(nombre => encabezados.indexOf(nombre));
        if (posiciones.includes(-1) || nombres.some(nombre => encabezados.filter(valor => valor === nombre).length !== 1)) {
            throw new Error('El Excel debe contener una columna de Mes, Año, Horas, Monto, IVA, Total, Observaciones y Porcentaje de cobro.');
        }

        const lineas = [];
        for (let indice = 1; indice < filas.length; indice++) {
            const fila = filas[indice];
            if (fila.every(valor => texto(valor) === '')) continue;
            const datos = posiciones.map(posicion => fila[posicion]);
            if (datos.some(valor => texto(valor) === '')) {
                throw new Error('Fila ' + (indice + 1) + ': todos los datos son requeridos.');
            }
            const [mes, anio, horas, monto, iva, total, observaciones, porcentaje] = datos;
            const m = numero(mes);
            const a = numero(anio);
            const h = numero(horas);
            const valor = numero(monto);
            const impuesto = numero(iva);
            const suma = numero(total);
            const cobro = numero(porcentaje);
            const valido = Number.isInteger(m) && m >= 1 && m <= 12
                && Number.isInteger(a) && a > 0
                && Number.isFinite(h) && h > 0
                && Number.isFinite(valor) && valor > 0
                && Number.isFinite(impuesto) && Number.isFinite(suma)
                && Number.isFinite(cobro) && cobro > 0 && cobro <= 100;
            if (!valido) throw new Error('Fila ' + (indice + 1) + ': revise los valores numéricos.');
            const ivaEsperado = redondear(valor * 0.13);
            if (Math.abs(impuesto - ivaEsperado) > 0.01 || Math.abs(suma - redondear(valor + ivaEsperado)) > 0.01) {
                throw new Error('Fila ' + (indice + 1) + ': el IVA o el total no coincide con el monto.');
            }
            lineas.push({mes: m, anio: a, horas: h, monto: valor, observaciones: texto(observaciones), porcentaje: cobro});
        }
        if (lineas.length === 0) throw new Error('El Excel no contiene líneas de propuesta.');
        return lineas;
    }

    const dialogo = document.getElementById('siges-fabian-modal');
    if (dialogo && dialogo.dataset.mensaje) mensaje('Participaciones', dialogo.dataset.mensaje);
    const consulta = document.getElementById('siges-consultar-participaciones');
    if (consulta) {
        consulta.addEventListener('submit', function (evento) {
            if (!consulta.checkValidity()) {
                evento.preventDefault();
                mensaje('Participaciones', 'La cédula jurídica debe tener el formato 3-XXX-XXXXXX.');
            }
        });
    }

    const archivo = document.getElementById('siges-archivo-excel');
    if (archivo) {
        document.getElementById('siges-confirmar-excel').addEventListener('click', async function () {
            try {
                const borrador = JSON.parse(sessionStorage.getItem('siges_excel_borrador'));
                if (!borrador) throw new Error('Ingrese desde la opción Cargar Excel de la propuesta.');
                if (!archivo.files.length) throw new Error('Seleccione un archivo Excel.');
                if (!/\.(xlsx|xls)$/i.test(archivo.files[0].name)) throw new Error('Seleccione un archivo Excel con extensión .xlsx o .xls.');
                if (!window.XLSX) throw new Error('No se pudo cargar el lector de Excel. Recargue la página.');
                const datos = await archivo.files[0].arrayBuffer();
                const libro = XLSX.read(datos);
                const lineas = leerLineas(libro.Sheets[libro.SheetNames[0]]);
                sessionStorage.setItem('siges_excel_lineas', JSON.stringify(lineas));
                window.location.assign(borrador.url);
            } catch (error) {
                mensaje('Carga de Excel', error.message || 'Revise el archivo e intente de nuevo.');
            }
        });
    }

    const formulario = document.getElementById('siges-form-ext4');
    if (!formulario) return;

    // El borrador conserva los campos y las líneas mientras se abre la pantalla de carga.
    document.addEventListener('siges:cargar-excel', function () {
        if (!window.sigesPaginaExcel) {
            mensaje('Carga de Excel', 'No se encontró la página Cargar Excel.');
            return;
        }
        try {
            const campos = {};
            for (const [nombre, valor] of new FormData(formulario).entries()) {
                if (!nombre.startsWith('siges_lineas[') && nombre !== 'siges_nonce') campos[nombre] = valor;
            }
            const lineas = [];
            formulario.querySelectorAll('#siges-lineas tr').forEach(fila => {
                const linea = {};
                fila.querySelectorAll('[name]').forEach(campo => {
                    const nombre = campo.name.match(/\[([^\]]+)\]$/)[1];
                    linea[nombre] = campo.value;
                });
                lineas.push(linea);
            });
            sessionStorage.setItem('siges_excel_borrador', JSON.stringify({url: window.location.href, campos, lineas}));
            sessionStorage.removeItem('siges_excel_lineas');
            window.location.assign(window.sigesPaginaExcel);
        } catch (error) {
            mensaje('Carga de Excel', 'No se pudieron conservar los datos de la propuesta en el navegador.');
        }
    });

    // Solo se restaura en la solicitud de origen. Un Excel válido reemplaza el desglose del borrador.
    function restaurarPropuesta() {
        try {
            const borrador = JSON.parse(sessionStorage.getItem('siges_excel_borrador'));
            if (!borrador || borrador.url !== window.location.href || !window.SigesPropuesta) return;
            for (const [nombre, valor] of Object.entries(borrador.campos)) {
                const campo = formulario.elements.namedItem(nombre);
                if (campo) campo.value = valor;
            }
            const cargadas = sessionStorage.getItem('siges_excel_lineas');
            window.SigesPropuesta.agregarLineas(cargadas ? JSON.parse(cargadas) : borrador.lineas, true);
            // La consulta del proveedor actualiza los campos de contacto y su obligatoriedad.
            document.getElementById('siges_cedula').dispatchEvent(new Event('blur'));
            sessionStorage.removeItem('siges_excel_borrador');
            sessionStorage.removeItem('siges_excel_lineas');
        } catch (error) {
            mensaje('Carga de Excel', 'No se pudieron recuperar los datos de la propuesta.');
        }
    }
    restaurarPropuesta();
    window.addEventListener('pageshow', function (evento) {
        if (evento.persisted) restaurarPropuesta();
    });
})();
