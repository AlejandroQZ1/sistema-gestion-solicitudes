# Sistema de gestión de solicitudes

Aplicación ASP.NET Core 8 con Razor Pages, Dapper, Bootstrap y base de datos MySQL/MariaDB.

La solución incluye usuarios y acceso, representantes, estados, solicitudes, desgloses, tareas y reportes. La rama `develop` reúne los módulos para pruebas de integración.

## Configuración

La conexión y las claves se configuran fuera del repositorio:

- `ConnectionStrings__DefaultConnection`: conexión a la base existente.
- `Cifrado__Clave`: clave usada para las contraseñas guardadas.
- `AdministradorInicial__NombreUsuario`, `AdministradorInicial__NombreCompleto`, `AdministradorInicial__Correo` y `AdministradorInicial__Contrasena`: creación del administrador si la tabla de usuarios está vacía.
- `Correo__SmtpServidor`, `Correo__SmtpPuerto`, `Correo__Usuario`, `Correo__Contrasena` y `Correo__Remitente`: envío de notificaciones por SMTP con STARTTLS.

El esquema utiliza `EstadoSolicitudID` para los estados y columnas calculadas para IVA y total. La integración no requiere modificar las tablas existentes. Los scripts de `BaseDatos` no deben importarse encima de una base que ya contiene datos.

## Ejecución

Compilar con `dotnet build AdministracionSoluciones.sln` y ejecutar con `dotnet run --project AdministracionSoluciones`.

La publicación se genera con `dotnet publish AdministracionSoluciones -c Release -o publicacion`. La configuración de conexión, cifrado y correo debe establecerse en el entorno del servidor.
