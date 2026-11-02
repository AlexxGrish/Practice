CREATE TABLE users (
    user_id INT NOT NULL PRIMARY KEY,
    login VARCHAR(50) NOT NULL,
    pass VARCHAR(255) NOT NULL,
    full_name VARCHAR(150) NOT NULL,
    role INT NOT NULL,
    last_changes TIMESTAMP
);

CREATE TABLE equipment (
    equipment_id INT NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    new_price DECIMAL(15, 2),
    old_price DECIMAL(15, 2),
    profit DECIMAL(15, 2),
    age INT
);

CREATE TABLE investments (
    investment_id INT NOT NULL PRIMARY KEY,
    total_resources INT,
    step_resource INT,
    profit_matrix TEXT
);

CREATE TABLE shops (
    shop_id INT NOT NULL PRIMARY KEY,
    address VARCHAR(255) NOT NULL,
    number_of_counters INT,
    avg_attendance DECIMAL(10, 2)
);

CREATE TABLE warehouses (
    warehouse_id INT NOT NULL PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    address VARCHAR(255),
    resources INT
);

CREATE TABLE products (
    product_id INT NOT NULL PRIMARY KEY,
    demand DECIMAL(12, 2),
    max_inventory DECIMAL(12, 2),
    max_production DECIMAL(12, 2),
    dp_table TEXT,
    choice_table TEXT
);

CREATE TABLE routes (
    route_id INT NOT NULL PRIMARY KEY,
    shop_id INT NOT NULL,
    warehouse_id INT NOT NULL,
    product_id INT NOT NULL,
    FOREIGN KEY (shop_id) REFERENCES shops(shop_id),
    FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
    FOREIGN KEY (product_id) REFERENCES products(product_id)
);

CREATE TABLE reports (
    report_id INT NOT NULL PRIMARY KEY,
    report_type INT NOT NULL,
    created_at TIMESTAMP,
    user_id INT,
    route_id INT,
    FOREIGN KEY (user_id) REFERENCES users(user_id),
    FOREIGN KEY (route_id) REFERENCES routes(route_id)
);
