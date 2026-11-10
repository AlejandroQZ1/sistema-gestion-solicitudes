# EXT7 — Aprobar propuesta

## Objetivo

EXT7 permite revisar las propuestas de una solicitud y aprobar una sola. Al aprobar,
el conjunto formal vigente de `desgloses` se reemplaza por todas las líneas de la
propuesta elegida. El conjunto previo se conserva en `historial_desgloses`; no se suma
al nuevo conjunto ni permanece como vigente.

## Flujo

1. Desde el listado interno de solicitudes se abre «Participaciones».
2. La lista muestra las propuestas de esa solicitud, proveedor, identificación, fecha y
   estado. Cada propuesta tiene un enlace al detalle.
3. El detalle muestra todas las líneas guardadas en `desgloses_propuesta`.
4. El POST antiforgery obtiene al aprobador desde `Sesion.NombreUsuario(HttpContext)`.
   El valor de usuario no procede de campos del formulario.
5. `PropuestaService` abre una transacción y bloquea la solicitud con `FOR UPDATE`.
   Verifica pertenencia, estado y validez de líneas; archiva filas actuales, las reemplaza,
   registra la aprobación y bitácora y confirma.
6. Si falla cualquier sentencia, incluida la bitácora, se revierte la transacción.

## Reglas e integridad

- La propuesta debe pertenecer a la solicitud indicada y tener al menos una línea válida.
- La solicitud se bloquea antes de comprobar aprobaciones, serializando dos POST
  concurrentes para la misma solicitud.
- `aprobaciones_propuesta` tiene una restricción única por `SolicitudID`; el `UPDATE`
  condicional de `propuestas.Aprobada` y la unicidad de la línea origen son defensas
  adicionales.
- Cada fila formal nueva guarda `PropuestaID` y `DesglosePropuestaID`. Claves foráneas
  compuestas verifican que la propuesta pertenezca a la solicitud y que la línea sea de
  esa propuesta.
- Las filas vigentes siguen en `desgloses`. Las consultas actuales de SOL2 y reportes
  consultan esa tabla y por eso continúan mostrando únicamente el nuevo conjunto.
- IVA y Total no se insertan desde la aplicación; se calculan mediante las columnas
  generadas de `desgloses`.
- El snapshot JSON conserva datos anteriores y nuevos en la cabecera de aprobación; las
  filas anteriores también se archivan en forma relacional, incluido su ID original e
  importes IVA/Total.

## Esquema de migración

`BaseDatos/05_Ext7_Aprobacion.sql` agrega claves compuestas necesarias en las tablas
EXT4, las columnas de origen en `desgloses`, `aprobaciones_propuesta` y
`historial_desgloses`. No elimina ni recrea objetos o información.

La migración requiere que `BaseDatos/04_Modulo_Externo.sql` ya haya creado
`propuestas` y `desgloses_propuesta`. Ese script no está en la rama local `Alejandro`;
la migración EXT7 no lo reemplaza ni implementa EXT4. Antes de desplegar, se debe
confirmar que el servidor tenga esas tablas con tipos, claves y motor compatibles, y
revisar sus claves foráneas reales. El SQL local consultado no muestra claves entrantes
a `desgloses.DesgloseID`; la variante de Jose agrega `Asignado`, pero no establece esa
relación. No se ha inspeccionado la base alojada.

Aplicar primero un respaldo de la base de prueba y revisar el destino. Ejecutar la
migración una sola vez en un entorno autorizado, después de verificar las precondiciones.
No ejecutarla sobre producción sin una revisión específica del esquema y autorización.

## Pruebas locales

```powershell
dotnet run --project PruebasEXT7/PruebasEXT7.csproj
dotnet build AdministracionSoluciones.sln --configuration Release
```

Las pruebas de consola usan un repositorio en memoria para verificar reglas del servicio,
rollback, historial, bitácora y dos llamadas concurrentes. No conectan a MySQL/MariaDB y
no prueban los bloqueos SQL reales. Antes de desplegar se requieren pruebas de integración
en una base desechable compatible: aprobación correcta, repetición, dos propuestas
concurrentes, fallo inducido dentro de transacción y verificación de los reportes.
