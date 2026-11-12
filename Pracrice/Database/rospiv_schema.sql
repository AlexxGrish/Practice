SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `RosPiv` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE `RosPiv`;
CREATE TABLE `Investment_companies` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Investment_rates` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `company_id` int(11) NOT NULL,
  `min_amount` decimal(15,2) NOT NULL,
  `max_amount` decimal(15,2) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_Investment_rates_company_id` (`company_id`),
  CONSTRAINT `fk_rates_company` FOREIGN KEY (`company_id`) REFERENCES `Investment_companies` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Product_recipes` (
  `product_id` int(11) NOT NULL,
  `resource_id` int(11) NOT NULL,
  `consumption` decimal(10,4) NOT NULL,
  PRIMARY KEY (`product_id`,`resource_id`),
  KEY `IX_Product_recipes_resource_id` (`resource_id`),
  CONSTRAINT `fk_recipes_product` FOREIGN KEY (`product_id`) REFERENCES `Products` (`id`),
  CONSTRAINT `fk_recipes_resource` FOREIGN KEY (`resource_id`) REFERENCES `Resources` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Products` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(150) NOT NULL,
  `sku` varchar(50) NOT NULL,
  `price` decimal(15,2) NOT NULL,
  `image_path` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `sku` (`sku`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Resources` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `unit` varchar(20) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Store_warehouse_distances` (
  `store_id` int(11) NOT NULL,
  `warehouse_id` int(11) NOT NULL,
  `distance_km` decimal(8,2) NOT NULL,
  PRIMARY KEY (`store_id`,`warehouse_id`),
  KEY `IX_Store_warehouse_distances_warehouse_id` (`warehouse_id`),
  CONSTRAINT `fk_distances_store` FOREIGN KEY (`store_id`) REFERENCES `Stores` (`id`),
  CONSTRAINT `fk_distances_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `Warehouses` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Stores` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `address` varchar(255) DEFAULT NULL,
  `phone` varchar(20) DEFAULT NULL,
  `demand` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `login` varchar(50) NOT NULL,
  `password_hash` varchar(100) NOT NULL,
  `role` varchar(20) NOT NULL,
  `full_name` varchar(150) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `login` (`login`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Warehouse_stocks` (
  `warehouse_id` int(11) NOT NULL,
  `product_id` int(11) NOT NULL,
  `quantity` int(11) NOT NULL,
  PRIMARY KEY (`warehouse_id`,`product_id`),
  KEY `IX_Warehouse_stocks_product_id` (`product_id`),
  CONSTRAINT `fk_stocks_product` FOREIGN KEY (`product_id`) REFERENCES `Products` (`id`),
  CONSTRAINT `fk_stocks_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `Warehouses` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE `Warehouses` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `address` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
