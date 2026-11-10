-- EXT7: aprobación única, historial y reemplazo transaccional de desgloses.
-- Compatible con MariaDB 10.5.29.
--
-- PRECONDICIONES:
-- 1. La estructura EXT4 (proveedores, propuestas, desgloses_propuesta) debe
--    existir en InnoDB. Este script NO altera solicitudes.Publicada/Publicado.
-- 2. PropuestaID, SolicitudID y DesglosePropuestaID deben ser INT firmado,
--    como en el respaldo real y en 04_Modulo_Externo.sql.
-- 3. Revisar antes de aplicar que los objetos con los nombres de abajo, si ya
--    existen, tengan las definiciones esperadas. Los IF NOT EXISTS lógicos
--    permiten reanudar una aplicación parcial o repetir este script.
-- 4. La aprobación elimina de desgloses las filas reemplazadas, tras copiar
--    todas sus columnas al historial dentro de una transacción. El respaldo
--    revisado no muestra FKs entrantes a desgloses.DesgloseID.
--
-- El script no elimina, trunca ni actualiza filas existentes. DDL MariaDB
-- realiza commits implícitos; por eso cada objeto se verifica antes de crearlo.

-- Verificar que las tablas fuente requeridas existen y son transaccionales.
SELECT TABLE_NAME, ENGINE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('solicitudes', 'desgloses', 'propuestas',
                     'desgloses_propuesta', 'proveedores');

-- Índices compuestos requeridos para las FKs EXT7.
SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'propuestas'
      AND INDEX_NAME = 'uq_propuestas_id_solicitud'
), 'SELECT 1',
   'ALTER TABLE propuestas ADD UNIQUE KEY uq_propuestas_id_solicitud (PropuestaID, SolicitudID)');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses_propuesta'
      AND INDEX_NAME = 'uq_desgloses_propuesta_id_propuesta'
), 'SELECT 1',
   'ALTER TABLE desgloses_propuesta ADD UNIQUE KEY uq_desgloses_propuesta_id_propuesta (DesglosePropuestaID, PropuestaID)');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

-- Columnas de trazabilidad: separar cada ALTER permite reanudar si una
-- instrucción previa quedó aplicada antes de un error.
SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND COLUMN_NAME = 'PropuestaID'
), 'SELECT 1', 'ALTER TABLE desgloses ADD COLUMN PropuestaID INT NULL');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND COLUMN_NAME = 'DesglosePropuestaID'
), 'SELECT 1', 'ALTER TABLE desgloses ADD COLUMN DesglosePropuestaID INT NULL');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND INDEX_NAME = 'uq_desgloses_linea_propuesta'
), 'SELECT 1',
   'ALTER TABLE desgloses ADD UNIQUE KEY uq_desgloses_linea_propuesta (DesglosePropuestaID)');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND INDEX_NAME = 'ix_desgloses_propuesta_solicitud'
), 'SELECT 1',
   'ALTER TABLE desgloses ADD KEY ix_desgloses_propuesta_solicitud (PropuestaID, SolicitudID)');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

-- Índice hijo compuesto explícito para que MariaDB no dependa de un índice
-- implícito al crear la FK de línea propuesta.
SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND INDEX_NAME = 'ix_desgloses_linea_propuesta_fk'
), 'SELECT 1',
   'ALTER TABLE desgloses ADD KEY ix_desgloses_linea_propuesta_fk (DesglosePropuestaID, PropuestaID)');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

-- Restricciones verificadas por nombre y tabla; si una restricción homónima
-- preexistente tiene otra definición, el administrador debe resolverla antes.
SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND CONSTRAINT_NAME = 'fk_desgloses_propuestas_solicitud'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
), 'SELECT 1',
   'ALTER TABLE desgloses ADD CONSTRAINT fk_desgloses_propuestas_solicitud FOREIGN KEY (PropuestaID, SolicitudID) REFERENCES propuestas (PropuestaID, SolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

SET @ext7_sql = IF(EXISTS (
    SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = 'desgloses'
      AND CONSTRAINT_NAME = 'fk_desgloses_propuesta_linea'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
), 'SELECT 1',
   'ALTER TABLE desgloses ADD CONSTRAINT fk_desgloses_propuesta_linea FOREIGN KEY (DesglosePropuestaID, PropuestaID) REFERENCES desgloses_propuesta (DesglosePropuestaID, PropuestaID) ON DELETE RESTRICT ON UPDATE CASCADE');
PREPARE ext7_stmt FROM @ext7_sql; EXECUTE ext7_stmt; DEALLOCATE PREPARE ext7_stmt;

-- Una aprobación por solicitud impide duplicados incluso ante concurrencia.
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
