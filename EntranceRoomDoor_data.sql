-- MySQL dump 10.13  Distrib 8.0.30, for Win64 (x86_64)
--
-- Host: localhost    Database: EntranceRoomDoors
-- ------------------------------------------------------
-- Server version	8.0.30

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Dumping data for table `Investment_companies`
--

LOCK TABLES `Investment_companies` WRITE;
/*!40000 ALTER TABLE `Investment_companies` DISABLE KEYS */;
INSERT INTO `Investment_companies` VALUES (1,'Alpha Invest'),(2,'Beta Capital');
/*!40000 ALTER TABLE `Investment_companies` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Investment_rates`
--

LOCK TABLES `Investment_rates` WRITE;
/*!40000 ALTER TABLE `Investment_rates` DISABLE KEYS */;
INSERT INTO `Investment_rates` VALUES (1,1,15000.00,30000.00),(2,1,22000.50,35000.00),(3,2,25000.00,36000.00),(4,2,28000.50,38000.00);
/*!40000 ALTER TABLE `Investment_rates` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Product_recipes`
--

LOCK TABLES `Product_recipes` WRITE;
/*!40000 ALTER TABLE `Product_recipes` DISABLE KEYS */;
INSERT INTO `Product_recipes` VALUES (1,1,0.2500),(1,2,0.1100),(2,1,0.4000),(2,2,0.0800);
/*!40000 ALTER TABLE `Product_recipes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Products`
--

LOCK TABLES `Products` WRITE;
/*!40000 ALTER TABLE `Products` DISABLE KEYS */;
INSERT INTO `Products` VALUES (1,'Дверь межкомнатная Модель А','SKU-001',12000.00),(2,'Дверь входная металлическая Модель Б','SKU-002',25000.00);
/*!40000 ALTER TABLE `Products` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Resources`
--

LOCK TABLES `Resources` WRITE;
/*!40000 ALTER TABLE `Resources` DISABLE KEYS */;
INSERT INTO `Resources` VALUES (1,'Древесина','м3'),(2,'Шпон','м2');
/*!40000 ALTER TABLE `Resources` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Store_warehouse_distances`
--

LOCK TABLES `Store_warehouse_distances` WRITE;
/*!40000 ALTER TABLE `Store_warehouse_distances` DISABLE KEYS */;
INSERT INTO `Store_warehouse_distances` VALUES (1,1,12.50),(2,2,34.00);
/*!40000 ALTER TABLE `Store_warehouse_distances` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Stores`
--

LOCK TABLES `Stores` WRITE;
/*!40000 ALTER TABLE `Stores` DISABLE KEYS */;
INSERT INTO `Stores` VALUES (1,'Магазин Центральный',NULL,NULL),(2,'Магазин Южный',NULL,NULL);
/*!40000 ALTER TABLE `Stores` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Users`
--

LOCK TABLES `Users` WRITE;
/*!40000 ALTER TABLE `Users` DISABLE KEYS */;
INSERT INTO `Users` VALUES (1,'ivanov','3c690962c2439fbde896afa40ce72493053359fd462cc3d3a07861865a0d0ded','administrator','Иванов Иван Иванович'),(2,'smirnova','8af1327d424e001284fde5e63d437f3747a43c10b77733b7b2f80d1baa294b73','administrator','Смирнова Анна Сергеевна'),(3,'kuznetsov','093c4126cc06e20057cb0f026993e57a8a5077c2af9f167340939fdbedadfb57','director','Кузнецов Сергей Петрович'),(4,'popova','a950818c31fe32408d16609c5920fddd1e08a2900f7ef773eaa763c1f9f0339e','director','Попова Ольга Викторовна'),(5,'sokolov','947ee99ce3341aa39a2d296b91fd54b54a84077517b873db3b0106be8b77b29a','manager','Соколов Дмитрий Андреевич'),(6,'lebedeva','a8a927b0282a534852b40bf6c957455137fcf6633387b99d163fdf2b708d7471','manager','Лебедева Елена Михайловна'),(7,'morozov','1d8fce9ef16b20ef151abdbf814f6bf5ae92846f515cd2ebf76f91aec327b4e9','manager','Морозов Алексей Николаевич'),(8,'novikova','9e5067ff4517c9e38c3ed046a7c4816c2c56f5db07cffd60052c65e5b6d8aa97','manager','Новикова Марина Игоревна'),(9,'volkov','c33c0aa3c9a463c9f04043e676185c6affa215e5ef49f9d33cd63f3208972ad2','manager','Волков Павел Дмитриевич'),(10,'zakharova','af08519519d0795ded8fa5e8276a7e6ec57d5446cc4cf90a9c41bfbeb8ac3186','manager','Захарова Татьяна Юрьевна');
/*!40000 ALTER TABLE `Users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Warehouse_stocks`
--

LOCK TABLES `Warehouse_stocks` WRITE;
/*!40000 ALTER TABLE `Warehouse_stocks` DISABLE KEYS */;
INSERT INTO `Warehouse_stocks` VALUES (1,1,100),(2,2,50);
/*!40000 ALTER TABLE `Warehouse_stocks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping data for table `Warehouses`
--

LOCK TABLES `Warehouses` WRITE;
/*!40000 ALTER TABLE `Warehouses` DISABLE KEYS */;
INSERT INTO `Warehouses` VALUES (1,'Основной склад',NULL),(2,'Северный склад',NULL);
/*!40000 ALTER TABLE `Warehouses` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-11-04 15:48:55
