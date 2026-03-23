# PeruShop Hub - Roadmap MVP → Go-Live

> Ultima atualizacao: 2026-03-23

## Stack Definitivo

| Camada | Tecnologia | Versao |
|--------|-----------|--------|
| Backend | C# / ASP.NET Core Web API | .NET 9 |
| Frontend | Angular (standalone components, signals) | 21 |
| UI Components | Custom components (CSS custom properties) | - |
| Banco de dados | PostgreSQL | 16 |
| Cache / Fila | Redis | 7+ |
| ORM | Entity Framework Core | 9 |
| Real-time | SignalR (Redis backplane) | - |
| Upload de arquivos | Local disk (abstração IFileStorageService) | - |
| Background Jobs | .NET BackgroundService | - |
| Containerizacao | Docker (containers individuais por enquanto) | - |
| CI/CD | GitHub Actions (pendente) | - |

---

## Pre-requisitos (antes de codar)

### Conta Mercado Livre

- [ ] Criar conta de vendedor no Mercado Livre (se nao tiver)
- [ ] Acessar DevCenter: https://developers.mercadolivre.com.br/devcenter
- [ ] Criar aplicacao (App ID + Secret Key + Redirect URI)
- [ ] Anotar: `client_id`, `client_secret`, `redirect_uri`
- [ ] Criar usuarios de teste (POST /users/test_user) para desenvolvimento

### Infraestrutura Local

- [x] PostgreSQL container rodando (`docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16`)
- [x] Redis container rodando (`docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine`)
- [ ] Nginx container (reverse proxy) — pendente para producao
- [ ] Volumes persistentes para banco e redis — pendente para producao

---

## Fase 0 - Fundacao ✅ CONCLUIDA

**Objetivo**: Projeto estruturado com backend funcional conectado ao frontend.

> Esta fase foi expandida para incluir o design system completo do frontend,
> a estruturacao completa do backend, e a conexao entre ambos.

### Entregas — Design System & Frontend (PR #1, branch `ralph/ui-ux-design-system`)

- [x] Design system completo com CSS custom properties (temas light + dark)
- [x] Fontes: Inter (UI) + Roboto Mono (dados financeiros)
- [x] Layout responsivo: sidebar colapsavel (256px/64px), header fixo (56px), drawer mobile
- [x] 12+ paginas completas com dados mockados:
  - Dashboard (KPIs, graficos receita/lucro, donut custos, top/bottom produtos)
  - Produtos (listagem, detalhe, formulario com variantes e galeria)
  - Categorias (arvore hierarquica com campos de variacao)
  - Vendas (listagem, detalhe com breakdown de custos, timeline, comprador)
  - Clientes (listagem, detalhe com historico de pedidos)
  - Suprimentos (CRUD com alertas de estoque)
  - Financeiro (KPIs, graficos, lucratividade por SKU, conciliacao, curva ABC)
  - Configuracoes (usuarios, integracoes, custos, alertas, aparencia)
  - Login
- [x] Componentes compartilhados: DataTable, KpiCard, Badge, Skeleton, EmptyState, Toast, SearchPalette (Ctrl+K)
- [x] Breakpoints responsivos: mobile (<768px), tablet (768-1023px), desktop (1024px+)

### Entregas — Backend & Wiring (branch `ralph/backend-wiring`)

- [x] Estrutura do projeto (monolito modular — 5 projetos .NET)
  ```
  PeruShopHub/
  ├── src/
  │   ├── PeruShopHub.Core/            # Dominio (12 entidades, interfaces, value objects)
  │   ├── PeruShopHub.Infrastructure/  # EF Core, Redis, SignalR, FileStorage
  │   ├── PeruShopHub.Application/     # 30+ DTOs, PagedResult
  │   ├── PeruShopHub.API/             # 11 controllers, 40+ endpoints, SignalR hub
  │   ├── PeruShopHub.Worker/          # StockAlertWorker, NotificationCleanupWorker
  │   └── PeruShopHub.Web/             # Frontend Angular 21
  ```
- [x] Migrations iniciais do banco (EF Core) com schema completo (12 tabelas)
- [x] Seed data: 27 categorias, 10 produtos, 12 variantes, 15 pedidos, 96 itens de custo, 10 clientes, 7 suprimentos, 8 notificacoes, 3 usuarios, 2 conexoes marketplace
- [x] Health check em `/health`
- [x] Swagger/OpenAPI em `/swagger`
- [x] Redis cache em endpoints de leitura pesada (dashboard, produtos)
- [x] SignalR hub em `/hubs/notifications` com Redis backplane
- [x] Background workers: alerta de estoque (15min) + limpeza de notificacoes (diario)
- [x] Upload de arquivos: `IFileStorageService` com armazenamento local (extensivel para S3/Azure Blob)
- [x] 11 servicos Angular com HttpClient (um por dominio)
- [x] Proxy Angular (`/api`, `/hubs`, `/uploads` → backend)
- [x] Interceptor de erros HTTP com toast notifications
- [x] Todas as 12+ paginas conectadas ao backend via API real
- [x] Zero dados mockados restantes no frontend

### Endpoints da API

| Dominio | Endpoints |
|---------|-----------|
| Dashboard | summary, chart/revenue-profit, chart/cost-breakdown, top-products, least-profitable, pending-actions |
| Produtos | list (paginado), getById, getVariants, create, update |
| Categorias | list por parent (lazy-load), getById, create, update, delete |
| Pedidos | list (paginado, filtros), getById (itens, comprador, envio, pagamento, custos) |
| Clientes | list (paginado), getById (com historico de pedidos) |
| Suprimentos | list (paginado), create, update |
| Financeiro | summary, chart/revenue-profit, chart/margin, sku-profitability, reconciliation, abc-curve |
| Configuracoes | users, integrations, costs |
| Notificacoes | list, mark-read, mark-all-read |
| Busca | busca global em produtos, pedidos, clientes |
| Arquivos | upload, list por entidade, delete |

### Modelo de dados implementado

```
products                  - Cadastro master de produtos (10 seed)
product_variants          - Variantes com atributos JSON (12 seed)
categories                - Categorias hierarquicas (27 seed)
orders                    - Pedidos com status e lucro (15 seed)
order_items               - Itens por pedido
order_costs               - Decomposicao de custos por pedido (96 seed)
customers                 - Clientes com historico (10 seed)
supplies                  - Suprimentos/embalagens (7 seed)
notifications             - Notificacoes do sistema (8 seed)
system_users              - Usuarios do sistema (3 seed)
marketplace_connections   - Conexoes com marketplaces (2 seed)
file_uploads              - Uploads de arquivos (polimorficos entityType+entityId)
```

### Debito tecnico identificado

| Item | Severidade | Descricao |
|------|-----------|-----------|
| Controllers gordos | Baixa | Logica de negocio nos controllers. Extrair para Application services ao adicionar testes. |
| `RemoveByPrefixAsync` stub | Baixa | Invalidacao por prefixo no Redis nao implementada (precisa `IConnectionMultiplexer` direto). |
| Versao NuGet no Worker | Baixa | `EntityFrameworkCore.Relational` 9.0.1 vs 9.0.14 — alinhar versoes. |
| Arquivos environment duplicados | Baixa | `environment.ts` em dois caminhos — limpar os orfaos. |
| Queries em memoria | Media | Dashboard e Finance carregam todos os itens de pedido em memoria. Adicionar filtro por data. |
| Hierarquia de categorias rasa | Baixa | 27 categorias mas maioria raiz. Aprofundar para testes mais realistas. |

---

## Fase 1 - MVP Core (Proxima)

**Objetivo**: Conectar ao Mercado Livre, receber pedidos reais, autenticacao do sistema.

> **Pre-requisito**: Fase 0 concluida. Conta no DevCenter ML criada.

### Sprint 1 - Autenticacao do Sistema

Backend:
- [ ] JWT authentication (login, access + refresh tokens)
- [ ] Hash de senha (bcrypt) para SystemUser
- [ ] Role-based authorization (admin, manager, viewer)
- [ ] Middleware de autenticacao em todos os endpoints
- [ ] Refresh token rotation

Frontend:
- [ ] Conectar tela de login ao endpoint de autenticacao
- [ ] Route guards em todas as rotas protegidas
- [ ] Interceptor HTTP para adicionar JWT no header
- [ ] Redirect automatico para login quando token expira

### Sprint 2 - Integracao Mercado Livre: OAuth + Produtos

Backend:
- [ ] OAuth 2.0 flow com Mercado Livre (authorize, token, refresh)
- [ ] Criptografia de tokens OAuth em repouso (AES-256)
- [ ] Background worker para renovacao proativa de tokens (30 min antes de expirar)
- [ ] Circuit breaker (Polly) para chamadas a API do ML
- [ ] Rate limiter (18k req/hora)
- [ ] `MercadoLivreAdapter` implementando `IMarketplaceAdapter` (DI keyed services)
- [ ] Sync de anuncios existentes do ML (GET /users/{id}/items)
- [ ] Mapeamento produto interno → anuncio ML

Frontend:
- [ ] Tela de setup da conexao ML (OAuth redirect flow)
- [ ] Status da conexao ML (ativo, expirando, erro)
- [ ] Indicador de sync por produto

### Sprint 3 - Integracao Mercado Livre: Pedidos + Webhooks

Backend:
- [ ] Webhook receiver para pedidos (`orders_v2`) — validacao + enqueue < 500ms
- [ ] Processamento assincrono de webhooks (Redis queue → Worker)
- [ ] Sync de pedidos historicos (GET /orders/search)
- [ ] Webhooks para: `items`, `questions`, `payments`, `shipments`
- [ ] Mapeamento de pedido ML → pedido interno
- [ ] Worker de processamento de fila de webhooks

Frontend:
- [ ] Tela de perguntas (listar, responder via API ML)
- [ ] Notificacoes em tempo real (SignalR) para novas vendas e perguntas

### Entregavel do MVP

Sistema funcional que:
1. Tem autenticacao propria (JWT + roles)
2. Conecta ao Mercado Livre via OAuth
3. Importa anuncios existentes
4. Recebe pedidos automaticamente via webhook
5. Exibe pedidos e produtos reais no dashboard
6. Permite responder perguntas

---

## Fase 2 - Financeiro Completo

**Objetivo**: Calculo real de lucratividade por venda. O diferencial do sistema.

> **Nota**: Atualmente os dados financeiros sao pre-calculados no seed.
> Esta fase implementa o motor de calculo real.

### Sprint 4 - Motor de Calculo de Custos

Backend:
- [ ] Engine de comissoes (varia por categoria, reputacao do vendedor, tipo de anuncio)
- [ ] Calculo de frete (peso × distancia × tabela da transportadora)
- [ ] Calculo de impostos (ICMS, PIS/COFINS, Simples Nacional — varia por estado e regime)
- [ ] Lookup de taxas de fulfillment (ML Full)
- [ ] Calculo de taxa de pagamento (depende do numero de parcelas)
- [ ] Servico de composicao que agrega todas as categorias de custo por venda
- [ ] Integracao com Billing API do ML (`GET /orders/{id}/billing_info`)
- [ ] Registro de custos adicionais manuais
- [ ] Historico de custos de produto (com effective_from/until)

Frontend:
- [ ] Detalhe financeiro do pedido (breakdown completo de custos reais)
- [ ] Configuracao de custos fixos (embalagem, por categoria)
- [ ] Edicao de custo de produto com historico
- [ ] Indicador de lucro por pedido (verde/amarelo/vermelho)

### Sprint 5 - Relatorios e Conciliacao

Backend:
- [ ] View materializada de lucratividade por SKU (refresh periodico)
- [ ] Conciliacao: valor depositado ML vs valor esperado (dados reais)
- [ ] Identificacao automatica de divergencias em comissoes
- [ ] Exportacao PDF (QuestPDF) e Excel (ClosedXML)
- [ ] Curva ABC por margem de lucro (dados reais)

Frontend:
- [ ] Tela de relatorios com dados reais:
  - Lucratividade por SKU (tabela + grafico)
  - Lucratividade por periodo
  - Comparativo receita bruta vs lucro liquido
  - Curva ABC
- [ ] Tela de conciliacao financeira
- [ ] Exportacao de relatorios (PDF/Excel)

### Sprint 6 - Alertas e Precificacao

Backend:
- [ ] Sistema de alertas configuravel:
  - Margem abaixo de X%
  - Estoque abaixo de Y unidades
  - Pergunta sem resposta ha Z horas
  - Divergencia financeira detectada
- [ ] Calculadora de preco (dado margem desejada, calcula preco de venda)
- [ ] Simulador de cenarios (e se comissao mudar? e se frete subir?)

Frontend:
- [ ] Configuracao de alertas
- [ ] Central de notificacoes (ja existe a infraestrutura SignalR)
- [ ] Calculadora de preco interativa
- [ ] Simulador de cenarios

---

## Fase 3 - Estoque e Fulfillment

**Objetivo**: Gestao completa de estoque, integracao com ML Full.

### Sprint 7 - Gestao de Estoque

Backend:
- [ ] CRUD de movimentacoes de estoque (entrada, saida, ajuste)
- [ ] Historico completo de movimentacoes por SKU
- [ ] Atualizacao automatica de estoque no ML ao registrar entrada
- [ ] Webhook de items para detectar mudancas externas de estoque
- [ ] Reconciliacao periodica (worker compara estoque local vs ML)
- [ ] Optimistic locking com coluna `version` para evitar overselling

Frontend:
- [ ] Tela de estoque (quantidades, status de sync)
- [ ] Registro de entrada de mercadoria (compra de fornecedor)
- [ ] Historico de movimentacoes
- [ ] Indicadores de estoque critico

### Sprint 8 - Fulfillment (ML Full)

Backend:
- [ ] Consulta de estoque no CD (GET /inventories/{id}/stock/fulfillment)
- [ ] Historico de operacoes Full (GET /stock/fulfillment/operations/search)
- [ ] Webhook `fbm_stock_operations` para atualizacoes em tempo real
- [ ] Calculo de custo de armazenagem acumulado por SKU
- [ ] Worker de calculo diario de armazenagem
- [ ] Simulador: Full vs envio proprio (custo comparativo)

Frontend:
- [ ] Painel de estoque Full (disponivel, danificado, em processo)
- [ ] Historico de operacoes Full
- [ ] Custo de armazenagem acumulado por produto
- [ ] Simulador Full vs proprio (tabela comparativa)
- [ ] Alerta de produtos com armazenagem alta e baixo giro

---

## Fase 4 - Marketing e Ads

**Objetivo**: Integracao com Mercado Ads, ROI real por campanha.

### Sprint 9

Backend:
- [ ] Consulta de campanhas e metricas (Advertising API)
- [ ] ACOS por produto (custo ads / receita)
- [ ] ROI real por campanha (considerando margem liquida)
- [ ] Atribuicao de custo de advertising por venda
- [ ] Gestao de promocoes (criar, editar, deletar via API)

Frontend:
- [ ] Painel de advertising:
  - Campanhas ativas, custo, clicks, conversao
  - ACOS e ROI por produto
  - Tendencia de gastos
- [ ] Gestao de promocoes (criar desconto, ver ativas)

---

## Fase 5 - Multi-Marketplace

**Objetivo**: Expandir para Amazon e Shopee.

### Sprint 10-11 - Amazon

Backend:
- [ ] Adapter Amazon SP-API (implementar IMarketplaceAdapter)
- [ ] OAuth com Amazon Seller Central
- [ ] Sync de produtos, pedidos, estoque
- [ ] Mapeamento de taxas Amazon para modelo financeiro unificado
- [ ] Webhook/notificacoes Amazon (SQS)

Frontend:
- [ ] Configuracao de conexao Amazon
- [ ] Todos os paineis existentes agora mostram dados Amazon tambem
- [ ] Filtro por marketplace em todas as telas

### Sprint 12-13 - Shopee + Sync Multi-Canal

Backend:
- [ ] Adapter Shopee Open Platform
- [ ] Estoque centralizado com alocacao por marketplace
- [ ] Sync automatico: venda em um → atualiza todos
- [ ] Lock otimista para evitar overselling
- [ ] Worker de reconciliacao multi-canal (a cada 15 min)

Frontend:
- [ ] Configuracao de conexao Shopee
- [ ] Dashboard comparativo entre marketplaces
- [ ] Painel de alocacao de estoque por canal
- [ ] Indicador de sync status por produto/marketplace

---

## Fase 6 - Pos-Venda e Mensageria

**Objetivo**: Gestao completa de comunicacao com compradores.

### Sprint 14

Backend:
- [ ] Mensagens pos-venda (enviar/receber por marketplace)
- [ ] Gestao de reclamacoes e devolucoes
- [ ] Templates de resposta (configuraveis por tipo de situacao)
- [ ] Webhook de mensagens para notificacao em tempo real

Frontend:
- [ ] Inbox unificado (perguntas + mensagens pos-venda, todos os marketplaces)
- [ ] Templates de resposta rapida
- [ ] Historico de comunicacao por pedido/comprador
- [ ] Gestao de reclamacoes (timeline, acoes, evidencias)

---

## Fase 7 - Infraestrutura & Go-Live

**Objetivo**: Sistema pronto para uso real em producao.

### Sprint 15 - Testes e CI/CD

- [ ] Projeto de testes unitarios (`tests/PeruShopHub.UnitTests/` com xUnit)
- [ ] Projeto de testes de integracao (`tests/PeruShopHub.IntegrationTests/` com TestContainers)
- [ ] Testes de controllers (status codes e shapes de resposta)
- [ ] Testes de logica de negocio
- [ ] Testes Angular (componentes + servicos)
- [ ] Pipeline CI/CD no GitHub Actions

### Sprint 16 - Docker e Deploy

- [ ] Dockerfile.api (multi-stage build)
- [ ] Dockerfile.worker (multi-stage build)
- [ ] Dockerfile.web (Nginx servindo build Angular)
- [ ] docker-compose.yml (API, Worker, Angular, PostgreSQL, Redis, Nginx)
- [ ] nginx.conf (reverse proxy)
- [ ] Gerenciamento de secrets e connection strings de producao
- [ ] Volumes persistentes para banco e redis
- [ ] Backup automatizado do PostgreSQL (cron + pg_dump)

### Sprint 17 - Seguranca e Polish

- [ ] Rate limiting na API propria
- [ ] Logs estruturados (Serilog → arquivo/seq)
- [ ] Monitoramento basico (uptime, erros, latencia)
- [ ] Tratamento de erros global + paginas de erro no frontend
- [ ] Revisao de UX em todas as telas
- [ ] Responsividade final (tablet/mobile para consultas rapidas)
- [ ] Onboarding flow (primeira conexao com marketplace)
- [ ] Documentacao tecnica basica (como rodar, variaveis de ambiente)

---

## Resumo do Roadmap

| Fase | Status | Entrega |
|------|--------|---------|
| 0 - Fundacao | ✅ Concluida | Design system, backend completo, 40+ endpoints, frontend conectado |
| 1 - MVP Core | ⬜ Proxima | Autenticacao JWT, OAuth ML, webhooks, pedidos reais |
| 2 - Financeiro | ⬜ Pendente | Motor de custos real, relatorios, conciliacao, alertas |
| 3 - Estoque/Full | ⬜ Pendente | Gestao estoque, integracao ML Full, armazenagem |
| 4 - Marketing | ⬜ Pendente | Mercado Ads, ROI real, promocoes |
| 5 - Multi-Marketplace | ⬜ Pendente | Amazon, Shopee, sync multi-canal |
| 6 - Pos-Venda | ⬜ Pendente | Mensageria unificada, reclamacoes, templates |
| 7 - Go-Live | ⬜ Pendente | Testes, Docker, seguranca, polish, documentacao |

---

## Dependencias Externas

| Item | Quando precisa | Acao necessaria |
|------|---------------|-----------------|
| Conta vendedor ML | Fase 1 | Criar em mercadolivre.com.br |
| App no DevCenter ML | Fase 1 | Registrar em developers.mercadolivre.com.br |
| CNPJ / MEI | Antes de vender | Necessario para NF-e e ML Coleta |
| Conta Amazon Seller | Fase 5 | Registrar em sellercentral.amazon.com.br |
| Conta Shopee Seller | Fase 5 | Registrar em seller.shopee.com.br |
| Dominio perushop.com.br | Fase 7 | Ja possui |

---

## Marcos de Validacao (Checkpoints)

| Marco | Criterio de aceite | Status |
|-------|-------------------|--------|
| **M0 - Fundacao** | Projeto estruturado, backend funcional, frontend conectado | ✅ Concluido |
| **M1 - Conexao** | Sistema conectado ao ML, importando anuncios existentes | ⬜ |
| **M2 - Primeira venda rastreada** | Pedido recebido via webhook com calculo de lucro | ⬜ |
| **M3 - Dashboard funcional** | Metricas de vendas/lucro visiveis com dados reais | ⬜ |
| **M4 - Financeiro completo** | Decomposicao total de custos, relatorio exportavel | ⬜ |
| **M5 - Estoque operacional** | Entrada de mercadoria reflete no ML automaticamente | ⬜ |
| **M6 - Multi-marketplace** | Venda na Amazon/Shopee atualiza estoque no ML | ⬜ |
| **M7 - Go-Live** | Sistema em producao, uso diario real | ⬜ |

---

## Como rodar o sistema atualmente

```bash
# 1. Iniciar PostgreSQL e Redis
docker run -d --name perushophub-db -p 5432:5432 -e POSTGRES_PASSWORD=dev postgres:16
docker run -d --name perushophub-redis -p 6379:6379 redis:7-alpine

# 2. Aplicar migrations (cria schema + seed data)
dotnet ef database update --project src/PeruShopHub.Infrastructure --startup-project src/PeruShopHub.API

# 3. Iniciar a API (http://localhost:5000)
dotnet run --project src/PeruShopHub.API

# 4. Iniciar o Worker (jobs em background)
dotnet run --project src/PeruShopHub.Worker

# 5. Iniciar o frontend Angular (http://localhost:4200)
cd src/PeruShopHub.Web && npm install && npx ng serve

# Swagger: http://localhost:5000/swagger
# Health: http://localhost:5000/health
```
