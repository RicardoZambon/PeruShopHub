# PeruShopHub — Roadmap & Progress

> Ultima atualizacao: 2026-03-27

## Visao Geral

**PeruShopHub** e um sistema centralizado de gestao multi-marketplace focado em **rastreabilidade real de lucratividade por venda**. Nenhum ERP/hub existente calcula o lucro liquido real considerando todos os custos (comissao, taxas fixas, frete real, fulfillment, advertising, impostos, custo do produto, embalagem, cupons absorvidos).

**Stack**: .NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL 16 / Redis 7 / SignalR / Angular 17+ / Chart.js

---

## Status Atual

**Branch ativa**: `ralph/backend-wiring`
**Fase atual**: Fase 0 concluida, Fase 1 parcialmente concluida

O sistema possui uma base funcional completa com backend, frontend e infraestrutura Docker. O proximo grande passo e a integracao real com o Mercado Livre.

---

## Fase 0 — Fundacao ✅ CONCLUIDA

**Objetivo**: Projeto estruturado com backend funcional conectado ao frontend.

### 0.1 Design System & Frontend (PR #1, merged to main)

- [x] Design system completo com CSS custom properties (temas light + dark)
- [x] Fontes: Inter (UI) + Roboto Mono (dados financeiros)
- [x] Layout responsivo: sidebar colapsavel (256px/64px), header fixo (56px), drawer mobile
- [x] 16 paginas completas (dashboard, produtos, categorias, vendas, clientes, suprimentos, financeiro, compras, estoque, configuracoes, admin, login, registro)
- [x] Componentes compartilhados: DataGrid, KpiCard, Badge, Skeleton, EmptyState, Toast, SearchPalette (Ctrl+K), Dialog, ConfirmDialog, FormField, MediaGallery
- [x] Breakpoints responsivos: mobile (<768px), tablet (768-1023px), desktop (1024px+)

### 0.2 Backend & Wiring (branch ralph/backend-wiring)

- [x] Estrutura monolito modular (5 projetos .NET: Core, Infrastructure, Application, API, Worker)
- [x] 21 entidades (Product, Order, Category, Customer, Supply, PurchaseOrder, Tenant, TenantUser, SystemUser, etc.)
- [x] 16 controllers, 60+ endpoints — todos delegam para services (thin controllers)
- [x] 11 service pairs (interface + implementation) com validacao, exceptions tipadas, DI
- [x] Seed data: 27 categorias, 10 produtos, 12 variantes, 15 pedidos, 96 itens de custo, 10 clientes, 7 suprimentos
- [x] Redis cache em endpoints de leitura pesada + SignalR backplane
- [x] SignalR hub com auto-reconnect para notificacoes em tempo real
- [x] Background workers: alerta de estoque (15min) + limpeza de notificacoes (diario)
- [x] Upload de arquivos com `IFileStorageService` (extensivel para S3/Azure Blob)
- [x] 20+ servicos Angular com HttpClient — zero dados mockados
- [x] Interceptors: auth (JWT injection), erros (toast + 409 conflict handling)
- [x] Guards: AuthGuard, TenantGuard, SuperAdminGuard, UnsavedChangesGuard

### 0.3 Service Layer Extraction (Backend Hardening Sprint) ✅

- [x] US-001: Typed exceptions (NotFoundException, ValidationException, ConflictException) + GlobalExceptionFilter
- [x] US-002: DI registration pattern (`AddApplicationServices()`)
- [x] US-003: CategoryService — hierarchy traversal, circular ref detection, slug uniqueness
- [x] US-004: ProductService — SKU auto-gen, category descendant filtering, analytics, cache
- [x] US-005: OrderService — timeline building, payment derivation, cost CRUD, fulfillment
- [x] US-006: PurchaseOrderService — cost allocation (by_value/by_quantity), status gating
- [x] US-007: DashboardService + FinanceService — KPI aggregation, ABC curve, reconciliation
- [x] US-008: Customer, Supply, Inventory, File, Search, Notification services
- [x] US-009: UserService — bcrypt, full CRUD, change-password
- [x] US-010: Role-based auth (Admin, Manager, Viewer) on all endpoints
- [x] US-011: Optimistic locking (Version + ConcurrencyToken) on Product, Category, PO, Supply
- [x] US-012: Frontend 409 conflict handling + version passthrough

### 0.4 Autenticacao & Multi-Tenancy ✅

- [x] JWT authentication (login, register, refresh token rotation)
- [x] Access token: 15 min, Refresh token: 7 dias
- [x] Tenant entity + TenantUser join table com roles (Owner, Admin, Manager, Viewer)
- [x] ITenantScoped interface — EF Core global query filters em todas as 18 entidades de dados
- [x] TenantMiddleware — resolve tenant a partir do JWT
- [x] Self-service signup: registro cria Tenant + SystemUser + TenantUser (Owner)
- [x] Switch-tenant endpoint para usuarios com acesso a multiplas lojas
- [x] Super-admin bypasses tenant filtering (IsSuperAdmin flag)
- [x] Unique indexes tenant-scoped (SKU, Slug, ExternalOrderId, MarketplaceId)
- [x] Frontend: login, register, tenant guard, admin page (/admin/tenants)

### 0.5 Docker & Infraestrutura ✅

- [x] Docker Compose: PostgreSQL 16, Redis 7, API, Worker, Nginx
- [x] Dockerfiles: api (multi-stage), worker (multi-stage), web (Nginx + Angular)
- [x] Health checks em todos os servicos
- [x] Volumes persistentes para dados
- [x] Nginx reverse proxy

---

## Fase 1 — Integracao Mercado Livre ⬜ PROXIMA

**Objetivo**: Conectar ao ML, importar anuncios, receber pedidos reais, responder perguntas.

**Pre-requisitos**:
- [ ] Conta de vendedor no Mercado Livre
- [ ] App registrado no DevCenter (client_id, client_secret, redirect_uri)
- [ ] Usuarios de teste criados (POST /users/test_user, max 10)

> **Referencia tecnica**: [guides/Mercado-Livre-API.md](guides/Mercado-Livre-API.md) e [guides/Mercado-Livre-Avancada.md](guides/Mercado-Livre-Avancada.md)

### Sprint 1 — OAuth & Conexao ML

Backend:
- [ ] `MercadoLivreAdapter` implementando `IMarketplaceAdapter` (DI keyed services)
- [ ] OAuth 2.0 flow (authorize → token → refresh) com PKCE
- [ ] Criptografia de tokens OAuth em repouso (AES-256 via `IDataProtectionProvider`)
- [ ] Background worker para renovacao proativa de tokens (30 min antes de expirar)
- [ ] Circuit breaker (Polly) para chamadas a API do ML — 3 falhas = inativo
- [ ] Rate limiter client-side (18.000 req/hora, ~300/min)
- [ ] Tela de setup: redirect OAuth, callback, armazenamento seguro

Frontend:
- [ ] Pagina de integracao ML em Configuracoes
- [ ] Status da conexao (ativo, expirando em X, erro, desconectado)
- [ ] Botao conectar/desconectar

### Sprint 2 — Sync de Produtos & Anuncios

Backend:
- [ ] Import de anuncios existentes (`GET /users/{id}/items/search` com scan mode)
- [ ] Mapeamento anuncio ML → produto interno (criar ou vincular por SKU)
- [ ] Sync de variacoes ML → variantes internas
- [ ] Sync de fotos ML → FileUpload
- [ ] Worker de sync periodico (detectar novos/alterados)

Frontend:
- [ ] Tela de Anuncios (`/anuncios`) — listagem de anuncios ML com status de sync
- [ ] Indicador de vinculacao: produto interno ↔ anuncio ML
- [ ] Acao: vincular anuncio existente a produto, ou criar produto a partir do anuncio

### Sprint 3 — Pedidos & Webhooks

Backend:
- [ ] Webhook receiver para `orders_v2` — validacao + enqueue Redis (< 500ms response)
- [ ] Worker de processamento de fila de webhooks
- [ ] Sync de pedidos historicos (`GET /orders/search`)
- [ ] Mapeamento pedido ML → Order interno (itens, comprador, envio, pagamento)
- [ ] Webhooks adicionais: `items`, `questions`, `payments`, `shipments`
- [ ] Webhook signature validation (IPs: 54.88.218.97, 18.215.140.160, etc.)
- [ ] Endpoint de missed feeds (`GET /missed_feeds`) para recuperar notificacoes perdidas

Frontend:
- [ ] Vendas mostram dados reais do ML (status, envio, rastreamento)
- [ ] Notificacoes em tempo real (SignalR) para novas vendas

### Sprint 4 — Perguntas & Mensagens

Backend:
- [ ] Questions API: listar, responder (`GET /my/received_questions/search`, `POST /answers`)
- [ ] Mensagens pos-venda (`GET/POST /messages/packs/{id}/sellers/{id}`)
- [ ] Webhook `questions` e `messages` para notificacao em tempo real
- [ ] Templates de resposta configuráveis

Frontend:
- [ ] Tela de Perguntas (`/perguntas`) — listar, filtrar por status, responder
- [ ] Indicador de perguntas nao respondidas no sidebar
- [ ] Inbox de mensagens pos-venda por pedido

### Entregavel da Fase 1

Sistema que:
1. Conecta ao Mercado Livre via OAuth
2. Importa anuncios existentes e os vincula a produtos internos
3. Recebe pedidos automaticamente via webhook
4. Exibe pedidos reais no dashboard
5. Permite responder perguntas e enviar mensagens

---

## Fase 2 — Financeiro Completo ⬜

**Objetivo**: Calculo real de lucratividade por venda — o diferencial do sistema.

> **Referencia tecnica**: [guides/Financial-Model.md](guides/Financial-Model.md)

### Sprint 5 — Motor de Calculo de Custos

- [ ] Engine de comissoes (varia por categoria, reputacao, tipo de anuncio)
- [ ] Integracao com Billing API do ML (`GET /billing/integration/group/ML/order/details`)
- [ ] Calculo de frete real (shipping costs API)
- [ ] Calculo de impostos (Simples Nacional, ICMS — varia por estado e regime)
- [ ] Lookup de taxas de fulfillment (ML Full)
- [ ] Taxa de pagamento (depende de parcelas)
- [ ] Servico de composicao que agrega todas as categorias de custo por venda
- [ ] Historico de custos de produto (effective_from/until)
- [ ] View materializada de lucratividade por SKU

### Sprint 6 — Relatorios & Conciliacao

- [ ] Conciliacao: valor depositado ML vs valor esperado (billing API)
- [ ] Identificacao automatica de divergencias em comissoes
- [ ] Exportacao PDF (QuestPDF) e Excel (ClosedXML)
- [ ] Curva ABC por margem de lucro (dados reais)
- [ ] Dashboard financeiro com dados reais (substituir seed data)

### Sprint 7 — Alertas & Precificacao

- [ ] Sistema de alertas configuravel (margem < X%, estoque < Y, pergunta sem resposta > Z horas)
- [ ] Calculadora de preco (dado margem desejada → calcula preco de venda)
- [ ] Simulador de cenarios (e se comissao mudar? e se frete subir?)

---

## Fase 3 — Estoque & Fulfillment ⬜

**Objetivo**: Gestao completa de estoque, integracao com ML Full.

> **Referencia tecnica**: [guides/Stock-Management.md](guides/Stock-Management.md)

### Sprint 8 — Gestao de Estoque

- [ ] Atualizacao automatica de estoque no ML ao registrar entrada (PUT /items/{id})
- [ ] Webhook `items` para detectar mudancas externas de estoque
- [ ] Worker de reconciliacao periodica (local vs ML, a cada 15 min)
- [ ] Alertas de divergencia de estoque

### Sprint 9 — Fulfillment (ML Full)

- [ ] Consulta de estoque no CD (`GET /inventories/{id}/stock/fulfillment`)
- [ ] Historico de operacoes Full (`GET /stock/fulfillment/operations/search`)
- [ ] Webhook `fbm_stock_operations` para atualizacoes em tempo real
- [ ] Calculo de custo de armazenagem acumulado por SKU (diario)
- [ ] Simulador: Full vs envio proprio (custo comparativo por SKU)

---

## Fase 4 — Marketing & Ads ⬜

**Objetivo**: Integracao com Mercado Ads, ROI real por campanha.

### Sprint 10

- [ ] Advertising API: campanhas, metricas (clicks, prints, cost, ACOS, ROAS)
- [ ] Atribuicao de custo de advertising por venda
- [ ] ROI real por campanha (considerando margem liquida)
- [ ] Gestao de promocoes (PRICE_DISCOUNT, LIGHTNING via API)
- [ ] Painel de advertising no frontend

---

## Fase 5 — Multi-Marketplace ⬜

**Objetivo**: Expandir para Amazon e Shopee.

### Sprint 11-12 — Amazon SP-API

- [ ] `AmazonAdapter` implementando `IMarketplaceAdapter`
- [ ] OAuth com Amazon Seller Central
- [ ] Sync de produtos, pedidos, estoque
- [ ] Mapeamento de taxas Amazon para modelo financeiro unificado

### Sprint 13-14 — Shopee + Sync Multi-Canal

- [ ] `ShopeeAdapter` implementando `IMarketplaceAdapter`
- [ ] Estoque centralizado com alocacao por marketplace
- [ ] Venda em um → atualiza estoque em todos
- [ ] Dashboard comparativo entre marketplaces

---

## Fase 6 — Pos-Venda & Mensageria ⬜

### Sprint 15

- [ ] Inbox unificado (perguntas + mensagens, todos os marketplaces)
- [ ] Templates de resposta (configuraveis por tipo de situacao)
- [ ] Gestao de reclamacoes e devolucoes (timeline, acoes, evidencias)

---

## Fase 7 — Testes, CI/CD & Go-Live ⬜

### Sprint 16 — Testes

- [ ] Testes unitarios (xUnit) — services, business logic
- [ ] Testes de integracao (TestContainers) — controllers, database
- [ ] Testes Angular (componentes + servicos)
- [ ] Cobertura minima: 70% em services

### Sprint 17 — CI/CD & Deploy

- [ ] GitHub Actions: build, test, lint no PR
- [ ] Docker image build + push (GitHub Container Registry)
- [ ] Deploy automatizado para VPS (Hetzner/Contabo)
- [ ] Backup automatizado PostgreSQL (cron + pg_dump)
- [ ] Logs estruturados (Serilog)
- [ ] Monitoramento basico (uptime, erros, latencia)

### Sprint 18 — Seguranca & Polish

- [ ] Rate limiting na API propria
- [ ] Revisao de UX em todas as telas
- [ ] Onboarding flow (primeira conexao com marketplace)
- [ ] Documentacao tecnica (como rodar, variaveis de ambiente)

---

## Marcos de Validacao

| Marco | Criterio | Status |
|-------|----------|--------|
| **M0 — Fundacao** | Backend + frontend funcional, Docker operacional | ✅ Concluido (2026-03-27) |
| **M1 — Conexao ML** | OAuth ML funcionando, anuncios importados | ⬜ |
| **M2 — Primeira venda rastreada** | Pedido recebido via webhook com custos registrados | ⬜ |
| **M3 — Financeiro real** | Decomposicao completa de custos com dados da Billing API | ⬜ |
| **M4 — Estoque operacional** | Entrada de mercadoria reflete no ML automaticamente | ⬜ |
| **M5 — Multi-marketplace** | Venda na Amazon/Shopee atualiza estoque no ML | ⬜ |
| **M6 — Go-Live** | Sistema em producao, uso diario real | ⬜ |

---

## Debito Tecnico Conhecido

| Item | Severidade | Status |
|------|-----------|--------|
| Dashboard/Finance carregam todos os itens em memoria | Media | Pendente — adicionar filtro por data |
| `RemoveByPrefixAsync` stub no Redis | Baixa | Pendente — precisa IConnectionMultiplexer direto |
| Worker NuGet version mismatch (EF 9.0.1 vs 9.0.14) | Baixa | Pendente |
| Hierarquia de categorias rasa no seed | Baixa | Pendente |
| Testes unitarios/integracao com cobertura minima | Media | Pendente — Fase 7 |
| CI/CD nao configurado | Media | Pendente — Fase 7 |
| Local file storage (sem S3/Azure Blob) | Baixa | Pendente |

---

## Dependencias Externas

| Item | Quando | Status |
|------|--------|--------|
| Conta vendedor ML + App DevCenter | Fase 1 | ⬜ Pendente |
| CNPJ / MEI (para NF-e e ML Coleta) | Antes de vender | ⬜ Pendente |
| Conta Amazon Seller | Fase 5 | ⬜ Pendente |
| Conta Shopee Seller | Fase 5 | ⬜ Pendente |
| Dominio perushop.com.br | Fase 7 | Ja possui |

---

## Como Rodar

```bash
# Docker Compose (tudo junto)
docker compose up -d

# Ou manualmente:
# 1. PostgreSQL + Redis
docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16
docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine

# 2. Migrations
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API

# 3. API (http://localhost:5000)
dotnet run --project src/PeruShopHub.API

# 4. Worker
dotnet run --project src/PeruShopHub.Worker

# 5. Frontend (http://localhost:4200)
cd src/PeruShopHub.Web && npm install && npx ng serve

# Swagger: http://localhost:5000/swagger
# Health: http://localhost:5000/health
```

---

## Indice de Documentacao

| Documento | Descricao |
|-----------|-----------|
| **[ROADMAP.md](ROADMAP.md)** | Este arquivo — progresso e proximos passos |
| **[Architecture.md](Architecture.md)** | Arquitetura tecnica, data model, patterns |
| **[Design-System.md](PeruShopHub-Design-System.md)** | Design tokens, componentes, telas |
| **[guides/Mercado-Livre-API.md](guides/Mercado-Livre-API.md)** | API reference completa (OAuth, Items, Orders, Shipping, etc.) |
| **[guides/Mercado-Livre-Avancada.md](guides/Mercado-Livre-Avancada.md)** | Fulfillment, Advertising, Billing, Catalog, Promotions |
| **[guides/Mercado-Livre-Modelos.md](guides/Mercado-Livre-Modelos.md)** | Full vs envio proprio, comissoes, reputacao |
| **[guides/Mercado-Livre-Produtos.md](guides/Mercado-Livre-Produtos.md)** | Categorias recomendadas, estrategia de investimento |
| **[guides/Stock-Management.md](guides/Stock-Management.md)** | Gestao de estoque, reconciliacao, ML Full |
| **[guides/Financial-Model.md](guides/Financial-Model.md)** | Motor de custos, decomposicao, lucratividade |
| **[guides/Multi-Tenancy.md](guides/Multi-Tenancy.md)** | Arquitetura multi-tenant, query filters, roles |
| **[guides/Authentication.md](guides/Authentication.md)** | JWT, refresh tokens, RBAC, tenant switching |
