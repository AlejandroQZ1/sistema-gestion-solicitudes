-- EXT7: variante de aplicación única sobre una base con EXT4 y sin EXT7.
-- Alternativa a 05_Ext7_Aprobacion.sql para cuentas sin acceso a information_schema.
-- Opera sobre la base seleccionada; no es acumulativa con la otra variante.

ALTER TABLE propuestas ADD UNIQUE KEY uq_propuestas_id_solicitud (PropuestaID, SolicitudID);

ALTER TABLE desgloses_propuesta ADD UNIQUE KEY uq_desgloses_propuesta_id_propuesta (DesglosePropuestaID, PropuestaID);

ALTER TABLE desgloses ADD COLUMN PropuestaID INT NULL;

ALTER TABLE desgloses ADD COLUMN DesglosePropuestaID INT NULL;

ALTER TABLE desgloses ADD UNIQUE KEY uq_desgloses_linea_propuesta (DesglosePropuestaID);

ALTER TABLE desgloses ADD KEY ix_desgloses_propuesta_solicitud (PropuestaID, SolicitudID);

ALTER TABLE desgloses ADD KEY ix_desgloses_linea_propuesta_fk (DesglosePropuestaID, PropuestaID);

ALTER TABLE desgloses ADD CONSTRAINT fk_desgloses_propuestas_solicitud FOREIGN KEY (PropuestaID, SolicitudID) REFERENCES propuestas (PropuestaID, SolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE;

ALTER TABLE desgloses ADD CONSTRAINT fk_desgloses_propuesta_linea FOREIGN KEY (DesglosePropuestaID, PropuestaID) REFERENCES desgloses_propuesta (DesglosePropuestaID, PropuestaID) ON DELETE RESTRICT ON UPDATE CASCADE;

CREATE TABLE IF NOT EXISTS aprobaciones_propuesta (
    AprobacionID INT NOT NULL AUTO_INCREMENT,
    SolicitudID INT NOT NULL,
    PropuestaID INT NOT NULL,
    Usuario VARCHAR(50) NOT NULL,
    FechaAprobacion DATETIME NOT NULL,
    DatosAnterioresJson LONGTEXT NOT NULL,
    DatosNuevosJson LONGTEXT NOT NULL,
    PRIMARY KEY (AprobacionID),
    UNIQUE KEY uq_aprobaciones_solicitud (SolicitudID),
    UNIQUE KEY uq_aprobaciones_propuesta (PropuestaID),
    CONSTRAINT fk_aprobaciones_propuesta_solicitud
        FOREIGN KEY (PropuestaID, SolicitudID)
        REFERENCES propuestas (PropuestaID, SolicitudID)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Copia histórica de todas las columnas reales de desgloses. DesgloseID se
-- conserva como dato de auditoría, sin FK a la tabla activa que se reemplaza.
CREATE TABLE IF NOT EXISTS historial_desgloses (
    HistorialDesgloseID INT NOT NULL AUTO_INCREMENT,
    AprobacionID INT NOT NULL,
    DesgloseID INT NOT NULL,
    SolicitudID INT NOT NULL,
    DesglosePropuestaID INT NULL,
    PropuestaID INT NULL,
    Mes TINYINT UNSIGNED NULL,
    Anio SMALLINT UNSIGNED NULL,
    Horas DECIMAL(10,2) NULL,
    Monto DECIMAL(14,2) NULL,
    IVA DECIMAL(14,2) NULL,
    Total DECIMAL(15,2) NULL,
    Observaciones TEXT NULL,
    PorcentajeCobro DECIMAL(5,2) NULL,
    PRIMARY KEY (HistorialDesgloseID),
    UNIQUE KEY uq_historial_aprobacion_desglose (AprobacionID, DesgloseID),
    CONSTRAINT fk_historial_desgloses_aprobacion
        FOREIGN KEY (AprobacionID) REFERENCES aprobaciones_propuesta (AprobacionID)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
