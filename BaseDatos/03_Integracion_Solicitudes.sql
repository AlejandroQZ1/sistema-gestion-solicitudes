-- Para la base existente de Joel y Camila. No elimina datos.
-- Si ya existe solicitudes, debe usar el esquema acordado con el equipo.
USE siges;
CREATE TABLE IF NOT EXISTS solicitudes (
  SolicitudID int NOT NULL AUTO_INCREMENT,
  ConsecutivoOficio varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  DocumentoRespuesta varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  DocumentoInicio varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  Titulo varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  Descripcion text COLLATE utf8mb4_unicode_ci,
  RepresentanteID int DEFAULT NULL,
  Observaciones text COLLATE utf8mb4_unicode_ci,
  EstadoSolicitudID int NOT NULL,
  FechaIngreso date NOT NULL,
  FechaRespuesta date DEFAULT NULL,
  FechaInicio date DEFAULT NULL,
  PRIMARY KEY (SolicitudID),
  UNIQUE KEY UQ_solicitudes_ConsecutivoOficio (ConsecutivoOficio),
  KEY FK_solicitudes_representantes (RepresentanteID),
  KEY FK_solicitudes_estados (EstadoSolicitudID),
  CONSTRAINT FK_solicitudes_estados FOREIGN KEY (EstadoSolicitudID) REFERENCES estados_solicitud (EstadoSolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT FK_solicitudes_representantes FOREIGN KEY (RepresentanteID) REFERENCES representantes (RepresentanteID) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Instala relaciones también si solicitudes ya existía sin claves foráneas.
-- Si hay datos huérfanos, MySQL detendrá el cambio para que se revisen.
SET @sql_fk = IF(EXISTS (
 SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
 WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='solicitudes'
 AND COLUMN_NAME='RepresentanteID' AND REFERENCED_TABLE_NAME='representantes'
), 'SELECT 1', 'ALTER TABLE solicitudes ADD CONSTRAINT FK_solicitudes_representantes FOREIGN KEY (RepresentanteID) REFERENCES representantes(RepresentanteID) ON DELETE RESTRICT ON UPDATE CASCADE');
PREPARE relacion FROM @sql_fk;
EXECUTE relacion;
DEALLOCATE PREPARE relacion;
SET @sql_fk = IF(EXISTS (
 SELECT 1 FROM information_schema.KEY_COLUMN_USAGE
 WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='solicitudes'
 AND COLUMN_NAME='EstadoSolicitudID' AND REFERENCED_TABLE_NAME='estados_solicitud'
), 'SELECT 1', 'ALTER TABLE solicitudes ADD CONSTRAINT FK_solicitudes_estados FOREIGN KEY (EstadoSolicitudID) REFERENCES estados_solicitud(EstadoSolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE');
PREPARE relacion FROM @sql_fk;
EXECUTE relacion;
DEALLOCATE PREPARE relacion;
