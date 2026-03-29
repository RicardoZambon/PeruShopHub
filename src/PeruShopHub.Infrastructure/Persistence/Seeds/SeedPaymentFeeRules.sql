-- Seed: Mercado Livre default payment fee rules
-- Rates based on ML's standard payment processing fees by installment count

INSERT INTO "PaymentFeeRules" ("Id", "TenantId", "InstallmentMin", "InstallmentMax", "FeePercentage", "IsDefault", "CreatedAt")
VALUES
    ('b2c3d4e5-0001-4000-8000-000000000001', 'a0000000-0000-0000-0000-000000000001', 1, 1, 4.99, false, NOW()),
    ('b2c3d4e5-0002-4000-8000-000000000002', 'a0000000-0000-0000-0000-000000000001', 2, 2, 7.49, false, NOW()),
    ('b2c3d4e5-0003-4000-8000-000000000003', 'a0000000-0000-0000-0000-000000000001', 3, 3, 9.99, false, NOW()),
    ('b2c3d4e5-0004-4000-8000-000000000004', 'a0000000-0000-0000-0000-000000000001', 4, 6, 12.49, false, NOW()),
    ('b2c3d4e5-0005-4000-8000-000000000005', 'a0000000-0000-0000-0000-000000000001', 7, 12, 16.99, false, NOW()),
    ('b2c3d4e5-0006-4000-8000-000000000006', 'a0000000-0000-0000-0000-000000000001', 1, 12, 4.99, true, NOW())
ON CONFLICT ("Id") DO NOTHING;
