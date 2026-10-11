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
-- Table structure for table `bitacora`
--

DROP TABLE IF EXISTS `bitacora`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bitacora` (
  `BitacoraID` int NOT NULL AUTO_INCREMENT,
  `Fecha` datetime NOT NULL,
  `Usuario` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Modulo` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Accion` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `Detalle` text COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`BitacoraID`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `bitacora`
--

LOCK TABLES `bitacora` WRITE;
/*!40000 ALTER TABLE `bitacora` DISABLE KEYS */;
INSERT INTO `bitacora` VALUES (1,'2026-10-10 19:38:08','admin','Representantes','El usuario consulta Representantes',NULL),(2,'2026-10-10 19:40:37','admin','Representantes','El usuario consulta Representantes',NULL),(3,'2026-10-10 19:40:38','admin','Representantes','El usuario consulta Representantes',NULL),(4,'2026-10-10 19:40:39','admin','Representantes','El usuario consulta Representantes',NULL),(5,'2026-10-10 19:40:46','admin','Representantes','El usuario consulta Representantes',NULL),(6,'2026-10-10 19:43:14','admin','Representantes','El usuario consulta Representantes',NULL),(7,'2026-10-10 19:43:14','admin','Solicitudes','Crear','{\"SolicitudID\":1,\"ConsecutivoOficio\":\"OF-2026-001\",\"Titulo\":\"Solicitud de prueba\",\"RepresentanteID\":1,\"EstadoSolicitudID\":1,\"FechaIngreso\":\"2026-10-10T00:00:00\",\"FechaRespuesta\":null,\"FechaInicio\":null}'),(8,'2026-10-10 21:39:05','admin','Representantes','El usuario consulta Representantes',NULL);
/*!40000 ALTER TABLE `bitacora` ENABLE KEYS */;
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
