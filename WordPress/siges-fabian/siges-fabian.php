<?php
/**
 * Plugin Name: SIGES - Carga y consulta de propuestas
 * Description: Carga de líneas desde Excel y consulta de participaciones por proveedor.
 * Version: 1.0.0
 */

if (!defined('ABSPATH')) {
    exit;
}

add_action('wp_enqueue_scripts', function () {
    global $post;

    if (!$post || (!has_shortcode($post->post_content, 'siges_participar')
        && !has_shortcode($post->post_content, 'siges_cargar_excel'))) {
        return;
    }

    wp_enqueue_script(
        'siges-xlsx',
        'https://cdn.sheetjs.com/xlsx-0.20.3/package/dist/xlsx.full.min.js',
        [],
        '0.20.3',
        true
    );
    wp_enqueue_script(
        'siges-cargar-excel',
        plugin_dir_url(__FILE__) . 'cargar-excel.js',
        ['siges-xlsx'],
        '1.0.0',
        true
    );

    $pagina_excel = get_page_by_path('cargar-excel');
    wp_add_inline_script(
        'siges-cargar-excel',
        'window.sigesPaginaExcel = ' . wp_json_encode($pagina_excel ? get_permalink($pagina_excel) : '') . ';',
        'before'
    );
});

add_shortcode('siges_cargar_excel', function () {
    return '<h1>Cargar Excel</h1>'
        . '<p>Seleccione el Excel con las líneas de la propuesta.</p>'
        . '<input id="siges-archivo-excel" type="file" accept=".xlsx,.xls">'
        . '<p id="siges-error-excel" role="alert"></p>'
        . '<button id="siges-confirmar-excel" type="button">Cargar</button> '
        . '<a id="siges-cancelar-excel" href="#">Cancelar</a>';
});

add_shortcode('siges_participaciones', function () {
    if (!function_exists('siges_db_conectada') || !siges_db_conectada()) {
        return '<p>Las participaciones no están disponibles en este momento.</p>';
    }

    $cedula = isset($_GET['cedula']) && is_string($_GET['cedula'])
        ? sanitize_text_field(wp_unslash($_GET['cedula']))
        : '';

    ob_start();
    ?>
    <form method="get">
        <label for="siges-buscar-cedula">Cédula jurídica del proveedor</label>
        <input id="siges-buscar-cedula" name="cedula" type="text" required
               maxlength="12" pattern="3-[0-9]{3}-[0-9]{6}"
               value="<?php echo esc_attr($cedula); ?>">
        <button type="submit">Consultar</button>
    </form>
    <?php

    if ($cedula !== '') {
        if (!siges_validar_cedula($cedula)) {
            echo '<p>La cédula jurídica debe tener el formato 3-XXX-XXXXXX.</p>';
        } else {
            $db = siges_db();
            $proveedor = $db->get_row($db->prepare(
                'SELECT ProveedorID FROM proveedores WHERE CedulaJuridica = %s',
                $cedula
            ));

            if ($db->last_error) {
                echo '<p>Las participaciones no están disponibles en este momento.</p>';
            } elseif (!$proveedor) {
                echo '<p>Proveedor no existe.</p>';
            } else {
                $solicitudes = $db->get_results($db->prepare(
                    'SELECT DISTINCT s.ConsecutivoOficio, s.Titulo
                       FROM propuestas p
                       INNER JOIN solicitudes s ON s.SolicitudID = p.SolicitudID
                      WHERE p.ProveedorID = %d
                      ORDER BY s.ConsecutivoOficio',
                    $proveedor->ProveedorID
                ));

                if ($db->last_error) {
                    echo '<p>Las participaciones no están disponibles en este momento.</p>';
                } elseif (!$solicitudes) {
                    echo '<p>Datos no encontrados.</p>';
                } else {
                    echo '<table><thead><tr><th>Consecutivo de oficio</th><th>Título</th></tr></thead><tbody>';
                    foreach ($solicitudes as $solicitud) {
                        echo '<tr><td>' . esc_html($solicitud->ConsecutivoOficio) . '</td><td>'
                            . esc_html($solicitud->Titulo) . '</td></tr>';
                    }
                    echo '</tbody></table>';
                }
            }

            siges_bitacora('externo:' . $cedula, 'Participaciones', 'El usuario consulta participaciones');
        }
    }

    return ob_get_clean();
});
