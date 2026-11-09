<?php
/**
 * Plugin Name: SIGES - Carga y consulta de propuestas
 * Description: Carga de líneas desde Excel y consulta de participaciones por proveedor.
 * Version: 1.0.1
 */

if (!defined('ABSPATH')) {
    exit;
}

// Los recursos se cargan únicamente en las páginas que usan estas funcionalidades.
add_action('wp_enqueue_scripts', function () {
    global $post;
    if (!$post) {
        return;
    }
    $participar = has_shortcode($post->post_content, 'siges_participar');
    $excel = has_shortcode($post->post_content, 'siges_cargar_excel');
    $consulta = has_shortcode($post->post_content, 'siges_participaciones');
    if (!$participar && !$excel && !$consulta) {
        return;
    }

    wp_enqueue_style('siges-bootstrap', 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css', [], '5.3.3');
    wp_enqueue_script('siges-bootstrap', 'https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js', [], '5.3.3', true);
    $dependencias = ['siges-bootstrap'];
    if ($excel) {
        wp_enqueue_script('siges-xlsx', 'https://cdn.sheetjs.com/xlsx-0.20.3/package/dist/xlsx.full.min.js', [], '0.20.3', true);
        $dependencias[] = 'siges-xlsx';
    }
    wp_enqueue_script('siges-cargar-excel', plugin_dir_url(__FILE__) . 'cargar-excel.js', $dependencias, '1.0.1', true);

    $pagina_excel = get_page_by_path('cargar-excel');
    wp_add_inline_script('siges-cargar-excel',
        'window.sigesPaginaExcel = ' . wp_json_encode($pagina_excel ? get_permalink($pagina_excel) : '') . ';', 'before');
});

/** Diálogo compartido por la carga de Excel y la consulta de participaciones. */
function siges_fabian_dialogo($mensaje = '')
{
    return '<div class="modal fade" id="siges-fabian-modal" tabindex="-1" aria-labelledby="siges-fabian-titulo" aria-hidden="true" data-mensaje="' . esc_attr($mensaje) . '">'
        . '<div class="modal-dialog modal-dialog-centered"><div class="modal-content">'
        . '<div class="modal-header"><h5 class="modal-title" id="siges-fabian-titulo">Participaciones</h5>'
        . '<button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Cerrar"></button></div>'
        . '<div class="modal-body"></div><div class="modal-footer">'
        . '<button type="button" class="btn btn-primary" data-bs-dismiss="modal">Aceptar</button>'
        . '</div></div></div></div>';
}

/** Conserva el detalle técnico en la bitácora; si la base no responde, usa el registro del servidor. */
function siges_fabian_registrar_error($usuario, $detalle)
{
    try {
        if (function_exists('siges_db_conectada') && function_exists('siges_bitacora') && siges_db_conectada()) {
            siges_bitacora($usuario, 'Participaciones', 'Error al consultar participaciones', ['error' => $detalle]);
            return;
        }
    } catch (Throwable $error) {
        error_log('SIGES - No se pudo registrar el error en la bitácora: ' . $error->getMessage());
    }
    error_log('SIGES - Error al consultar participaciones: ' . $detalle);
}

add_shortcode('siges_cargar_excel', function () {
    return '<h1>Cargar Excel</h1>'
        . '<label class="form-label" for="siges-archivo-excel">Archivo Excel</label>'
        . '<input class="form-control mb-3" id="siges-archivo-excel" type="file" accept=".xlsx,.xls">'
        . '<button class="btn btn-primary" id="siges-confirmar-excel" type="button">Cargar</button>'
        . siges_fabian_dialogo();
});

add_shortcode('siges_participaciones', function () {
    $cedula = isset($_GET['cedula']) && is_string($_GET['cedula'])
        ? sanitize_text_field(wp_unslash($_GET['cedula'])) : '';
    $mensaje = '';
    $solicitudes = [];
    $usuario = 'externo:' . $cedula;

    // Una consulta distingue un proveedor inexistente de uno que aún no tiene propuestas.
    if (isset($_GET['cedula'])) {
        try {
            if (!preg_match('/^3-[0-9]{3}-[0-9]{6}$/', $cedula)) {
                $mensaje = 'La cédula jurídica debe tener el formato 3-XXX-XXXXXX.';
            } elseif (!function_exists('siges_db_conectada') || !siges_db_conectada()) {
                siges_fabian_registrar_error($usuario, 'No se pudo establecer la conexión con SIGES.');
                $mensaje = 'Las participaciones no están disponibles en este momento.';
            } else {
                $db = siges_db();
                $proveedor = $db->get_row($db->prepare(
                    'SELECT ProveedorID FROM proveedores WHERE CedulaJuridica = %s', $cedula));
                if ($db->last_error) {
                    siges_fabian_registrar_error($usuario, $db->last_error);
                    $mensaje = 'Las participaciones no están disponibles en este momento.';
                } elseif (!$proveedor) {
                    $mensaje = 'Proveedor no existe.';
                } else {
                    $solicitudes = $db->get_results($db->prepare(
                        'SELECT DISTINCT s.ConsecutivoOficio, s.Titulo
                           FROM propuestas p
                           INNER JOIN solicitudes s ON s.SolicitudID = p.SolicitudID
                          WHERE p.ProveedorID = %d
                          ORDER BY s.ConsecutivoOficio', $proveedor->ProveedorID));
                    if ($db->last_error) {
                        siges_fabian_registrar_error($usuario, $db->last_error);
                        $solicitudes = [];
                        $mensaje = 'Las participaciones no están disponibles en este momento.';
                    } elseif (!$solicitudes) {
                        $mensaje = 'Datos no encontrados.';
                    }
                }
                siges_bitacora($usuario, 'Participaciones', 'El usuario consulta participaciones');
            }
        } catch (Throwable $error) {
            siges_fabian_registrar_error($usuario, $error->getMessage());
            $solicitudes = [];
            $mensaje = 'Las participaciones no están disponibles en este momento.';
        }
    }

    ob_start();
    ?>
    <form method="get" id="siges-consultar-participaciones" novalidate>
        <?php if (isset($_GET['page_id'])) : ?>
            <input type="hidden" name="page_id" value="<?php echo absint($_GET['page_id']); ?>">
        <?php endif; ?>
        <label class="form-label" for="siges-buscar-cedula">Cédula jurídica del proveedor</label>
        <input class="form-control mb-3" id="siges-buscar-cedula" name="cedula" type="text" required
               maxlength="12" pattern="3-[0-9]{3}-[0-9]{6}" value="<?php echo esc_attr($cedula); ?>">
        <button class="btn btn-primary" type="submit">Consultar</button>
    </form>
    <?php if ($solicitudes) : ?>
        <table class="table mt-3">
            <thead><tr><th>Consecutivo de oficio</th><th>Título</th></tr></thead>
            <tbody>
            <?php foreach ($solicitudes as $solicitud) : ?>
                <tr><td><?php echo esc_html($solicitud->ConsecutivoOficio); ?></td>
                    <td><?php echo esc_html($solicitud->Titulo); ?></td></tr>
            <?php endforeach; ?>
            </tbody>
        </table>
    <?php endif;
    echo siges_fabian_dialogo($mensaje);
    return ob_get_clean();
});
