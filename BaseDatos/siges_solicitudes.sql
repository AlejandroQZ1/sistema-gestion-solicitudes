-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: siges
-- ------------------------------------------------------
-- Server version	8.0.46

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `solicitudes`
--

DROP TABLE IF EXISTS `solicitudes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
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
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `solicitudes`
--

LOCK TABLES `solicitudes` WRITE;
/*!40000 ALTER TABLE `solicitudes` DISABLE KEYS */;
INSERT INTO `solicitudes` VALUES (1,'OF-2026-001',NULL,NULL,'Solicitud de prueba',NULL,1,NULL,1,'2026-10-10',NULL,NULL);
/*!40000 ALTER TABLE `solicitudes` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-10-10 22:20:14
