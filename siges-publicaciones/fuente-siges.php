<?php
namespace SigesPublicaciones;

defined('ABSPATH') || exit;

add_filter('siges_ext_publicaciones', __NAMESPACE__ . '\\consultar_publicaciones', 10, 2);

/** Obtiene las publicaciones de la misma base que recibe las propuestas. */
function consultar_publicaciones($records, $id)
{
    if (!function_exists('siges_db_conectada') || !\siges_db_conectada()) {
        return new \WP_Error('siges_sin_conexion');
    }

    $db = \siges_db();
    try {
        $sql = 'SELECT s.SolicitudID AS id, s.Publicado AS publicada,
                       s.ConsecutivoOficio AS consecutivo, s.Titulo AS titulo,
                       s.DocumentoRespuesta AS documento_respuesta,
                       s.DocumentoInicio AS documento_inicio, s.Descripcion AS descripcion,
                       r.Nombre AS representante, s.Observaciones AS observaciones,
                       e.Nombre AS estado, s.FechaIngreso AS fecha_ingreso,
                       s.FechaRespuesta AS fecha_respuesta, s.FechaInicio AS fecha_inicio,
                       s.FechaVencimientoPublicacion AS fecha_vencimiento
                  FROM solicitudes s
                  LEFT JOIN representantes r ON r.RepresentanteID = s.RepresentanteID
                  INNER JOIN estados_solicitud e ON e.EstadoSolicitudID = s.EstadoSolicitudID
                 WHERE s.Publicado = 1 AND s.FechaVencimientoPublicacion >= %s';
        $args = [current_datetime()->format('Y-m-d')];
        if ($id !== null) {
            $sql .= ' AND s.SolicitudID = %s';
            $args[] = $id;
        }
        $sql .= ' ORDER BY s.ConsecutivoOficio';
        $rows = $db->get_results($db->prepare($sql, ...$args), ARRAY_A);
        if ($db->last_error) {
            throw new \RuntimeException($db->last_error);
        }
        foreach ($rows as &$row) {
            $row['id'] = (string) $row['id'];
            $row['publicada'] = (int) $row['publicada'] === 1;
        }
        unset($row);
        \siges_bitacora('externo', 'Publicaciones', 'El usuario consulta publicaciones');
        return $rows;
    } catch (\Throwable $error) {
        \siges_bitacora('externo', 'Publicaciones', 'Error al consultar publicaciones', ['error' => $error->getMessage()]);
        return new \WP_Error('siges_error_publicaciones');
    }
}
