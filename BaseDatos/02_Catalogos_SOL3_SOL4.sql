-- Ejecutar después de 01_Seguridad_Usuarios_Bitacora.sql.
USE siges;
CREATE TABLE IF NOT EXISTS representantes (
    RepresentanteID INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Email VARCHAR(150) NOT NULL
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS estados_solicitud (
    EstadoSolicitudID INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Identificador VARCHAR(50) NOT NULL,
    Nombre VARCHAR(150) NOT NULL,
    CONSTRAINT UQ_estados_identificador UNIQUE (Identificador)
) ENGINE=InnoDB;
INSERT INTO estados_solicitud (Identificador, Nombre) VALUES
('NUEVA', 'Nueva'), ('RESPONDIDA', 'Respondida'), ('INICIADA', 'Iniciada'),
('FINALIZADA', 'Finalizada'), ('VENCIDA', 'Vencida')
ON DUPLICATE KEY UPDATE Identificador = estados_solicitud.Identificador;

-- Contrato de integración con SOL1: la tabla solicitudes debe tener
-- RepresentanteID y EstadoSolicitudID, con el mismo tipo INT y estas FK:
-- FOREIGN KEY (RepresentanteID) REFERENCES representantes(RepresentanteID) ON DELETE RESTRICT
-- FOREIGN KEY (EstadoSolicitudID) REFERENCES estados_solicitud(EstadoSolicitudID) ON DELETE RESTRICT
-- No ejecutar ALTER sobre una tabla existente sin verificar el esquema acordado.
