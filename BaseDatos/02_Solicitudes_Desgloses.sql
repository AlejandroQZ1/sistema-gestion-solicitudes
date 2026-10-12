/* =============================================================
   SIGES - Tablas del módulo de Solicitudes y Desgloses (SOL1, SOL2, SOL7)
   Base de datos: MySQL 8
   Ejecutar en MySQL Workbench o consola MySQL después de 01_Seguridad_Usuarios_Bitacora.sql
   ============================================================= */

USE siges;
SET NAMES utf8mb4;

-- Tabla de Solicitudes (base para desgloses y reportes)
CREATE TABLE IF NOT EXISTS solicitudes (
    SolicitudID         INT           NOT NULL AUTO_INCREMENT,
    ConsecutivoOficio   VARCHAR(50)   NOT NULL,
    DocumentoRespuesta  VARCHAR(100)  NULL,
    DocumentoInicio     VARCHAR(100)  NULL,
    Titulo              VARCHAR(200)  NOT NULL,
    Descripcion         TEXT          NULL,
    RepresentanteID     INT           NULL,
    Observaciones       TEXT          NULL,
    Estado              VARCHAR(30)   NOT NULL DEFAULT 'Nueva',
    FechaIngreso        DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaRespuesta      DATETIME      NULL,
    FechaInicio         DATETIME      NULL,
    CONSTRAINT PK_solicitudes PRIMARY KEY (SolicitudID),
    CONSTRAINT UQ_solicitudes_Consecutivo UNIQUE (ConsecutivoOficio)
) ENGINE = InnoDB;

-- Tabla de Desgloses (SOL2)
CREATE TABLE IF NOT EXISTS desgloses (
    DesgloseID       INT            NOT NULL AUTO_INCREMENT,
    SolicitudID      INT            NOT NULL,
    Mes              INT            NOT NULL,
    Anio             INT            NOT NULL,
    Horas            DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
    Monto            DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    IVA              DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    Total            DECIMAL(18,2)  NOT NULL DEFAULT 0.00,
    Observaciones    TEXT           NULL,
    PorcentajeCobro  DECIMAL(5,2)   NOT NULL DEFAULT 100.00,
    Asignado         TINYINT(1)     NOT NULL DEFAULT 0,
    CONSTRAINT PK_desgloses PRIMARY KEY (DesgloseID),
    CONSTRAINT FK_desgloses_solicitudes FOREIGN KEY (SolicitudID)
        REFERENCES solicitudes (SolicitudID) ON DELETE RESTRICT
) ENGINE = InnoDB;

-- Datos de prueba para demostración y verificación de HU SOL1, SOL2 y SOL7
INSERT IGNORE INTO solicitudes (SolicitudID, ConsecutivoOficio, Titulo, Descripcion, Estado, FechaIngreso)
VALUES 
(1, 'SOL-2026-001', 'Mantenimiento preventivo de servidores web', 'Revisión y optimización de infraestructura web pública', 'Nueva', NOW()),
(2, 'SOL-2026-002', 'Desarrollo de módulo de facturación electrónica', 'Implementación de integración con Hacienda para facturación', 'Iniciada', DATE_SUB(NOW(), INTERVAL 3 DAY)),
(3, 'SOL-2026-003', 'Auditoría de seguridad y pentesting', 'Evaluación de vulnerabilidades del portal institucional', 'Respondida', DATE_SUB(NOW(), INTERVAL 10 DAY));

INSERT IGNORE INTO desgloses (DesgloseID, SolicitudID, Mes, Anio, Horas, Monto, IVA, Total, Observaciones, PorcentajeCobro, Asignado)
VALUES
(1, 1, 10, 2026, 40.00, 500000.00, 65000.00, 565000.00, 'Fase 1: Mantenimiento mensual preventivo', 100.00, 0),
(2, 2, 10, 2026, 60.00, 850000.00, 110500.00, 960500.00, 'Entrega de sprint 1 módulo facturación', 100.00, 1),
(3, 3, 10, 2026, 25.00, 320000.00, 416000.00 * 0.13, 320000.00 * 1.13, 'Informe inicial de vulnerabilidades', 50.00, 0),
(4, 1, 11, 2026, 35.00, 450000.00, 58500.00, 508500.00, 'Fase 2: Monitoreo y soporte', 100.00, 0);
