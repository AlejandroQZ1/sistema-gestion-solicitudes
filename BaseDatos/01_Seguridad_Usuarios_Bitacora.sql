/* =============================================================
   SIGES - Tablas del módulo de seguridad (USR1, USR2, USR4)
   Base de datos: MySQL 8
   Ejecutar en MySQL Workbench (botón del rayo) o en la consola de MySQL.
   Si la base de datos del equipo ya trae estas tablas con otros
   nombres, no ejecutar este script: ajustar las consultas de
   AdministracionSoluciones.Repository para que coincidan.
   ============================================================= */

CREATE DATABASE IF NOT EXISTS siges
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE siges;

CREATE TABLE IF NOT EXISTS usuarios (
    UsuarioID        INT           NOT NULL AUTO_INCREMENT,
    NombreUsuario    VARCHAR(50)   NOT NULL,
    NombreCompleto   VARCHAR(150)  NOT NULL,
    Correo           VARCHAR(150)  NOT NULL,
    Contrasena       VARCHAR(500)  NOT NULL,  -- cifrada con AES-GCM 256 (Base64)
    Estado           VARCHAR(20)   NOT NULL DEFAULT 'Activo',
    IntentosFallidos INT           NOT NULL DEFAULT 0,
    CONSTRAINT PK_usuarios PRIMARY KEY (UsuarioID),
    CONSTRAINT UQ_usuarios_NombreUsuario UNIQUE (NombreUsuario),
    CONSTRAINT CK_usuarios_Estado CHECK (Estado IN ('Activo', 'Inactivo', 'Bloqueado'))
) ENGINE = InnoDB;

CREATE TABLE IF NOT EXISTS bitacora (
    BitacoraID INT          NOT NULL AUTO_INCREMENT,
    Fecha      DATETIME     NOT NULL,
    Usuario    VARCHAR(50)  NOT NULL,
    Modulo     VARCHAR(50)  NOT NULL,
    Accion     VARCHAR(50)  NOT NULL,
    Detalle    TEXT         NULL,  -- JSON con los datos, nunca incluye la contraseña
    CONSTRAINT PK_bitacora PRIMARY KEY (BitacoraID)
) ENGINE = InnoDB;

/* El usuario administrador inicial (admin / Admin123*) lo crea la
   aplicación automáticamente la primera vez que se ejecuta, porque la
   contraseña tiene que guardarse cifrada con la clave de appsettings.json. */
