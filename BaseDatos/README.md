# Base de datos completa SIGES

`siges.sql` crea la base MySQL `siges`, sus siete tablas y sus relaciones:

- `usuarios`
- `bitacora`
- `representantes`
- `estados_solicitud`
- `solicitudes`
- `tareas`
- `desgloses`

Incluye los cinco estados iniciales: Nueva, Respondida, Iniciada, Finalizada y
Vencida. No incluye cuentas, contrasenas, claves de cifrado ni datos exportados.

## Uso

Ejecutar el archivo completo en MySQL sobre una base nueva o vacia, con una cuenta
que tenga permisos para crear la base y sus tablas. No ejecutarlo sobre una base
que ya contenga tablas. No es un script de actualizacion ni debe ejecutarse junto
con otros scripts que creen las mismas tablas.

La base compartida ya instalada en el servidor no necesita volver a crearse.

## Integracion

Se conservaron los nombres y tipos de las cinco tablas exportadas por el equipo.
Se agregaron `tareas` para TAR1-TAR3 y `desgloses` para SOL2-SOL7.

En `desgloses`, `IVA` y `Total` son columnas calculadas por MySQL. Los INSERT y
UPDATE deben omitirlas: IVA es el 13 % del monto, redondeado a dos decimales, y
Total es Monto + IVA.

El reporte SOL7 consulta `desgloses` junto con `solicitudes`, filtrando por mes y
ano. La plantilla USR3 utiliza los datos de `usuarios` y no requiere otra tabla.
Los modulos comparten la tabla `bitacora`.

Este archivo prepara el almacenamiento. Las pantallas y consultas de cada historia
corresponden al codigo de su integrante.
