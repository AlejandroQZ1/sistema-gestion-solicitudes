-- Base comun del sistema. Solo para una base nueva o vacia.
-- No elimina tablas existentes. Si ya existen, la importacion debe detenerse.
CREATE DATABASE IF NOT EXISTS siges CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE siges;
SET NAMES utf8mb4;
CREATE TABLE `usuarios` (
  `UsuarioID` int NOT NULL AUTO_INCREMENT,
  `NombreUsuario` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `NombreCompleto` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Correo` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Contrasena` varchar(500) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Estado` varchar(20) COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'Activo',
  `IntentosFallidos` int NOT NULL DEFAULT '0',
  PRIMARY KEY (`UsuarioID`),
  UNIQUE KEY `UQ_usuarios_NombreUsuario` (`NombreUsuario`),
  CONSTRAINT `CK_usuarios_Estado` CHECK ((`Estado` in (_utf8mb4'Activo',_utf8mb4'Inactivo',_utf8mb4'Bloqueado')))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `bitacora` (
  `BitacoraID` int NOT NULL AUTO_INCREMENT,
  `Fecha` datetime NOT NULL,
  `Usuario` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Modulo` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Accion` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Detalle` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`BitacoraID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `representantes` (
  `RepresentanteID` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Email` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`RepresentanteID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `estados_solicitud` (
  `EstadoSolicitudID` int NOT NULL AUTO_INCREMENT,
  `Identificador` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Nombre` varchar(150) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`EstadoSolicitudID`),
  UNIQUE KEY `UQ_estados_identificador` (`Identificador`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `solicitudes` (
  `SolicitudID` int NOT NULL AUTO_INCREMENT,
  `ConsecutivoOficio` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `DocumentoRespuesta` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `DocumentoInicio` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `Titulo` varchar(200) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Descripcion` text COLLATE utf8mb4_unicode_ci,
  `RepresentanteID` int DEFAULT NULL,
  `Observaciones` text COLLATE utf8mb4_unicode_ci,
  `EstadoSolicitudID` int NOT NULL,
  `FechaIngreso` date NOT NULL,
  `FechaRespuesta` date DEFAULT NULL,
  `FechaInicio` date DEFAULT NULL,
  PRIMARY KEY (`SolicitudID`),
  UNIQUE KEY `UQ_solicitudes_ConsecutivoOficio` (`ConsecutivoOficio`),
  KEY `FK_solicitudes_representantes` (`RepresentanteID`),
  KEY `FK_solicitudes_estados` (`EstadoSolicitudID`),
  CONSTRAINT `FK_solicitudes_estados` FOREIGN KEY (`EstadoSolicitudID`) REFERENCES `estados_solicitud` (`EstadoSolicitudID`) ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_solicitudes_representantes` FOREIGN KEY (`RepresentanteID`) REFERENCES `representantes` (`RepresentanteID`) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- TAR1. La aplicacion obtiene el usuario predeterminado de la sesion.
-- Los campos opcionales conservan la ausencia de dato mediante NULL.
CREATE TABLE tareas (
    TareaID INT NOT NULL AUTO_INCREMENT,
    SolicitudID INT NOT NULL,
    DescripcionTarea TEXT NULL,
    Fecha DATE NULL,
    UsuarioID INT NOT NULL,
    HorasInvertidas DECIMAL(10,2) NULL,
    PRIMARY KEY (TareaID),
    CONSTRAINT FK_tareas_solicitudes FOREIGN KEY (SolicitudID)
        REFERENCES solicitudes (SolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT FK_tareas_usuarios FOREIGN KEY (UsuarioID)
        REFERENCES usuarios (UsuarioID) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- SOL2. Se incorpora la estructura; su mantenimiento corresponde al modulo de solicitudes.
-- Las formulas de IVA y total provienen de la historia SOL2.
CREATE TABLE desgloses (
    DesgloseID INT NOT NULL AUTO_INCREMENT,
    SolicitudID INT NOT NULL,
    Mes TINYINT UNSIGNED NULL,
    Anio SMALLINT UNSIGNED NULL,
    Horas DECIMAL(10,2) NULL,
    Monto DECIMAL(14,2) NULL,
    IVA DECIMAL(14,2) GENERATED ALWAYS AS (ROUND(Monto * 0.13, 2)) STORED,
    Total DECIMAL(15,2) GENERATED ALWAYS AS (Monto + IVA) STORED,
    Observaciones TEXT NULL,
    PorcentajeCobro DECIMAL(5,2) NULL,
    PRIMARY KEY (DesgloseID),
    CONSTRAINT FK_desgloses_solicitudes FOREIGN KEY (SolicitudID)
        REFERENCES solicitudes (SolicitudID) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Estados iniciales definidos en SOL4.
INSERT INTO `estados_solicitud` VALUES (1,'NUEVA','Nueva'),(2,'RESPONDIDA','Respondida'),(3,'INICIADA','Iniciada'),(4,'FINALIZADA','Finalizada'),(5,'VENCIDA','Vencida');
