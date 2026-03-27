# Mercado Livre API - Recursos Avancados para Sellers

Referencia tecnica com endpoints reais, exemplos de requisicoes e detalhes de implementacao.

**Base URL:** `https://api.mercadolibre.com`
**Site ID Brasil:** `MLB`
**Autenticacao:** Bearer Token (OAuth 2.0) em todas as chamadas.

---

## 1. Mercado Livre Full / Fulfillment API

O Mercado Full (Fulfillment by Mercado Livre) significa que os produtos do seller ficam nos armazens do ML, e toda a logistica e gerenciada pelo Mercado Livre.

**Importante:** O envio de produtos para os armazens (inbounding) e a mudanca de logistica de um anuncio para fulfillment sao feitos pelo **Seller Center** (interface web). Via API, voce consegue apenas **consultar** estoque e operacoes.

### 1.1 Obter o inventory_id de um item

```bash
GET /items/{ITEM_ID}

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  https://api.mercadolibre.com/items/MLB1557246024
```

Resposta relevante:
```json
{
  "inventory_id": "LCQI05831",
  "available_quantity": 50,
  "sold_quantity": 0
}
```

### 1.2 Consultar estoque Fulfillment

```bash
GET /inventories/{INVENTORY_ID}/stock/fulfillment

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  https://api.mercadolibre.com/inventories/LCQI05831/stock/fulfillment
```

Resposta:
```json
{
  "total": 55,
  "available_quantity": 50,
  "not_available_detail": [
    { "status": "damage", "quantity": 2 },
    { "status": "internal_process", "quantity": 3 }
  ],
  "external_references": [...]
}
```

Status possiveis de `not_available_detail`: `damage`, `lost`, `withdrawal`, `internal_process`, `transfer`, `noFiscalCoverage` (especifico Brasil), `not_supported`.

### 1.3 Consultar operacoes de estoque

```bash
GET /stock/fulfillment/operations/search?seller_id={SELLER_ID}&inventory_id={INVENTORY_ID}&date_from=2024-01-01&date_to=2024-01-31

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/stock/fulfillment/operations/search?seller_id=384741716&inventory_id=NFWV18668&date_from=2024-06-29&date_to=2024-07-28&type=SALE_CONFIRMATION'
```

**Parametros:**
| Parametro | Obrigatorio | Descricao |
|-----------|-------------|-----------|
| `seller_id` | Sim | ID do vendedor |
| `inventory_id` ou `seller_product_id` | Sim | Identificador(es) do produto |
| `date_from` / `date_to` | Sim | Intervalo de datas (max 60 dias) |
| `type` | Nao | Filtro de tipo de operacao |
| `limit` | Nao | Maximo 1000 (padrao 1000) |
| `scroll` | Nao | Token de paginacao (expira em 5 min) |

**Tipos de operacao:**
- **Entrada (Inbound):** `inbound_reception`, `fiscal_coverage_adjustment`
- **Venda:** `sale_confirmation`, `sale_cancelation`, `sale_delivery_cancelation`, `sale_return`
- **Retirada:** `withdrawal_reservation`, `withdrawal_cancelation`, `withdrawal_delivery`, `withdrawal_discarded`
- **Transferencia:** `transfer_reservation`, `transfer_adjustment`, `transfer_delivery`
- **Qualidade:** `quarantine_reservation`, `quarantine_restock`, `lost_refund`, `disposed_tainted`, `disposed_expired`
- **Remocao:** `removal_reservation`, `removal_completion`, `stranded_disposal_removal`
- **Ajustes:** `adjustment`, `identification_problem_remove`, `identification_problem_add`

### 1.4 Notificacoes de estoque Fulfillment

Inscreva seu app no topico `fbm_stock_operations` via API de Notificacoes para receber atualizacoes em tempo real.

**Restricoes:**
- Dados disponiveis apenas dos ultimos 12 meses
- Consulta retorna ate o dia anterior (exclui data atual)
- Items com variacoes tem `inventory_id` separado por variacao
- Disponivel em: Brasil, Argentina, Mexico, Chile, Colombia

---

## 2. Advertising API (Mercado Ads / Product Ads)

### 2.1 Verificar advertiser

```bash
GET /advertising/advertisers?product_id=$PRODUCT_ID

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  https://api.mercadolibre.com/advertising/advertisers?product_id=product_ads
```

### 2.2 Listar campanhas

```bash
GET /advertising/advertisers/{ADVERTISER_ID}/product_ads/campaigns

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/advertising/advertisers/123456/product_ads/campaigns?limit=10&offset=0&date_from=2024-01-01&date_to=2024-01-31&metrics=clicks,prints,cost,cpc'
```

### 2.3 Detalhes de uma campanha

```bash
GET /advertising/product_ads/campaigns/{CAMPAIGN_ID}

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/advertising/product_ads/campaigns/CAMP123?date_from=2024-01-01&date_to=2024-01-31'
```

### 2.4 Listar anuncios de um advertiser

```bash
GET /advertising/advertisers/{ADVERTISER_ID}/product_ads/items

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/advertising/advertisers/123456/product_ads/items?limit=50&offset=0&metrics=clicks,cost,prints'
```

### 2.5 Detalhes de um anuncio especifico

```bash
GET /advertising/product_ads/items/{ITEM_ID}
```

### 2.6 Metricas disponiveis

- `clicks` - Cliques no anuncio
- `prints` - Impressoes
- `cost` - Custo total
- `cpc` - Custo por clique
- `acos` - Custo de publicidade sobre vendas
- `roas` - Retorno sobre investimento em ads
- `organic_sales_quantity` / `organic_sales_amount`
- `direct_sales_quantity` / `direct_sales_amount`
- `indirect_sales_quantity` / `indirect_sales_amount`
- `conversion_rate`

**Modos de gerenciamento:**
- **Automatico:** ML seleciona os anuncios com melhor desempenho automaticamente
- **Personalizado:** Voce cria multiplas campanhas, define orcamento e objetivo individualmente

**Restricao:** Intervalo maximo de datas para metricas e de 90 dias retroativos.

---

## 3. Financial API / Billing / Mercado Pago

### 3.1 Periodos de cobranca

```bash
GET /billing/integration/monthly/periods?group={ML|MP}&document_type={BILL|CREDIT_NOTE}&offset=0&limit=6

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/billing/integration/monthly/periods?group=ML&document_type=BILL&limit=6'
```

Resposta inclui: `amount`, `unpaid_amount`, `date_from`, `date_to`, `expiration_date`, status (`OPEN`/`CLOSED`).

### 3.2 Documentos de um periodo (Notas Fiscais / Faturas)

```bash
GET /billing/integration/periods/key/{KEY}/documents?group={ML|MP}&document_type={BILL|CREDIT_NOTE}&limit=1000

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/billing/integration/periods/key/2024-06-01/documents?group=ML&document_type=BILL&limit=100'
```

### 3.3 Resumo de cobrancas e bonificacoes

```bash
GET /billing/integration/periods/key/{KEY}/summary/details

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/billing/integration/periods/key/2024-10-01/summary/details'
```

Resposta inclui:
- **Charges (cobranças):** comissoes de vendas, custos de publicacao, Mercado Envios, Mercado Shops, Mercado Ads, percepcoes fiscais
- **Bonuses (bonificacoes):** reembolsos de frete, ajustes de publicidade, devolucoes de impostos
- **Payment collected:** totais pagos e dividas pendentes

### 3.4 Detalhes de cobranca por pedido

```bash
GET /billing/integration/group/ML/order/details?order_ids={ORDER_ID}

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/billing/integration/group/ML/order/details?order_ids=12345678'
```

Retorna: `sale_fee` (comissao da plataforma), `shipping_cost` (custo de frete do seller), `discounts`, `coupon`.

### 3.5 Detalhes de pagamento por pedido

```bash
GET /billing/integration/periods/{EXPIRATION_DATE}/group/ML/payment/details

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/billing/integration/periods/2024-06-15/group/ML/payment/details'
```

**Permissao necessaria:** `billing` no escopo do app.

**Restricoes:**
- Historico disponivel dos ultimos 12 meses
- Limite de paginacao: min 1, max 1000, padrao 150
- Recomendado: consumo sequencial (1 chamada por dia por usuario para o summary)

---

## 4. Categories e Attributes API

### 4.1 Listar categorias de primeiro nivel (Brasil)

```bash
GET /sites/MLB/categories

curl -X GET https://api.mercadolibre.com/sites/MLB/categories
```

### 4.2 Detalhes de uma categoria (subcategorias, configuracoes)

```bash
GET /categories/{CATEGORY_ID}

curl -X GET https://api.mercadolibre.com/categories/MLB1744
```

Retorna: `path_from_root`, `children_categories` (com contagem de items), `settings` (requisitos de listagem), `attributable`.

### 4.3 Atributos obrigatorios e opcionais de uma categoria

```bash
GET /categories/{CATEGORY_ID}/attributes

curl -X GET https://api.mercadolibre.com/categories/MLB1744/attributes
```

Resposta inclui para cada atributo:
- `tags.required` = `true` indica obrigatorio
- `values` com IDs e nomes possiveis
- `hierarchy` e `relevance`
- Agrupamentos de atributos

### 4.4 Predicao de categoria a partir do titulo

```bash
GET /sites/MLB/domain_discovery/search?q={TITULO}&limit=1

curl -X GET 'https://api.mercadolibre.com/sites/MLB/domain_discovery/search?limit=3&q=iPhone%2015%20Pro%20Max'
```

Retorna: `domain_id`, `category_id`, `category_name` ordenados por maior probabilidade.

### 4.5 Top values de um atributo (por dominio)

```bash
POST /catalog_domains/{DOMAIN_ID}/attributes/{ATTRIBUTE_ID}/top_values

curl -X POST -H 'Content-Type: application/json' \
  -d '{"known_attributes": [{"id": "BRAND", "values": [{"name": "Apple"}]}]}' \
  https://api.mercadolibre.com/catalog_domains/MLB-CELLPHONES/attributes/MODEL/top_values
```

### 4.6 Download completo da arvore de categorias

```bash
GET /sites/MLB/categories/all

# Com atributos incluidos:
GET /sites/MLB/categories/all?withAttributes=true
```

Retorna JSON comprimido (gzip) com toda a hierarquia. Header `X-Content-Created` indica a data da ultima atualizacao.

---

## 5. Promotions / Deals API

### 5.1 Listar promocoes do vendedor

```bash
GET /seller-promotions/users/{USER_ID}?app_version=v2

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'app_version: v2' \
  'https://api.mercadolibre.com/seller-promotions/users/123456789?app_version=v2'
```

### 5.2 Criar desconto de preco (PRICE_DISCOUNT)

```bash
POST /seller-promotions/items/{ITEM_ID}?app_version=v2

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -H 'app_version: v2' \
  -d '{
    "deal_price": 79.90,
    "top_deal_price": 69.90,
    "start_date": "2024-07-01T00:00:00Z",
    "finish_date": "2024-07-14T23:59:59Z",
    "promotion_type": "PRICE_DISCOUNT"
  }' \
  'https://api.mercadolibre.com/seller-promotions/items/MLB12345678?app_version=v2'
```

**Parametros:**
- `deal_price`: preco com desconto para todos os compradores
- `top_deal_price` (opcional): preco especial para compradores Mercado Pontos nivel 3-6
- `start_date` / `finish_date`: datas em ISO 8601
- `promotion_type`: `PRICE_DISCOUNT`

**Regras:**
- Desconto minimo: 5%
- Desconto maximo: menos de 80%
- Duracao maxima: 14 dias (a partir de 03/2025)
- A diferenca entre desconto geral e top deve ser pelo menos 5% (ate 35% de desconto) ou 10% (acima de 35%)

### 5.3 Criar Lightning Deal (oferta relampago)

```bash
POST /marketplace/seller-promotions/items/{ITEM_ID}?user_id={USER_ID}

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -H 'version: v2' \
  -d '{
    "deal_id": "LGH-MLB1000",
    "original_price": 100.00,
    "deal_price": 64.00,
    "promotion_type": "LIGHTNING",
    "stock": 50
  }' \
  'https://api.mercadolibre.com/marketplace/seller-promotions/items/MLB2183693560?user_id=1317418851'
```

### 5.4 Deletar promocao

```bash
DELETE /seller-promotions/items/{ITEM_ID}?promotion_type=PRICE_DISCOUNT&app_version=v2

curl -X DELETE -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/seller-promotions/items/MLB12345678?promotion_type=PRICE_DISCOUNT&app_version=v2'
```

**Nota:** NAO se aplica a DOD e LIGHTNING (gerenciados pelo ML).

### 5.5 Consultar itens candidatos a promocoes

```bash
GET /seller-promotions/candidates/{CANDIDATE_ID}?app_version=v2
```

### 5.6 Consultar promocoes de um item

```bash
GET /seller-promotions/items/{ITEM_ID}?app_version=v2
```

### 5.7 Tipos de promocao disponiveis

| Tipo | Descricao |
|------|-----------|
| `DEAL` | Campanhas tradicionais |
| `MARKETPLACE_CAMPAIGN` | Campanhas co-financiadas |
| `PRICE_DISCOUNT` | Desconto individual por item |
| `LIGHTNING` | Ofertas relampago |
| `DOD` | Oferta do dia |
| `VOLUME` | Desconto por volume |
| `PRE_NEGOTIATED` | Desconto pre-negociado |
| `SELLER_CAMPAIGN` | Campanha do vendedor |
| `SMART` | Campanha automatizada co-financiada |
| `PRICE_MATCHING` | Precificacao competitiva |
| `UNHEALTHY_STOCK` | Liquidacao de estoque Full |
| `SELLER_COUPON_CAMPAIGN` | Cupons do vendedor (apenas MLB) |

### 5.8 Gerenciar lista de exclusao

```bash
# Excluir seller de promocoes automaticas
POST /seller-promotions/exclusion-list/seller?app_version=v2
Body: {"exclusion_status": "true"}

# Excluir item especifico
POST /seller-promotions/exclusion-list/item?app_version=v2
Body: {"item_id": "MLB12345678", "exclusion_status": "true"}
```

---

## 6. Sincronizacao de Estoque Multi-Canal

O ML nao oferece uma API nativa de sincronizacao multi-canal. A estrategia e usar os endpoints de estoque como ponto central.

### 6.1 Atualizar estoque de um item

```bash
PUT /items/{ITEM_ID}

curl -X PUT -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{"available_quantity": 25}' \
  https://api.mercadolibre.com/items/MLB12345678
```

**Comportamento automatico:**
- Se `available_quantity` = 0: status muda para `paused` com sub_status `out_of_stock`
- Se `available_quantity` > 0 e status era `out_of_stock`: status muda para `active`

### 6.2 Multi-Origin Stock (Estoque Multi-Origem)

Para sellers com multiplas lojas/depositos:

```bash
# Consultar estoque por localidade
GET /user-products/{USER_PRODUCT_ID}/stock

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'x-version: 2' \
  https://api.mercadolibre.com/user-products/UPROD123456/stock
```

```bash
# Atualizar estoque (obrigatorio header x-version)
PUT /user-products/{USER_PRODUCT_ID}/stock

curl -X PUT -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -H 'x-version: 2' \
  -d '{"locations": [{"id": "LOC1", "available_quantity": 10}]}' \
  https://api.mercadolibre.com/user-products/UPROD123456/stock
```

### 6.3 Estrategia recomendada para multi-canal

1. **Manter estoque centralizado** no seu ERP/sistema proprio
2. **Usar webhooks/notificacoes** do ML (topicos: `orders_v2`, `items`) para detectar vendas
3. **Atualizar `available_quantity`** via PUT /items/{ITEM_ID} sempre que houver venda em qualquer canal
4. **Usar notificacoes** de pedidos para receber alertas de vendas em tempo real
5. Se usar fulfillment, monitorar via `fbm_stock_operations`

---

## 7. Billing API (Faturas e Comissoes)

### 7.1 Periodos de cobranca

```bash
GET /billing/integration/monthly/periods?group=ML&document_type=BILL&limit=12
```

### 7.2 Documentos (faturas e notas de credito)

```bash
GET /billing/integration/periods/key/{YYYY-MM-DD}/documents?group=ML&document_type=BILL
```

### 7.3 Resumo detalhado de cobrancas

```bash
GET /billing/integration/periods/key/{YYYY-MM-DD}/summary/details
```

Retorna breakdown completo:
- **Comissoes de venda** (`sale_fee`)
- **Custos de frete** (Mercado Envios)
- **Custos de publicacao** (anuncios premium/classico)
- **Servicos** (Mercado Shops, Ads)
- **Percepcoes fiscais**
- **Bonificacoes** (reembolsos de frete, ajustes)

### 7.4 Detalhes por pedido

```bash
GET /billing/integration/group/ML/order/details?order_ids={ORDER_ID1},{ORDER_ID2}
```

Retorna para cada pedido: `sale_fee`, `marketplace_fee`, `shipping_cost`, `discounts`, `coupon`.

**Grupos de cobranca:**
- `ML` = Mercado Libre (comissoes de venda, envios, etc.)
- `MP` = Mercado Pago (taxas de processamento de pagamento)

---

## 8. Catalog / Product Matching

O ML possui um catalogo unificado. Vendedores podem "competir" no mesmo produto (Buy Box).

### 8.1 Buscar produto no catalogo

```bash
GET /products/search?status=active&site_id=MLB&q={TERMO}

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  'https://api.mercadolibre.com/products/search?status=active&site_id=MLB&q=iPhone+15+Pro+Max+256GB'
```

### 8.2 Criar listagem no catalogo (publicacao direta)

```bash
POST /items

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    "catalog_product_id": "MLB20444289",
    "catalog_listing": true,
    "price": 7999.00,
    "available_quantity": 10,
    "condition": "new",
    "listing_type_id": "gold_special",
    "currency_id": "BRL"
  }' \
  https://api.mercadolibre.com/items
```

### 8.3 Opt-in de publicacao existente para catalogo

```bash
POST /items/catalog_listings

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    "item_id": "MLB12345678",
    "catalog_product_id": "MLB20444289"
  }' \
  https://api.mercadolibre.com/items/catalog_listings
```

Se o item tiver variacoes, inclua tambem `variation_id`.

### 8.4 Verificar status de sincronizacao

```bash
GET /public/buybox/sync/{ITEM_ID}

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  https://api.mercadolibre.com/public/buybox/sync/MLB12345678
```

Retorna: `SYNC` ou `UNSYNC` com timestamp e relacoes.

### 8.5 Corrigir sincronizacao

```bash
POST /public/buybox/sync

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{"id": "MLB12345678"}' \
  https://api.mercadolibre.com/public/buybox/sync
```

### 8.6 Buscar itens auto-otimizados para catalogo

```bash
GET /users/{SELLER_ID}/items/search?status=active&tags=catalog_boost
```

**Conceitos importantes:**
- `catalog_product_id` identifica o produto no catalogo do ML
- Variacoes NAO sao permitidas em itens de catalogo; variacoes do marketplace sao convertidas em valores de atributo
- Sincronizacao automatica entre marketplace e catalogo para: preco, estoque, garantia e frete
- O seller e responsavel por confirmar que o produto corresponde exatamente a ficha tecnica do `catalog_product_id`

---

## 9. Upload de Imagens

### 9.1 Upload via multipart

```bash
POST /pictures/items/upload

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: multipart/form-data' \
  -F 'file=@/caminho/para/imagem.jpg' \
  https://api.mercadolibre.com/pictures/items/upload
```

Resposta:
```json
{
  "id": "959699-MLB43299127002_092024",
  "max_size": "994x1020",
  "dominant_color": null,
  "crop": { "y_offset": null, "y_size": null, "x_offset": null, "x_size": null },
  "variations": [...]
}
```

### 9.2 Associar imagem a um item

```bash
POST /items/{ITEM_ID}/pictures

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{"id": "959699-MLB43299127002_092024"}' \
  https://api.mercadolibre.com/items/MLB12345678/pictures
```

### 9.3 Substituir/reordenar imagens de um item

```bash
PUT /items/{ITEM_ID}

curl -X PUT -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    "pictures": [
      {"source": "https://exemplo.com/imagem1.jpg"},
      {"source": "https://exemplo.com/imagem2.jpg"},
      {"id": "959699-MLB43299127002_092024"}
    ]
  }' \
  https://api.mercadolibre.com/items/MLB12345678
```

### 9.4 Adicionar imagem com reordenamento e variacoes

```bash
PUT /items/{ITEM_ID}

curl -X PUT -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{
    "pictures": [
      {"source": "https://nova-imagem.jpg"},
      {"id": "111111-MLB_EXISTING_111111"}
    ],
    "variations": [{
      "id": "16787985187",
      "picture_ids": [
        "https://nova-imagem.jpg",
        "111111-MLB_EXISTING_111111"
      ]
    }]
  }' \
  https://api.mercadolibre.com/items/MLB12345678
```

### 9.5 Verificar erros de imagem

```bash
GET /pictures/{PICTURE_ID}/errors

curl -X GET -H 'Authorization: Bearer $ACCESS_TOKEN' \
  https://api.mercadolibre.com/pictures/970736-MLB11111111111_092024/errors
```

### 9.6 Especificacoes de imagem

| Propriedade | Valor |
|-------------|-------|
| Formatos suportados | JPG, JPEG, PNG |
| Tamanho maximo do arquivo | 10 MB |
| Resolucao recomendada | 1200 x 1200 px |
| Resolucao maxima aceita | 1920 x 1920 px |
| Resolucao minima | 500 x 500 px |
| Zoom ativado | Largura > 800 px |
| Qualidade | RGB (melhor que CMYK), produto ocupando 95% do espaco |

**Dicas:**
- Para substituir uma imagem, use uma nova URL de source (reutilizar a mesma URL nao atualiza)
- Para deletar imagens, envie PUT com apenas as imagens que deseja manter
- Apenas upload multipart e suportado (dados diretos, nao URL remota no upload)

---

## 10. Ambiente de Testes / Sandbox

**O Mercado Livre NAO possui ambiente sandbox.** Todos os testes sao feitos em **producao** usando **usuarios de teste**.

### 10.1 Criar usuario de teste

```bash
POST /users/test_user

curl -X POST -H 'Authorization: Bearer $ACCESS_TOKEN' \
  -H 'Content-Type: application/json' \
  -d '{"site_id": "MLB"}' \
  https://api.mercadolibre.com/users/test_user
```

Resposta:
```json
{
  "id": 123456789,
  "nickname": "TEST_USER_XXXXXXX",
  "password": "qatest12345",
  "site_status": "active"
}
```

### 10.2 Restricoes dos usuarios de teste

- Maximo **10 usuarios de teste** por conta real
- Usuarios inativos por **60 dias** sao removidos automaticamente
- **Salve as credenciais imediatamente** - nao existe endpoint para recupera-las
- Usuarios de teste so interagem entre si (comprar, vender, perguntar)
- Publicar apenas na categoria "Outros"
- Nunca usar `gold` ou `gold_premium` como listing_type
- Items de teste sao purgados periodicamente

### 10.3 Cartoes de teste (Mercado Pago)

Para simular compras entre usuarios de teste, use cartoes de teste do Mercado Pago. O resultado do pagamento e controlado pelo nome do titular:

| Nome do titular | Resultado |
|-----------------|-----------|
| `APRO APRO` | Pagamento aprovado |
| `OTHE OTHE` | Pagamento recusado |
| `CONT CONT` | Pagamento pendente |

### 10.4 Fluxo de teste recomendado

1. Criar 2 usuarios de teste (comprador e vendedor)
2. Autenticar ambos via OAuth 2.0
3. Publicar um item com o usuario vendedor (categoria "Outros")
4. Fazer uma compra com o usuario comprador usando cartao de teste
5. Testar fluxo de envio, perguntas, avaliacoes

---

## Referencia Rapida de Permissoes (Scopes)

| Scope | Recursos |
|-------|----------|
| `read` | Consulta de items, categorias, usuarios |
| `write` | Criacao/edicao de items |
| `offline_access` | Refresh token |
| `billing` | Faturas, cobrancas, relatorios financeiros |
| `advertising` | Product Ads, campanhas |

---

## Links Uteis

- Documentacao oficial BR: https://developers.mercadolivre.com.br/
- Documentacao Global Selling: https://global-selling.mercadolibre.com/devsite/api-docs
- API Base: https://api.mercadolibre.com
- Referencia de sites: `GET /sites` (MLB = Brasil, MLA = Argentina, MLM = Mexico, etc.)
