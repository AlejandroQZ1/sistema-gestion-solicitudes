<?php
/**
 * Plugin Name: SIGES - Módulo Externo
 * Description: Módulo externo de SIGES para proveedores. Incluye EXT4 (registrar propuesta) con el shortcode [siges_participar].
 * Version:     0.3.0
 * Author:      Equipo SIGES - CUC
 *
 * ---------------------------------------------------------------------------
 * CONFIGURACIÓN: agregar en wp-config.php (antes de "That's all, stop editing"):
 *
 *   define('SIGES_DB_HOST', 'localhost');
 *   define('SIGES_DB_NAME', 'nombre_bd_siges');
 *   define('SIGES_DB_USER', 'usuario');
 *   define('SIGES_DB_PASS', 'contraseña');
 *
 * USO: crear una página "Participar" con el shortcode:
 *   [siges_participar detalle_url="/detalle-solicitud"]
 * La página se abre como /participar?solicitud=ID  (desde el botón "Participar" de EXT3).
 *
 * TABLAS (base de datos tiusr22pl_siges):
 *   solicitudes(SolicitudID, ConsecutivoOficio, Titulo, ..., Publicado, FechaVencimientoPublicacion)
 *   proveedores(ProveedorID, CedulaJuridica, Nombre, NombreRepresentante, Telefono, Correo)
 *   propuestas(PropuestaID, SolicitudID, ProveedorID, FechaPropuesta, Aprobada)
 *   desgloses_propuesta(DesglosePropuestaID, PropuestaID, Mes, Anio, Horas, Monto,
 *                       IVA y Total = columnas calculadas por la BD, Observaciones, PorcentajeCobro)
 *   bitacora(BitacoraID, Fecha, Usuario, Modulo, Accion, Detalle)
 * ---------------------------------------------------------------------------
 */

if (!defined('ABSPATH')) {
    exit;
}

/* ============================================================
 * Utilidades compartidas (las demás HU pueden reutilizarlas)
 * ============================================================ */

/** Conexión a la base de datos de SIGES (distinta a la de WordPress). */
function siges_db()
{
    static $db = null;
    if ($db === null) {
        $db = new wpdb(SIGES_DB_USER, SIGES_DB_PASS, SIGES_DB_NAME, SIGES_DB_HOST);
        $db->show_errors(false);
        $db->suppress_errors(true);
    }
    return $db;
}

/** true si la conexión a la BD de SIGES funciona (wpdb falla en silencio si no). */
function siges_db_conectada()
{
    if (!defined('SIGES_DB_HOST')) {
        return false;
    }
    $db = siges_db();
    return !empty($db->dbh) && $db->get_var('SELECT 1') === '1';
}

/** Valida el formato de cédula jurídica de Costa Rica: 3-XXX-XXXXXX. */
function siges_validar_cedula($cedula)
{
    return (bool) preg_match('/^3-\d{3}-\d{6}$/', $cedula);
}

/** Registra en la bitácora de SIGES (acción + JSON). Nunca rompe el flujo si falla. */
function siges_bitacora($usuario, $modulo, $accion, $datos = null)
{
    $detalle = $datos !== null ? wp_json_encode($datos, JSON_UNESCAPED_UNICODE) : null;
    $ok = siges_db()->insert('bitacora', [
        'Fecha'   => current_time('mysql'),
        'Usuario' => mb_substr($usuario, 0, 50),
        'Modulo'  => mb_substr($modulo, 0, 50),
        'Accion'  => mb_substr($accion, 0, 50),
        'Detalle' => $detalle,
    ]);
    if (!$ok) {
        error_log("SIGES bitácora no registrada: $modulo / $accion $detalle");
    }
}

/** Devuelve la solicitud si está publicada y vigente; si no, null. */
function siges_solicitud_publicada($id)
{
    if (!$id) {
        return null;
    }
    $db = siges_db();
    return $db->get_row($db->prepare(
        "SELECT SolicitudID AS id, ConsecutivoOficio AS consecutivo_oficio, Titulo AS titulo
           FROM solicitudes
          WHERE SolicitudID = %d
            AND Publicado = 1
            AND FechaVencimientoPublicacion >= CURDATE()",
        $id
    ));
}

/** Carga Bootstrap 5 solo en páginas que usan shortcodes de SIGES. */
add_action('wp_enqueue_scripts', function () {
    global $post;
    if (!$post) {
        return;
    }
    foreach (['siges_publicaciones', 'siges_detalle', 'siges_participar', 'siges_participaciones'] as $sc) {
        if (has_shortcode($post->post_content, $sc)) {
            wp_enqueue_style('siges-bootstrap', 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css', [], '5.3.3');
            wp_enqueue_script('siges-bootstrap', 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js', [], '5.3.3', true);
            return;
        }
    }
});

/* ============================================================
 * EXT4 - Registrar propuesta  (Joel Barquero)
 * ============================================================ */

/** Errores y datos enviados, para volver a pintar el formulario si algo falla. */
$GLOBALS['siges_ext4_estado'] = ['errores' => [], 'post' => []];

/**
 * El POST se procesa antes de pintar la página para poder redirigir
 * (patrón POST-Redirect-GET: evita que se duplique la propuesta al refrescar).
 */
add_action('template_redirect', function () {
    if ($_SERVER['REQUEST_METHOD'] !== 'POST' || ($_POST['siges_accion'] ?? '') !== 'ext4_confirmar') {
        return;
    }

    $solicitud_id = absint($_GET['solicitud'] ?? 0);
    $resultado    = siges_ext4_guardar($solicitud_id);

    if ($resultado === true) {
        wp_safe_redirect(add_query_arg(['solicitud' => $solicitud_id, 'enviado' => 1], get_permalink()));
        exit;
    }

    $GLOBALS['siges_ext4_estado'] = ['errores' => $resultado, 'post' => wp_unslash($_POST)];
});

/**
 * Valida y guarda la propuesta.
 * @return true|string[]  true si se guardó; si no, la lista de errores.
 */
function siges_ext4_guardar($solicitud_id)
{
    if (!isset($_POST['siges_nonce']) || !wp_verify_nonce($_POST['siges_nonce'], 'siges_ext4')) {
        return ['La página expiró. Recárguela e intente de nuevo.'];
    }

    $sol = siges_solicitud_publicada($solicitud_id);
    if (!$sol) {
        return ['La solicitud no existe o ya no está publicada.'];
    }

    $p       = wp_unslash($_POST);
    $errores = [];

    // --- Proveedor -------------------------------------------------------
    $cedula = sanitize_text_field($p['siges_cedula'] ?? '');
    $nombre = sanitize_text_field($p['siges_nombre'] ?? '');

    if ($cedula === '') {
        $errores[] = 'La cédula jurídica es requerida.';
    } elseif (!siges_validar_cedula($cedula)) {
        $errores[] = 'La cédula jurídica debe tener el formato 3-XXX-XXXXXX.';
    }
    if ($nombre === '') {
        $errores[] = 'El nombre del proveedor es requerido.';
    }

    $db        = siges_db();
    $proveedor = $cedula !== '' ? $db->get_row($db->prepare(
        'SELECT ProveedorID AS id, Nombre AS nombre FROM proveedores WHERE CedulaJuridica = %s',
        $cedula
    )) : null;

    $nuevo = null;
    if (!$proveedor) {
        // Proveedor nuevo: se piden representante, teléfono y correo.
        $rep    = sanitize_text_field($p['siges_representante'] ?? '');
        $tel    = sanitize_text_field($p['siges_telefono'] ?? '');
        $correo = sanitize_email($p['siges_correo'] ?? '');

        if ($rep === '') {
            $errores[] = 'El nombre del representante es requerido.';
        }
        if (!preg_match('/^\d{4}-?\d{4}$/', $tel)) {
            $errores[] = 'El teléfono debe tener 8 dígitos (ej. 8888-8888).';
        }
        if (!is_email($correo)) {
            $errores[] = 'El correo electrónico no es válido.';
        }
        $nuevo = [
            'CedulaJuridica'      => $cedula,
            'Nombre'              => $nombre,
            'NombreRepresentante' => $rep,
            'Telefono'            => $tel,
            'Correo'              => $correo,
        ];
    }

    // --- Líneas de desglose ----------------------------------------------
    $lineas_post = isset($p['siges_lineas']) && is_array($p['siges_lineas']) ? array_values($p['siges_lineas']) : [];
    $lineas      = [];

    if (count($lineas_post) === 0) {
        $errores[] = 'Debe agregar al menos una línea al detalle de la propuesta.';
    }

    foreach ($lineas_post as $i => $l) {
        $n     = $i + 1;
        $mes   = filter_var($l['mes'] ?? '', FILTER_VALIDATE_INT);
        $anio  = filter_var($l['anio'] ?? '', FILTER_VALIDATE_INT);
        $horas = $l['horas'] ?? '';
        $monto = $l['monto'] ?? '';
        $obs   = sanitize_text_field($l['observaciones'] ?? '');
        $pct   = $l['porcentaje'] ?? '';

        if ($mes === false || $mes < 1 || $mes > 12) {
            $errores[] = "Línea $n: el mes es requerido (1 a 12).";
        }
        if ($anio === false || $anio < 2000 || $anio > 2100) {
            $errores[] = "Línea $n: el año no es válido.";
        }
        if (!is_numeric($horas) || $horas <= 0) {
            $errores[] = "Línea $n: las horas deben ser mayores a 0.";
        }
        if (!is_numeric($monto) || $monto <= 0) {
            $errores[] = "Línea $n: el monto debe ser mayor a 0.";
        }
        if ($obs === '') {
            $errores[] = "Línea $n: las observaciones son requeridas.";
        }
        if (!is_numeric($pct) || $pct <= 0 || $pct > 100) {
            $errores[] = "Línea $n: el porcentaje de cobro debe estar entre 0 y 100.";
        }

        // IVA y Total NO se envían: la BD los calcula (columnas generadas, igual que en "desgloses").
        $lineas[] = [
            'Mes'             => (int) $mes,
            'Anio'            => (int) $anio,
            'Horas'           => round((float) $horas, 2),
            'Monto'           => round((float) $monto, 2),
            'Observaciones'   => $obs,
            'PorcentajeCobro' => round((float) $pct, 2),
        ];
    }

    if ($errores) {
        return $errores;
    }

    // --- Guardar todo en una transacción --------------------------------
    $usuario = 'externo:' . $cedula;
    $db->query('START TRANSACTION');
    try {
        if ($proveedor) {
            $proveedor_id = (int) $proveedor->id;
        } else {
            if (!$db->insert('proveedores', $nuevo)) {
                throw new Exception('Insert proveedores: ' . $db->last_error);
            }
            $proveedor_id = (int) $db->insert_id;
            siges_bitacora($usuario, 'Proveedores', 'Crear', ['ProveedorID' => $proveedor_id] + $nuevo);
        }

        $propuesta = [
            'SolicitudID'    => (int) $sol->id,
            'ProveedorID'    => $proveedor_id,
            'FechaPropuesta' => current_time('mysql'),
            'Aprobada'       => 0,
        ];
        if (!$db->insert('propuestas', $propuesta)) {
            throw new Exception('Insert propuestas: ' . $db->last_error);
        }
        $propuesta_id = (int) $db->insert_id;

        // Se guarda en desgloses_propuesta, separado de los desgloses en firme de la solicitud.
        foreach ($lineas as $linea) {
            if (!$db->insert('desgloses_propuesta', ['PropuestaID' => $propuesta_id] + $linea)) {
                throw new Exception('Insert desgloses_propuesta: ' . $db->last_error);
            }
        }

        $db->query('COMMIT');
        siges_bitacora($usuario, 'Propuestas', 'Crear', ['PropuestaID' => $propuesta_id] + $propuesta + ['Desglose' => $lineas]);
        return true;
    } catch (Exception $e) {
        $db->query('ROLLBACK');
        siges_bitacora($usuario, 'Propuestas', 'Error', ['error' => $e->getMessage()]);
        return ['Ocurrió un error al registrar la propuesta. Intente de nuevo más tarde.'];
    }
}

/** AJAX: al escribir la cédula, indica si el proveedor ya existe. */
add_action('wp_ajax_siges_buscar_proveedor', 'siges_ajax_buscar_proveedor');
add_action('wp_ajax_nopriv_siges_buscar_proveedor', 'siges_ajax_buscar_proveedor');
function siges_ajax_buscar_proveedor()
{
    check_ajax_referer('siges_ext4', 'nonce');
    $cedula = sanitize_text_field(wp_unslash($_GET['cedula'] ?? ''));
    if (!siges_validar_cedula($cedula)) {
        wp_send_json_error(['mensaje' => 'Formato inválido']);
    }
    $db = siges_db();
    $p  = $db->get_row($db->prepare('SELECT Nombre FROM proveedores WHERE CedulaJuridica = %s', $cedula));
    wp_send_json_success(['existe' => (bool) $p, 'nombre' => $p ? $p->Nombre : '']);
}

/** Shortcode [siges_participar detalle_url="/detalle-solicitud"] */
add_shortcode('siges_participar', 'siges_ext4_shortcode');
function siges_ext4_shortcode($atts)
{
    $atts         = shortcode_atts(['detalle_url' => '/detalle-solicitud'], $atts);
    if (!siges_db_conectada()) {
        return '<div class="alert alert-danger">No se pudo conectar a la base de datos de SIGES. Revise los datos SIGES_DB_* en wp-config.php y el acceso remoto.</div>';
    }
    $solicitud_id = absint($_GET['solicitud'] ?? 0);
    $url_detalle  = esc_url(add_query_arg('solicitud', $solicitud_id, home_url($atts['detalle_url'])));
    $sol          = siges_solicitud_publicada($solicitud_id);

    if (!$sol) {
        return '<div class="alert alert-warning">La solicitud no existe o ya no está publicada.</div>';
    }

    $estado  = $GLOBALS['siges_ext4_estado'];
    $errores = $estado['errores'];
    $old     = $estado['post'];
    $enviado = isset($_GET['enviado']);
    $v       = function ($k) use ($old) {
        return esc_attr($old[$k] ?? '');
    };
    $lineas_previas = isset($old['siges_lineas']) && is_array($old['siges_lineas']) ? array_values($old['siges_lineas']) : [];

    ob_start(); ?>
<div class="siges-ext4 alignwide container-fluid px-0">

    <h3 class="mb-1">Participar en la solicitud</h3>
    <p class="text-muted mb-4">
        <strong><?php echo esc_html($sol->consecutivo_oficio); ?></strong> · <?php echo esc_html($sol->titulo); ?>
    </p>

    <form method="post" id="siges-form-ext4" novalidate>
        <input type="hidden" name="siges_accion" value="ext4_confirmar">
        <?php wp_nonce_field('siges_ext4', 'siges_nonce'); ?>

        <div class="card mb-4">
            <div class="card-header fw-semibold">Datos del proveedor</div>
            <div class="card-body row g-3">
                <div class="col-md-4">
                    <label class="form-label" for="siges_cedula">Cédula jurídica</label>
                    <input type="text" class="form-control" id="siges_cedula" name="siges_cedula"
                           placeholder="3-101-123456" maxlength="12" required value="<?php echo $v('siges_cedula'); ?>">
                    <div class="invalid-feedback">Formato requerido: 3-XXX-XXXXXX.</div>
                    <div class="form-text" id="siges_cedula_estado"></div>
                </div>
                <div class="col-md-8">
                    <label class="form-label" for="siges_nombre">Nombre del proveedor</label>
                    <input type="text" class="form-control" id="siges_nombre" name="siges_nombre"
                           maxlength="150" required value="<?php echo $v('siges_nombre'); ?>">
                    <div class="invalid-feedback">El nombre es requerido.</div>
                </div>

                <!-- Solo se muestra si el proveedor NO existe -->
                <div id="siges-nuevo-proveedor" class="row g-3 m-0 p-0 d-none">
                    <div class="col-12"><div class="alert alert-info py-2 mb-0">Proveedor nuevo: complete los datos de contacto.</div></div>
                    <div class="col-md-5">
                        <label class="form-label" for="siges_representante">Nombre del representante</label>
                        <input type="text" class="form-control" id="siges_representante" name="siges_representante"
                               maxlength="150" value="<?php echo $v('siges_representante'); ?>">
                        <div class="invalid-feedback">El representante es requerido.</div>
                    </div>
                    <div class="col-md-3">
                        <label class="form-label" for="siges_telefono">Teléfono</label>
                        <input type="tel" class="form-control" id="siges_telefono" name="siges_telefono"
                               placeholder="8888-8888" maxlength="9" pattern="\d{4}-?\d{4}" value="<?php echo $v('siges_telefono'); ?>">
                        <div class="invalid-feedback">8 dígitos.</div>
                    </div>
                    <div class="col-md-4">
                        <label class="form-label" for="siges_correo">Correo electrónico</label>
                        <input type="email" class="form-control" id="siges_correo" name="siges_correo"
                               maxlength="150" value="<?php echo $v('siges_correo'); ?>">
                        <div class="invalid-feedback">Correo inválido.</div>
                    </div>
                </div>
            </div>
        </div>

        <div class="card mb-4">
            <div class="card-header d-flex justify-content-between align-items-center flex-wrap gap-2">
                <span class="fw-semibold">Detalle de la propuesta</span>
                <div class="d-flex gap-2">
                    <!-- EXT5 (Fabián) escucha el evento "siges:cargar-excel" y llama a SigesPropuesta.agregarLineas() -->
                    <button type="button" class="btn btn-outline-success btn-sm" id="siges-btn-excel">Cargar Excel</button>
                    <button type="button" class="btn btn-outline-primary btn-sm" id="siges-btn-agregar">+ Agregar línea</button>
                </div>
            </div>
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-sm align-middle mb-0">
                        <thead class="table-light">
                            <tr>
                                <th style="min-width:120px">Mes</th>
                                <th style="min-width:90px">Año</th>
                                <th style="min-width:90px">Horas</th>
                                <th style="min-width:120px">Monto</th>
                                <th style="min-width:110px">IVA (13%)</th>
                                <th style="min-width:120px">Total</th>
                                <th style="min-width:180px">Observaciones</th>
                                <th style="min-width:100px">% cobro</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody id="siges-lineas"></tbody>
                        <tfoot class="table-light fw-semibold">
                            <tr>
                                <td colspan="3" class="text-end">Totales</td>
                                <td id="siges-sum-monto">0.00</td>
                                <td id="siges-sum-iva">0.00</td>
                                <td id="siges-sum-total">0.00</td>
                                <td colspan="3"></td>
                            </tr>
                        </tfoot>
                    </table>
                </div>
                <div id="siges-sin-lineas" class="text-center text-muted py-3">Agregue al menos una línea o cargue un Excel.</div>
            </div>
        </div>

        <div class="d-flex gap-2 justify-content-end">
            <a href="<?php echo $url_detalle; ?>" class="btn btn-secondary">Cancelar</a>
            <button type="submit" class="btn btn-primary">Confirmar</button>
        </div>
    </form>

    <!-- Modal de mensajes (requisito: mensajes como diálogo Bootstrap) -->
    <div class="modal fade" id="siges-modal" tabindex="-1" aria-hidden="true">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="siges-modal-titulo"></h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>
                <div class="modal-body" id="siges-modal-cuerpo"></div>
                <div class="modal-footer" id="siges-modal-pie">
                    <button type="button" class="btn btn-primary" data-bs-dismiss="modal">Aceptar</button>
                </div>
            </div>
        </div>
    </div>
</div>

<script>
(function () {
    const MESES = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];
    const cfg = <?php echo wp_json_encode([
        'ajax'      => admin_url('admin-ajax.php'),
        'nonce'     => wp_create_nonce('siges_ext4'),
        'detalle'   => $url_detalle,
        'errores'   => $errores,
        'enviado'   => $enviado,
        'previas'   => $lineas_previas,
        'existe'    => null,
    ]); ?>;

    const form    = document.getElementById('siges-form-ext4');
    const tbody   = document.getElementById('siges-lineas');
    const vacio   = document.getElementById('siges-sin-lineas');
    const cedula  = document.getElementById('siges_cedula');
    const nombre  = document.getElementById('siges_nombre');
    const bloqueNuevo = document.getElementById('siges-nuevo-proveedor');
    const camposNuevo = ['siges_representante', 'siges_telefono', 'siges_correo'].map(id => document.getElementById(id));
    const reCedula = /^3-\d{3}-\d{6}$/;
    let indice = 0;

    const r2  = n => Math.round((n + Number.EPSILON) * 100) / 100;
    const fmt = n => r2(n).toLocaleString('es-CR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    /* ---------- Modal ---------- */
    function modal(titulo, html, pieHtml) {
        document.getElementById('siges-modal-titulo').textContent = titulo;
        document.getElementById('siges-modal-cuerpo').innerHTML = html;
        if (pieHtml) document.getElementById('siges-modal-pie').innerHTML = pieHtml;
        if (window.bootstrap && bootstrap.Modal) {
            bootstrap.Modal.getOrCreateInstance(document.getElementById('siges-modal')).show();
        } else {
            // Respaldo si Bootstrap no cargó: mensaje simple para no perder la información.
            window.alert(titulo + '\n\n' + document.getElementById('siges-modal-cuerpo').innerText);
        }
    }
    const esc = s => String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

    /* ---------- Líneas de desglose ---------- */
    function agregarFila(d) {
        d = d || {};
        const i  = indice++;
        const tr = document.createElement('tr');
        const opcionesMes = MESES.map((m, k) => `<option value="${k + 1}">${m}</option>`).join('');
        tr.innerHTML = `
            <td><select class="form-select form-select-sm" name="siges_lineas[${i}][mes]" required>
                    <option value="">--</option>${opcionesMes}</select></td>
            <td><input type="number" class="form-control form-control-sm" name="siges_lineas[${i}][anio]" min="2000" max="2100" required></td>
            <td><input type="number" class="form-control form-control-sm" name="siges_lineas[${i}][horas]" min="0.01" step="0.01" required></td>
            <td><input type="number" class="form-control form-control-sm siges-monto" name="siges_lineas[${i}][monto]" min="0.01" step="0.01" required></td>
            <td><input type="text" class="form-control form-control-sm siges-iva" readonly tabindex="-1"></td>
            <td><input type="text" class="form-control form-control-sm siges-total" readonly tabindex="-1"></td>
            <td><input type="text" class="form-control form-control-sm" name="siges_lineas[${i}][observaciones]" maxlength="500" required></td>
            <td><input type="number" class="form-control form-control-sm" name="siges_lineas[${i}][porcentaje]" min="0.01" max="100" step="0.01" required></td>
            <td><button type="button" class="btn btn-sm btn-outline-danger siges-quitar" title="Quitar línea">&times;</button></td>`;

        // Los valores se asignan con .value (no se interpolan en el HTML) para evitar inyección.
        const set = (campo, valor) => { if (valor !== undefined && valor !== null && valor !== '') tr.querySelector(`[name$="[${campo}]"]`).value = valor; };
        set('mes', d.mes); set('anio', d.anio || new Date().getFullYear()); set('horas', d.horas);
        set('monto', d.monto); set('observaciones', d.observaciones);
        set('porcentaje', d.porcentaje ?? d.porcentaje_cobro ?? d.PorcentajeCobro);

        tbody.appendChild(tr);
        recalcular(tr);
        actualizarVacio();
        return tr;
    }

    function recalcular(tr) {
        const monto = parseFloat(tr.querySelector('.siges-monto').value) || 0;
        const iva   = r2(monto * 0.13);
        tr.querySelector('.siges-iva').value   = fmt(iva);
        tr.querySelector('.siges-total').value = fmt(monto + iva);
        totales();
    }

    function totales() {
        let m = 0;
        tbody.querySelectorAll('.siges-monto').forEach(inp => m += parseFloat(inp.value) || 0);
        const iva = r2(m * 0.13);
        document.getElementById('siges-sum-monto').textContent = fmt(m);
        document.getElementById('siges-sum-iva').textContent   = fmt(iva);
        document.getElementById('siges-sum-total').textContent = fmt(m + iva);
    }

    function actualizarVacio() {
        vacio.classList.toggle('d-none', tbody.children.length > 0);
    }

    tbody.addEventListener('input', e => {
        if (e.target.classList.contains('siges-monto')) recalcular(e.target.closest('tr'));
    });
    tbody.addEventListener('click', e => {
        if (e.target.classList.contains('siges-quitar')) {
            e.target.closest('tr').remove();
            totales();
            actualizarVacio();
        }
    });
    document.getElementById('siges-btn-agregar').addEventListener('click', () => agregarFila());

    /* ---------- API pública para EXT5 (Cargar Excel) ----------
     * Fabián: cuando lea el Excel, llamar
     *   SigesPropuesta.agregarLineas([{mes, anio, horas, monto, observaciones, porcentaje}, ...], true)
     * El 2º parámetro (true) reemplaza las líneas actuales. IVA y total se calculan solos.
     */
    window.SigesPropuesta = {
        agregarLineas(lineas, reemplazar) {
            if (reemplazar) tbody.innerHTML = '';
            (lineas || []).forEach(l => agregarFila(l));
            totales();
            actualizarVacio();
        }
    };
    document.getElementById('siges-btn-excel').addEventListener('click', () => {
        document.dispatchEvent(new CustomEvent('siges:cargar-excel'));
    });

    /* ---------- Proveedor existente / nuevo ---------- */
    function modoProveedorNuevo(esNuevo) {
        bloqueNuevo.classList.toggle('d-none', !esNuevo);
        camposNuevo.forEach(c => c.required = esNuevo);
    }

    async function consultarProveedor() {
        const estado = document.getElementById('siges_cedula_estado');
        const valor  = cedula.value.trim();
        cfg.existe = null;
        if (!reCedula.test(valor)) {
            estado.textContent = '';
            modoProveedorNuevo(false);
            nombre.readOnly = false;
            return;
        }
        estado.textContent = 'Verificando…';
        try {
            const url  = `${cfg.ajax}?action=siges_buscar_proveedor&nonce=${encodeURIComponent(cfg.nonce)}&cedula=${encodeURIComponent(valor)}`;
            const resp = await fetch(url, { credentials: 'same-origin' });
            const json = await resp.json();
            if (!json.success) { estado.textContent = ''; return; }
            cfg.existe = json.data.existe;
            if (json.data.existe) {
                nombre.value    = json.data.nombre;
                nombre.readOnly = true;
                estado.innerHTML = '<span class="text-success">Proveedor registrado.</span>';
                modoProveedorNuevo(false);
            } else {
                nombre.readOnly = false;
                estado.innerHTML = '<span class="text-primary">Proveedor nuevo.</span>';
                modoProveedorNuevo(true);
            }
        } catch (err) {
            estado.textContent = '';
        }
    }

    // Formato automático 3-XXX-XXXXXX mientras se escribe.
    cedula.addEventListener('input', () => {
        const d = cedula.value.replace(/\D/g, '').slice(0, 10);
        let out = d.slice(0, 1);
        if (d.length > 1) out += '-' + d.slice(1, 4);
        if (d.length > 4) out += '-' + d.slice(4, 10);
        cedula.value = out;
        cedula.classList.remove('is-invalid');
        if (reCedula.test(out)) consultarProveedor();
    });
    cedula.addEventListener('blur', consultarProveedor);

    // Quita la marca roja en cuanto el campo queda válido.
    form.addEventListener('input', e => {
        if (e.target.classList.contains('is-invalid') && e.target.checkValidity()) e.target.classList.remove('is-invalid');
    });
    form.addEventListener('change', e => {
        if (e.target.classList.contains('is-invalid') && e.target.checkValidity()) e.target.classList.remove('is-invalid');
    });

    /* ---------- Validación en el navegador antes de enviar ---------- */
    form.addEventListener('submit', e => {
        let valido = true;
        cedula.setCustomValidity(reCedula.test(cedula.value.trim()) ? '' : 'formato');
        form.querySelectorAll('input, select').forEach(el => {
            if (el.readOnly && el.classList.contains('siges-iva')) return;
            if (el.closest('.d-none')) return;
            const ok = el.checkValidity();
            el.classList.toggle('is-invalid', !ok);
            if (!ok) valido = false;
        });
        if (tbody.children.length === 0) {
            valido = false;
            modal('Datos incompletos', 'Debe agregar al menos una línea al detalle de la propuesta.');
        } else if (!valido) {
            modal('Datos incompletos', 'Todos los datos son requeridos. Revise los campos marcados en rojo.');
        }
        if (!valido) e.preventDefault();
    });

    /* ---------- Estado inicial ---------- */
    if (cfg.previas.length) {
        cfg.previas.forEach(l => agregarFila(l));
    } else {
        agregarFila();
    }
    if (cedula.value) consultarProveedor();

    window.addEventListener('load', () => {
        if (cfg.enviado) {
            form.classList.add('d-none');
            modal('Propuesta registrada',
                  'Su propuesta fue registrada correctamente. ¡Gracias por participar!',
                  `<a href="${esc(cfg.detalle)}" class="btn btn-primary">Volver a la solicitud</a>`);
        } else if (cfg.errores.length) {
            modal('No se pudo registrar la propuesta',
                  '<ul class="mb-0">' + cfg.errores.map(m => `<li>${esc(m)}</li>`).join('') + '</ul>');
        }
    });
})();
</script>
<?php
    return ob_get_clean();
}
