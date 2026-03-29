-- =============================================
-- PeruShopHub Seed Data
-- =============================================

-- Categories (27 total, hierarchical)
-- Root categories
INSERT INTO "Categories" ("Id", "Name", "Slug", "ParentId", "Icon", "IsActive", "ProductCount", "Order", "CreatedAt", "UpdatedAt") VALUES
('a0000001-0000-0000-0000-000000000001', 'Eletrônicos', 'eletronicos', NULL, 'devices', true, 0, 1, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000002', 'Informática', 'informatica', NULL, 'computer', true, 0, 2, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000003', 'Moda', 'moda', NULL, 'checkroom', true, 0, 3, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000004', 'Casa e Decoração', 'casa-e-decoracao', NULL, 'home', true, 0, 4, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000005', 'Esportes', 'esportes', NULL, 'sports_soccer', true, 0, 5, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000006', 'Beleza', 'beleza', NULL, 'spa', true, 0, 6, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000007', 'Automotivo', 'automotivo', NULL, 'directions_car', true, 0, 7, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000008', 'Brinquedos', 'brinquedos', NULL, 'toys', true, 0, 8, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000009', 'Livros', 'livros', NULL, 'menu_book', true, 0, 9, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000010', 'Ferramentas', 'ferramentas', NULL, 'build', true, 0, 10, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000011', 'Saúde', 'saude', NULL, 'health_and_safety', true, 0, 11, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000012', 'Alimentos', 'alimentos', NULL, 'restaurant', true, 0, 12, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000013', 'Pet', 'pet', NULL, 'pets', true, 0, 13, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000014', 'Jardim', 'jardim', NULL, 'yard', true, 0, 14, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000015', 'Escritório', 'escritorio', NULL, 'business', true, 0, 15, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000016', 'Bebês', 'bebes', NULL, 'child_friendly', true, 0, 16, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000017', 'Games', 'games', NULL, 'sports_esports', true, 0, 17, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000018', 'Música', 'musica', NULL, 'music_note', true, 0, 18, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000019', 'Filmes', 'filmes', NULL, 'movie', true, 0, 19, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000020', 'Papelaria', 'papelaria', NULL, 'edit_note', true, 0, 20, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000021', 'Telefonia', 'telefonia', NULL, 'phone_android', true, 0, 21, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000022', 'Tablets', 'tablets', NULL, 'tablet', true, 0, 22, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000023', 'Câmeras', 'cameras', NULL, 'photo_camera', true, 0, 23, NOW(), NOW());

-- Child categories
INSERT INTO "Categories" ("Id", "Name", "Slug", "ParentId", "Icon", "IsActive", "ProductCount", "Order", "CreatedAt", "UpdatedAt") VALUES
('a0000001-0000-0000-0000-000000000024', 'Celulares e Telefones', 'celulares-e-telefones', 'a0000001-0000-0000-0000-000000000001', 'smartphone', true, 3, 1, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000025', 'Acessórios de Celular', 'acessorios-de-celular', 'a0000001-0000-0000-0000-000000000001', 'phonelink_ring', true, 2, 2, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000026', 'Notebooks', 'notebooks', 'a0000001-0000-0000-0000-000000000002', 'laptop', true, 2, 1, NOW(), NOW()),
('a0000001-0000-0000-0000-000000000027', 'Acessórios de Informática', 'acessorios-de-informatica', 'a0000001-0000-0000-0000-000000000002', 'keyboard', true, 1, 2, NOW(), NOW());

-- Update root category product counts
UPDATE "Categories" SET "ProductCount" = 5 WHERE "Id" = 'a0000001-0000-0000-0000-000000000001';
UPDATE "Categories" SET "ProductCount" = 3 WHERE "Id" = 'a0000001-0000-0000-0000-000000000002';
UPDATE "Categories" SET "ProductCount" = 3 WHERE "Id" = 'a0000001-0000-0000-0000-000000000024';
UPDATE "Categories" SET "ProductCount" = 2 WHERE "Id" = 'a0000001-0000-0000-0000-000000000025';
UPDATE "Categories" SET "ProductCount" = 2 WHERE "Id" = 'a0000001-0000-0000-0000-000000000026';
UPDATE "Categories" SET "ProductCount" = 1 WHERE "Id" = 'a0000001-0000-0000-0000-000000000027';

-- Products (10 total)
INSERT INTO "Products" ("Id", "Sku", "Name", "Description", "CategoryId", "Price", "PurchaseCost", "PackagingCost", "Supplier", "Status", "NeedsReview", "IsActive", "Weight", "Height", "Width", "Length", "CreatedAt", "UpdatedAt") VALUES
('b0000001-0000-0000-0000-000000000001', 'SAM-S24-ULT-256', 'Samsung Galaxy S24 Ultra 256GB', 'Smartphone Samsung Galaxy S24 Ultra com 256GB de armazenamento, 12GB RAM, câmera de 200MP', 'a0000001-0000-0000-0000-000000000024', 7499.0000, 5200.0000, 15.0000, 'Samsung Brasil', 'Ativo', false, true, 0.2320, 16.3000, 7.9000, 0.8600, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000002', 'IPH-15-PRO-128', 'iPhone 15 Pro 128GB', 'Apple iPhone 15 Pro com chip A17 Pro, câmera de 48MP, corpo de titânio', 'a0000001-0000-0000-0000-000000000024', 8499.0000, 6100.0000, 18.0000, 'Apple Distribuidor', 'Ativo', false, true, 0.1870, 14.6000, 7.0700, 0.8300, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000003', 'XIA-RED-N13-128', 'Xiaomi Redmi Note 13 128GB', 'Xiaomi Redmi Note 13 com tela AMOLED 120Hz, câmera de 108MP', 'a0000001-0000-0000-0000-000000000024', 1299.0000, 750.0000, 12.0000, 'Xiaomi Oficial', 'Ativo', false, true, 0.1880, 16.1700, 7.4600, 0.7800, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000004', 'CAP-SAM-S24-SIL', 'Capinha Samsung Galaxy S24 Ultra Silicone', 'Capinha protetora de silicone premium para Samsung Galaxy S24 Ultra', 'a0000001-0000-0000-0000-000000000025', 49.9000, 8.5000, 3.0000, 'AcessóriosBR', 'Ativo', false, true, 0.0350, 17.0000, 8.5000, 1.2000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000005', 'PEL-VID-IPH15', 'Película de Vidro iPhone 15 Pro', 'Kit com 3 películas de vidro temperado 9H para iPhone 15 Pro', 'a0000001-0000-0000-0000-000000000025', 29.9000, 4.0000, 2.5000, 'AcessóriosBR', 'Ativo', true, true, 0.0200, 16.0000, 8.0000, 0.5000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000006', 'NTB-LEN-I5-16', 'Notebook Lenovo IdeaPad 3i Intel i5 16GB', 'Notebook Lenovo IdeaPad 3i com Intel Core i5-1235U, 16GB RAM, SSD 512GB', 'a0000001-0000-0000-0000-000000000026', 3299.0000, 2400.0000, 25.0000, 'Lenovo Brasil', 'Ativo', false, true, 1.6300, 22.0000, 32.5000, 2.1000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000007', 'NTB-ASUS-R5-8', 'Notebook ASUS VivoBook 15 Ryzen 5 8GB', 'Notebook ASUS VivoBook 15 com AMD Ryzen 5 5600H, 8GB RAM, SSD 256GB', 'a0000001-0000-0000-0000-000000000026', 2599.0000, 1850.0000, 25.0000, 'ASUS Brasil', 'Ativo', false, true, 1.8000, 23.5000, 35.9000, 2.3000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000008', 'TEC-LOG-K380', 'Teclado Bluetooth Logitech K380', 'Teclado multidispositivo Bluetooth Logitech K380, layout ABNT2', 'a0000001-0000-0000-0000-000000000027', 249.0000, 140.0000, 10.0000, 'Logitech BR', 'Ativo', false, true, 0.4230, 12.4000, 27.9000, 1.6000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000009', 'FON-JBL-T520', 'Fone de Ouvido JBL Tune 520BT', 'Fone de ouvido Bluetooth JBL Tune 520BT com até 57h de bateria', 'a0000001-0000-0000-0000-000000000001', 199.0000, 95.0000, 8.0000, 'JBL/Harman', 'Ativo', false, true, 0.1550, 20.0000, 17.0000, 7.0000, NOW(), NOW()),
('b0000001-0000-0000-0000-000000000010', 'CAR-SAN-64-ULT', 'Cartão de Memória SanDisk Ultra 64GB', 'Cartão microSDXC SanDisk Ultra 64GB, velocidade até 140MB/s, Classe 10', 'a0000001-0000-0000-0000-000000000001', 59.9000, 25.0000, 2.0000, 'SanDisk/WD', 'Ativo', false, true, 0.0050, 10.0000, 7.0000, 0.3000, NOW(), NOW());

-- Product Variants (12 across 3 products)
INSERT INTO "ProductVariants" ("Id", "ProductId", "Sku", "Attributes", "Price", "Stock", "IsActive", "NeedsReview", "PurchaseCost", "Weight", "Height", "Width", "Length") VALUES
('c0000001-0000-0000-0000-000000000001', 'b0000001-0000-0000-0000-000000000001', 'SAM-S24-ULT-256-BLK', '{"cor": "Preto Titânio"}', 7499.0000, 15, true, false, 5200.0000, 0.2320, 16.3000, 7.9000, 0.8600),
('c0000001-0000-0000-0000-000000000002', 'b0000001-0000-0000-0000-000000000001', 'SAM-S24-ULT-256-GRY', '{"cor": "Cinza Titânio"}', 7499.0000, 8, true, false, 5200.0000, 0.2320, 16.3000, 7.9000, 0.8600),
('c0000001-0000-0000-0000-000000000003', 'b0000001-0000-0000-0000-000000000001', 'SAM-S24-ULT-256-VIO', '{"cor": "Violeta Titânio"}', 7499.0000, 5, true, false, 5200.0000, 0.2320, 16.3000, 7.9000, 0.8600),
('c0000001-0000-0000-0000-000000000004', 'b0000001-0000-0000-0000-000000000001', 'SAM-S24-ULT-256-YEL', '{"cor": "Amarelo Titânio"}', 7499.0000, 3, true, false, 5200.0000, 0.2320, 16.3000, 7.9000, 0.8600),
('c0000001-0000-0000-0000-000000000005', 'b0000001-0000-0000-0000-000000000002', 'IPH-15-PRO-128-NAT', '{"cor": "Titânio Natural"}', 8499.0000, 10, true, false, 6100.0000, 0.1870, 14.6000, 7.0700, 0.8300),
('c0000001-0000-0000-0000-000000000006', 'b0000001-0000-0000-0000-000000000002', 'IPH-15-PRO-128-BLU', '{"cor": "Titânio Azul"}', 8499.0000, 7, true, false, 6100.0000, 0.1870, 14.6000, 7.0700, 0.8300),
('c0000001-0000-0000-0000-000000000007', 'b0000001-0000-0000-0000-000000000002', 'IPH-15-PRO-128-WHT', '{"cor": "Titânio Branco"}', 8499.0000, 4, true, false, 6100.0000, 0.1870, 14.6000, 7.0700, 0.8300),
('c0000001-0000-0000-0000-000000000008', 'b0000001-0000-0000-0000-000000000002', 'IPH-15-PRO-128-BLK', '{"cor": "Titânio Preto"}', 8499.0000, 12, true, false, 6100.0000, 0.1870, 14.6000, 7.0700, 0.8300),
('c0000001-0000-0000-0000-000000000009', 'b0000001-0000-0000-0000-000000000003', 'XIA-RED-N13-128-BLK', '{"cor": "Preto Meia-Noite"}', 1299.0000, 25, true, false, 750.0000, 0.1880, 16.1700, 7.4600, 0.7800),
('c0000001-0000-0000-0000-000000000010', 'b0000001-0000-0000-0000-000000000003', 'XIA-RED-N13-128-BLU', '{"cor": "Azul Gelo"}', 1299.0000, 20, true, false, 750.0000, 0.1880, 16.1700, 7.4600, 0.7800),
('c0000001-0000-0000-0000-000000000011', 'b0000001-0000-0000-0000-000000000003', 'XIA-RED-N13-128-GRN', '{"cor": "Verde Menta"}', 1299.0000, 18, true, false, 750.0000, 0.1880, 16.1700, 7.4600, 0.7800),
('c0000001-0000-0000-0000-000000000012', 'b0000001-0000-0000-0000-000000000003', 'XIA-RED-N13-128-WHT', '{"cor": "Branco Ártico"}', 1299.0000, 12, true, false, 750.0000, 0.1880, 16.1700, 7.4600, 0.7800);

-- Customers (10 total)
INSERT INTO "Customers" ("Id", "Name", "Nickname", "Email", "Phone", "TotalOrders", "TotalSpent", "LastPurchase", "CreatedAt") VALUES
('d0000001-0000-0000-0000-000000000001', 'Maria Silva Santos', 'mari***', 'm***@gmail.com', '(11) 9****-1234', 3, 16297.0000, NOW() - INTERVAL '2 days', NOW() - INTERVAL '60 days'),
('d0000001-0000-0000-0000-000000000002', 'João Pedro Oliveira', 'joao***', 'j***@hotmail.com', '(21) 9****-5678', 2, 10098.0000, NOW() - INTERVAL '5 days', NOW() - INTERVAL '45 days'),
('d0000001-0000-0000-0000-000000000003', 'Ana Carolina Ferreira', 'anac***', 'a***@yahoo.com', '(31) 9****-9012', 2, 3598.0000, NOW() - INTERVAL '8 days', NOW() - INTERVAL '30 days'),
('d0000001-0000-0000-0000-000000000004', 'Carlos Eduardo Lima', 'carl***', 'c***@gmail.com', '(41) 9****-3456', 1, 8499.0000, NOW() - INTERVAL '3 days', NOW() - INTERVAL '20 days'),
('d0000001-0000-0000-0000-000000000005', 'Fernanda Costa Souza', 'fern***', 'f***@outlook.com', '(51) 9****-7890', 1, 249.0000, NOW() - INTERVAL '12 days', NOW() - INTERVAL '15 days'),
('d0000001-0000-0000-0000-000000000006', 'Roberto Almeida Neto', 'robe***', 'r***@gmail.com', '(61) 9****-2345', 1, 2599.0000, NOW() - INTERVAL '1 day', NOW() - INTERVAL '10 days'),
('d0000001-0000-0000-0000-000000000007', 'Juliana Mendes Ribeiro', 'juli***', 'j***@gmail.com', '(71) 9****-6789', 2, 1358.0000, NOW() - INTERVAL '15 days', NOW() - INTERVAL '50 days'),
('d0000001-0000-0000-0000-000000000008', 'Lucas Barbosa Gomes', 'luca***', 'l***@hotmail.com', '(81) 9****-0123', 1, 59.9000, NOW() - INTERVAL '20 days', NOW() - INTERVAL '25 days'),
('d0000001-0000-0000-0000-000000000009', 'Patricia Rocha Dias', 'patr***', 'p***@gmail.com', '(85) 9****-4567', 1, 7499.0000, NOW() - INTERVAL '4 days', NOW() - INTERVAL '12 days'),
('d0000001-0000-0000-0000-000000000010', 'Thiago Santos Pereira', 'thia***', 't***@yahoo.com', '(91) 9****-8901', 1, 3299.0000, NOW() - INTERVAL '6 days', NOW() - INTERVAL '8 days');

-- Orders (15 total: 3 Pago, 4 Enviado, 5 Entregue, 2 Cancelado, 1 Devolvido)
-- Order 1 - Entregue
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000001', 'ML-2024001234567', 'Maria Silva Santos', 'mari***', 'm***@gmail.com', '(11) 9****-1234', 2, 7548.9000, 1203.3200, 'Entregue', NOW() - INTERVAL '30 days', 'BR123456789ML', 'Correios', 'fulfillment', 'credit_card', 12, 7548.9000, 'approved', 'd0000001-0000-0000-0000-000000000001', NOW() - INTERVAL '30 days');
-- Order 2 - Entregue
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000002', 'ML-2024001234568', 'João Pedro Oliveira', 'joao***', 'j***@hotmail.com', '(21) 9****-5678', 1, 1599.0000, 312.8600, 'Entregue', NOW() - INTERVAL '25 days', 'BR987654321ML', 'Correios', 'coleta', 'credit_card', 6, 1599.0000, 'approved', 'd0000001-0000-0000-0000-000000000002', NOW() - INTERVAL '25 days');
-- Order 3 - Entregue
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000003', 'ML-2024001234569', 'Ana Carolina Ferreira', 'anac***', 'a***@yahoo.com', '(31) 9****-9012', 2, 3598.0000, 611.6600, 'Entregue', NOW() - INTERVAL '20 days', 'BR456789012ML', 'Jadlog', 'drop_off', 'credit_card', 10, 3598.0000, 'approved', 'd0000001-0000-0000-0000-000000000003', NOW() - INTERVAL '20 days');
-- Order 4 - Entregue
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000004', 'ML-2024001234570', 'Maria Silva Santos', 'mari***', 'm***@gmail.com', '(11) 9****-1234', 1, 1299.0000, 267.8800, 'Entregue', NOW() - INTERVAL '15 days', 'BR111222333ML', 'Correios', 'fulfillment', 'pix', NULL, 1299.0000, 'approved', 'd0000001-0000-0000-0000-000000000001', NOW() - INTERVAL '15 days');
-- Order 5 - Entregue
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000005', 'ML-2024001234571', 'Juliana Mendes Ribeiro', 'juli***', 'j***@gmail.com', '(71) 9****-6789', 3, 1159.0000, 218.4400, 'Entregue', NOW() - INTERVAL '18 days', 'BR444555666ML', 'Correios', 'coleta', 'credit_card', 3, 1159.0000, 'approved', 'd0000001-0000-0000-0000-000000000007', NOW() - INTERVAL '18 days');
-- Order 6 - Enviado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000006', 'ML-2024001234572', 'Carlos Eduardo Lima', 'carl***', 'c***@gmail.com', '(41) 9****-3456', 1, 8499.0000, 1309.8600, 'Enviado', NOW() - INTERVAL '3 days', 'BR777888999ML', 'Correios', 'fulfillment', 'credit_card', 12, 8499.0000, 'approved', 'd0000001-0000-0000-0000-000000000004', NOW() - INTERVAL '3 days');
-- Order 7 - Enviado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000007', 'ML-2024001234573', 'Patricia Rocha Dias', 'patr***', 'p***@gmail.com', '(85) 9****-4567', 1, 7499.0000, 1197.3200, 'Enviado', NOW() - INTERVAL '4 days', 'BR000111222ML', 'Jadlog', 'drop_off', 'pix', NULL, 7499.0000, 'approved', 'd0000001-0000-0000-0000-000000000009', NOW() - INTERVAL '4 days');
-- Order 8 - Enviado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000008', 'ML-2024001234574', 'João Pedro Oliveira', 'joao***', 'j***@hotmail.com', '(21) 9****-5678', 1, 8499.0000, 1309.8600, 'Enviado', NOW() - INTERVAL '5 days', 'BR333444555ML', 'Correios', 'fulfillment', 'credit_card', 10, 8499.0000, 'approved', 'd0000001-0000-0000-0000-000000000002', NOW() - INTERVAL '5 days');
-- Order 9 - Enviado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000009', 'ML-2024001234575', 'Thiago Santos Pereira', 'thia***', 't***@yahoo.com', '(91) 9****-8901', 1, 3299.0000, 455.8600, 'Enviado', NOW() - INTERVAL '6 days', 'BR666777888ML', 'Correios', 'coleta', 'credit_card', 8, 3299.0000, 'approved', 'd0000001-0000-0000-0000-000000000010', NOW() - INTERVAL '6 days');
-- Order 10 - Pago
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000010', 'ML-2024001234576', 'Roberto Almeida Neto', 'robe***', 'r***@gmail.com', '(61) 9****-2345', 1, 2599.0000, 348.8600, 'Pago', NOW() - INTERVAL '1 day', NULL, NULL, NULL, 'pix', NULL, 2599.0000, 'approved', 'd0000001-0000-0000-0000-000000000006', NOW() - INTERVAL '1 day');
-- Order 11 - Pago
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000011', 'ML-2024001234577', 'Maria Silva Santos', 'mari***', 'm***@gmail.com', '(11) 9****-1234', 2, 7449.0000, 1178.3200, 'Pago', NOW() - INTERVAL '2 days', NULL, NULL, NULL, 'credit_card', 12, 7449.0000, 'approved', 'd0000001-0000-0000-0000-000000000001', NOW() - INTERVAL '2 days');
-- Order 12 - Pago
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000012', 'ML-2024001234578', 'Fernanda Costa Souza', 'fern***', 'f***@outlook.com', '(51) 9****-7890', 1, 249.0000, 54.7200, 'Pago', NOW() - INTERVAL '12 days', NULL, NULL, NULL, 'pix', NULL, 249.0000, 'approved', 'd0000001-0000-0000-0000-000000000005', NOW() - INTERVAL '12 days');
-- Order 13 - Cancelado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000013', 'ML-2024001234579', 'Lucas Barbosa Gomes', 'luca***', 'l***@hotmail.com', '(81) 9****-0123', 1, 59.9000, 0.0000, 'Cancelado', NOW() - INTERVAL '20 days', NULL, NULL, NULL, 'pix', NULL, 59.9000, 'refunded', 'd0000001-0000-0000-0000-000000000008', NOW() - INTERVAL '20 days');
-- Order 14 - Cancelado
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000014', 'ML-2024001234580', 'Ana Carolina Ferreira', 'anac***', 'a***@yahoo.com', '(31) 9****-9012', 1, 1299.0000, 0.0000, 'Cancelado', NOW() - INTERVAL '8 days', NULL, NULL, NULL, 'credit_card', 3, 1299.0000, 'refunded', 'd0000001-0000-0000-0000-000000000003', NOW() - INTERVAL '8 days');
-- Order 15 - Devolvido
INSERT INTO "Orders" ("Id", "ExternalOrderId", "BuyerName", "BuyerNickname", "BuyerEmail", "BuyerPhone", "ItemCount", "TotalAmount", "Profit", "Status", "OrderDate", "TrackingNumber", "Carrier", "LogisticType", "PaymentMethod", "Installments", "PaymentAmount", "PaymentStatus", "CustomerId", "CreatedAt") VALUES
('e0000001-0000-0000-0000-000000000015', 'ML-2024001234581', 'Juliana Mendes Ribeiro', 'juli***', 'j***@gmail.com', '(71) 9****-6789', 1, 199.0000, -95.0000, 'Devolvido', NOW() - INTERVAL '15 days', 'BR999000111ML', 'Correios', 'fulfillment', 'credit_card', 1, 199.0000, 'refunded', 'd0000001-0000-0000-0000-000000000007', NOW() - INTERVAL '15 days');

-- Order Items
-- Order 1 items (Samsung S24 Ultra + Capinha)
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000001', 'b0000001-0000-0000-0000-000000000001', 'Samsung Galaxy S24 Ultra 256GB', 'SAM-S24-ULT-256-BLK', 'Preto Titânio', 1, 7499.0000, 7499.0000),
('f0000001-0000-0000-0000-000000000002', 'e0000001-0000-0000-0000-000000000001', 'b0000001-0000-0000-0000-000000000004', 'Capinha Samsung Galaxy S24 Ultra Silicone', 'CAP-SAM-S24-SIL', NULL, 1, 49.9000, 49.9000);
-- Order 2 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000003', 'e0000001-0000-0000-0000-000000000002', 'b0000001-0000-0000-0000-000000000003', 'Xiaomi Redmi Note 13 128GB', 'XIA-RED-N13-128-BLU', 'Azul Gelo', 1, 1299.0000, 1299.0000),
('f0000001-0000-0000-0000-000000000004', 'e0000001-0000-0000-0000-000000000002', 'b0000001-0000-0000-0000-000000000005', 'Película de Vidro iPhone 15 Pro', 'PEL-VID-IPH15', NULL, 1, 29.9000, 29.9000),
('f0000001-0000-0000-0000-000000000041', 'e0000001-0000-0000-0000-000000000002', 'b0000001-0000-0000-0000-000000000010', 'Cartão de Memória SanDisk Ultra 64GB', 'CAR-SAN-64-ULT', NULL, 1, 59.9000, 59.9000),
('f0000001-0000-0000-0000-000000000042', 'e0000001-0000-0000-0000-000000000002', NULL, 'Frete Grátis ML', 'FRETE', NULL, 1, 0.0000, 0.0000);
-- Order 3 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000005', 'e0000001-0000-0000-0000-000000000003', 'b0000001-0000-0000-0000-000000000006', 'Notebook Lenovo IdeaPad 3i Intel i5 16GB', 'NTB-LEN-I5-16', NULL, 1, 3299.0000, 3299.0000),
('f0000001-0000-0000-0000-000000000006', 'e0000001-0000-0000-0000-000000000003', 'b0000001-0000-0000-0000-000000000008', 'Teclado Bluetooth Logitech K380', 'TEC-LOG-K380', NULL, 1, 249.0000, 249.0000),
('f0000001-0000-0000-0000-000000000043', 'e0000001-0000-0000-0000-000000000003', 'b0000001-0000-0000-0000-000000000005', 'Película de Vidro iPhone 15 Pro', 'PEL-VID-IPH15', NULL, 1, 29.9000, 29.9000);
-- Order 4 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000007', 'e0000001-0000-0000-0000-000000000004', 'b0000001-0000-0000-0000-000000000003', 'Xiaomi Redmi Note 13 128GB', 'XIA-RED-N13-128-GRN', 'Verde Menta', 1, 1299.0000, 1299.0000);
-- Order 5 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000008', 'e0000001-0000-0000-0000-000000000005', 'b0000001-0000-0000-0000-000000000009', 'Fone de Ouvido JBL Tune 520BT', 'FON-JBL-T520', NULL, 2, 199.0000, 398.0000),
('f0000001-0000-0000-0000-000000000009', 'e0000001-0000-0000-0000-000000000005', 'b0000001-0000-0000-0000-000000000010', 'Cartão de Memória SanDisk Ultra 64GB', 'CAR-SAN-64-ULT', NULL, 3, 59.9000, 179.7000),
('f0000001-0000-0000-0000-000000000044', 'e0000001-0000-0000-0000-000000000005', 'b0000001-0000-0000-0000-000000000005', 'Película de Vidro iPhone 15 Pro', 'PEL-VID-IPH15', NULL, 1, 29.9000, 29.9000);
-- Order 6 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000010', 'e0000001-0000-0000-0000-000000000006', 'b0000001-0000-0000-0000-000000000002', 'iPhone 15 Pro 128GB', 'IPH-15-PRO-128-NAT', 'Titânio Natural', 1, 8499.0000, 8499.0000);
-- Order 7 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000011', 'e0000001-0000-0000-0000-000000000007', 'b0000001-0000-0000-0000-000000000001', 'Samsung Galaxy S24 Ultra 256GB', 'SAM-S24-ULT-256-GRY', 'Cinza Titânio', 1, 7499.0000, 7499.0000);
-- Order 8 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000012', 'e0000001-0000-0000-0000-000000000008', 'b0000001-0000-0000-0000-000000000002', 'iPhone 15 Pro 128GB', 'IPH-15-PRO-128-BLU', 'Titânio Azul', 1, 8499.0000, 8499.0000);
-- Order 9 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000013', 'e0000001-0000-0000-0000-000000000009', 'b0000001-0000-0000-0000-000000000006', 'Notebook Lenovo IdeaPad 3i Intel i5 16GB', 'NTB-LEN-I5-16', NULL, 1, 3299.0000, 3299.0000);
-- Order 10 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000014', 'e0000001-0000-0000-0000-000000000010', 'b0000001-0000-0000-0000-000000000007', 'Notebook ASUS VivoBook 15 Ryzen 5 8GB', 'NTB-ASUS-R5-8', NULL, 1, 2599.0000, 2599.0000);
-- Order 11 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000015', 'e0000001-0000-0000-0000-000000000011', 'b0000001-0000-0000-0000-000000000001', 'Samsung Galaxy S24 Ultra 256GB', 'SAM-S24-ULT-256-VIO', 'Violeta Titânio', 1, 7499.0000, 7499.0000),
('f0000001-0000-0000-0000-000000000016', 'e0000001-0000-0000-0000-000000000011', 'b0000001-0000-0000-0000-000000000004', 'Capinha Samsung Galaxy S24 Ultra Silicone', 'CAP-SAM-S24-SIL', NULL, 1, 49.9000, 49.9000);
-- Order 12 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000017', 'e0000001-0000-0000-0000-000000000012', 'b0000001-0000-0000-0000-000000000008', 'Teclado Bluetooth Logitech K380', 'TEC-LOG-K380', NULL, 1, 249.0000, 249.0000);
-- Order 13 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000018', 'e0000001-0000-0000-0000-000000000013', 'b0000001-0000-0000-0000-000000000010', 'Cartão de Memória SanDisk Ultra 64GB', 'CAR-SAN-64-ULT', NULL, 1, 59.9000, 59.9000);
-- Order 14 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000019', 'e0000001-0000-0000-0000-000000000014', 'b0000001-0000-0000-0000-000000000003', 'Xiaomi Redmi Note 13 128GB', 'XIA-RED-N13-128-WHT', 'Branco Ártico', 1, 1299.0000, 1299.0000);
-- Order 15 items
INSERT INTO "OrderItems" ("Id", "OrderId", "ProductId", "Name", "Sku", "Variation", "Quantity", "UnitPrice", "Subtotal") VALUES
('f0000001-0000-0000-0000-000000000020', 'e0000001-0000-0000-0000-000000000015', 'b0000001-0000-0000-0000-000000000009', 'Fone de Ouvido JBL Tune 520BT', 'FON-JBL-T520', NULL, 1, 199.0000, 199.0000);

-- Order Costs (6-9 cost items per delivered/shipped/paid order)
-- Order 1 costs (TotalAmount=7548.90, Profit=1203.32, so costs=6345.58)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000001', 'e0000001-0000-0000-0000-000000000001', 'product_cost', 'Custo do produto Samsung S24 Ultra', 5200.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000002', 'e0000001-0000-0000-0000-000000000001', 'product_cost', 'Custo do produto Capinha Silicone', 8.5000, 'Sistema'),
('10000001-0000-0000-0000-000000000003', 'e0000001-0000-0000-0000-000000000001', 'marketplace_commission', 'Comissão Mercado Livre 12%', 905.8680, 'Marketplace'),
('10000001-0000-0000-0000-000000000004', 'e0000001-0000-0000-0000-000000000001', 'fixed_fee', 'Taxa fixa por venda', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000005', 'e0000001-0000-0000-0000-000000000001', 'shipping', 'Frete vendedor', 45.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000006', 'e0000001-0000-0000-0000-000000000001', 'payment_fee', 'Taxa de pagamento 4.99%', 376.6910, 'Marketplace'),
('10000001-0000-0000-0000-000000000007', 'e0000001-0000-0000-0000-000000000001', 'packaging', 'Embalagem', 18.0000, 'Manual'),
('10000001-0000-0000-0000-000000000008', 'e0000001-0000-0000-0000-000000000001', 'tax', 'Imposto estimado', 150.0000, 'Manual'),
('10000001-0000-0000-0000-000000000009', 'e0000001-0000-0000-0000-000000000001', 'fulfillment_fee', 'Taxa de fulfillment', 22.5210, 'Marketplace');

-- Order 2 costs (TotalAmount=1599.00, Profit=312.86, costs=1286.14)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000010', 'e0000001-0000-0000-0000-000000000002', 'product_cost', 'Custo Xiaomi Redmi Note 13', 750.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000011', 'e0000001-0000-0000-0000-000000000002', 'product_cost', 'Custo Película de Vidro', 4.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000045', 'e0000001-0000-0000-0000-000000000002', 'product_cost', 'Custo Cartão SanDisk', 25.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000012', 'e0000001-0000-0000-0000-000000000002', 'marketplace_commission', 'Comissão ML 12%', 191.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000013', 'e0000001-0000-0000-0000-000000000002', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000014', 'e0000001-0000-0000-0000-000000000002', 'shipping', 'Frete vendedor', 32.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000015', 'e0000001-0000-0000-0000-000000000002', 'payment_fee', 'Taxa pagamento 4.99%', 79.7900, 'Marketplace'),
('10000001-0000-0000-0000-000000000016', 'e0000001-0000-0000-0000-000000000002', 'packaging', 'Embalagem', 14.5000, 'Manual'),
('10000001-0000-0000-0000-000000000046', 'e0000001-0000-0000-0000-000000000002', 'tax', 'Imposto estimado', 45.0000, 'Manual');

-- Order 3 costs (TotalAmount=3598.00, Profit=611.66, costs=2986.34)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000017', 'e0000001-0000-0000-0000-000000000003', 'product_cost', 'Custo Notebook Lenovo', 2400.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000018', 'e0000001-0000-0000-0000-000000000003', 'product_cost', 'Custo Teclado Logitech', 140.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000047', 'e0000001-0000-0000-0000-000000000003', 'product_cost', 'Custo Película de Vidro', 4.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000019', 'e0000001-0000-0000-0000-000000000003', 'marketplace_commission', 'Comissão ML 12%', 431.7600, 'Marketplace'),
('10000001-0000-0000-0000-000000000020', 'e0000001-0000-0000-0000-000000000003', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000021', 'e0000001-0000-0000-0000-000000000003', 'shipping', 'Frete vendedor', 55.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000022', 'e0000001-0000-0000-0000-000000000003', 'payment_fee', 'Taxa pagamento 4.99%', 179.5400, 'Marketplace'),
('10000001-0000-0000-0000-000000000023', 'e0000001-0000-0000-0000-000000000003', 'packaging', 'Embalagem', 37.5000, 'Manual'),
('10000001-0000-0000-0000-000000000048', 'e0000001-0000-0000-0000-000000000003', 'tax', 'Imposto estimado', 85.0000, 'Manual');

-- Order 4 costs (TotalAmount=1299.00, Profit=267.88, costs=1031.12)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000024', 'e0000001-0000-0000-0000-000000000004', 'product_cost', 'Custo Xiaomi Redmi Note 13', 750.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000025', 'e0000001-0000-0000-0000-000000000004', 'marketplace_commission', 'Comissão ML 12%', 155.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000026', 'e0000001-0000-0000-0000-000000000004', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000027', 'e0000001-0000-0000-0000-000000000004', 'shipping', 'Frete vendedor', 28.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000028', 'e0000001-0000-0000-0000-000000000004', 'payment_fee', 'Taxa pagamento (PIX 0%)', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000029', 'e0000001-0000-0000-0000-000000000004', 'packaging', 'Embalagem', 12.0000, 'Manual'),
('10000001-0000-0000-0000-000000000049', 'e0000001-0000-0000-0000-000000000004', 'fulfillment_fee', 'Taxa de fulfillment', 18.2400, 'Marketplace'),
('10000001-0000-0000-0000-000000000050', 'e0000001-0000-0000-0000-000000000004', 'tax', 'Imposto estimado', 30.0000, 'Manual');

-- Order 5 costs (TotalAmount=1159.00, Profit=218.44, costs=940.56)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000030', 'e0000001-0000-0000-0000-000000000005', 'product_cost', 'Custo 2x Fone JBL', 190.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000031', 'e0000001-0000-0000-0000-000000000005', 'product_cost', 'Custo 3x Cartão SanDisk', 75.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000051', 'e0000001-0000-0000-0000-000000000005', 'product_cost', 'Custo Película de Vidro', 4.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000032', 'e0000001-0000-0000-0000-000000000005', 'marketplace_commission', 'Comissão ML 12%', 139.0800, 'Marketplace'),
('10000001-0000-0000-0000-000000000033', 'e0000001-0000-0000-0000-000000000005', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000034', 'e0000001-0000-0000-0000-000000000005', 'shipping', 'Frete vendedor', 38.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000035', 'e0000001-0000-0000-0000-000000000005', 'payment_fee', 'Taxa pagamento 4.99%', 57.8300, 'Marketplace'),
('10000001-0000-0000-0000-000000000036', 'e0000001-0000-0000-0000-000000000005', 'packaging', 'Embalagem', 22.5000, 'Manual'),
('10000001-0000-0000-0000-000000000052', 'e0000001-0000-0000-0000-000000000005', 'tax', 'Imposto estimado', 28.0000, 'Manual');

-- Order 6 costs (TotalAmount=8499.00, Profit=1309.86, costs=7189.14)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000037', 'e0000001-0000-0000-0000-000000000006', 'product_cost', 'Custo iPhone 15 Pro', 6100.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000038', 'e0000001-0000-0000-0000-000000000006', 'marketplace_commission', 'Comissão ML 12%', 1019.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000039', 'e0000001-0000-0000-0000-000000000006', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000040', 'e0000001-0000-0000-0000-000000000006', 'shipping', 'Frete vendedor', 35.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000053', 'e0000001-0000-0000-0000-000000000006', 'payment_fee', 'Taxa pagamento 4.99%', 424.1000, 'Marketplace'),
('10000001-0000-0000-0000-000000000054', 'e0000001-0000-0000-0000-000000000006', 'packaging', 'Embalagem', 18.0000, 'Manual'),
('10000001-0000-0000-0000-000000000055', 'e0000001-0000-0000-0000-000000000006', 'fulfillment_fee', 'Taxa de fulfillment', 22.1600, 'Marketplace'),
('10000001-0000-0000-0000-000000000056', 'e0000001-0000-0000-0000-000000000006', 'tax', 'Imposto estimado', 175.0000, 'Manual');

-- Order 7 costs (TotalAmount=7499.00, Profit=1197.32, costs=6301.68)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000057', 'e0000001-0000-0000-0000-000000000007', 'product_cost', 'Custo Samsung S24 Ultra', 5200.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000058', 'e0000001-0000-0000-0000-000000000007', 'marketplace_commission', 'Comissão ML 12%', 899.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000059', 'e0000001-0000-0000-0000-000000000007', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000060', 'e0000001-0000-0000-0000-000000000007', 'shipping', 'Frete vendedor', 42.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000061', 'e0000001-0000-0000-0000-000000000007', 'payment_fee', 'Taxa pagamento (PIX 0%)', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000062', 'e0000001-0000-0000-0000-000000000007', 'packaging', 'Embalagem', 15.0000, 'Manual'),
('10000001-0000-0000-0000-000000000063', 'e0000001-0000-0000-0000-000000000007', 'tax', 'Imposto estimado', 150.0000, 'Manual');

-- Order 8 costs (TotalAmount=8499.00, Profit=1309.86, costs=7189.14)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000064', 'e0000001-0000-0000-0000-000000000008', 'product_cost', 'Custo iPhone 15 Pro', 6100.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000065', 'e0000001-0000-0000-0000-000000000008', 'marketplace_commission', 'Comissão ML 12%', 1019.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000066', 'e0000001-0000-0000-0000-000000000008', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000067', 'e0000001-0000-0000-0000-000000000008', 'shipping', 'Frete vendedor', 35.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000068', 'e0000001-0000-0000-0000-000000000008', 'payment_fee', 'Taxa pagamento 4.99%', 424.1000, 'Marketplace'),
('10000001-0000-0000-0000-000000000069', 'e0000001-0000-0000-0000-000000000008', 'packaging', 'Embalagem', 18.0000, 'Manual'),
('10000001-0000-0000-0000-000000000070', 'e0000001-0000-0000-0000-000000000008', 'fulfillment_fee', 'Taxa de fulfillment', 22.1600, 'Marketplace'),
('10000001-0000-0000-0000-000000000071', 'e0000001-0000-0000-0000-000000000008', 'tax', 'Imposto estimado', 175.0000, 'Manual');

-- Order 9 costs (TotalAmount=3299.00, Profit=455.86, costs=2843.14)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000072', 'e0000001-0000-0000-0000-000000000009', 'product_cost', 'Custo Notebook Lenovo', 2400.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000073', 'e0000001-0000-0000-0000-000000000009', 'marketplace_commission', 'Comissão ML 12%', 395.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000074', 'e0000001-0000-0000-0000-000000000009', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000075', 'e0000001-0000-0000-0000-000000000009', 'shipping', 'Frete vendedor', 48.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000076', 'e0000001-0000-0000-0000-000000000009', 'payment_fee', 'Taxa pagamento 4.99%', 164.6200, 'Marketplace'),
('10000001-0000-0000-0000-000000000077', 'e0000001-0000-0000-0000-000000000009', 'packaging', 'Embalagem', 25.0000, 'Manual'),
('10000001-0000-0000-0000-000000000078', 'e0000001-0000-0000-0000-000000000009', 'tax', 'Imposto estimado', 70.0000, 'Manual');

-- Order 10 costs (TotalAmount=2599.00, Profit=348.86, costs=2250.14)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000079', 'e0000001-0000-0000-0000-000000000010', 'product_cost', 'Custo Notebook ASUS', 1850.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000080', 'e0000001-0000-0000-0000-000000000010', 'marketplace_commission', 'Comissão ML 12%', 311.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000081', 'e0000001-0000-0000-0000-000000000010', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000082', 'e0000001-0000-0000-0000-000000000010', 'shipping', 'Frete vendedor', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000083', 'e0000001-0000-0000-0000-000000000010', 'payment_fee', 'Taxa pagamento (PIX 0%)', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000084', 'e0000001-0000-0000-0000-000000000010', 'packaging', 'Embalagem', 25.0000, 'Manual'),
('10000001-0000-0000-0000-000000000085', 'e0000001-0000-0000-0000-000000000010', 'tax', 'Imposto estimado', 58.2600, 'Manual');

-- Order 11 costs (TotalAmount=7449.00, Profit=1178.32, costs=6270.68)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000086', 'e0000001-0000-0000-0000-000000000011', 'product_cost', 'Custo Samsung S24 Ultra', 5200.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000087', 'e0000001-0000-0000-0000-000000000011', 'product_cost', 'Custo Capinha Silicone', 8.5000, 'Sistema'),
('10000001-0000-0000-0000-000000000088', 'e0000001-0000-0000-0000-000000000011', 'marketplace_commission', 'Comissão ML 12%', 893.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000089', 'e0000001-0000-0000-0000-000000000011', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000090', 'e0000001-0000-0000-0000-000000000011', 'shipping', 'Frete vendedor', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000091', 'e0000001-0000-0000-0000-000000000011', 'payment_fee', 'Taxa pagamento 4.99%', 371.7000, 'Marketplace'),
('10000001-0000-0000-0000-000000000092', 'e0000001-0000-0000-0000-000000000011', 'packaging', 'Embalagem', 18.0000, 'Manual'),
('10000001-0000-0000-0000-000000000093', 'e0000001-0000-0000-0000-000000000011', 'tax', 'Imposto estimado', 148.6000, 'Manual');

-- Order 12 costs (TotalAmount=249.00, Profit=54.72, costs=194.28)
INSERT INTO "OrderCosts" ("Id", "OrderId", "Category", "Description", "Value", "Source") VALUES
('10000001-0000-0000-0000-000000000094', 'e0000001-0000-0000-0000-000000000012', 'product_cost', 'Custo Teclado Logitech', 140.0000, 'Sistema'),
('10000001-0000-0000-0000-000000000095', 'e0000001-0000-0000-0000-000000000012', 'marketplace_commission', 'Comissão ML 12%', 29.8800, 'Marketplace'),
('10000001-0000-0000-0000-000000000096', 'e0000001-0000-0000-0000-000000000012', 'fixed_fee', 'Taxa fixa', 5.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000097', 'e0000001-0000-0000-0000-000000000012', 'shipping', 'Frete vendedor', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000098', 'e0000001-0000-0000-0000-000000000012', 'payment_fee', 'Taxa pagamento (PIX 0%)', 0.0000, 'Marketplace'),
('10000001-0000-0000-0000-000000000099', 'e0000001-0000-0000-0000-000000000012', 'packaging', 'Embalagem', 10.0000, 'Manual'),
('10000001-0000-0000-0000-000000000100', 'e0000001-0000-0000-0000-000000000012', 'tax', 'Imposto estimado', 9.4000, 'Manual');

-- Supplies (7 total, some below minimum stock)
INSERT INTO "Supplies" ("Id", "Name", "Sku", "Category", "UnitCost", "Stock", "MinimumStock", "Supplier", "Status", "CreatedAt", "UpdatedAt") VALUES
('20000001-0000-0000-0000-000000000001', 'Caixa de Papelão P', 'EMB-CX-P', 'Embalagem', 2.5000, 150, 50, 'EmbalagensBR', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000002', 'Caixa de Papelão M', 'EMB-CX-M', 'Embalagem', 4.5000, 80, 40, 'EmbalagensBR', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000003', 'Caixa de Papelão G', 'EMB-CX-G', 'Embalagem', 7.0000, 15, 30, 'EmbalagensBR', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000004', 'Plástico Bolha (rolo 50m)', 'EMB-PB-50', 'Embalagem', 35.0000, 8, 10, 'EmbalagensBR', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000005', 'Fita Adesiva Transparente', 'EMB-FT-TR', 'Embalagem', 5.5000, 45, 20, 'EmbalagensBR', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000006', 'Etiqueta Térmica (rolo 500un)', 'EMB-ET-500', 'Etiqueta', 28.0000, 3, 5, 'Kalunga', 'Ativo', NOW(), NOW()),
('20000001-0000-0000-0000-000000000007', 'Saco Plástico Anti-Estática', 'EMB-SP-AE', 'Embalagem', 0.8000, 200, 100, 'EmbalagensBR', 'Ativo', NOW(), NOW());

-- Notifications (8 total)
INSERT INTO "Notifications" ("Id", "Type", "Title", "Description", "Timestamp", "IsRead", "NavigationTarget") VALUES
('30000001-0000-0000-0000-000000000001', 'order', 'Nova venda realizada', 'Pedido ML-2024001234576 - Notebook ASUS VivoBook por R$ 2.599,00', NOW() - INTERVAL '1 day', false, '/vendas/e0000001-0000-0000-0000-000000000010'),
('30000001-0000-0000-0000-000000000002', 'order', 'Nova venda realizada', 'Pedido ML-2024001234577 - Samsung Galaxy S24 Ultra + Capinha por R$ 7.449,00', NOW() - INTERVAL '2 days', false, '/vendas/e0000001-0000-0000-0000-000000000011'),
('30000001-0000-0000-0000-000000000003', 'stock', 'Estoque baixo: Caixa de Papelão G', 'Estoque atual: 15 unidades (mínimo: 30)', NOW() - INTERVAL '3 hours', false, '/insumos'),
('30000001-0000-0000-0000-000000000004', 'stock', 'Estoque baixo: Plástico Bolha', 'Estoque atual: 8 rolos (mínimo: 10)', NOW() - INTERVAL '3 hours', false, '/insumos'),
('30000001-0000-0000-0000-000000000005', 'stock', 'Estoque baixo: Etiqueta Térmica', 'Estoque atual: 3 rolos (mínimo: 5)', NOW() - INTERVAL '3 hours', false, '/insumos'),
('30000001-0000-0000-0000-000000000006', 'shipping', 'Pedido entregue', 'Pedido ML-2024001234571 foi entregue ao comprador', NOW() - INTERVAL '5 days', true, '/vendas/e0000001-0000-0000-0000-000000000005'),
('30000001-0000-0000-0000-000000000007', 'return', 'Devolução solicitada', 'Pedido ML-2024001234581 - Fone JBL Tune 520BT devolvido', NOW() - INTERVAL '15 days', true, '/vendas/e0000001-0000-0000-0000-000000000015'),
('30000001-0000-0000-0000-000000000008', 'system', 'Sincronização concluída', 'Dados do Mercado Livre sincronizados com sucesso', NOW() - INTERVAL '6 hours', true, '/configuracoes/integracoes');

-- System Users (3 total)
INSERT INTO "SystemUsers" ("Id", "Email", "Name", "Role", "IsActive", "LastLogin", "CreatedAt") VALUES
('40000001-0000-0000-0000-000000000001', 'admin@perushophub.com', 'Administrador', 'Admin', true, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '90 days'),
('40000001-0000-0000-0000-000000000002', 'gerente@perushophub.com', 'Gerente de Vendas', 'Manager', true, NOW() - INTERVAL '3 hours', NOW() - INTERVAL '60 days'),
('40000001-0000-0000-0000-000000000003', 'analista@perushophub.com', 'Analista Financeiro', 'Viewer', true, NOW() - INTERVAL '1 day', NOW() - INTERVAL '30 days');

-- Marketplace Connections (2 total)
INSERT INTO "MarketplaceConnections" ("Id", "MarketplaceId", "Name", "Logo", "IsConnected", "SellerNickname", "LastSyncAt", "ComingSoon", "CreatedAt") VALUES
('50000001-0000-0000-0000-000000000001', 'mercadolivre', 'Mercado Livre', '/assets/images/ml-logo.svg', true, 'PERUSHOP_OFICIAL', NOW() - INTERVAL '6 hours', false, NOW() - INTERVAL '90 days'),
('50000001-0000-0000-0000-000000000002', 'amazon', 'Amazon', '/assets/images/amazon-logo.svg', false, NULL, NULL, true, NOW() - INTERVAL '90 days');
