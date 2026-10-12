# Sistema de gestión de solicitudes

Aplicación ASP.NET Core 8 con Razor Pages, Dapper y Bootstrap.

La conexión, la clave de cifrado y la contraseña del administrador inicial se configuran fuera del repositorio mediante `ConnectionStrings__DefaultConnection`, `Cifrado__Clave` y `AdministradorInicial__Contrasena`. La clave de cifrado debe coincidir con la usada para las contraseñas existentes.

Compilación: `dotnet build AdministracionSoluciones.sln`.

El módulo de desgloses utiliza la base existente: los estados se consultan mediante `EstadoSolicitudID` y las columnas `IVA` y `Total` se calculan en la base. No requiere cambios de tablas.

Los scripts de `BaseDatos` conservan el esquema inicial. No son una migración y no deben ejecutarse sobre la base de Plesk; el script `02_Solicitudes_Desgloses.sql` no corresponde al esquema que usa actualmente el módulo.
