CREATE TABLE Investment_companies (
id int(11) NOT NULL,
  name varchar(100) NOT NULL
);
CREATE TABLE Investment_projects (
id int(11) NOT NULL,
  name varchar(150) NOT NULL,
  company_id int(11) NOT NULL,
  investment decimal(15,2) NOT NULL,
  annual_income decimal(15,2) NOT NULL
);
CREATE TABLE Investment_rates (
id int(11) NOT NULL,
  company_id int(11) NOT NULL,
  min_amount decimal(15,2) NOT NULL,
  max_amount decimal(15,2) NOT NULL
);
CREATE TABLE Products (
id int(11) NOT NULL,
  name varchar(150) NOT NULL,
  sku varchar(50) NOT NULL,
  price decimal(15,2) NOT NULL,
  image_path varchar(255) DEFAULT NULL
);
CREATE TABLE Product_recipes (
product_id int(11) NOT NULL,
  resource_id int(11) NOT NULL,
  consumption decimal(10,4) NOT NULL
);
CREATE TABLE Resources (
id int(11) NOT NULL,
  name varchar(100) NOT NULL,
  unit varchar(20) NOT NULL
);
CREATE TABLE Stores (
id int(11) NOT NULL,
  name varchar(100) NOT NULL,
  address varchar(255) DEFAULT NULL,
  phone varchar(20) DEFAULT NULL,
  demand int(11) NOT NULL DEFAULT '0',
  customer_flow int(11) NOT NULL DEFAULT '0',
  service_rate int(11) NOT NULL DEFAULT '0',
  register_limit int(11) NOT NULL DEFAULT '0'
);
CREATE TABLE Store_warehouse_distances (
store_id int(11) NOT NULL,
  warehouse_id int(11) NOT NULL,
  distance_km decimal(8,2) NOT NULL
);
CREATE TABLE Users (
id int(11) NOT NULL,
  login varchar(50) NOT NULL,
  password_hash varchar(100) NOT NULL,
  role varchar(20) NOT NULL,
  full_name varchar(150) NOT NULL
);
CREATE TABLE Warehouses (
id int(11) NOT NULL,
  name varchar(100) NOT NULL,
  address varchar(255) DEFAULT NULL
);
CREATE TABLE Warehouse_stocks (
warehouse_id int(11) NOT NULL,
  product_id int(11) NOT NULL,
  quantity int(11) NOT NULL
);
ALTER TABLE Investment_projects ADD CONSTRAINT fk_projects_company FOREIGN KEY (company_id) REFERENCES Investment_companies (id);
ALTER TABLE Investment_rates ADD CONSTRAINT fk_rates_company FOREIGN KEY (company_id) REFERENCES Investment_companies (id);
ALTER TABLE Product_recipes ADD CONSTRAINT fk_recipes_product FOREIGN KEY (product_id) REFERENCES Products (id);
ALTER TABLE Store_warehouse_distances ADD CONSTRAINT fk_distances_store FOREIGN KEY (store_id) REFERENCES Stores (id);
ALTER TABLE Warehouse_stocks ADD CONSTRAINT fk_stocks_product FOREIGN KEY (product_id) REFERENCES Products (id);