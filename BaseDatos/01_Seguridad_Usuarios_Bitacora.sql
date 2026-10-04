/* =============================================================
   SIGES - Tablas del módulo de seguridad (USR1, USR2, USR4)
   Base de datos: SQL Server
   Si la base de datos del equipo ya trae estas tablas con otros
   nombres, no ejecutar este script: ajustar Models/Usuario.cs y
   Models/Bitacora.cs para que coincidan.
   ============================================================= */

IF DB_ID('SIGES') IS NULL
    CREATE DATABASE SIGES;
GO

USE SIGES;
GO

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuarios (
        Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
        NombreUsuario    NVARCHAR(50)  NOT NULL,
        NombreCompleto   NVARCHAR(150) NOT NULL,
        Correo           NVARCHAR(150) NOT NULL,
        Contrasena       NVARCHAR(500) NOT NULL,  -- cifrada con AES-GCM 256 (Base64)
        Estado           NVARCHAR(20)  NOT NULL CONSTRAINT DF_Usuarios_Estado DEFAULT ('Activo'),
        IntentosFallidos INT           NOT NULL CONSTRAINT DF_Usuarios_Intentos DEFAULT (0),
        CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT CK_Usuarios_Estado CHECK (Estado IN ('Activo', 'Inactivo', 'Bloqueado'))
    );
END
GO

IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora (
        Id       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
        Fecha    DATETIME2     NOT NULL,
        Usuario  NVARCHAR(50)  NOT NULL,
        Modulo   NVARCHAR(50)  NOT NULL,
        Accion   NVARCHAR(50)  NOT NULL,
        Detalle  NVARCHAR(MAX) NULL      -- JSON con los datos, nunca incluye la contraseña
    );
END
GO

/* El usuario administrador inicial (admin / Admin123*) lo crea la
   aplicación automáticamente la primera vez que se ejecuta, porque la
   contraseña tiene que guardarse cifrada con la clave de appsettings.json. */
