# Módulo externo SIGES (WordPress)

Plugin `siges-externo` con los shortcodes del módulo externo.

| HU | Shortcode | Responsable |
|----|-----------|-------------|
| EXT4 - Registrar propuesta | `[siges_participar detalle_url="/detalle-solicitud"]` | Joel |

## Instalación
1. Comprimir la carpeta `siges-externo` en .zip y subirla en **Plugins → Añadir nuevo → Subir plugin**. Activar.
2. En `wp-config.php` (arriba de "That's all, stop editing"):
   ```php
   define('SIGES_DB_HOST', 'servidor-bd');
   define('SIGES_DB_NAME', 'tiusr22pl_siges');
   define('SIGES_DB_USER', 'usuario');
   define('SIGES_DB_PASS', 'contraseña');
   ```
3. Crear la página **Participar** con el shortcode. Se abre como `/participar?solicitud=ID`
   (el botón "Participar" de EXT3 debe apuntar ahí).

## Base de datos
Script: `BaseDatos/04_Modulo_Externo.sql` (columnas `Publicado` y `FechaVencimientoPublicacion`
en `solicitudes`, tablas `proveedores`, `propuestas` y `desgloses_propuesta`).

## Integración con EXT5 (Cargar Excel)
El botón "Cargar Excel" dispara el evento `siges:cargar-excel`. Tras leer el archivo, llamar:
```js
SigesPropuesta.agregarLineas([{mes, anio, horas, monto, observaciones, porcentaje}, ...], true);
```
IVA y total se calculan solos.

## Pruebas locales
`pruebas/siges_test_local.sql` crea una BD `siges_test` con la misma estructura y datos de prueba.
**Solo para uso local: borra y recrea la base `siges_test`.**
