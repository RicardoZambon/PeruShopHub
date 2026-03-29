-- Seed 5 common response templates for Demo Shop tenant
INSERT INTO "ResponseTemplates" ("Id", "TenantId", "Name", "Category", "Body", "Placeholders", "UsageCount", "Order", "IsActive", "CreatedAt", "UpdatedAt", "Version")
VALUES
  ('f1000001-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001',
   'Disponibilidade em Estoque', 'Estoque',
   'Olá! O produto {produto} está disponível em estoque e pronto para envio imediato. Caso tenha mais dúvidas, estou à disposição!',
   '["produto"]', 0, 1, true, NOW(), NOW(), 0),

  ('f1000001-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001',
   'Prazo de Entrega', 'Envio',
   'Olá! O prazo estimado de entrega para o produto {produto} é de {prazo}. O envio é feito assim que o pagamento for confirmado. Qualquer dúvida, estou aqui!',
   '["produto", "prazo"]', 0, 2, true, NOW(), NOW(), 0),

  ('f1000001-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001',
   'Informação de Preço', 'Preço',
   'Olá! O valor do produto {produto} é {preco}. Este preço já inclui todos os impostos aplicáveis. Posso ajudar com mais alguma coisa?',
   '["produto", "preco"]', 0, 3, true, NOW(), NOW(), 0),

  ('f1000001-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001',
   'Garantia do Produto', 'Garantia',
   'Olá! O produto {produto} possui garantia de fábrica conforme especificado no anúncio. Em caso de defeito, entre em contato conosco para resolvermos da melhor forma possível!',
   '["produto"]', 0, 4, true, NOW(), NOW(), 0),

  ('f1000001-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001',
   'Agradecimento pela Compra', 'Pós-venda',
   'Olá! Agradecemos pela sua compra do produto {produto}! Esperamos que aproveite bastante. Se precisar de qualquer ajuda, não hesite em entrar em contato. Obrigado pela preferência!',
   '["produto"]', 0, 5, true, NOW(), NOW(), 0)
ON CONFLICT DO NOTHING;
