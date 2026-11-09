-- =====================================================================
--  SIGES - Base de datos de PRUEBA LOCAL para EXT4
--  Copia la estructura REAL de tiusr22pl_siges (nombres y tipos).
--
--  ⚠️  SOLO PARA TU COMPU (LocalWP / XAMPP).
--  ⚠️  NUNCA correr en el hosting: la primera línea BORRA la base de datos.
-- =====================================================================

SET NAMES utf8mb4;
DROP DATABASE IF EXISTS siges_test;
CREATE DATABASE siges_test CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE siges_test;

-- ---------------- Tablas existentes (como están en el hosting) --------

CREATE TABLE bitacora (
    BitacoraID INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Fecha      DATETIME    NOT NULL,
    Usuario    VARCHAR(50) NOT NULL,
    Modulo     VARCHAR(50) NOT NULL,
    Accion     VARCHAR(50) NOT NULL,
    Detalle    TEXT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE estados_solicitud (
    EstadoSolicitudID INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Identificador     VARCHAR(50)  NOT NULL UNIQUE,
    Nombre            VARCHAR(150) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE representantes (
    RepresentanteID INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nombre          VARCHAR(150) NOT NULL,
    Email           VARCHAR(150) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE usuarios (
    UsuarioID        INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NombreUsuario    VARCHAR(50)  NOT NULL UNIQUE,
    NombreCompleto   VARCHAR(150) NOT NULL,
    Correo           VARCHAR(150) NOT NULL,
    Contrasena       VARCHAR(500) NOT NULL,
    Estado           VARCHAR(20)  NOT NULL,
    IntentosFallidos INT(11)      NOT NULL DEFAULT 0
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE solicitudes (
    SolicitudID        INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    ConsecutivoOficio  VARCHAR(100) NOT NULL UNIQUE,
    DocumentoRespuesta VARCHAR(255) NULL,
    DocumentoInicio    VARCHAR(255) NULL,
    Titulo             VARCHAR(200) NOT NULL,
    Descripcion        TEXT NULL,
    RepresentanteID    INT(11) NULL,
    Observaciones      TEXT NULL,
    EstadoSolicitudID  INT(11) NOT NULL,
    FechaIngreso       DATE NOT NULL,
    FechaRespuesta     DATE NULL,
    FechaInicio        DATE NULL,
    FOREIGN KEY (RepresentanteID)   REFERENCES representantes (RepresentanteID),
    FOREIGN KEY (EstadoSolicitudID) REFERENCES estados_solicitud (EstadoSolicitudID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE desgloses (
    DesgloseID      INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SolicitudID     INT(11) NOT NULL,
    Mes             TINYINT(3) UNSIGNED NULL,
    Anio            SMALLINT(5) UNSIGNED NULL,
    Horas           DECIMAL(10,2) NULL,
    Monto           DECIMAL(14,2) NULL,
    IVA             DECIMAL(14,2) GENERATED ALWAYS AS (ROUND(Monto * 0.13, 2)) STORED,
    Total           DECIMAL(15,2) GENERATED ALWAYS AS (Monto + ROUND(Monto * 0.13, 2)) STORED,
    Observaciones   TEXT NULL,
    PorcentajeCobro DECIMAL(5,2) NULL,
    FOREIGN KEY (SolicitudID) REFERENCES solicitudes (SolicitudID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE tareas (
    TareaID          INT(11) NOT NULL AUTO_INCREMENT PRIMARY KEY,
    SolicitudID      INT(11) NOT NULL,
    DescripcionTarea TEXT NULL,
    Fecha            DATE NULL,
    UsuarioID        INT(11) NOT NULL,
    HorasInvertidas  DECIMAL(10,2) NULL,
    FOREIGN KEY (SolicitudID) REFERENCES solicitudes (SolicitudID),
    FOREIGN KEY (UsuarioID)   REFERENCES usuarios (UsuarioID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------- Cambios del segundo avance (EXT_cambios_bd.sql) -----

ALTER TABLE solicitudes
    ADD COLUMN Publicado TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN FechaVencimientoPublicacion DATE NULL;

CREATE TABLE proveedores (
    ProveedorID         INT(11)      NOT NULL AUTO_INCREMENT,
    CedulaJuridica      VARCHAR(12)  NOT NULL,
    Nombre              VARCHAR(150) NOT NULL,
    NombreRepresentante VARCHAR(150) NOT NULL,
    Telefono            VARCHAR(20)  NOT NULL,
    Correo              VARCHAR(150) NOT NULL,
    PRIMARY KEY (ProveedorID),
    UNIQUE KEY uq_proveedores_cedula (CedulaJuridica)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE propuestas (
    PropuestaID    INT(11)    NOT NULL AUTO_INCREMENT,
    SolicitudID    INT(11)    NOT NULL,
    ProveedorID    INT(11)    NOT NULL,
    FechaPropuesta DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Aprobada       TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (PropuestaID),
    CONSTRAINT fk_propuestas_solicitudes FOREIGN KEY (SolicitudID) REFERENCES solicitudes (SolicitudID),
    CONSTRAINT fk_propuestas_proveedores FOREIGN KEY (ProveedorID) REFERENCES proveedores (ProveedorID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE desgloses_propuesta (
    DesglosePropuestaID INT(11)              NOT NULL AUTO_INCREMENT,
    PropuestaID         INT(11)              NOT NULL,
    Mes                 TINYINT(3) UNSIGNED  NOT NULL,
    Anio                SMALLINT(5) UNSIGNED NOT NULL,
    Horas               DECIMAL(10,2)        NOT NULL,
    Monto               DECIMAL(14,2)        NOT NULL,
    IVA                 DECIMAL(14,2) GENERATED ALWAYS AS (ROUND(Monto * 0.13, 2)) STORED,
    Total               DECIMAL(15,2) GENERATED ALWAYS AS (Monto + ROUND(Monto * 0.13, 2)) STORED,
    Observaciones       TEXT                 NOT NULL,
    PorcentajeCobro     DECIMAL(5,2)         NOT NULL,
    PRIMARY KEY (DesglosePropuestaID),
    CONSTRAINT fk_desgloses_propuesta_propuestas FOREIGN KEY (PropuestaID) REFERENCES propuestas (PropuestaID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ---------------- Datos de prueba -------------------------------------

INSERT INTO estados_solicitud (Identificador, Nombre) VALUES
    ('NUE', 'Nueva'), ('RES', 'Respondida'), ('INI', 'Iniciada'), ('FIN', 'Finalizada'), ('VEN', 'Vencida');

INSERT INTO representantes (Nombre, Email) VALUES
    ('María Rodríguez Solís', 'maria.rodriguez@publica.cr'),
    ('Carlos Jiménez Mora',   'carlos.jimenez@publica.cr');

INSERT INTO solicitudes
    (ConsecutivoOficio, Titulo, Descripcion, RepresentanteID, EstadoSolicitudID, FechaIngreso,
     Publicado, FechaVencimientoPublicacion)
VALUES
    ('OF-2026-001', 'Mantenimiento de servidores', 'Soporte mensual a la infraestructura.', 1,    1, '2026-11-02', 1, '2026-12-31'),  -- vigente
    ('OF-2026-002', 'Desarrollo de portal web',    NULL,                                    NULL, 1, '2026-11-03', 1, '2026-12-15'),  -- vigente, sin representante
    ('OF-2026-003', 'Auditoría de seguridad',      'Revisión anual.',                       2,    1, '2026-10-01', 1, '2026-10-31'),  -- vencida
    ('OF-2026-004', 'Licencias de software',       'Renovación de licencias.',              2,    1, '2026-11-05', 0, NULL);          -- no publicada

INSERT INTO proveedores (CedulaJuridica, Nombre, NombreRepresentante, Telefono, Correo) VALUES
    ('3-101-123456', 'Soluciones Tecnológicas S.A.', 'Ana Vargas Quesada', '8888-1234', 'ana@soltec.cr');

-- Consultas para revisar después de cada prueba:
-- SELECT * FROM proveedores;
-- SELECT * FROM propuestas;
-- SELECT * FROM desgloses_propuesta;
-- SELECT * FROM desgloses;                    -- debe seguir VACÍA
-- SELECT * FROM bitacora ORDER BY BitacoraID DESC;
