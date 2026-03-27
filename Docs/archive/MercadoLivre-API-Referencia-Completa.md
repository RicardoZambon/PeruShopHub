# Mercado Livre API - Referencia Completa para Sistema de Gestao de Vendedor

> **Base URL da API**: `https://api.mercadolibre.com`
> **Portal do Desenvolvedor (Brasil)**: `https://developers.mercadolivre.com.br`
> **Site ID Brasil**: `MLB`

---

## Indice

1. [Autenticacao (OAuth 2.0)](#1-autenticacao-oauth-20)
2. [Produtos/Anuncios (Items API)](#2-produtosanuncios-items-api)
3. [Pedidos (Orders API)](#3-pedidos-orders-api)
4. [Estoque e Inventario](#4-estoque-e-inventario)
5. [Perguntas e Respostas](#5-perguntas-e-respostas)
6. [Mensagens Pos-Venda](#6-mensagens-pos-venda)
7. [Envios (Shipping API)](#7-envios-shipping-api)
8. [Reclamacoes e Devolucoes](#8-reclamacoes-e-devolucoes)
9. [Notificacoes/Webhooks](#9-notificacoeswebhooks)
10. [Metricas, Relatorios e Reputacao](#10-metricas-relatorios-e-reputacao)
11. [Rate Limits e Restricoes](#11-rate-limits-e-restricoes)
12. [SDKs e Bibliotecas](#12-sdks-e-bibliotecas)

---

## 1. Autenticacao (OAuth 2.0)

### 1.1 Registro da Aplicacao

Acesse o DevCenter em `https://applications.mercadolibre.com` e crie uma nova aplicacao. Voce recebera:
- **App ID** (client_id)
- **Secret Key** (client_secret)
- **Redirect URI** (deve ser identica em todas as chamadas)

### 1.2 Escopos Disponiveis

| Escopo | Descricao |
|--------|-----------|
| `read` | Somente leitura de dados (consultar items, pedidos, etc.) |
| `write` | Leitura + escrita (criar anuncios, responder perguntas, etc.) |
| `offline_access` | Permite refresh token para renovar acesso sem login do usuario |

**Recomendado para sistema de gestao**: usar todos os tres escopos.

### 1.3 Fluxo de Autorizacao (Authorization Code Grant)

**Etapa 1 - Redirecionar usuario para login:**
```
GET https://auth.mercadolivre.com.br/authorization
    ?response_type=code
    &client_id={APP_ID}
    &redirect_uri={REDIRECT_URI}
    &state={RANDOM_STATE}
    &code_challenge={CODE_CHALLENGE}          # opcional (PKCE)
    &code_challenge_method=S256               # opcional (PKCE)
```

Apos login e autorizacao, o usuario e redirecionado para:
```
{REDIRECT_URI}?code={AUTH_CODE}&state={RANDOM_STATE}
```

**Etapa 2 - Trocar codigo por access token:**
```http
POST https://api.mercadolibre.com/oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id={APP_ID}
&client_secret={SECRET_KEY}
&code={AUTH_CODE}
&redirect_uri={REDIRECT_URI}
&code_verifier={CODE_VERIFIER}    # se usou PKCE
```

**Resposta:**
```json
{
  "access_token": "APP_USR-1234567890123456-031820-abcdefghijklmnop-123456789",
  "token_type": "bearer",
  "expires_in": 21600,
  "scope": "offline_access read write",
  "user_id": 123456789,
  "refresh_token": "TG-abcdefghijklmnop-123456789"
}
```

- **Validade do access_token**: 6 horas (21600 segundos)
- **Validade do refresh_token**: 6 meses

### 1.4 Renovar Token (Refresh)

```http
POST https://api.mercadolibre.com/oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token
&client_id={APP_ID}
&client_secret={SECRET_KEY}
&refresh_token={REFRESH_TOKEN}
```

**Importante**: O refresh_token e de uso unico. Cada renovacao retorna um novo refresh_token.

### 1.5 Usar o Token em Todas as Requisicoes

```http
Authorization: Bearer APP_USR-1234567890123456-031820-abcdefghijklmnop-123456789
```

### 1.6 Erros de Autenticacao

| Codigo | Erro | Causa |
|--------|------|-------|
| 400 | `invalid_client` | client_id ou secret invalido |
| 400 | `invalid_grant` | Codigo ou token expirado/revogado |
| 400 | `invalid_scope` | Escopo nao permitido |
| 400 | `invalid_request` | Parametro obrigatorio ausente |
| 403 | `forbidden` | IP bloqueado, scopes insuficientes |
| 429 | `local_rate_limited` | Limite de requisicoes excedido |

### 1.7 Revogacao Automatica de Tokens

Tokens sao invalidados antes da expiracao por:
- Alteracao de senha do usuario
- Atualizacao do Client Secret da aplicacao
- Revogacao de permissoes pelo usuario
- Inatividade por 4 meses
- Exclusao de sessoes (fraude, troca de dispositivo)

---

## 2. Produtos/Anuncios (Items API)

### 2.1 Criar Anuncio

```http
POST https://api.mercadolibre.com/items
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json
```

```json
{
  "title": "Capinha Premium iPhone 15 Pro Max Silicone",
  "category_id": "MLB417798",
  "price": 69.90,
  "currency_id": "BRL",
  "available_quantity": 50,
  "buying_mode": "buy_it_now",
  "listing_type_id": "gold_special",
  "condition": "new",
  "site_id": "MLB",
  "pictures": [
    {"source": "https://exemplo.com/foto1.jpg"},
    {"source": "https://exemplo.com/foto2.jpg"}
  ],
  "attributes": [
    {"id": "BRAND", "value_name": "GenericBrand"},
    {"id": "MODEL", "value_name": "iPhone 15 Pro Max"},
    {"id": "GTIN", "value_name": "7891234567890"}
  ],
  "sale_terms": [
    {"id": "WARRANTY_TYPE", "value_name": "Garantia do vendedor"},
    {"id": "WARRANTY_TIME", "value_name": "90 dias"}
  ],
  "shipping": {
    "mode": "me2",
    "local_pick_up": false,
    "free_shipping": true
  }
}
```

**Tipos de anuncio (`listing_type_id`)**:

| ID | Nome | Comissao | Parcelamento |
|----|------|----------|-------------|
| `free` | Gratis | 0% | Nao |
| `bronze` | Classico | 10-14% | Nao |
| `gold_special` | Premium | 15-19% | Ate 12x sem juros |

### 2.2 Consultar Anuncio

```http
GET https://api.mercadolibre.com/items/{ITEM_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Consultar multiplos itens (multiget, ate 20)**:
```http
GET https://api.mercadolibre.com/items?ids=MLB1234,MLB5678,MLB9012
Authorization: Bearer {ACCESS_TOKEN}
```

**Selecionar campos especificos**:
```http
GET https://api.mercadolibre.com/items?ids=MLB1234&attributes=id,title,price,available_quantity
```

### 2.3 Atualizar Anuncio

```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json
```

**Atualizar preco e titulo:**
```json
{
  "title": "Novo Titulo do Produto",
  "price": 79.90
}
```

**Atualizar estoque:**
```json
{
  "available_quantity": 100
}
```

### 2.4 Gerenciar Status do Anuncio

**Pausar:**
```json
PUT /items/{ITEM_ID}
{ "status": "paused" }
```

**Reativar:**
```json
PUT /items/{ITEM_ID}
{ "status": "active" }
```

**Fechar (encerrar definitivamente):**
```json
PUT /items/{ITEM_ID}
{ "status": "closed" }
```

**Excluir (apos fechar):**
```json
PUT /items/{ITEM_ID}
{ "deleted": "true" }
```

### 2.5 Descricao do Item

**Criar descricao (somente plain text):**
```http
POST https://api.mercadolibre.com/items/{ITEM_ID}/description
Content-Type: application/json

{
  "plain_text": "Capinha premium em silicone de alta qualidade.\n\nCompativel com iPhone 15 Pro Max.\nProtecao contra quedas e impactos.\n\nConteudo da embalagem:\n- 1x Capinha de silicone\n- 1x Pano de limpeza"
}
```

**Atualizar descricao:**
```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}/description?api_version=2
Content-Type: application/json

{
  "plain_text": "Descricao atualizada aqui..."
}
```

**Consultar descricao:**
```http
GET https://api.mercadolibre.com/items/{ITEM_ID}/description
```

**Nota**: Somente texto puro (`plain_text`), sem HTML. Quebras de linha com `\n`.

### 2.6 Fotos/Imagens

**Upload de imagem:**
```http
POST https://api.mercadolibre.com/pictures/items/upload
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: multipart/form-data

file: [arquivo_da_imagem]
```

Retorna `picture_id` para uso no item.

**Vincular imagem ao item:**
```http
POST https://api.mercadolibre.com/items/{ITEM_ID}/pictures
Content-Type: application/json

{ "id": "{PICTURE_ID}" }
```

**Substituir todas as imagens:**
```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Content-Type: application/json

{
  "pictures": [
    {"id": "foto_existente_id"},
    {"source": "https://nova-foto.com/img.jpg"}
  ]
}
```

**Verificar erros em imagem:**
```http
GET https://api.mercadolibre.com/pictures/{PICTURE_ID}/errors
```

**Requisitos de imagem:**

| Especificacao | Valor |
|---------------|-------|
| Tamanho maximo | 10 MB |
| Resolucao recomendada | 1200 x 1200 px |
| Resolucao maxima | 1920 x 1920 px |
| Resolucao minima | 500 x 500 px |
| Formatos aceitos | JPG, JPEG, PNG |
| Zoom ativado | Imagens > 800 px de largura |

### 2.7 Variacoes

**Criar item com variacoes:**
```http
POST https://api.mercadolibre.com/items
Content-Type: application/json

{
  "title": "Capinha Silicone iPhone 15 Pro Max",
  "category_id": "MLB417798",
  "price": 69.90,
  "currency_id": "BRL",
  "available_quantity": 30,
  "buying_mode": "buy_it_now",
  "listing_type_id": "gold_special",
  "condition": "new",
  "pictures": [
    {"id": "foto_preta_id"},
    {"id": "foto_azul_id"},
    {"id": "foto_vermelha_id"}
  ],
  "variations": [
    {
      "attribute_combinations": [
        {"id": "COLOR", "value_id": "52049", "value_name": "Preto"}
      ],
      "price": 69.90,
      "available_quantity": 10,
      "picture_ids": ["foto_preta_id"],
      "attributes": [
        {"id": "SELLER_SKU", "value_name": "CAP-IPH15PM-PRETO"},
        {"id": "GTIN", "value_name": "7891234567890"}
      ]
    },
    {
      "attribute_combinations": [
        {"id": "COLOR", "value_id": "52014", "value_name": "Azul"}
      ],
      "price": 69.90,
      "available_quantity": 10,
      "picture_ids": ["foto_azul_id"],
      "attributes": [
        {"id": "SELLER_SKU", "value_name": "CAP-IPH15PM-AZUL"},
        {"id": "GTIN", "value_name": "7891234567891"}
      ]
    },
    {
      "attribute_combinations": [
        {"id": "COLOR", "value_id": "52028", "value_name": "Vermelho"}
      ],
      "price": 69.90,
      "available_quantity": 10,
      "picture_ids": ["foto_vermelha_id"],
      "attributes": [
        {"id": "SELLER_SKU", "value_name": "CAP-IPH15PM-VERM"},
        {"id": "GTIN", "value_name": "7891234567892"}
      ]
    }
  ]
}
```

**Adicionar variacao a item existente:**
```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}

{
  "variations": [
    {"id": 12345678},
    {
      "attribute_combinations": [
        {"id": "COLOR", "value_id": "52005", "value_name": "Verde"}
      ],
      "price": 69.90,
      "available_quantity": 5,
      "picture_ids": ["foto_verde_id"]
    }
  ]
}
```

**Atualizar estoque de variacao:**
```json
PUT /items/{ITEM_ID}
{
  "variations": [
    {"id": 12345678, "available_quantity": 25}
  ]
}
```

**Atualizar preco de variacao:**
```json
PUT /items/{ITEM_ID}
{
  "variations": [
    {"id": 12345678, "price": 79.90},
    {"id": 87654321, "price": 79.90}
  ]
}
```

**Remover variacao**: Omita o `id` da variacao no PUT (envie somente as que devem permanecer).

**Consultar variacoes:**
```http
GET https://api.mercadolibre.com/items/{ITEM_ID}/variations
GET https://api.mercadolibre.com/items/{ITEM_ID}/variations/{VARIATION_ID}?include_attributes=all
```

**Limites**: Maximo 100 variacoes por item (250 para Moda, Acessorios de Celular e Autopecas).

### 2.8 Precos

**Consultar preco de venda (com contexto):**
```http
GET https://api.mercadolibre.com/items/{ITEM_ID}/sale_price
    ?context=channel_marketplace,buyer_loyalty_3
```

**Consultar todos os precos:**
```http
GET https://api.mercadolibre.com/items/{ITEM_ID}/prices
```

**Atualizar preco** (via endpoint principal):
```json
PUT /items/{ITEM_ID}
{ "price": 89.90 }
```

### 2.9 Buscar Anuncios do Vendedor

**Busca publica:**
```http
GET https://api.mercadolibre.com/sites/MLB/search?seller_id={SELLER_ID}
GET https://api.mercadolibre.com/sites/MLB/search?nickname={NICKNAME}
GET https://api.mercadolibre.com/sites/MLB/search?seller_id={SELLER_ID}&category={CAT_ID}
```

**Busca privada (todos os itens):**
```http
GET https://api.mercadolibre.com/users/{USER_ID}/items/search
Authorization: Bearer {ACCESS_TOKEN}
```

**Filtros disponiveis:**
```http
GET /users/{USER_ID}/items/search?status=active
GET /users/{USER_ID}/items/search?sku={SELLER_CUSTOM_FIELD}
GET /users/{USER_ID}/items/search?seller_sku={SELLER_SKU}
GET /users/{USER_ID}/items/search?listing_type_id=gold_special
GET /users/{USER_ID}/items/search?orders=start_time_desc
```

**Paginacao para grandes volumes (>1000 itens) - modo scan:**
```http
GET /users/{USER_ID}/items/search?search_type=scan
GET /users/{USER_ID}/items/search?search_type=scan&scroll_id={SCROLL_ID}
```
O `scroll_id` expira em 5 minutos.

### 2.10 Categorias e Atributos

**Consultar categorias do site:**
```http
GET https://api.mercadolibre.com/sites/MLB/categories
```

**Consultar subcategorias:**
```http
GET https://api.mercadolibre.com/categories/{CATEGORY_ID}
```

**Consultar atributos obrigatorios da categoria:**
```http
GET https://api.mercadolibre.com/categories/{CATEGORY_ID}/attributes
```

**Consultar termos de venda da categoria:**
```http
GET https://api.mercadolibre.com/categories/{CATEGORY_ID}/sale_terms
```

**Prever categoria por titulo:**
```http
GET https://api.mercadolibre.com/sites/MLB/domain_discovery/search?q=capinha+iphone+15
```

### 2.11 Catalogo

Para itens de catalogo (produtos ja existentes na base do ML):

**Buscar produto no catalogo:**
```http
GET https://api.mercadolibre.com/products/search?site_id=MLB&q=iPhone+15+Pro+Max
```

**Criar anuncio vinculado ao catalogo:**
```json
POST /items
{
  "catalog_product_id": "MLB12345678",
  "price": 69.90,
  "currency_id": "BRL",
  "available_quantity": 10,
  "listing_type_id": "gold_special",
  "condition": "new"
}
```

---

## 3. Pedidos (Orders API)

### 3.1 Consultar Pedido

```http
GET https://api.mercadolibre.com/orders/{ORDER_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Resposta inclui**:
- `id`, `status`, `status_detail`
- `date_created`, `date_closed`
- `order_items[]` (item_id, title, quantity, unit_price, variation_id)
- `payments[]` (id, status, transaction_amount, payment_type)
- `buyer` (id, nickname, first_name, last_name)
- `shipping.id` (para consultar envio separadamente)
- `total_amount`, `paid_amount`
- `pack_id` (se faz parte de um carrinho)
- `tags[]`

### 3.2 Buscar Pedidos do Vendedor

```http
GET https://api.mercadolibre.com/orders/search
    ?seller={SELLER_ID}
    &order.status=paid
    &order.date_created.from=2026-01-01T00:00:00.000-03:00
    &order.date_created.to=2026-03-21T23:59:59.999-03:00
    &sort=date_desc
    &limit=50
    &offset=0
Authorization: Bearer {ACCESS_TOKEN}
```

**Filtros disponiveis:**
- `order.status`: `confirmed`, `payment_required`, `payment_in_process`, `partially_paid`, `paid`, `partially_refunded`, `pending_cancel`, `cancelled`
- `order.date_created.from/to`: intervalo de datas
- `order.date_closed.from/to`: data de fechamento
- `q`: busca generica (ID do pedido, ID do item, titulo, nickname)
- `tags`: filtrar por tags (separadas por virgula)
- `sort`: `date_asc`, `date_desc`

### 3.3 Packs (Carrinho de Compras)

Quando o comprador adquire multiplos itens do mesmo vendedor, eles sao agrupados em um pack.

```http
GET https://api.mercadolibre.com/packs/{PACK_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Resposta**:
- `id`, `status` (released, error, pending_cancel, cancelled)
- `orders[]` (IDs dos pedidos associados)
- `shipment.id`
- `buyer.id`
- `date_created`, `last_updated`

### 3.4 Descontos do Pedido

```http
GET https://api.mercadolibre.com/orders/{ORDER_ID}/discounts
Authorization: Bearer {ACCESS_TOKEN}
```

Retorna cupons, cashback, campanhas aplicadas.

### 3.5 Informacoes de Produto do Pedido

```http
GET https://api.mercadolibre.com/orders/{ORDER_ID}/product
Authorization: Bearer {ACCESS_TOKEN}
```

Retorna atributos do item e detalhes como IMEI (para eletronicos).

---

## 4. Estoque e Inventario

### 4.1 Atualizar Estoque (Item Simples)

```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "available_quantity": 50
}
```

### 4.2 Atualizar Estoque (Item com Variacoes)

```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "variations": [
    {"id": 111111, "available_quantity": 20},
    {"id": 222222, "available_quantity": 15},
    {"id": 333333, "available_quantity": 30}
  ]
}
```

### 4.3 Fulfillment (Full) - Consultar Estoque no CD

**Obter inventory_id do item:**
```http
GET https://api.mercadolibre.com/items/{ITEM_ID}
```
O campo `inventory_id` identifica o item no sistema de fulfillment.

**Consultar estoque no CD:**
```http
GET https://api.mercadolibre.com/inventories/{INVENTORY_ID}/stock/fulfillment
Authorization: Bearer {ACCESS_TOKEN}
```

**Resposta:**
```json
{
  "total": 100,
  "available_quantity": 85,
  "unavailable_quantity": 15,
  "unavailable_detail": {
    "reserved": 10,
    "damaged": 3,
    "not_in_logistic_center": 2
  }
}
```

### 4.4 Historico de Operacoes de Estoque (Fulfillment)

```http
GET https://api.mercadolibre.com/stock/fulfillment/operations/search
    ?seller_id={SELLER_ID}
    &inventory_id={INVENTORY_ID}
    &date_from=2026-01-01T00:00:00.000-03:00
    &date_to=2026-03-21T23:59:59.999-03:00
    &type=sale_confirmation
    &limit=100
    &sort=date_desc
Authorization: Bearer {ACCESS_TOKEN}
```

**Tipos de operacao:**

| Tipo | Descricao |
|------|-----------|
| `inbound_reception` | Recebimento no CD |
| `fiscal_coverage_adjustment` | Ajuste de cobertura fiscal |
| `sale_confirmation` | Confirmacao de venda |
| `sale_cancelation` | Cancelamento de venda |
| `sale_delivery_cancelation` | Cancelamento de entrega |
| `sale_return` | Devolucao |
| `withdrawal_reservation` | Reserva para retirada |
| `transfer_reservation` | Transferencia entre CDs |
| `quarantine_reservation` | Quarentena |
| `removal_reservation` | Remocao do CD |
| `adjustment` | Ajuste manual |
| `identification_problem_remove` | Remocao por problema de identificacao |
| `identification_problem_add` | Adicao apos resolucao de identificacao |

**Limite de consulta**: ultimos 12 meses.

### 4.5 Tempo de Fabricacao (Sob Encomenda)

```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Content-Type: application/json

{
  "sale_terms": [
    {"id": "MANUFACTURING_TIME", "value_name": "20 dias"}
  ]
}
```

### 4.6 Quantidade Maxima por Compra

```http
PUT https://api.mercadolibre.com/items/{ITEM_ID}
Content-Type: application/json

{
  "sale_terms": [
    {"id": "PURCHASE_MAX_QUANTITY", "value_name": "10"}
  ]
}
```

---

## 5. Perguntas e Respostas

### 5.1 Listar Perguntas Recebidas

```http
GET https://api.mercadolibre.com/my/received_questions/search
Authorization: Bearer {ACCESS_TOKEN}
```

**Filtros:**
```http
GET /my/received_questions/search?status=UNANSWERED&sort_fields=date_created&sort_types=DESC
```

### 5.2 Buscar Perguntas por Item

```http
GET https://api.mercadolibre.com/questions/search?item_id={ITEM_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Status disponiveis para filtro:**
- `UNANSWERED` - Nao respondida
- `ANSWERED` - Respondida
- `CLOSED_UNANSWERED` - Fechada sem resposta
- `UNDER_REVIEW` - Em revisao
- `BANNED` - Banida
- `DELETED` - Excluida
- `DISABLED` - Desabilitada

### 5.3 Consultar Pergunta Especifica

```http
GET https://api.mercadolibre.com/questions/{QUESTION_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Com dados do comprador (email, telefone, nome):**
```http
GET https://api.mercadolibre.com/questions/{QUESTION_ID}?api_version=4
```

### 5.4 Responder Pergunta

```http
POST https://api.mercadolibre.com/answers
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "question_id": 3957150025,
  "text": "Olá! Sim, temos em estoque. O prazo de envio é de 1 a 2 dias úteis. Obrigado pelo interesse!"
}
```

### 5.5 Lista Negra (Bloquear Compradores)

**Listar bloqueados:**
```http
GET https://api.mercadolibre.com/users/{SELLER_ID}/questions_blacklist
```

**Bloquear usuario:**
```http
POST https://api.mercadolibre.com/users/{SELLER_ID}/questions_blacklist
Content-Type: application/json

{"user_id": 987654321}
```

**Desbloquear usuario:**
```http
DELETE https://api.mercadolibre.com/users/{SELLER_ID}/questions_blacklist/{USER_ID}
```

---

## 6. Mensagens Pos-Venda

### 6.1 Enviar Mensagem ao Comprador

```http
POST https://api.mercadolibre.com/messages/packs/{PACK_ID}/sellers/{SELLER_ID}?tag=post_sale
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "from": {"user_id": "{SELLER_ID}"},
  "to": {"user_id": "{BUYER_ID}"},
  "text": "Olá! Seu pedido já foi embalado e será despachado amanhã. Obrigado pela compra!"
}
```

**Limites de caracteres:**
- Vendedor: **350 caracteres**
- Comprador: 3500 caracteres
- Codificacao: ISO-8859-1 (latin1)

### 6.2 Listar Mensagens de um Pack

```http
GET https://api.mercadolibre.com/messages/packs/{PACK_ID}/sellers/{SELLER_ID}
    ?limit=50
    &offset=0
    &mark_as_read=false
Authorization: Bearer {ACCESS_TOKEN}
```

### 6.3 Consultar Mensagem Especifica

```http
GET https://api.mercadolibre.com/messages/{MESSAGE_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

### 6.4 Enviar Anexo

**Upload do arquivo:**
```http
POST https://api.mercadolibre.com/messages/attachments?tag=post_sale&site_id=MLB
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: multipart/form-data

file: [arquivo]
```

**Depois, enviar mensagem com anexo:**
```json
{
  "from": {"user_id": "{SELLER_ID}"},
  "to": {"user_id": "{BUYER_ID}"},
  "text": "Segue a nota fiscal em anexo.",
  "attachments": ["{ATTACHMENT_ID}"]
}
```

**Limites de anexo:**
- Tamanho maximo: 25 MB
- Formatos: JPG, PNG, PDF, TXT
- Maximo 25 anexos por mensagem

**Baixar anexo:**
```http
GET https://api.mercadolibre.com/messages/attachments/{ATTACHMENT_ID}?tag=post_sale&site_id=MLB
```

### 6.5 Status de Moderacao de Mensagens

| Status | Descricao |
|--------|-----------|
| `clean` | Aprovada |
| `rejected` | Rejeitada por violacao |
| `pending` | Em analise |
| `non_moderated` | Nao moderada |

**Motivos de rejeicao**: linguagem ofensiva, links de redes sociais, URLs encurtadas, dados pessoais, links do MercadoPago/PayPal, evasao de reclamacao.

### 6.6 Rate Limits (Mensagens)

- **GET**: 500 requisicoes por minuto (compartilhado)
- **POST/PUT**: 500 requisicoes por minuto (compartilhado)

**Nota**: Mensagens sao bloqueadas em pedidos cancelados, exceto se houver conversa previa (prazo de 30 dias apos ultima mensagem do comprador).

---

## 7. Envios (Shipping API)

### 7.1 Consultar Envio

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}
Authorization: Bearer {ACCESS_TOKEN}
x-format-new: true
```

### 7.2 Itens do Envio

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/items
Authorization: Bearer {ACCESS_TOKEN}
```

### 7.3 Custos do Envio

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/costs
Authorization: Bearer {ACCESS_TOKEN}
X-Costs-New: true
```

### 7.4 Historico de Status (Rastreamento)

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/history
Authorization: Bearer {ACCESS_TOKEN}
```

Retorna todas as mudancas de status e substatus ao longo do ciclo de vida do envio.

### 7.5 Prazos de Entrega (Lead Time)

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/lead_time
Authorization: Bearer {ACCESS_TOKEN}
```

Retorna datas estimadas de entrega, prazo de manuseio e deadlines.

### 7.6 Atrasos

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/delays
Authorization: Bearer {ACCESS_TOKEN}
```

### 7.7 Transportadora

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/carrier
Authorization: Bearer {ACCESS_TOKEN}
```

Retorna nome da transportadora e URL de rastreamento.

### 7.8 Pagamentos do Envio

```http
GET https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/payments
Authorization: Bearer {ACCESS_TOKEN}
```

### 7.9 Todos os Status Possiveis

```http
GET https://api.mercadolibre.com/shipment_statuses
```

### 7.10 Marcar como Pronto para Envio (ME2)

```http
POST https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/process/ready_to_ship
Authorization: Bearer {ACCESS_TOKEN}
```

### 7.11 Dividir Envio em Multiplos Pacotes

```http
POST https://api.mercadolibre.com/shipments/{SHIPMENT_ID}/split
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "reason": "DIMENSIONS_EXCEEDED",
  "orders": [
    {"id": 123456, "quantity": 2},
    {"id": 789012, "quantity": 1}
  ]
}
```

**Razoes validas**: `FRAGILE`, `ANOTHER_WAREHOUSE`, `IRREGULAR_SHAPE`, `OTHER_MOTIVE`, `DIMENSIONS_EXCEEDED`

### 7.12 Etiqueta de Envio

```http
GET https://api.mercadolibre.com/shipment_labels?shipment_ids={SHIPMENT_ID}&response_type=pdf
Authorization: Bearer {ACCESS_TOKEN}
```

---

## 8. Reclamacoes e Devolucoes

### 8.1 Buscar Reclamacoes

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/search
    ?status=opened
    &stage=claim
    &sort=date_desc
    &limit=50
    &offset=0
Authorization: Bearer {ACCESS_TOKEN}
```

**Filtros:**
- `status`: `opened`, `closed`
- `stage`: `claim`, `dispute`, `recontact`, `stale`, `none`
- `resource`: `shipment`, `payment`, `order`, `purchase`
- `reason_id`: motivo especifico
- `range`: intervalo de datas

### 8.2 Consultar Reclamacao

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

### 8.3 Detalhes da Reclamacao

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/detail
```

Retorna data limite para acao, responsavel e descricao do estado.

### 8.4 Historico de Acoes

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/actions-history
```

### 8.5 Historico de Status

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/status-history
```

### 8.6 Impacto na Reputacao

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/affects-reputation
```

### 8.7 Motivo da Reclamacao

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/reasons/{REASON_ID}
```

### 8.8 Consultar Devolucao

```http
GET https://api.mercadolibre.com/post-purchase/v2/claims/{CLAIM_ID}/returns
Authorization: Bearer {ACCESS_TOKEN}
```

**Tipos de devolucao:**
- `claim` - Devolucao por reclamacao do comprador
- `dispute` - Devolucao por disputa entre partes
- `automatic` - Devolucao automatica processada pelo sistema

### 8.9 Custo de Devolucao

```http
GET https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/charges/return-cost
    ?calculate_amount_usd=false
Authorization: Bearer {ACCESS_TOKEN}
```

### 8.10 Reembolso Parcial

```http
POST https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/expected-resolutions/partial-refund
Authorization: Bearer {ACCESS_TOKEN}
```

**Nota**: Nao e permitido oferecer reembolso de 100% por este endpoint.

### 8.11 Revisao de Devolucao (Verificar Produto Devolvido)

```http
POST https://api.mercadolibre.com/post-purchase/v1/returns/{RETURN_ID}/return-review
Authorization: Bearer {ACCESS_TOKEN}
Content-Type: application/json

{
  "reason": "produto_danificado",
  "message": "Produto devolvido com danos não mencionados.",
  "attachments": ["{ATTACHMENT_ID}"]
}
```

**Upload de evidencias:**
```http
POST https://api.mercadolibre.com/post-purchase/v1/claims/{CLAIM_ID}/returns/attachments
Content-Type: multipart/form-data

file: [foto_do_produto]
```

### 8.12 Motivos para Revisao com Falha

```http
GET https://api.mercadolibre.com/post-purchase/v1/returns/reasons
    ?flow=seller_return_failed
    &claim_id={CLAIM_ID}
```

---

## 9. Notificacoes/Webhooks

### 9.1 Configuracao

1. Acesse `https://applications.mercadolibre.com`
2. Edite sua aplicacao
3. Configure a **Callback URL** (URL publica HTTPS do seu servidor)
4. Selecione os **Topicos** que deseja receber

### 9.2 Topicos Disponiveis

| Topico | Descricao | Uso Recomendado |
|--------|-----------|-----------------|
| `orders_v2` | Confirmacao e alteracao de vendas | **Essencial** |
| `items` | Mudancas em publicacoes | **Essencial** |
| `questions` | Perguntas e respostas | **Essencial** |
| `messages` | Novas mensagens (subtopicos: created, read) | **Essencial** |
| `payments` | Criacao/mudanca de status de pagamentos | **Essencial** |
| `shipments` | Criacao e atualizacao de envios | **Essencial** |
| `claims` | Notificacoes de disputas | **Importante** |
| `items_prices` | Criacao/atualizacao/exclusao de precos | Recomendado |
| `stock_locations` | Modificacoes de estoque do produto | Recomendado |
| `stock_fulfillment` | Operacoes de estoque no CD (Full) | Recomendado (se usa Full) |
| `invoices` | Notas fiscais automaticas (somente Full/Brasil) | Recomendado (se usa Full) |
| `orders_feedback` | Mudancas em avaliacoes | Opcional |
| `item_competition` | Status de anuncios no catalogo | Opcional |
| `public_offers` | Criacao/mudanca de ofertas | Opcional |
| `best_price_eligible` | Promocoes e precos competitivos | Opcional |
| `flex_handshakes` | Transferencias Flex | Opcional (se usa Flex) |
| `catalog_suggestions` | Sugestoes do Brand Central | Opcional |
| `items_prices` | Mudancas de preco | Opcional |

### 9.3 Formato da Notificacao

```json
{
  "_id": "f9f08571-1f65-4c46-9e0a-c0f43faas1557e",
  "resource": "/items/MLB1234567890",
  "user_id": 123456789,
  "topic": "items",
  "application_id": 9876543210,
  "attempts": 1,
  "sent": "2026-03-21T15:30:00.000Z",
  "received": "2026-03-21T15:30:00.500Z"
}
```

### 9.4 Requisitos de Resposta

- Retornar **HTTP 200** em ate **500 milissegundos**
- Processar a notificacao de forma assincrona (apenas confirme recebimento)
- Apos confirmar, faca GET no `resource` para obter os dados completos

```
GET https://api.mercadolibre.com{resource}
Authorization: Bearer {ACCESS_TOKEN}
```

### 9.5 Politica de Retry

- Se nao receber HTTP 200, o ML retenta em intervalos de **1 hora**
- Apos **8 tentativas** sem sucesso, a notificacao e descartada como "lost"
- Falhas repetidas causam **desativacao automatica** do topico (requer reativacao manual)

### 9.6 Notificacoes Perdidas

```http
GET https://api.mercadolibre.com/missed_feeds?app_id={APP_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

Suporta filtro por topico e paginacao (limit/offset).

### 9.7 IPs de Origem (para Firewall)

```
54.88.218.97
18.215.140.160
18.213.114.129
18.206.34.84
```

---

## 10. Metricas, Relatorios e Reputacao

### 10.1 Reputacao do Vendedor

```http
GET https://api.mercadolibre.com/users/{USER_ID}
Authorization: Bearer {ACCESS_TOKEN}
```

**Campos relevantes no objeto `seller_reputation`:**

```json
{
  "seller_reputation": {
    "level_id": "5_green",
    "power_seller_status": "gold",
    "real_level": "5_green",
    "protection_end_date": "2026-06-01T00:00:00.000-03:00",
    "transactions": {
      "canceled": 2,
      "completed": 350,
      "period": "historic",
      "ratings": {
        "negative": 0.01,
        "neutral": 0.02,
        "positive": 0.97
      },
      "total": 360
    },
    "metrics": {
      "sales": {
        "period": "60 days",
        "completed": 120
      },
      "claims": {
        "rate": 0.02,
        "value": 3
      },
      "delayed_handling_time": {
        "rate": 0.01,
        "value": 1
      },
      "cancellations": {
        "rate": 0.01,
        "value": 1
      }
    }
  }
}
```

**Niveis de reputacao:**
- `1_red` - Vermelho (pior)
- `2_orange` - Laranja
- `3_yellow` - Amarelo
- `4_light_green` - Verde claro
- `5_green` - Verde escuro (melhor)

**Status Mercado Lider:**
- `null` - Sem status
- `silver` - Mercado Lider
- `gold` - Mercado Lider Gold
- `platinum` - Mercado Lider Platinum

### 10.2 Relatorios de Faturamento

**Listar periodos de faturamento:**
```http
GET https://api.mercadolibre.com/billing/integration/monthly/periods
    ?group=ML
    &document_type=BILL
    &limit=12
Authorization: Bearer {ACCESS_TOKEN}
```

Grupos: `ML` (Mercado Livre) ou `MP` (Mercado Pago)
Tipos: `BILL` (fatura) ou `CREDIT_NOTE` (nota de credito)

**Documentos de um periodo:**
```http
GET https://api.mercadolibre.com/billing/integration/periods/key/{KEY}/documents
    ?document_type=BILL
    &limit=100
Authorization: Bearer {ACCESS_TOKEN}
```

**Resumo do periodo (totais):**
```http
GET https://api.mercadolibre.com/billing/integration/periods/key/{KEY}/summary/details
Authorization: Bearer {ACCESS_TOKEN}
```

**Dados retornados incluem:**
- Comissoes de venda
- Custos de publicacao
- Percepcoes tributarias
- Cobracas de servicos
- Campanhas de publicidade
- Reembolsos de comissoes/frete
- Creditos de publicidade
- Devolucoes de percepcoes tributarias
- Total de pagamentos, notas de credito, debitos

### 10.3 Conversao de Moedas

```http
GET https://api.mercadolibre.com/currency_conversions/search?from=USD&to=BRL
```

---

## 11. Rate Limits e Restricoes

### 11.1 Limites Gerais

| Recurso | Limite |
|---------|--------|
| **Requisicoes por hora (por app)** | 18.000 |
| **Media por minuto** | ~300 |
| **Mensagens pos-venda (GET)** | 500 rpm |
| **Mensagens pos-venda (POST/PUT)** | 500 rpm |

### 11.2 Erro de Rate Limit

Quando excedido, a API retorna:
```
HTTP 429 - Too Many Requests
```

**Header de autenticacao com rate limit:**
```json
{
  "error": "local_rate_limited",
  "message": "caller exceeds rate limit",
  "status": 429
}
```

### 11.3 Restricoes Importantes

- **Multiget**: Maximo 20 IDs por chamada (`/items?ids=...`)
- **Paginacao**: `limit` maximo 100, `offset` maximo 1000
- **Scan mode**: Necessario para >1000 resultados (`search_type=scan`)
- **scroll_id**: Expira em 5 minutos
- **Descricao**: Somente plain text, sem HTML
- **Imagens**: Maximo 10 MB, min 500x500px, formatos JPG/PNG
- **Variacoes**: Maximo 100 (250 em categorias especificas)
- **Mensagens do vendedor**: Maximo 350 caracteres
- **Webhooks**: Responder HTTP 200 em 500ms; 8 retries antes de descarte
- **Billing reports**: Maximo 12 periodos por consulta, 1000 documentos por pagina
- **Historico de estoque (Full)**: Ultimos 12 meses apenas

### 11.4 Boas Praticas

1. **Implemente cache local** para dados que mudam pouco (categorias, atributos)
2. **Use webhooks** em vez de polling para atualizacoes em tempo real
3. **Processe notificacoes de forma assincrona** (confirme HTTP 200 imediatamente)
4. **Use multiget** quando precisar de multiplos itens (1 chamada vs N chamadas)
5. **Selecione campos especificos** com `attributes=` para reduzir payload
6. **Implemente retry com backoff exponencial** para erros 429
7. **Use scan mode** para listar grande volume de itens

---

## 12. SDKs e Bibliotecas

### 12.1 SDKs Oficiais (DESCONTINUADOS)

Os SDKs oficiais do Mercado Livre foram **descontinuados em abril de 2021** e nao recebem mais manutencao. Os repositorios estao arquivados no GitHub.

| SDK | Repositorio | Status |
|-----|-------------|--------|
| Python | github.com/mercadolibre/python-sdk | Arquivado (2021) |
| Node.js | github.com/mercadolibre/nodejs-sdk | Arquivado (2021) |
| Java | github.com/mercadolibre/java-sdk | Arquivado |
| .NET | github.com/mercadolibre/net-sdk | Arquivado |
| Ruby | github.com/mercadolibre/ruby-sdk | Arquivado |
| PHP | github.com/mercadolibre/php-sdk | Arquivado |

**Recomendacao oficial**: Integrar diretamente via HTTP usando a documentacao da API REST.

### 12.2 Abordagem Recomendada (2025-2026)

Como os SDKs oficiais estao descontinuados, a melhor abordagem e criar um **cliente HTTP proprio** usando:

**Python:**
```python
import requests

class MercadoLivreAPI:
    BASE_URL = "https://api.mercadolibre.com"

    def __init__(self, access_token):
        self.session = requests.Session()
        self.session.headers.update({
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json",
            "Accept": "application/json"
        })

    def get_item(self, item_id):
        return self.session.get(f"{self.BASE_URL}/items/{item_id}").json()

    def update_stock(self, item_id, quantity):
        return self.session.put(
            f"{self.BASE_URL}/items/{item_id}",
            json={"available_quantity": quantity}
        ).json()

    def search_orders(self, seller_id, status="paid"):
        return self.session.get(
            f"{self.BASE_URL}/orders/search",
            params={"seller": seller_id, "order.status": status}
        ).json()

    def answer_question(self, question_id, text):
        return self.session.post(
            f"{self.BASE_URL}/answers",
            json={"question_id": question_id, "text": text}
        ).json()

    def refresh_token(self, client_id, client_secret, refresh_token):
        resp = requests.post(f"{self.BASE_URL}/oauth/token", data={
            "grant_type": "refresh_token",
            "client_id": client_id,
            "client_secret": client_secret,
            "refresh_token": refresh_token
        })
        return resp.json()
```

**Node.js / TypeScript:**
```typescript
import axios, { AxiosInstance } from 'axios';

class MercadoLivreAPI {
  private client: AxiosInstance;
  private static BASE_URL = 'https://api.mercadolibre.com';

  constructor(accessToken: string) {
    this.client = axios.create({
      baseURL: MercadoLivreAPI.BASE_URL,
      headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
      }
    });
  }

  async getItem(itemId: string) {
    return (await this.client.get(`/items/${itemId}`)).data;
  }

  async updateStock(itemId: string, quantity: number) {
    return (await this.client.put(`/items/${itemId}`, {
      available_quantity: quantity
    })).data;
  }

  async searchOrders(sellerId: string, status = 'paid') {
    return (await this.client.get('/orders/search', {
      params: { seller: sellerId, 'order.status': status }
    })).data;
  }

  async answerQuestion(questionId: number, text: string) {
    return (await this.client.post('/answers', {
      question_id: questionId, text
    })).data;
  }

  async refreshToken(clientId: string, clientSecret: string, refreshToken: string) {
    return (await axios.post(`${MercadoLivreAPI.BASE_URL}/oauth/token`, null, {
      params: {
        grant_type: 'refresh_token',
        client_id: clientId,
        client_secret: clientSecret,
        refresh_token: refreshToken
      }
    })).data;
  }
}
```

### 12.3 Bibliotecas da Comunidade (Nao-Oficiais)

Existem bibliotecas mantidas pela comunidade no GitHub (buscar por `mercado-livre` ou `mercadolibre` nos topics). Avalie a atividade e manutencao antes de adotar.

---

## Resumo de Endpoints Essenciais para Sistema de Gestao

| Funcionalidade | Metodo | Endpoint |
|---------------|--------|----------|
| **Autenticacao** | POST | `/oauth/token` |
| **Criar anuncio** | POST | `/items` |
| **Atualizar anuncio** | PUT | `/items/{ITEM_ID}` |
| **Consultar anuncio** | GET | `/items/{ITEM_ID}` |
| **Consultar multiplos** | GET | `/items?ids=ID1,ID2` |
| **Listar meus anuncios** | GET | `/users/{USER_ID}/items/search` |
| **Descricao** | POST/PUT | `/items/{ITEM_ID}/description` |
| **Upload foto** | POST | `/pictures/items/upload` |
| **Variacoes** | GET | `/items/{ITEM_ID}/variations` |
| **Buscar pedidos** | GET | `/orders/search?seller={ID}` |
| **Detalhe do pedido** | GET | `/orders/{ORDER_ID}` |
| **Pack (carrinho)** | GET | `/packs/{PACK_ID}` |
| **Perguntas recebidas** | GET | `/my/received_questions/search` |
| **Responder pergunta** | POST | `/answers` |
| **Mensagens pos-venda** | GET/POST | `/messages/packs/{PACK_ID}/sellers/{ID}` |
| **Detalhe do envio** | GET | `/shipments/{SHIPMENT_ID}` |
| **Rastreamento** | GET | `/shipments/{SHIPMENT_ID}/history` |
| **Etiqueta** | GET | `/shipment_labels?shipment_ids={ID}` |
| **Estoque Full** | GET | `/inventories/{INV_ID}/stock/fulfillment` |
| **Reclamacoes** | GET | `/post-purchase/v1/claims/search` |
| **Devolucoes** | GET | `/post-purchase/v2/claims/{ID}/returns` |
| **Reputacao** | GET | `/users/{USER_ID}` |
| **Faturamento** | GET | `/billing/integration/monthly/periods` |
| **Notificacoes perdidas** | GET | `/missed_feeds?app_id={APP_ID}` |
| **Categorias** | GET | `/sites/MLB/categories` |
| **Atributos** | GET | `/categories/{CAT_ID}/attributes` |

---

## Fontes

- [Autenticacao e Autorizacao](https://developers.mercadolivre.com.br/pt_br/autenticacao-e-autorizacao)
- [Items & Searches](https://developers.mercadolivre.com.br/en_us/items-and-searches)
- [Sync and Modify Listings](https://developers.mercadolivre.com.br/en_us/products-sync-listings)
- [Variations](https://developers.mercadolivre.com.br/en_us/variations)
- [Pictures](https://developers.mercadolivre.com.br/en_us/working-with-pictures)
- [Item Description](https://developers.mercadolivre.com.br/en_us/item-description-2)
- [Prices](https://developers.mercadolivre.com.br/en_us/price-apl)
- [Catalog Listing](https://developers.mercadolivre.com.br/en_us/catalog-listing)
- [Orders](https://developers.mercadolivre.com.br/pt_br/gerenciamento-de-vendas)
- [Pack Management](https://developers.mercadolivre.com.br/pt_br/gestao-packs)
- [Questions & Answers](https://developers.mercadolivre.com.br/en_us/questions)
- [Messaging After Sale](https://developers.mercadolivre.com.br/en_us/messaging-after-sale)
- [Shipment Handling](https://developers.mercadolivre.com.br/en_us/shipment-handling)
- [Fulfillment](https://developers.mercadolivre.com.br/en_us/fulfillment)
- [Notifications](https://developers.mercadolivre.com.br/en_us/products-receive-notifications)
- [Claims](https://developers.mercadolivre.com.br/pt_br/gerenciar-reclamacoes)
- [Returns](https://developers.mercadolivre.com.br/pt_br/gerenciar-devolucoes)
- [Seller Reputation](https://developers.mercadolivre.com.br/en_us/sellers-reputation)
- [Billing Reports](https://developers.mercadolivre.com.br/en_us/billing-reports)
- [Application Manager](https://developers.mercadolivre.com.br/en_us/manage-your-applications)
- [Register Application](https://developers.mercadolivre.com.br/en_us/register-your-application)
- [Python SDK (archived)](https://github.com/mercadolibre/python-sdk)
- [Node.js SDK (archived)](https://github.com/mercadolibre/nodejs-sdk)
