# Evidencias de SOL3 y SOL4

Fecha: 11 de octubre de 2026. Rama: Camila.

Se integró la gestión de solicitudes de Alejandro (eb24076) y se comprobó con el esquema compartido del equipo. Las capturas y los registros corresponden a datos ficticios en una instancia temporal de MySQL 8. No se modificó la base de datos del usuario.

La solución compila con cero errores y cero advertencias. Las 33 comprobaciones de `PruebasCatalogos` pasaron; el bloqueo de eliminación usa la tabla real `solicitudes`, con las claves foráneas del esquema compartido, y no una tabla simulada.

| Historia | Criterio probado | Evidencia |
|---|---|---|
| SOL3 | Listar nombre y correo, máximo 10 por página | [Listado](SOL3_Listado.png) |
| SOL3 | Crear representante y volver al listado con mensaje | [Creación](SOL3_Crear.png) |
| SOL3 | Actualizar representante | [Edición](SOL3_Editar.png) |
| SOL3 | Confirmar eliminación con Sí/No | [Confirmación](SOL3_Confirmacion.png) |
| SOL3 | Impedir eliminación de representante asignado | [Bloqueo](SOL3_EliminarAsignado.png) |
| SOL4 | Listar nombre e identificador con paginación | [Listado](SOL4_Listado.png) |
| SOL4 | Crear estado | [Creación](SOL4_Crear.png) |
| SOL4 | Actualizar estado | [Edición](SOL4_Editar.png) |
| SOL4 | Eliminar estado sin relaciones | [Eliminación](SOL4_EliminarLibre.png) |
| SOL4 | Impedir eliminación de estado asignado | [Bloqueo](SOL4_EliminarAsignado.png) |
| SOL3/SOL4 | Mostrar catálogos en solicitud y formularios | [Solicitud](Integracion_Solicitudes.png), [formulario](Integracion_Desplegables.png) |
| SOL3/SOL4 | Auditar altas, modificaciones, eliminaciones y consultas | `Bitacoras_Prueba.tsv` y comprobaciones automatizadas de JSON anterior/actual |

Las comprobaciones automatizadas cubren además los cinco estados iniciales, correo inválido, nombre e identificador vacíos, identificadores duplicados, páginas sin repeticiones, límites de paginación, registros inexistentes, eliminación libre de representantes y reversión del cambio ante un fallo en la bitácora.

## Ejecutar en la base que ya configuraste

Ejecutar `BaseDatos/03_Integracion_Solicitudes.sql` en MySQL Workbench sobre la base `siges` creada con los scripts 01 y 02. No borra datos. Si ya existe la tabla solicitudes, debe tener las columnas del esquema compartido. Si falla por datos relacionados inconsistentes, corregir esos datos antes de continuar.

Para una base completamente nueva, usar `BaseDatos/siges_completa.sql`, que incluye las demás tablas del equipo. Ese archivo no debe importarse encima de una base existente.

La conexión, clave de cifrado y administrador inicial se mantienen en configuración local excluida de Git. Abrir `AdministracionSoluciones.sln`, seleccionar el proyecto web y ejecutar. No se enviaron correos de prueba.
