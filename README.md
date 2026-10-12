# sistema-gestion-solicitudes

Esta rama contiene la solución entregada en el ZIP de José para USR3 (plantilla), SOL2 (desgloses) y SOL7 (reporte de facturación), junto con sus dependencias de seguridad.

Requiere .NET 8 y MySQL. Las credenciales y la clave de cifrado se configuran fuera del repositorio mediante las variables de entorno `ConnectionStrings__DefaultConnection`, `Cifrado__Clave` y `AdministradorInicial__Contrasena`. Para usuarios existentes, la clave de cifrado debe coincidir con la utilizada al guardar sus contraseñas.

La solución se compila con `dotnet build AdministracionSoluciones.sln`.

Los scripts de `BaseDatos` corresponden al esquema de esta entrega. La adaptación al esquema compartido y la integración con los otros módulos del equipo están pendientes; estos scripts no migran una base existente.
