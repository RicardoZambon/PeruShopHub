# PeruShop Hub - Design & Pre-Projeto

---

## 1. Identidade Visual

### Nome
- **PeruShop Hub** - Sistema de gestao multi-marketplace
- **Peru Shop** - Nome da loja/marca
- Homenagem ao **Pepperoni**, Shih Tzu do fundador

### Sugestao de Identidade

| Elemento | Sugestao |
|----------|---------|
| Logotipo | Silhueta estilizada de um Shih Tzu + icone de hub/conexao |
| Paleta primaria | Azul escuro (#1A237E) + Laranja (#FF6F00) |
| Paleta secundaria | Cinza claro (#F5F5F5), Branco (#FFFFFF), Verde sucesso (#2E7D32), Vermelho alerta (#C62828) |
| Tipografia | Inter (UI) + Roboto (corpo) |
| Tom | Profissional mas amigavel, limpo, data-driven |

> **Nota**: A identidade visual sera refinada na etapa de Design (UI/UX). As cores e fontes acima sao ponto de partida.

---

## 2. Definicao da Estrutura de Dados

Baseado nas capacidades da API do Mercado Livre, mapeando todas as entidades e campos relevantes.

### 2.1 Categorias e Subcategorias

**Fonte API**: `GET /sites/MLB/categories` + `GET /categories/{id}`

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| id | VARCHAR(20) | ID da categoria (ex: MLB1744) | Sim |
| name | VARCHAR(100) | Nome da categoria | Sim |
| parent_id | VARCHAR(20) | Categoria pai (null se raiz) | Sim |
| path_from_root | JSON | Caminho completo da raiz | Sim |
| picture | VARCHAR(500) | URL da imagem | Nao |
| permalink | VARCHAR(500) | URL no ML | Nao |
| total_items | INT | Itens nesta categoria | Nao |
| attributable | BOOLEAN | Aceita atributos | Sim |

**Configuracoes por categoria** (settings):
- Modos de compra permitidos
- Condicoes aceitas (novo, usado)
- Max fotos, max titulo, max descricao
- Modos de envio aceitos
- Pagamento imediato (obrigatorio ou opcional)

**Atributos por categoria** (importante para listagem correta):
- Atributos obrigatorios (BRAND, MODEL, etc.)
- Atributos de variacao (COLOR, SIZE)
- Tipos: string, number, number_unit, boolean, list
- Hierarquia e relevancia

### 2.2 Produtos

**Dados internos do PeruShop Hub** (nao vem da API):

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| internal_id | UUID | ID interno | Sim |
| sku | VARCHAR(100) | SKU unico do vendedor | Sim |
| name | VARCHAR(200) | Nome interno do produto | Sim |
| purchase_cost | DECIMAL(18,4) | Custo de aquisicao atual | Sim |
| packaging_cost | DECIMAL(18,4) | Custo de embalagem | Sim |
| supplier | VARCHAR(200) | Fornecedor | Sim |
| supplier_url | VARCHAR(500) | Link do fornecedor | Nao |
| notes | TEXT | Observacoes internas | Nao |
| tags | TEXT[] | Tags internas | Nao |
| is_active | BOOLEAN | Produto ativo | Sim |
| created_at | TIMESTAMPTZ | Data de criacao | Sim |
| updated_at | TIMESTAMPTZ | Data de atualizacao | Sim |

**Dados sincronizados com Mercado Livre** (via API):

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| ml_item_id | VARCHAR(20) | ID no ML (MLB...) | Sim |
| title | VARCHAR(60) | Titulo do anuncio | Sim |
| subtitle | VARCHAR(70) | Subtitulo | Nao |
| description | TEXT | Descricao (texto puro) | Sim |
| category_id | VARCHAR(20) | Categoria no ML | Sim |
| price | DECIMAL(15,2) | Preco de venda | Sim |
| original_price | DECIMAL(15,2) | Preco original (riscado) | Nao |
| currency_id | VARCHAR(5) | Moeda (BRL) | Sim |
| condition | VARCHAR(10) | Condicao (new, used) | Sim |
| available_quantity | INT | Qtd disponivel | Sim |
| sold_quantity | INT | Qtd vendida | Sim |
| listing_type_id | VARCHAR(20) | Tipo anuncio (free, gold_special) | Sim |
| status | VARCHAR(20) | Status (active, paused, closed) | Sim |
| permalink | VARCHAR(500) | URL do anuncio | Sim |
| health | DECIMAL(3,2) | Saude do anuncio (0-1) | Nao |
| catalog_product_id | VARCHAR(30) | ID no catalogo ML | Nao |
| catalog_listing | BOOLEAN | Vinculado ao catalogo | Nao |
| inventory_id | VARCHAR(20) | ID inventario (Full) | Sim |
| seller_custom_field | VARCHAR(100) | Campo SKU no ML | Sim |
| warranty | VARCHAR(200) | Garantia | Sim |
| video_id | VARCHAR(50) | Video do produto | Nao |
| buying_mode | VARCHAR(20) | buy_it_now / auction | Sim |
| channels | TEXT[] | Canais (marketplace, mshops) | Nao |
| tags | TEXT[] | Tags ML | Nao |
| date_created | TIMESTAMPTZ | Data criacao no ML | Sim |
| last_updated | TIMESTAMPTZ | Ultima atualizacao | Sim |

**Fotos (Galeria)**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| id | VARCHAR(100) | ID da imagem no ML | Sim |
| url | VARCHAR(500) | URL | Sim |
| secure_url | VARCHAR(500) | URL HTTPS | Sim |
| size | VARCHAR(20) | Tamanho (500x500) | Nao |
| max_size | VARCHAR(20) | Tamanho maximo | Nao |
| sort_order | INT | Ordem de exibicao | Sim |

**Atributos do produto** (BRAND, MODEL, COLOR, GTIN, etc.):

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| attribute_id | VARCHAR(50) | ID (BRAND, MODEL) | Sim |
| name | VARCHAR(100) | Nome legivel | Sim |
| value_id | VARCHAR(30) | ID do valor | Sim |
| value_name | VARCHAR(255) | Texto do valor | Sim |
| attribute_group_id | VARCHAR(30) | Grupo | Nao |

**Variacoes** (cor, tamanho):

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| variation_id | BIGINT | ID da variacao | Sim |
| price | DECIMAL(15,2) | Preco da variacao | Sim |
| available_quantity | INT | Qtd disponivel | Sim |
| sold_quantity | INT | Qtd vendida | Sim |
| picture_ids | TEXT[] | Fotos associadas | Sim |
| attributes (cor, tamanho, SKU) | JSON | Atributos da variacao | Sim |

**Termos de venda**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| warranty_type | VARCHAR(50) | Tipo garantia | Sim |
| warranty_time | VARCHAR(50) | Tempo garantia | Sim |
| manufacturing_time | VARCHAR(50) | Tempo fabricacao | Nao |
| purchase_max_quantity | INT | Max por compra | Nao |

**Configuracao de envio do produto**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| mode | VARCHAR(20) | Modo envio (me2, custom) | Sim |
| free_shipping | BOOLEAN | Frete gratis | Sim |
| logistic_type | VARCHAR(30) | fulfillment, cross_docking, drop_off | Sim |
| dimensions | VARCHAR(50) | Dimensoes | Sim |
| local_pick_up | BOOLEAN | Retirada local | Nao |

### 2.3 Vendas (Pedidos)

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| order_id | BIGINT | ID do pedido ML | Sim |
| status | VARCHAR(30) | confirmed, paid, cancelled | Sim |
| status_detail | VARCHAR(100) | Detalhe do status | Sim |
| date_created | TIMESTAMPTZ | Data do pedido | Sim |
| date_closed | TIMESTAMPTZ | Data confirmacao | Sim |
| total_amount | DECIMAL(15,2) | Valor total | Sim |
| paid_amount | DECIMAL(15,2) | Valor pago | Sim |
| currency_id | VARCHAR(5) | Moeda | Sim |
| shipping_id | BIGINT | ID do envio | Sim |
| pack_id | BIGINT | ID do carrinho | Sim |
| coupon_amount | DECIMAL(15,2) | Cupom aplicado | Sim |
| taxes_amount | DECIMAL(15,2) | Impostos | Sim |
| tags | TEXT[] | Tags do pedido | Sim |
| context_channel | VARCHAR(30) | Canal (marketplace, mshops) | Nao |
| fulfilled | BOOLEAN | Pedido cumprido | Sim |
| cancel_detail | JSON | Detalhe cancelamento | Nao |

**Itens do pedido**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| item_id | VARCHAR(20) | ID do anuncio | Sim |
| title | VARCHAR(200) | Titulo no momento da compra | Sim |
| category_id | VARCHAR(20) | Categoria | Sim |
| variation_id | BIGINT | Variacao comprada | Sim |
| seller_sku | VARCHAR(100) | SKU do vendedor | Sim |
| quantity | INT | Quantidade | Sim |
| unit_price | DECIMAL(15,2) | Preco unitario | Sim |
| full_unit_price | DECIMAL(15,2) | Preco cheio (sem desconto) | Sim |
| condition | VARCHAR(10) | new, used | Sim |

**Pagamentos**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| payment_id | BIGINT | ID do pagamento | Sim |
| status | VARCHAR(20) | approved, pending, refunded | Sim |
| payment_type | VARCHAR(30) | credit_card, pix, boleto | Sim |
| payment_method_id | VARCHAR(30) | visa, master, pix | Sim |
| transaction_amount | DECIMAL(15,2) | Valor da transacao | Sim |
| total_paid_amount | DECIMAL(15,2) | Total pago (com juros) | Sim |
| installments | INT | Numero de parcelas | Sim |
| date_approved | TIMESTAMPTZ | Data aprovacao | Sim |
| marketplace_fee | DECIMAL(15,2) | Taxa do marketplace | Sim |
| shipping_cost | DECIMAL(15,2) | Custo frete no pagamento | Sim |

**Envio**:

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| shipment_id | BIGINT | ID do envio | Sim |
| status | VARCHAR(30) | pending, shipped, delivered | Sim |
| substatus | VARCHAR(50) | Sub-status | Sim |
| tracking_number | VARCHAR(50) | Rastreamento | Sim |
| logistic_type | VARCHAR(30) | fulfillment, drop_off | Sim |
| carrier_name | VARCHAR(100) | Transportadora | Sim |
| receiver_address | JSON | Endereco completo | Sim |
| dimensions | JSON | Peso, altura, largura, comprimento | Sim |
| date_shipped | TIMESTAMPTZ | Data envio | Sim |
| date_delivered | TIMESTAMPTZ | Data entrega | Sim |
| sender_cost | DECIMAL(15,2) | Custo para vendedor | Sim |
| receiver_cost | DECIMAL(15,2) | Custo para comprador | Sim |

**Custos detalhados por venda** (diferencial do sistema):

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| sale_id | UUID | Ref a venda | Sim |
| cost_category | VARCHAR(50) | Tipo de custo (ver abaixo) | Sim |
| amount | DECIMAL(18,4) | Valor | Sim |
| percentage_rate | DECIMAL(8,4) | Percentual (se aplicavel) | Sim |
| source | VARCHAR(50) | api, manual, calculated | Sim |

Categorias de custo:
- `marketplace_commission` - Comissao ML
- `fixed_fee` - Taxa fixa
- `shipping_seller` - Frete pago pelo vendedor
- `payment_fee` - Taxa de pagamento
- `fulfillment_fee` - Taxa de fulfillment
- `storage_daily` - Armazenagem diaria
- `storage_prolonged` - Armazenagem prolongada
- `product_cost` - Custo do produto
- `packaging` - Embalagem
- `advertising` - Custo de ads atribuido
- `tax` - Impostos
- `other` - Outros

### 2.4 Perguntas

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| question_id | BIGINT | ID da pergunta | Sim |
| item_id | VARCHAR(20) | Anuncio | Sim |
| from_id | BIGINT | Quem perguntou | Sim |
| from_name | VARCHAR(200) | Nome (api_version=4) | Nao |
| text | TEXT | Pergunta | Sim |
| status | VARCHAR(30) | UNANSWERED, ANSWERED, etc | Sim |
| date_created | TIMESTAMPTZ | Data | Sim |
| answer_text | TEXT | Resposta | Sim |
| answer_date | TIMESTAMPTZ | Data da resposta | Sim |
| answer_status | VARCHAR(20) | ACTIVE, BANNED | Sim |
| deleted_from_listing | BOOLEAN | Removida | Nao |
| hold | BOOLEAN | Em espera | Nao |

### 2.5 Clientes (Compradores)

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| user_id | BIGINT | ID no ML | Sim |
| nickname | VARCHAR(100) | Apelido | Sim |
| first_name | VARCHAR(100) | Nome | Sim |
| last_name | VARCHAR(100) | Sobrenome | Sim |
| email | VARCHAR(200) | Email (mascarado apos 30d) | Sim |
| phone | VARCHAR(30) | Telefone | Sim |
| doc_type | VARCHAR(10) | CPF/CNPJ | Nao |
| doc_number | VARCHAR(20) | Numero doc | Nao |
| registration_date | TIMESTAMPTZ | Cadastro no ML | Nao |
| country_id | VARCHAR(5) | Pais | Nao |
| address | JSON | Endereco | Nao |
| total_orders | INT | Total de pedidos (calculado) | Sim |
| total_spent | DECIMAL(15,2) | Total gasto (calculado) | Sim |
| last_order_date | TIMESTAMPTZ | Ultima compra (calculado) | Sim |

### 2.6 Usuarios do Sistema (PeruShop Hub)

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| id | UUID | ID interno | Sim |
| email | VARCHAR(200) | Email de login | Sim |
| password_hash | VARCHAR(500) | Senha (hash bcrypt) | Sim |
| name | VARCHAR(200) | Nome completo | Sim |
| role | VARCHAR(20) | admin, manager, viewer | Sim |
| is_active | BOOLEAN | Ativo | Sim |
| last_login | TIMESTAMPTZ | Ultimo acesso | Sim |
| created_at | TIMESTAMPTZ | Data criacao | Sim |

### 2.7 Controle de Acessos

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| role | VARCHAR(20) | Papel | Sim |
| permission | VARCHAR(50) | Permissao | Sim |

**Permissoes planejadas**:

| Permissao | Admin | Manager | Viewer |
|-----------|-------|---------|--------|
| Dashboard (visualizar) | Sim | Sim | Sim |
| Produtos (CRUD) | Sim | Sim | Nao |
| Pedidos (visualizar) | Sim | Sim | Sim |
| Perguntas (responder) | Sim | Sim | Nao |
| Financeiro (visualizar) | Sim | Sim | Nao |
| Financeiro (exportar) | Sim | Sim | Nao |
| Configuracoes | Sim | Nao | Nao |
| Usuarios (CRUD) | Sim | Nao | Nao |
| Integracoes (marketplace) | Sim | Nao | Nao |

### 2.8 Dashboard e Relatorios

**Cards principais**:
- Vendas hoje / semana / mes (quantidade e valor)
- Receita bruta vs lucro liquido
- Margem media
- Ticket medio
- Perguntas sem resposta
- Pedidos pendentes de envio

**Graficos**:
- Vendas ao longo do tempo (linha)
- Receita vs lucro (barras empilhadas)
- Top 10 produtos mais vendidos (barras horizontal)
- Top 10 mais lucrativos (barras horizontal)
- Distribuicao de custos (pizza/donut)
- Curva ABC por margem

**Filtros globais**:
- Periodo (hoje, 7d, 30d, custom)
- Marketplace (MVP: apenas ML)
- Categoria
- Produto especifico

### 2.9 Plataformas (Marketplace Connections)

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| id | UUID | ID interno | Sim |
| marketplace_id | VARCHAR(50) | mercadolivre, amazon, shopee | Sim |
| marketplace_name | VARCHAR(100) | Nome display | Sim |
| seller_id | VARCHAR(200) | ID do vendedor na plataforma | Sim |
| seller_nickname | VARCHAR(100) | Apelido/nome na plataforma | Sim |
| access_token | TEXT | Token (criptografado) | Sim |
| refresh_token | TEXT | Refresh (criptografado) | Sim |
| token_expires_at | TIMESTAMPTZ | Expiracao | Sim |
| scopes | TEXT[] | Escopos autorizados | Sim |
| is_active | BOOLEAN | Conexao ativa | Sim |
| last_sync_at | TIMESTAMPTZ | Ultima sincronizacao | Sim |
| config | JSON | Configuracoes por plataforma | Nao |

**MVP**: Apenas Mercado Livre. Estrutura pronta para adicionar outros.

### 2.10 Outros: Reclamacoes e Devolucoes

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| claim_id | BIGINT | ID da reclamacao | Nao |
| order_id | BIGINT | Pedido relacionado | Nao |
| status | VARCHAR(20) | opened, closed | Nao |
| type | VARCHAR(30) | claim, dispute | Nao |
| reason_id | VARCHAR(50) | Motivo | Nao |
| resolution | VARCHAR(50) | Resolucao | Nao |
| affects_reputation | BOOLEAN | Afeta reputacao | Nao |

> Delegado para pos-MVP. Estrutura prevista mas nao implementada na primeira entrega.

### 2.11 Outros: Promocoes e Advertising

| Campo | Tipo | Descricao | MVP |
|-------|------|-----------|-----|
| Promocoes (PRICE_DISCOUNT, LIGHTNING) | - | Criar/deletar descontos | Nao |
| Campanhas Ads (budget, clicks, ACOS) | - | Metricas de publicidade | Nao |

> Delegado para pos-MVP.

---

## 3. Telas do Sistema (Design / UI / UX)

### 3.1 Mapa de Navegacao

```
PeruShop Hub
├── Login
├── Dashboard (Home)
├── Produtos
│   ├── Listagem (tabela com filtros, busca, status)
│   ├── Cadastro / Edicao
│   │   ├── Informacoes basicas (titulo, descricao, categoria)
│   │   ├── Preco e custo
│   │   ├── Fotos (galeria com drag-and-drop)
│   │   ├── Atributos (dinamicos por categoria)
│   │   ├── Variacoes (cor, tamanho)
│   │   ├── Envio (modo, dimensoes, frete gratis)
│   │   └── Termos de venda (garantia)
│   └── Detalhe do produto (metricas, historico, variacoes)
├── Vendas
│   ├── Listagem de pedidos (filtros por status, data, valor)
│   ├── Detalhe do pedido
│   │   ├── Itens comprados
│   │   ├── Dados do comprador
│   │   ├── Pagamento
│   │   ├── Envio e rastreamento
│   │   └── Decomposicao de custos / lucro ★
│   └── Timeline do pedido (status changes)
├── Perguntas
│   ├── Listagem (nao respondidas primeiro)
│   ├── Responder (inline ou modal)
│   └── Filtros (por produto, status, data)
├── Clientes
│   ├── Listagem (compradores unicos)
│   └── Perfil do cliente (historico de compras, total gasto)
├── Financeiro ★
│   ├── Resumo (receita, custos, lucro por periodo)
│   ├── Lucratividade por SKU
│   ├── Conciliacao (depositado vs esperado)
│   ├── Curva ABC
│   └── Exportar (PDF / Excel)
├── Estoque
│   ├── Visao geral (qtd por produto, status sync)
│   ├── Movimentacoes (entradas, saidas, ajustes)
│   └── Estoque Full (pos-MVP)
├── Configuracoes
│   ├── Perfil da empresa
│   ├── Usuarios e permissoes
│   ├── Integracoes (conectar/desconectar marketplace)
│   ├── Custos fixos (embalagem padrao, impostos)
│   └── Alertas
└── Notificacoes (sino no header, painel lateral)
```

### 3.2 Principios de UI/UX

- **Layout**: Sidebar fixa + header com busca global + area de conteudo
- **Tema**: Light mode padrao, dark mode opcional
- **Tabelas**: Paginacao server-side, colunas configuraveis, exportacao
- **Responsivo**: Desktop-first, mas funcional em tablet
- **Feedback**: Loading states, toasts para acoes, confirmacao para acoes destrutivas
- **Real-time**: Badge de notificacao no header, atualizacao automatica de dados

---

## 4. Fases Revisadas (Alinhamento)

### Design & Pre-Projeto (Semana 0-1) ← ESTAMOS AQUI
- [x] Identidade visual (definicao inicial)
- [x] Definicao da estrutura de dados (baseada na API ML)
- [x] Mapa de navegacao e telas
- [ ] Wireframes das telas principais (Dashboard, Produtos, Vendas, Perguntas)
- [ ] Definicao de componentes reutilizaveis

### MVP - Fase 1: Backend + Layout (Semanas 2-4)
**PRD UI/UX**:
- Wireframes de todas as telas
- Design system (cores, tipografia, componentes)
- Prototipo navegavel

**Backend**:
- Estrutura do projeto (.NET 8 + EF Core + PostgreSQL)
- Docker Compose (API, Worker, DB, Redis, Nginx)
- Modelo de dados (migrations)
- Autenticacao do sistema (JWT)
- CRUD de produtos (interno)
- CRUD de pedidos (interno)
- CRUD de perguntas (interno)
- CRUD de clientes (interno)
- CRUD de usuarios e permissoes
- Endpoints de dashboard (metricas)
- Swagger/OpenAPI

**Frontend**:
- Projeto Angular + Angular Material/PrimeNG
- Layout base (sidebar, header, roteamento)
- Auth guard + tela de login
- Todas as telas com dados mockados
- Tabelas com paginacao, filtros, busca
- Dashboard com graficos
- Formularios de cadastro/edicao

### MVP - Fase 2: Integracao com Mercado Livre (Semanas 5-7)
- OAuth 2.0 (autorizar, token, refresh)
- Worker de renovacao de tokens
- Sync de produtos existentes do ML
- Webhooks (pedidos, perguntas, items)
- Processamento assincrono de webhooks
- Consulta de taxas por pedido (Billing API)
- Calculo de lucro por venda
- Sync de envios e rastreamento
- Atualizacao de estoque bidirecional
- Tela de conexao com marketplace
- Dados reais substituem mocks

### MVP - Fase 3: Exportacao e IA (Semanas 8-9)
- Exportar dados de produtos (Excel/CSV)
- Exportar perguntas (Excel/CSV)
- Exportar relatorio financeiro (PDF/Excel)
- **IA para perguntas**:
  - Exportar perguntas nao respondidas em lote
  - Enviar para LLM (Claude API) com contexto do produto
  - Gerar sugestoes de resposta
  - Revisar e aprovar respostas em lote
  - Enviar respostas aprovadas via API ML

---

## 5. O que fica para DEPOIS do MVP

| Funcionalidade | Fase | Prioridade |
|----------------|------|-----------|
| Reclamacoes e devolucoes | Pos-MVP 1 | Alta |
| Mensagens pos-venda | Pos-MVP 1 | Alta |
| Estoque Full (armazenagem, custos) | Pos-MVP 1 | Alta |
| Conciliacao financeira detalhada | Pos-MVP 1 | Alta |
| Alertas configuraveis | Pos-MVP 2 | Media |
| Calculadora de preco / simulador | Pos-MVP 2 | Media |
| Promocoes e descontos via API | Pos-MVP 2 | Media |
| Mercado Ads (campanhas, ROI) | Pos-MVP 2 | Media |
| Integracao Amazon | Pos-MVP 3 | Media |
| Integracao Shopee | Pos-MVP 3 | Media |
| Sync multi-marketplace | Pos-MVP 3 | Alta |
| Precificacao dinamica automatica | Pos-MVP 4 | Baixa |
| IA para respostas em tempo real | Pos-MVP 4 | Baixa |
| Previsao de demanda | Pos-MVP 4 | Baixa |
| App mobile | Pos-MVP 4 | Baixa |

---

## 6. Stack Tecnico Confirmado

| Camada | Tecnologia |
|--------|-----------|
| Backend | C# / ASP.NET Core 8+ Web API |
| ORM | Entity Framework Core 8 |
| Banco | PostgreSQL 16 |
| Cache/Fila | Redis 7+ |
| Background Jobs | .NET BackgroundService + Hangfire |
| Real-time | SignalR |
| Frontend | Angular 17+ |
| UI Components | Angular Material ou PrimeNG (a definir) |
| Graficos | ngx-charts ou Chart.js (via ng2-charts) |
| Tabelas | Angular Material Table ou PrimeNG Table |
| HTTP Client | HttpClientFactory + Polly (retry, circuit breaker) |
| Autenticacao | JWT + Refresh Token |
| Exportacao | QuestPDF (PDF) + ClosedXML (Excel) |
| IA (perguntas) | Claude API (Anthropic SDK) |
| Containerizacao | Docker + Docker Compose |
| Reverse Proxy | Nginx |
| CI/CD | GitHub Actions |

---

## 7. Proximos Passos

1. **Voce valida** este documento de pre-projeto
2. Eu crio os **wireframes** das telas principais
3. Eu inicio a **Fase 1** (backend + frontend base)
4. Entrega incremental sprint a sprint para validacao
