SET NAMES utf8mb4;
USE RosPiv;
SET FOREIGN_KEY_CHECKS = 0;
DELETE FROM Product_recipes;
DELETE FROM Warehouse_stocks;
DELETE FROM Store_warehouse_distances;
DELETE FROM Products;
DELETE FROM Resources;
DELETE FROM Warehouses;
DELETE FROM Stores;
DELETE FROM Investment_rates;
DELETE FROM Investment_companies;
DELETE FROM Users WHERE login <> 'admin';
UPDATE Users SET role = 'administrator', full_name = 'Иванов Иван Иванович' WHERE login = 'admin';
INSERT INTO Users (id, login, password_hash, role, full_name) VALUES
(2, 'director', '$2a$11$82SopweySumTeW8WORAxreapSsdHCJyjb81yHidJFgrxTxaWhGVUu', 'director', 'Смирнов Алексей Владимирович'),
(3, 'manager', '$2a$11$LUTXRwCNsrDL.7CGw9pTKuxyifxFzjD2GaP5xHzV3nG90YWFgD4Hy', 'manager', 'Петрова Анна Сергеевна'),
(4, 'manager2', '$2a$11$8/Bkm1or/l7hoPxzen4bCu0a32QurJflqiHKmczB1ia.vC8JrHuGO', 'manager', 'Соколов Дмитрий Андреевич'),
(5, 'manager3', '$2a$11$DzQNPMfCGllKESKSXCrCb.8RimTYRYR7teJGsQgYuDz2wTPGiGSom', 'manager', 'Кузнецова Мария Викторовна'),
(6, 'storekeeper', '$2a$11$SNyT7UbyvMVyHMrvsFX2wOfWIha1IDEjwNeVMHFy3sGIXVkZ6ZqM2', 'manager', 'Волков Павел Дмитриевич');
INSERT INTO Products (id, name, sku, price, image_path) VALUES
(1, 'Пиво светлое «Роспив Классическое»', 'BEER-001', 450.00, 'images/beer-classic.png'),
(2, 'Пиво тёмное «Роспив Бархатное»', 'BEER-002', 520.00, 'images/beer-dark.png'),
(3, 'Пиво пшеничное «Роспив Пшеничное»', 'BEER-003', 490.00, 'images/beer-wheat.png'),
(4, 'Пиво IPA «Роспив Хмельное»', 'BEER-004', 580.00, 'images/beer-ipa.png'),
(5, 'Пиво безалкогольное «Роспив Лёгкое»', 'BEER-005', 390.00, 'images/beer-light.png');
INSERT INTO Resources (id, name, unit) VALUES
(1, 'Солод светлый', 'кг'),
(2, 'Солод тёмный', 'кг'),
(3, 'Солод пшеничный', 'кг'),
(4, 'Хмель', 'кг'),
(5, 'Дрожжи', 'кг'),
(6, 'Вода', 'л'),
(7, 'Стеклотара', 'шт.'),
(8, 'Этикетка', 'шт.'),
(9, 'Крышка', 'шт.');
INSERT INTO Product_recipes (product_id, resource_id, consumption) VALUES
(1, 1, 0.0250), (1, 4, 0.0020), (1, 5, 0.0010), (1, 6, 0.5000), (1, 7, 1.0000), (1, 8, 1.0000), (1, 9, 1.0000),
(2, 2, 0.0260), (2, 4, 0.0025), (2, 5, 0.0010), (2, 6, 0.5000), (2, 7, 1.0000), (2, 8, 1.0000), (2, 9, 1.0000),
(3, 3, 0.0240), (3, 1, 0.0050), (3, 4, 0.0015), (3, 5, 0.0010), (3, 6, 0.5000), (3, 7, 1.0000), (3, 8, 1.0000), (3, 9, 1.0000),
(4, 1, 0.0300), (4, 4, 0.0060), (4, 5, 0.0010), (4, 6, 0.5500), (4, 7, 1.0000), (4, 8, 1.0000), (4, 9, 1.0000),
(5, 1, 0.0180), (5, 4, 0.0010), (5, 5, 0.0005), (5, 6, 0.6000), (5, 7, 1.0000), (5, 8, 1.0000), (5, 9, 1.0000);
INSERT INTO Warehouses (id, name, address) VALUES
(1, 'Основной склад', 'г. Москва, ул. Пивоваренная, 12'),
(2, 'Северный склад', 'г. Москва, ул. Северная, 48');
INSERT INTO Warehouse_stocks (warehouse_id, product_id, quantity) VALUES
(1, 1, 120), (1, 2, 80), (1, 3, 60), (1, 4, 45), (1, 5, 90),
(2, 1, 70), (2, 2, 50), (2, 3, 40), (2, 4, 30), (2, 5, 65);
INSERT INTO Stores (id, name, address, phone, demand) VALUES
(1, 'Магазин «Центральный»', 'г. Москва, ул. Тверская, 15', '+7 (495) 111-22-33', 250),
(2, 'Магазин «Южный»', 'г. Москва, ул. Южная, 7', '+7 (495) 222-33-44', 220),
(3, 'Магазин «Западный»', 'г. Москва, пр-т Западный, 21', '+7 (495) 333-44-55', 180);
INSERT INTO Store_warehouse_distances (store_id, warehouse_id, distance_km) VALUES
(1, 1, 12.50), (1, 2, 18.00),
(2, 1, 24.00), (2, 2, 9.50),
(3, 1, 31.50), (3, 2, 22.00);
INSERT INTO Investment_companies (id, name) VALUES
(1, 'Альфа-Инвест'),
(2, 'Бета-Капитал'),
(3, 'Северные инвестиции');
INSERT INTO Investment_rates (id, company_id, min_amount, max_amount) VALUES
(1, 1, 100000.00, 500000.00),
(2, 1, 500000.00, 1500000.00),
(3, 2, 200000.00, 800000.00),
(4, 2, 800000.00, 2500000.00),
(5, 3, 300000.00, 1200000.00);
ALTER TABLE Users AUTO_INCREMENT = 7;
ALTER TABLE Products AUTO_INCREMENT = 6;
ALTER TABLE Resources AUTO_INCREMENT = 10;
ALTER TABLE Warehouses AUTO_INCREMENT = 3;
ALTER TABLE Stores AUTO_INCREMENT = 4;
ALTER TABLE Investment_companies AUTO_INCREMENT = 4;
ALTER TABLE Investment_rates AUTO_INCREMENT = 6;
SET FOREIGN_KEY_CHECKS = 1;
