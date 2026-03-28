# PeruShopHub — Roadmap & Progress

> Ultima atualizacao: 2026-03-28

## Visao Geral

**PeruShopHub** e um sistema centralizado de gestao multi-marketplace focado em **rastreabilidade real de lucratividade por venda**. Nenhum ERP/hub existente calcula o lucro liquido real considerando todos os custos (comissao, taxas fixas, frete real, fulfillment, advertising, impostos, custo do produto, embalagem, cupons absorvidos).

**Posicionamento**: "O primeiro hub que mostra seu lucro real por venda" — plataforma unica para gestao completa de loja em marketplaces. O vendedor gerencia tudo dentro do PeruShopHub e a plataforma sincroniza com os marketplaces conectados.

**Target**: Vendedores pequenos faturando R$10k-50k/mes no Mercado Livre, expandindo para Shopee/Amazon.

**Stack**: .NET 9 / ASP.NET Core / EF Core 9 / PostgreSQL 16 / Redis 7 / SignalR / Angular 17+ / Chart.js

---

## Status Atual

**Branch ativa**: `ralph/backend-wiring`
**Fase atual**: Fase 0 concluida. Proxima: Fase 0.5 (DevOps & Quality Foundation)

O sistema possui uma base funcional completa com backend, frontend e infraestrutura Docker. O proximo passo e estabelecer a fundacao de qualidade (CI/CD, testes, observabilidade) antes de avancar para features de produto.

---

## Visao das Fases

```
Fase 0    — Fundacao                                    ✅ CONCLUIDA
Fase 0.5  — DevOps & Quality Foundation                 ⬜ PROXIMA
Fase 1    — Estoque & Fulfillment                       ⬜
Fase 2    — Motor Financeiro                            ⬜
Fase 3    — Integracao Mercado Livre                    ⬜
Fase 4    — Pos-Venda & Mensageria                      ⬜
Fase 5    — Testes Finais, Seguranca & Go-Live          ⬜

═══════════ MVP RELEASED (beta fechado, 10-20 sellers) ═══════════

Fase 6    — Billing & Subscriptions                     ⬜
Fase 7    — NF-e Integration                            ⬜
Fase 8    — Marketing & Ads + AI                        ⬜

═══════════ PRODUTO RELEASED (lancamento publico) ═══════════════

Fase 9    — Multi-Marketplace                           ⬜
Fase 10   — PWA, Mobile Push & Advanced Features        ⬜
```

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

## Fase 0.5 — DevOps & Quality Foundation ⬜ PROXIMA

**Objetivo**: Estabelecer infraestrutura de qualidade antes de construir features de produto. CI protege contra regressoes, testes guardam calculos financeiros, observabilidade permite debug em producao.

### Sprint 1 — CI/CD

- [ ] GitHub Actions: build + lint + typecheck em cada PR
- [ ] Pipeline: backend (`dotnet build`, `dotnet test`) + frontend (`ng build`, `ng test`)
- [ ] Docker image build + push (GitHub Container Registry)
- [ ] Branch protection rules no main

### Sprint 2 — Test Infrastructure

- [ ] Setup xUnit + TestContainers (PostgreSQL + Redis em container)
- [ ] Testes unitarios para calculos financeiros (OrderCost, margem, decomposicao)
- [ ] Testes unitarios para services criticos (ProductService, OrderService, FinanceService)
- [ ] Testes de integracao basicos (controllers + database)
- [ ] Testes Angular: servicos criticos + componentes compartilhados
- [ ] Cobertura minima inicial: 40% em services (sobe para 70% ate Fase 5)

### Sprint 3 — Rate Limiting & Observability

- [ ] Rate limiting na API propria (por tenant, prevenir que um tenant derrube o sistema)
- [ ] Serilog structured logging (JSON, correlacao por request)
- [ ] Error tracking (Sentry ou similar)
- [ ] Health check dashboard basico
- [ ] Metricas de request (latencia, erro rate, por endpoint)

### Entregavel da Fase 0.5

Pipeline CI/CD funcional, suite de testes basica rodando automaticamente, logs estruturados e error tracking configurados. Toda feature futura deve incluir testes.

---

## Fase 1 — Estoque & Fulfillment ⬜

**Objetivo**: Gestao completa de estoque interno. Entradas, saidas, movimentacoes, alertas, purchase orders com alocacao de custos.

> **Nota**: A infra basica de estoque ja existe (entidades, CRUD, worker de alertas). Esta fase adiciona a logica inteligente e prepara para integracao com marketplaces na Fase 3.

> **Referencia tecnica**: [guides/Stock-Management.md](guides/Stock-Management.md)

### Sprint 4 — Gestao de Estoque Avancada

- [ ] Fluxo completo de entrada de mercadoria (PurchaseOrder → recebimento → ajuste automatico de estoque)
- [ ] Alocacao de estoque por canal (master stock → alocacoes por marketplace)
- [ ] Regras de estoque minimo/maximo por produto
- [ ] Alertas configuráveis (estoque abaixo de X, proxima reposicao)
- [ ] Historico completo de movimentacoes com rastreabilidade (quem, quando, por que)
- [ ] Reconciliacao interna (estoque fisico vs sistema)

### Sprint 5 — Custos de Produto & Historico

- [ ] Historico de custos de produto (effective_from/until) — custo muda ao longo do tempo
- [ ] Custo medio ponderado automatico (FIFO ou media ponderada)
- [ ] Custo de embalagem por produto
- [ ] Custo de armazenagem estimado por SKU (configuravel)

### Entregavel da Fase 1

Sistema de estoque robusto com rastreabilidade completa, custos historicos, e alocacao por canal. Pronto para receber atualizacoes automaticas de marketplaces na Fase 3.

---

## Fase 2 — Motor Financeiro ⬜

**Objetivo**: Calculo real de lucratividade por venda — o diferencial do sistema. Regras de precificacao por margem-alvo.

> **Referencia tecnica**: [guides/Financial-Model.md](guides/Financial-Model.md)

### Sprint 6 — Motor de Calculo de Custos

- [ ] Engine de comissoes (varia por categoria, reputacao, tipo de anuncio)
- [ ] Calculo de impostos (Simples Nacional, ICMS — varia por estado e regime)
- [ ] Taxa de pagamento (depende de parcelas)
- [ ] Servico de composicao que agrega todas as categorias de custo por venda
- [ ] View materializada de lucratividade por SKU
- [ ] Decomposicao completa por venda: comissao + taxa fixa + frete + pagamento + fulfillment + armazenagem + custo produto + embalagem + advertising + imposto = lucro liquido

### Sprint 7 — Relatorios & Conciliacao

- [ ] Exportacao PDF (QuestPDF) e Excel (ClosedXML)
- [ ] Curva ABC por margem de lucro (dados reais)
- [ ] Dashboard financeiro com dados reais (substituir seed data)
- [ ] Relatorios automatizados por email (semanal/mensal — digest de lucratividade)
- [ ] Audit trail / log de atividades (quem mudou precos, estoque, custos)

### Sprint 8 — Precificacao por Margem-Alvo

- [ ] Regras de preco por marketplace ("margem desejada de 20% → preco calculado automaticamente considerando taxas do marketplace")
- [ ] Calculadora de preco (dado margem desejada → calcula preco de venda)
- [ ] Simulador de cenarios (e se comissao mudar? e se frete subir?)
- [ ] Sistema de alertas configuravel (margem < X%, divergencia de custo)

### Entregavel da Fase 2

Motor financeiro completo que decompoe automaticamente todos os custos por venda. Regras de precificacao por margem-alvo por marketplace. Relatorios exportaveis e alertas de margem.

---

## Fase 3 — Integracao Mercado Livre ⬜

**Objetivo**: Conectar ao ML, importar anuncios, receber pedidos reais, sincronizar estoque automaticamente.

**Pre-requisitos**:
- [ ] Conta de vendedor no Mercado Livre
- [ ] App registrado no DevCenter (client_id, client_secret, redirect_uri)
- [ ] Usuarios de teste criados (POST /users/test_user, max 10)

> **Referencia tecnica**: [guides/Mercado-Livre-API.md](guides/Mercado-Livre-API.md) e [guides/Mercado-Livre-Avancada.md](guides/Mercado-Livre-Avancada.md)

### Sprint 9 — OAuth & Conexao ML

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

### Sprint 10 — Sync de Produtos & Anuncios

Backend:
- [ ] Import de anuncios existentes (`GET /users/{id}/items/search` com scan mode)
- [ ] Mapeamento anuncio ML → produto interno (criar ou vincular por SKU)
- [ ] Sync de variacoes ML → variantes internas
- [ ] Sync de fotos ML → FileUpload
- [ ] Worker de sync periodico (detectar novos/alterados)
- [ ] Customizacao de titulo/descricao/preco por marketplace

Frontend:
- [ ] Tela de Anuncios (`/anuncios`) — listagem de anuncios ML com status de sync
- [ ] Indicador de vinculacao: produto interno ↔ anuncio ML
- [ ] Acao: vincular anuncio existente a produto, ou criar produto a partir do anuncio

### Sprint 11 — Pedidos & Webhooks

Backend:
- [ ] Webhook receiver para `orders_v2` — validacao + enqueue Redis (< 500ms response)
- [ ] Worker de processamento de fila de webhooks
- [ ] Sync de pedidos historicos (`GET /orders/search`)
- [ ] Mapeamento pedido ML → Order interno (itens, comprador, envio, pagamento)
- [ ] Webhooks adicionais: `items`, `questions`, `payments`, `shipments`
- [ ] Webhook signature validation (IPs: 54.88.218.97, 18.215.140.160, etc.)
- [ ] Endpoint de missed feeds (`GET /missed_feeds`) para recuperar notificacoes perdidas
- [ ] Integracao com Billing API do ML (`GET /billing/integration/group/ML/order/details`)
- [ ] Calculo de frete real (shipping costs API)
- [ ] Lookup de taxas de fulfillment (ML Full)

Frontend:
- [ ] Vendas mostram dados reais do ML (status, envio, rastreamento)
- [ ] Notificacoes em tempo real (SignalR) para novas vendas

### Sprint 12 — Estoque ML & Fulfillment

Backend:
- [ ] Atualizacao automatica de estoque no ML ao registrar entrada (PUT /items/{id})
- [ ] Webhook `items` para detectar mudancas externas de estoque
- [ ] Worker de reconciliacao periodica (local vs ML, a cada 15 min)
- [ ] Alertas de divergencia de estoque
- [ ] Consulta de estoque no CD (`GET /inventories/{id}/stock/fulfillment`)
- [ ] Historico de operacoes Full (`GET /stock/fulfillment/operations/search`)
- [ ] Webhook `fbm_stock_operations` para atualizacoes em tempo real
- [ ] Calculo de custo de armazenagem acumulado por SKU (diario)
- [ ] Simulador: Full vs envio proprio (custo comparativo por SKU)

### Sprint 13 — Emails Transacionais & Conta

Backend:
- [ ] Integracao com provedor de email (SendGrid, Resend, ou similar)
- [ ] Email de boas-vindas no registro
- [ ] Fluxo de "esqueci minha senha" (forgot password → email → reset)
- [ ] Notificacoes por email: nova venda, estoque baixo, alerta de margem
- [ ] Self-service: editar perfil, mudar email, gerenciar equipe

Frontend:
- [ ] Tela de "esqueci minha senha"
- [ ] Pagina de perfil do usuario
- [ ] Configuracoes de notificacoes por email (opt-in/opt-out)

### Sprint 14 — Onboarding Flow

- [ ] Wizard de primeira conexao com marketplace (passo a passo visual)
- [ ] Import guiado de produtos existentes
- [ ] "Seu primeiro relatorio de lucratividade" — momento aha
- [ ] Checklist de setup (perfil, marketplace, produtos, custos)
- [ ] Tooltips e guias contextuais para features-chave

### Entregavel da Fase 3

Sistema conectado ao Mercado Livre: importa anuncios, recebe pedidos via webhook, sincroniza estoque automaticamente, mostra lucratividade real por venda com dados da Billing API. Onboarding guiado e emails transacionais funcionais.

---

## Fase 4 — Pos-Venda & Mensageria ⬜

**Objetivo**: Gestao completa de pos-venda, perguntas, mensagens e compliance.

### Sprint 15 — Perguntas & Mensagens ML

Backend:
- [ ] Questions API: listar, responder (`GET /my/received_questions/search`, `POST /answers`)
- [ ] Mensagens pos-venda (`GET/POST /messages/packs/{id}/sellers/{id}`)
- [ ] Webhook `questions` e `messages` para notificacao em tempo real
- [ ] Templates de resposta configuraveis por tipo de situacao

Frontend:
- [ ] Tela de Perguntas (`/perguntas`) — listar, filtrar por status, responder
- [ ] Indicador de perguntas nao respondidas no sidebar
- [ ] Inbox de mensagens pos-venda por pedido

### Sprint 16 — Reclamacoes, Devolucoes & LGPD

- [ ] Gestao de reclamacoes e devolucoes (timeline, acoes, evidencias)
- [ ] Alerta configuravel: pergunta sem resposta > X horas
- [ ] LGPD compliance: politica de privacidade, termos de uso
- [ ] Consentimento de cookies
- [ ] Endpoint de exportacao de dados do usuario (data portability)
- [ ] Endpoint de exclusao de dados do usuario (right to be forgotten)
- [ ] Integracao com software contabil (export para Bling/Tiny via API — para sellers que mantem ERP para NF-e)

### Entregavel da Fase 4

Inbox completo de perguntas e mensagens, gestao de pos-venda, compliance LGPD, e integracao contabil para sellers que ainda usam ERP externo.

---

## Fase 5 — Testes Finais, Seguranca & Go-Live ⬜

**Objetivo**: Hardening completo antes do lancamento do MVP para beta fechado.

### Sprint 17 — Testes & Cobertura

- [ ] Cobertura de testes: 70%+ em services
- [ ] Testes de integracao completos (todos os controllers, webhooks, OAuth flow)
- [ ] Testes de carga basicos (simular 100 webhooks simultaneos)
- [ ] Testes Angular: cobertura em componentes criticos + fluxos de usuario
- [ ] Testes de seguranca: tenant isolation, auth bypass attempts

### Sprint 18 — Seguranca & Deploy

- [ ] Deploy automatizado para VPS (Hetzner/Contabo)
- [ ] Backup automatizado PostgreSQL (cron + pg_dump + offsite)
- [ ] Monitoramento basico (uptime, erros, latencia)
- [ ] Revisao de seguranca: SQL injection, XSS, CSRF, tenant leakage
- [ ] Revisao de UX em todas as telas
- [ ] Documentacao tecnica (como rodar, variaveis de ambiente)
- [ ] Landing page de waitlist publicada

### Entregavel da Fase 5 — MVP RELEASE

Sistema pronto para beta fechado com 10-20 sellers selecionados. Onboarding pessoal, feedback direto, iteracao rapida. Sem cobranca.

---

## ═══════════ MVP RELEASED ═══════════

---

## Fase 6 — Billing & Subscriptions ⬜

**Objetivo**: Monetizacao do produto. Planos, trial, cobranca recorrente.

> **Referencia tecnica**: [business/pricing-model.md](business/pricing-model.md)

### Sprint 19 — Payment Provider & Plans

- [ ] Integracao com provedor de pagamento (Stripe ou local: Asaas, Pagar.me) para BRL recorrente
- [ ] Entidade Plan: Starter (R$89), Pro (R$199), Business (R$449)
- [ ] Feature gating por plano (orders/mes, marketplaces, produtos, usuarios)
- [ ] Usage metering: contagem de pedidos/mes, produtos, usuarios por tenant

### Sprint 20 — Trial & Billing Portal

- [ ] Logica de trial: Pro completo no registro, clock inicia 14 dias apos primeira venda, soft-land para Starter
- [ ] Billing portal: faturas, troca de plano, metodo de pagamento, cancelamento
- [ ] Emails de billing: fatura, falha de cobranca, trial expirando, downgrade
- [ ] Desconto anual (20%)
- [ ] Landing page convertida para site de marketing com planos e precos

### Entregavel da Fase 6

Produto monetizado com planos, trial inteligente, e billing self-service. Landing page publica com precos.

---

## Fase 7 — NF-e Integration ⬜

**Objetivo**: Emissao de Nota Fiscal Eletronica via provedor externo, eliminando necessidade de ERP separado.

### Sprint 21 — NF-e via API Externa

- [ ] Integracao com Focus NFe (ou eNotas, Nuvem Fiscal)
- [ ] Emissao de NF-e de venda automatica (a partir do pedido)
- [ ] Emissao de NF-e de entrada (a partir do PurchaseOrder)
- [ ] Consulta de status, cancelamento, carta de correcao
- [ ] Configuracao fiscal por produto (NCM, CFOP, CST)
- [ ] Regras fiscais por estado (ICMS, substituicao tributaria)
- [ ] Armazenamento de XML e DANFE

Frontend:
- [ ] Tela de notas fiscais (listagem, filtro, status)
- [ ] Emissao manual e automatica (configuravel)
- [ ] Visualizacao/download de DANFE

### Entregavel da Fase 7

Sellers podem emitir NF-e direto pelo PeruShopHub. Nao precisam mais de Bling/Tiny para compliance fiscal.

---

## Fase 8 — Marketing & Ads + AI ⬜

**Objetivo**: Integracao com Mercado Ads, ROI real por campanha, insights via IA.

### Sprint 22 — Advertising & Promocoes

- [ ] Advertising API: campanhas, metricas (clicks, prints, cost, ACOS, ROAS)
- [ ] Atribuicao de custo de advertising por venda
- [ ] ROI real por campanha (considerando margem liquida, nao apenas receita)
- [ ] Gestao de promocoes (PRICE_DISCOUNT, LIGHTNING via API)
- [ ] Painel de advertising no frontend

### Sprint 23 — AI Insights

- [ ] Integracao Claude API (Anthropic SDK)
- [ ] Sumarios de lucratividade em linguagem natural
- [ ] Deteccao automatica de erosao de margem (produto X perdeu Y% de margem nos ultimos 30 dias)
- [ ] Sugestoes de ajuste de preco baseadas em margem-alvo
- [ ] Resposta inteligente de perguntas (sugestao de resposta baseada em contexto do produto)

### Entregavel da Fase 8

Gestao completa de advertising com ROI real. Insights via IA que proativamente alertam sobre problemas e oportunidades.

---

## ═══════════ PRODUTO RELEASED ═══════════

---

## Fase 9 — Multi-Marketplace ⬜

**Objetivo**: Expandir para Amazon e Shopee. Gestao unificada de estoque, precos e pedidos.

### Sprint 24-25 — Amazon SP-API

- [ ] `AmazonAdapter` implementando `IMarketplaceAdapter`
- [ ] OAuth com Amazon Seller Central
- [ ] Sync de produtos, pedidos, estoque
- [ ] Mapeamento de taxas Amazon para modelo financeiro unificado
- [ ] Regras de preco especificas para Amazon (considerando taxas Amazon)

### Sprint 26-27 — Shopee + Sync Multi-Canal

- [ ] `ShopeeAdapter` implementando `IMarketplaceAdapter`
- [ ] Estoque centralizado com alocacao por marketplace
- [ ] Venda em um → atualiza estoque em todos
- [ ] Dashboard comparativo entre marketplaces
- [ ] Inbox unificado (perguntas + mensagens, todos os marketplaces)

### Entregavel da Fase 9

Venda na Amazon/Shopee com estoque sincronizado em todos os canais. Dashboard comparativo mostra lucratividade por marketplace.

---

## Fase 10 — PWA, Mobile Push & Advanced Features ⬜

**Objetivo**: Experiencia mobile aprimorada e features avancadas.

### Sprint 28+

- [ ] PWA (Progressive Web App) com instalacao no celular
- [ ] Push notifications: nova venda, estoque baixo, pergunta sem resposta, alerta de margem
- [ ] Conciliacao financeira: valor depositado ML vs valor esperado (billing API)
- [ ] Identificacao automatica de divergencias em comissoes/taxas indevidas
- [ ] Monitoramento de concorrentes (precos, posicionamento)
- [ ] API publica para integracao com ferramentas de terceiros

---

## Marcos de Validacao

| Marco | Criterio | Status |
|-------|----------|--------|
| **M0 — Fundacao** | Backend + frontend funcional, Docker operacional | ✅ Concluido (2026-03-27) |
| **M0.5 — Quality** | CI/CD rodando, testes basicos, observabilidade | ⬜ |
| **M1 — Estoque** | Gestao de estoque completa com custos historicos | ⬜ |
| **M2 — Financeiro** | Motor de lucratividade funcionando com dados internos | ⬜ |
| **M3 — Conexao ML** | OAuth ML, anuncios importados, pedidos via webhook | ⬜ |
| **M4 — Pos-Venda** | Perguntas, mensagens, LGPD compliance | ⬜ |
| **M5 — MVP** | Beta fechado com 10-20 sellers, feedback direto | ⬜ |
| **M6 — Monetizacao** | Billing ativo, trial funcionando, planos definidos | ⬜ |
| **M7 — NF-e** | Emissao de NF-e via provedor externo | ⬜ |
| **M8 — Produto** | Lancamento publico, marketing site, ads + AI | ⬜ |
| **M9 — Multi-MP** | Amazon + Shopee integrados, estoque unificado | ⬜ |

---

## Debito Tecnico Conhecido

| Item | Severidade | Status |
|------|-----------|--------|
| Dashboard/Finance carregam todos os itens em memoria | Media | Pendente — adicionar filtro por data |
| `RemoveByPrefixAsync` stub no Redis | Baixa | Pendente — precisa IConnectionMultiplexer direto |
| Worker NuGet version mismatch (EF 9.0.1 vs 9.0.14) | Baixa | Pendente |
| Hierarquia de categorias rasa no seed | Baixa | Pendente |
| Local file storage (sem S3/Azure Blob) | Baixa | Pendente |

---

## Dependencias Externas

| Item | Quando | Status |
|------|--------|--------|
| Conta vendedor ML + App DevCenter | Fase 3 | ⬜ Pendente |
| CNPJ / MEI (para NF-e e ML Coleta) | Fase 7 | ⬜ Pendente |
| Provedor de email (SendGrid/Resend) | Fase 3 | ⬜ Pendente |
| Provedor de pagamento (Stripe/Asaas) | Fase 6 | ⬜ Pendente |
| Provedor de NF-e (Focus NFe/eNotas) | Fase 7 | ⬜ Pendente |
| Error tracking (Sentry) | Fase 0.5 | ⬜ Pendente |
| Conta Amazon Seller | Fase 9 | ⬜ Pendente |
| Conta Shopee Seller | Fase 9 | ⬜ Pendente |
| Dominio perushop.com.br | Fase 5 | Ja possui |

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
| **[business/pricing-model.md](business/pricing-model.md)** | Modelo de precos, analise competitiva, estrategia de monetizacao |
| **[business/landing-page-brief.md](business/landing-page-brief.md)** | Brief para landing page / site marketing |
| **[guides/Mercado-Livre-API.md](guides/Mercado-Livre-API.md)** | API reference completa (OAuth, Items, Orders, Shipping, etc.) |
| **[guides/Mercado-Livre-Avancada.md](guides/Mercado-Livre-Avancada.md)** | Fulfillment, Advertising, Billing, Catalog, Promotions |
| **[guides/Mercado-Livre-Modelos.md](guides/Mercado-Livre-Modelos.md)** | Full vs envio proprio, comissoes, reputacao |
| **[guides/Mercado-Livre-Produtos.md](guides/Mercado-Livre-Produtos.md)** | Categorias recomendadas, estrategia de investimento |
| **[guides/Stock-Management.md](guides/Stock-Management.md)** | Gestao de estoque, reconciliacao, ML Full |
| **[guides/Financial-Model.md](guides/Financial-Model.md)** | Motor de custos, decomposicao, lucratividade |
| **[guides/Multi-Tenancy.md](guides/Multi-Tenancy.md)** | Arquitetura multi-tenant, query filters, roles |
| **[guides/Authentication.md](guides/Authentication.md)** | JWT, refresh tokens, RBAC, tenant switching |
