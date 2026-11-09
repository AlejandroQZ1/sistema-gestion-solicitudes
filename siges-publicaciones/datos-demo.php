<?php

defined('ABSPATH') || exit;

// Solicitudes de prueba con fechas relativas al día actual de WordPress.
$complete = [
    'id' => 'demo-101',
    'publicada' => true,
    'consecutivo' => 'DEMO-2026-001',
    'titulo' => 'Mantenimiento de equipos informáticos',
    'documento_respuesta' => 'DEMO-RESP-001',
    'documento_inicio' => 'DEMO-INI-001',
    'descripcion' => "Revisión preventiva del equipo de cómputo.\nIncluye diagnóstico y limpieza.",
    'representante' => 'Andrea Ejemplo',
    'observaciones' => 'Coordinar las visitas con el área de soporte.',
    'estado' => 'Asignada',
    'fecha_ingreso' => $today->modify('-5 days')->format('Y-m-d'),
    'fecha_respuesta' => $today->modify('-2 days')->format('Y-m-d'),
    'fecha_inicio' => $today->modify('-1 day')->format('Y-m-d'),
    'fecha_vencimiento' => $today->modify('+14 days')->format('Y-m-d'),
];

return [
    $complete,
    [
        'id' => 'demo-102',
        'publicada' => true,
        'consecutivo' => 'DEMO-2026-002',
        'titulo' => 'Actualización de señalización',
        'descripcion' => 'Sustitución de rótulos en las zonas de atención.',
        'representante' => null,
        'estado' => 'Nueva',
        'fecha_ingreso' => $today->format('Y-m-d'),
        'fecha_vencimiento' => $today->format('Y-m-d'),
    ],
    array_replace($complete, [
        'id' => 'demo-103',
        'consecutivo' => 'DEMO-2026-003',
        'fecha_vencimiento' => $today->modify('-1 day')->format('Y-m-d'),
    ]),
    array_replace($complete, [
        'id' => 'demo-104',
        'consecutivo' => 'DEMO-2026-004',
        'publicada' => false,
    ]),
];
