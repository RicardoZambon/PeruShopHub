-- Seed: Mercado Livre default commission rules
-- Rates based on ML's standard fee structure per category

INSERT INTO "CommissionRules" ("Id", "MarketplaceId", "CategoryPattern", "ListingType", "Rate", "IsDefault", "CreatedAt")
VALUES
    ('a1b2c3d4-0001-4000-8000-000000000001', 'mercadolivre', 'Eletrônicos', 'classico', 0.1300, false, NOW()),
    ('a1b2c3d4-0002-4000-8000-000000000002', 'mercadolivre', 'Eletrônicos', 'premium', 0.1600, false, NOW()),
    ('a1b2c3d4-0003-4000-8000-000000000003', 'mercadolivre', 'Informática', 'classico', 0.1200, false, NOW()),
    ('a1b2c3d4-0004-4000-8000-000000000004', 'mercadolivre', 'Informática', 'premium', 0.1500, false, NOW()),
    ('a1b2c3d4-0005-4000-8000-000000000005', 'mercadolivre', 'Moda', 'classico', 0.1400, false, NOW()),
    ('a1b2c3d4-0006-4000-8000-000000000006', 'mercadolivre', 'Moda', 'premium', 0.1700, false, NOW()),
    ('a1b2c3d4-0007-4000-8000-000000000007', 'mercadolivre', 'Casa e Decoração', 'classico', 0.1300, false, NOW()),
    ('a1b2c3d4-0008-4000-8000-000000000008', 'mercadolivre', 'Casa e Decoração', 'premium', 0.1600, false, NOW()),
    ('a1b2c3d4-0009-4000-8000-000000000009', 'mercadolivre', 'Esportes', 'classico', 0.1300, false, NOW()),
    ('a1b2c3d4-000a-4000-8000-00000000000a', 'mercadolivre', 'Esportes', 'premium', 0.1600, false, NOW()),
    ('a1b2c3d4-000b-4000-8000-00000000000b', 'mercadolivre', 'Beleza', 'classico', 0.1500, false, NOW()),
    ('a1b2c3d4-000c-4000-8000-00000000000c', 'mercadolivre', 'Beleza', 'premium', 0.1800, false, NOW()),
    ('a1b2c3d4-000d-4000-8000-00000000000d', 'mercadolivre', NULL, NULL, 0.1400, true, NOW())
ON CONFLICT ("Id") DO NOTHING;
