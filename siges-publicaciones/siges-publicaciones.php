<?php
/**
 * Plugin Name: SIGES Publicaciones
 * Description: Listado y detalle de solicitudes públicas de SIGES (EXT2 y EXT3).
 * Version: 1.0.0
 * Requires PHP: 7.4
 * Requires at least: 5.3
 */

namespace SigesPublicaciones;

defined('ABSPATH') || exit;

const SHORTCODE = 'siges_publicaciones';
const ID_PARAM = 'siges_solicitud';
const DEMO_PARAM = 'siges_demo_participar';

add_shortcode(SHORTCODE, __NAMESPACE__ . '\\render');
add_action('template_redirect', __NAMESPACE__ . '\\disable_page_cache');

function disable_page_cache(): void
{
    $page = get_queried_object();
    if (is_page() && $page instanceof \WP_Post && has_shortcode($page->post_content, SHORTCODE)) {
        nocache_headers();
    }
}

function render($attributes): string
{
    $options = shortcode_atts(['modo' => 'real', 'pagina_ext4' => ''], $attributes, SHORTCODE);
    $demo = $options['modo'] === 'demo';
    $list_url = get_permalink();
    if (!$list_url) {
        return '<p>El listado debe estar en una página publicada de WordPress.</p>';
    }

    $id = null;
    if (isset($_GET[ID_PARAM])) {
        $id = is_string($_GET[ID_PARAM]) ? wp_unslash($_GET[ID_PARAM]) : '';
        if (!valid_id($id)) {
            return unavailable($list_url);
        }
    }

    // Fuente de EXT2 y EXT3. El proveedor puede filtrar por ID al consultar el detalle.
    $today = current_datetime();
    $today_date = $today->format('Y-m-d');
    $records = $demo
        ? require __DIR__ . '/datos-demo.php'
        : apply_filters('siges_ext_publicaciones', new \WP_Error('siges_sin_fuente'), $id);
    if (!is_array($records)) {
        $message = '<p>Las publicaciones no están disponibles en este momento. Intentá de nuevo más tarde.</p>';
        return $id === null ? $message : $message . back_button($list_url);
    }

    $active = [];
    foreach ($records as $record) {
        if (!is_array($record) || !is_visible($record, $today_date)) {
            continue;
        }
        if ($id === $record['id']) {
            $detail_url = add_query_arg(ID_PARAM, $id, $list_url);
            if ($demo && ($_GET[DEMO_PARAM] ?? null) === '1') {
                return '<h2>Participar · Demostración</h2><p>Solicitud seleccionada: '
                    . esc_html($record['consecutivo']) . ' (ID: ' . esc_html($id) . ').</p>'
                    . '<p>Esta pantalla prueba la navegación hacia EXT4.</p>'
                    . button('Regresar al detalle', $detail_url);
            }

            // Destino de Participar en EXT4.
            $participation_url = $demo ? add_query_arg(DEMO_PARAM, '1', $detail_url) : '';
            $ext4_page = absint($options['pagina_ext4']);
            if ($ext4_page && $ext4_page !== get_the_ID() && get_post_type($ext4_page) === 'page'
                && get_post_status($ext4_page) === 'publish') {
                $ext4_url = get_permalink($ext4_page);
                if ($ext4_url) {
                    $participation_url = add_query_arg(ID_PARAM, $id, $ext4_url);
                }
            }
            $participation_url = apply_filters('siges_ext_participar_url', $participation_url, $id, $detail_url, $demo);

            return detail($record, $list_url, is_string($participation_url) ? $participation_url : '');
        }
        $active[] = $record;
    }

    return $id === null ? listing($active, $list_url) : unavailable($list_url);
}

function valid_id(string $id): bool
{
    return preg_match('/\A[A-Za-z0-9_-]{1,128}\z/', $id) === 1;
}

function is_visible(array $record, string $today): bool
{
    $id = $record['id'] ?? null;
    $expires = $record['fecha_vencimiento'] ?? null;

    return ($record['publicada'] ?? false) === true
        && is_string($id) && valid_id($id)
        && field($record, 'consecutivo') !== ''
        && field($record, 'titulo') !== ''
        && field($record, 'fecha_ingreso') !== ''
        && is_string($expires)
        && preg_match('/\A(\d{4})-(\d{2})-(\d{2})\z/', $expires, $parts) === 1
        && checkdate((int) $parts[2], (int) $parts[3], (int) $parts[1])
        && $expires >= $today;
}

// EXT2: listado de publicaciones vigentes.
function listing(array $records, string $list_url): string
{
    if (!$records) {
        return '<p>No hay solicitudes publicadas vigentes.</p>';
    }

    $html = '<figure class="wp-block-table table-responsive"><table class="table">'
        . '<caption>Solicitudes publicadas vigentes</caption><thead><tr>'
        . '<th scope="col">Consecutivo de oficio</th><th scope="col">Título</th>'
        . '<th scope="col">Representante</th></tr></thead><tbody>';
    foreach ($records as $record) {
        $url = add_query_arg(ID_PARAM, $record['id'], $list_url);
        $html .= '<tr><th scope="row"><a href="' . esc_url($url) . '">'
            . esc_html($record['consecutivo']) . '</a></th><td>' . esc_html($record['titulo'])
            . '</td><td>' . esc_html(field($record, 'representante')) . '</td></tr>';
    }

    return $html . '</tbody></table></figure>';
}

// EXT3: campos y navegación del detalle.
function detail(array $record, string $list_url, string $participation_url): string
{
    $fields = [
        'consecutivo' => 'Consecutivo de oficio de la solicitud',
        'documento_respuesta' => 'Documento de respuesta',
        'documento_inicio' => 'Documento de inicio',
        'titulo' => 'Título de la solicitud',
        'descripcion' => 'Descripción',
        'representante' => 'Representante',
        'observaciones' => 'Observaciones',
        'estado' => 'Estado de la solicitud',
        'fecha_ingreso' => 'Fecha de ingreso',
        'fecha_respuesta' => 'Fecha de respuesta',
        'fecha_inicio' => 'Fecha de inicio',
        'fecha_vencimiento' => 'Fecha de vencimiento de la publicación',
    ];
    $html = '<h2>Detalle de la solicitud</h2><dl>';
    foreach ($fields as $key => $label) {
        $value = field($record, $key);
        $display = $value === ''
            ? '<em>&lt;No indicado&gt;</em>'
            : nl2br(esc_html($value), false);
        $html .= '<dt>' . esc_html($label) . '</dt><dd>' . $display . '</dd>';
    }

    $html .= '</dl><div class="wp-block-buttons">' . back_button($list_url);
    if (esc_url($participation_url) !== '') {
        return $html . button('Participar', $participation_url) . '</div>';
    }

    return $html . '<div class="wp-block-button"><button type="button" disabled '
        . 'class="wp-block-button__link btn btn-primary">Participar</button></div>'
        . '</div><p>La pantalla de participación todavía no está conectada.</p>';
}

function field(array $record, string $key): string
{
    $value = $record[$key] ?? null;
    return is_string($value) ? trim($value) : '';
}

function button(string $label, string $url): string
{
    return '<div class="wp-block-button"><a class="wp-block-button__link wp-element-button btn btn-primary" href="'
        . esc_url($url) . '">' . esc_html($label) . '</a></div>';
}

function back_button(string $list_url): string
{
    return button('Regresar', $list_url);
}

function unavailable(string $list_url): string
{
    return '<p>La solicitud no está disponible.</p>' . back_button($list_url);
}
