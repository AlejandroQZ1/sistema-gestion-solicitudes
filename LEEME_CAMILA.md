# Parte 2: SOL3 y SOL4

Base: rama Joel, commit 91f7144. Los catálogos SOL3 y SOL4 se integran en la rama Camila del repositorio sistema-gestion-solicitudes.

## Abrir y ejecutar

1. Abra `AdministracionSoluciones.sln` en Visual Studio con soporte para ASP.NET y .NET 8.
2. Inicie su servicio MySQL desde MySQL Workbench o Servicios de Windows.
3. Ejecute en Workbench `BaseDatos/01_Seguridad_Usuarios_Bitacora.sql` si aún no tiene las tablas de Joel. Luego ejecute `BaseDatos/02_Catalogos_SOL3_SOL4.sql`.
4. Configure su conexión local en `AdministracionSoluciones/appsettings.Development.json` o con secretos de usuario. La base usada por los scripts es `siges`. No suba contraseñas reales al repositorio.
5. Seleccione `AdministracionSoluciones` como proyecto de inicio y ejecute. Inicie sesión con un usuario existente; en una base nueva, Joel configura la creación del administrador inicial en `Program.cs`.
6. Abra **Representantes** y **Estados de solicitudes** desde el menú lateral.

## Implementación

- SOL3: nombre y correo, validación, listado de 10 registros, crear y editar en formulario, eliminar con diálogo Sí/No.
- SOL4: identificador único y nombre, paginación, CRUD y cinco estados iniciales: Nueva, Respondida, Iniciada, Finalizada y Vencida.
- Sesión: se reutiliza el filtro de Joel; las nuevas páginas quedan protegidas.
- Bitácoras: alta con JSON, edición con valores anteriores y actuales, eliminación con datos eliminados, consulta sin JSON y registro de errores técnicos.
- Consistencia: el cambio y la bitácora se guardan en una misma transacción. Si falla la bitácora, se revierte el cambio.
- Eliminación relacionada: MySQL la rechaza mediante claves foráneas y la pantalla muestra el mensaje requerido.

## Integración pendiente con SOL1

Joel todavía no incluye la tabla de solicitudes. El integrante de SOL1 debe relacionar su tabla con `representantes.RepresentanteID` y `estados_solicitud.EstadoSolicitudID` mediante claves foráneas `ON DELETE RESTRICT` (sin cascada). Los identificadores son INT. Los nombres de tablas y columnas deben acordarse con el esquema del curso antes de usar el script sobre una base existente.

La prevención de eliminación se probó con una tabla temporal de solicitudes que utiliza esas relaciones. Sin estas claves foráneas, una futura tabla de solicitudes no estará protegida por el catálogo.

## Validación realizada

Compilación de los cuatro proyectos: cero errores y cero advertencias. Se ejecutaron 33 comprobaciones de integración con MySQL 8 en una instancia temporal, sin modificar la base del usuario. Incluyen CRUD de ambos catálogos, validaciones, paginación 10+2, identificadores duplicados, JSON anterior/actual, consultas, registros inexistentes, eliminación relacionada y reversión ante fallo de bitácora.

El programa de pruebas está en `PruebasCatalogos`. Requiere una base temporal vacía creada con los dos scripts y la variable `CATALOGOS_TEST_CONNECTION`; ejecutar `dotnet run --project PruebasCatalogos`. Las pruebas crean datos y tablas: no ejecutarlas en una base real.

Pendiente para la entrega: capturas de las pantallas en ejecución y prueba final contra la base compartida del equipo. No se presentan capturas ni pruebas visuales como realizadas.

## Archivos de tu aporte

Nuevos: `CatalogoItem.cs`, `CatalogoRepository.cs`, `CatalogoService.cs`, carpetas de páginas `Representantes` y `EstadosSolicitudes`, script `02_Catalogos_SOL3_SOL4.sql` y pruebas.

Cambios a archivos de Joel: registro de servicios en `Program.cs` y enlaces en `Pages/Shared/_Layout.cshtml`.

Abra la solución de esta carpeta del repositorio. La rama de trabajo es Camila. La copia anterior de desarrollo se conserva en TrabajoCamila. Los archivos locales que existían antes de integrar esta parte se conservan en la carpeta hermana RespaldoAntesCamila y en el stash del repositorio.
