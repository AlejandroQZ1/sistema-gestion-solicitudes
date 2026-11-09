-- =====================================================================
--  SIGES - Segundo avance (módulo externo)
--  Cambios a la base de datos tiusr22pl_siges
--  Propuesto por: Joel Barquero Calderón (EXT4)
--
--  Sigue las convenciones actuales: tablas en plural, columnas PascalCase,
--  llave <Tabla>ID, InnoDB + utf8mb4_unicode_ci.
--  El desglose de propuesta replica la tabla "desgloses" (IVA y Total
--  calculados por la BD) para que EXT7 pueda copiarlo al aprobar.
-- =====================================================================

-- EXT1: publicación de solicitudes
ALTER TABLE solicitudes
    ADD COLUMN Publicado TINYINT(1) NOT NULL DEFAULT 0,
    ADD COLUMN FechaVencimientoPublicacion DATE NULL;

-- EXT4: proveedores que participan desde el sitio externo
CREATE TABLE proveedores (
    ProveedorID         INT(11)      NOT NULL AUTO_INCREMENT,
    CedulaJuridica      VARCHAR(12)  NOT NULL,          -- formato 3-XXX-XXXXXX
    Nombre              VARCHAR(150) NOT NULL,
    NombreRepresentante VARCHAR(150) NOT NULL,
    Telefono            VARCHAR(20)  NOT NULL,
    Correo              VARCHAR(150) NOT NULL,
    PRIMARY KEY (ProveedorID),
    UNIQUE KEY uq_proveedores_cedula (CedulaJuridica)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- EXT4: propuesta de un proveedor para una solicitud (EXT7 la aprueba)
CREATE TABLE propuestas (
    PropuestaID    INT(11)    NOT NULL AUTO_INCREMENT,
    SolicitudID    INT(11)    NOT NULL,
    ProveedorID    INT(11)    NOT NULL,
    FechaPropuesta DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Aprobada       TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (PropuestaID),
    KEY idx_propuestas_solicitud (SolicitudID),
    KEY idx_propuestas_proveedor (ProveedorID),
    CONSTRAINT fk_propuestas_solicitudes FOREIGN KEY (SolicitudID) REFERENCES solicitudes (SolicitudID),
    CONSTRAINT fk_propuestas_proveedores FOREIGN KEY (ProveedorID) REFERENCES proveedores (ProveedorID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- EXT4: líneas de la propuesta, SEPARADAS de los desgloses en firme
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
    KEY idx_desgloses_propuesta (PropuestaID),
    CONSTRAINT fk_desgloses_propuesta_propuestas FOREIGN KEY (PropuestaID) REFERENCES propuestas (PropuestaID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
