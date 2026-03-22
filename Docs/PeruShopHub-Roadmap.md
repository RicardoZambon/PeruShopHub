# PeruShop Hub - Roadmap MVP → Go-Live

## Stack Definitivo

| Camada | Tecnologia | Versao |
|--------|-----------|--------|
| Backend | C# / ASP.NET Core Web API | .NET 8+ |
| Frontend | Angular | 17+ |
| UI Components | Angular Material ou PrimeNG | - |
| Banco de dados | PostgreSQL | 16 |
| Cache / Fila | Redis | 7+ |
| ORM | Entity Framework Core | 8+ |
| Real-time | SignalR | - |
| Containerizacao | Docker + Docker Compose | - |
| Reverse Proxy | Nginx | - |
| CI/CD | GitHub Actions | - |

---

## Pre-requisitos (antes de codar)

### Conta Mercado Livre

- [ ] Criar conta de vendedor no Mercado Livre (se nao tiver)
- [ ] Acessar DevCenter: https://developers.mercadolivre.com.br/devcenter
- [ ] Criar aplicacao (App ID + Secret Key + Redirect URI)
- [ ] Anotar: `client_id`, `client_secret`, `redirect_uri`
- [ ] Criar usuarios de teste (POST /users/test_user) para desenvolvimento

### Infraestrutura Local (Docker)

- [ ] PostgreSQL container rodando
- [ ] Redis container rodando
- [ ] Nginx container (reverse proxy) configurado
- [ ] Volumes persistentes para banco e redis

---

## Fase 0 - Fundacao (Semana 1)

**Objetivo**: Projeto estruturado, rodando em Docker, com CI basico.

### Entregas

- [ ] Estrutura do projeto (monolito modular)
  ```
  PeruShopHub/
  ├── src/
  │   ├── PeruShopHub.Core/            # Dominio (entidades, interfaces, value objects)
  │   ├── PeruShopHub.Infrastructure/  # Adapters, persistencia, clients HTTP
  │   ├── PeruShopHub.Application/     # Casos de uso, services
  │   ├── PeruShopHub.API/             # Controllers, webhooks, SignalR hubs
  │   ├── PeruShopHub.Worker/          # Background jobs
  │   └── PeruShopHub.Web/             # Frontend Angular
  ├── tests/
  │   ├── PeruShopHub.UnitTests/
  │   └── PeruShopHub.IntegrationTests/
  ├── docker/
  │   ├── Dockerfile.api
  │   ├── Dockerfile.worker
  │   ├── Dockerfile.web
  │   └── nginx.conf
  ├── docker-compose.yml
  ├── docker-compose.override.yml      # Config local (secrets, ports)
  └── .github/workflows/ci.yml
  ```
- [ ] Docker Compose com todos os servicos (API, Worker, Angular, PostgreSQL, Redis, Nginx)
- [ ] Migrations iniciais do banco (EF Core)
- [ ] Health checks em todos os containers
- [ ] Swagger/OpenAPI configurado
- [ ] Estrutura base do Angular (layout, roteamento, auth guard)

### Modelo de dados inicial

```
marketplace_connections   - Conexoes OAuth por marketplace
products                  - Cadastro master de produtos
product_costs             - Historico de custos por SKU
inventory                 - Estoque master
inventory_allocations     - Alocacao por marketplace
product_listings          - Mapeamento produto → anuncio em cada marketplace
```

---

## Fase 1 - MVP Core (Semanas 2-4)

**Objetivo**: Conectar ao Mercado Livre, receber pedidos, ver lucratividade basica.

### Sprint 1 (Semana 2) - Autenticacao + Produtos

Backend:
- [ ] OAuth 2.0 flow com Mercado Livre (authorize, token, refresh)
- [ ] Background worker para renovacao proativa de tokens
- [ ] Circuit breaker (Polly) para chamadas a API do ML
- [ ] Rate limiter (18k req/hora)
- [ ] CRUD de produtos (SKU master)
- [ ] Registro de custo de aquisicao por produto
- [ ] Sync de anuncios existentes do ML (GET /users/{id}/items)

Frontend:
- [ ] Tela de login / setup da conexao ML (OAuth redirect)
- [ ] Tela de produtos (listagem, cadastro, edicao)
- [ ] Campo de custo de aquisicao por produto
- [ ] Status da conexao ML (ativo, expirando, erro)

### Sprint 2 (Semana 3) - Pedidos + Financeiro Basico

Backend:
- [ ] Webhook receiver para pedidos (`orders_v2`)
- [ ] Processamento assincrono de webhooks (Redis queue)
- [ ] Sync de pedidos (GET /orders/search)
- [ ] Mapeamento de pedido → produto interno
- [ ] Consulta de taxas por pedido (Billing API)
- [ ] Calculo automatico: receita - comissao - taxa fixa - custo produto = lucro

Frontend:
- [ ] Tela de pedidos (listagem com filtros por status, data)
- [ ] Detalhe do pedido (itens, comprador, envio, custos)
- [ ] Indicador de lucro por pedido (verde/amarelo/vermelho)

### Sprint 3 (Semana 4) - Dashboard + Perguntas

Backend:
- [ ] Endpoints de metricas (vendas, receita, lucro por periodo)
- [ ] Webhook receiver para perguntas (`questions`)
- [ ] Listar e responder perguntas via API
- [ ] Endpoint de resumo financeiro

Frontend:
- [ ] Dashboard home:
  - Vendas hoje / semana / mes
  - Receita bruta vs lucro liquido
  - Top 5 produtos mais vendidos
  - Top 5 produtos mais lucrativos
- [ ] Tela de perguntas (listar, responder, marcar como lida)
- [ ] Notificacoes em tempo real (SignalR) para novas vendas e perguntas

### Entregavel do MVP

Sistema funcional que:
1. Conecta ao Mercado Livre via OAuth
2. Recebe pedidos automaticamente via webhook
3. Mostra lucro por venda (receita - comissao - custo produto)
4. Permite responder perguntas
5. Dashboard basico com metricas

---

## Fase 2 - Financeiro Completo (Semanas 5-7)

**Objetivo**: Rastreabilidade total de custos. O diferencial do sistema.

### Sprint 4 (Semana 5) - Decomposicao de Custos

Backend:
- [ ] Integracao completa com Billing API (periodos, documentos, summary)
- [ ] Decomposicao automatica por venda:
  - Comissao do marketplace
  - Taxa fixa
  - Custo de frete (seller)
  - Taxa de pagamento (Mercado Pago)
  - Custo do produto
  - Custo de embalagem (configuravel)
- [ ] Registro de custos adicionais manuais
- [ ] Historico de custos de produto (com effective_from/until)

Frontend:
- [ ] Detalhe financeiro do pedido (breakdown completo de custos)
- [ ] Configuracao de custos fixos (embalagem, por categoria)
- [ ] Edicao de custo de produto com historico

### Sprint 5 (Semana 6) - Relatorios e Conciliacao

Backend:
- [ ] View materializada de lucratividade por SKU
- [ ] Conciliacao: valor depositado ML vs valor esperado
- [ ] Identificacao de divergencias em comissoes
- [ ] Exportacao PDF (QuestPDF) e Excel (ClosedXML)
- [ ] Curva ABC por margem de lucro

Frontend:
- [ ] Tela de relatorios:
  - Lucratividade por SKU (tabela + grafico)
  - Lucratividade por periodo
  - Comparativo receita bruta vs lucro liquido
  - Curva ABC
- [ ] Tela de conciliacao financeira
- [ ] Exportacao de relatorios (PDF/Excel)

### Sprint 6 (Semana 7) - Alertas e Precificacao

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
- [ ] Central de notificacoes
- [ ] Calculadora de preco interativa
- [ ] Simulador de cenarios

---

## Fase 3 - Estoque e Fulfillment (Semanas 8-9)

**Objetivo**: Gestao completa de estoque, integracao com ML Full.

### Sprint 7 (Semana 8) - Gestao de Estoque

Backend:
- [ ] CRUD de movimentacoes de estoque (entrada, saida, ajuste)
- [ ] Historico completo de movimentacoes por SKU
- [ ] Atualizacao automatica de estoque no ML ao registrar entrada
- [ ] Webhook de items para detectar mudancas externas de estoque
- [ ] Reconciliacao periodica (worker compara estoque local vs ML)

Frontend:
- [ ] Tela de estoque (quantidades, status de sync)
- [ ] Registro de entrada de mercadoria (compra de fornecedor)
- [ ] Historico de movimentacoes
- [ ] Indicadores de estoque critico

### Sprint 8 (Semana 9) - Fulfillment (ML Full)

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

## Fase 4 - Marketing e Ads (Semana 10)

**Objetivo**: Integracao com Mercado Ads, ROI real por campanha.

### Sprint 9 (Semana 10)

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

## Fase 5 - Multi-Marketplace (Semanas 11-14)

**Objetivo**: Expandir para Amazon e Shopee.

### Sprint 10-11 (Semanas 11-12) - Amazon

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

### Sprint 12-13 (Semanas 13-14) - Shopee + Sync Multi-Canal

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

## Fase 6 - Pos-Venda e Mensageria (Semana 15)

**Objetivo**: Gestao completa de comunicacao com compradores.

### Sprint 14 (Semana 15)

Backend:
- [ ] Mensagens pos-venda (enviar/receber por marketplace)
- [ ] Gestao de reclamacoes e devolucoes
- [ ] Templates de resposta (configuráveis por tipo de situacao)
- [ ] Webhook de mensagens para notificacao em tempo real

Frontend:
- [ ] Inbox unificado (perguntas + mensagens pos-venda, todos os marketplaces)
- [ ] Templates de resposta rapida
- [ ] Historico de comunicacao por pedido/comprador
- [ ] Gestao de reclamacoes (timeline, acoes, evidencias)

---

## Fase 7 - Polish e Go-Live (Semanas 16-17)

**Objetivo**: Sistema pronto para uso real em producao.

### Sprint 15 (Semana 16) - Seguranca e Robustez

- [ ] Autenticacao do sistema (login, JWT, refresh)
- [ ] Criptografia de tokens OAuth em repouso (AES-256)
- [ ] Rate limiting na API propria
- [ ] Logs estruturados (Serilog → arquivo/seq)
- [ ] Health checks completos
- [ ] Backup automatizado do PostgreSQL (cron + pg_dump)
- [ ] Monitoramento basico (uptime, erros, latencia)
- [ ] Tratamento de erros global + paginas de erro no frontend

### Sprint 16 (Semana 17) - UX Final e Documentacao

- [ ] Revisao de UX em todas as telas
- [ ] Responsividade (funcionar em tablet/mobile para consultas rapidas)
- [ ] Onboarding flow (primeira conexao com marketplace)
- [ ] Configuracoes do sistema (dados da empresa, impostos, custos fixos)
- [ ] Tela "Sobre" com versao e info do sistema
- [ ] Documentacao tecnica basica (como rodar, variaveis de ambiente)

---

## Resumo do Roadmap

| Fase | Semanas | Entrega |
|------|---------|---------|
| 0 - Fundacao | 1 | Projeto estruturado, Docker, migrations |
| 1 - MVP Core | 2-4 | OAuth ML, pedidos, lucro basico, perguntas, dashboard |
| 2 - Financeiro | 5-7 | Custos detalhados, relatorios, conciliacao, alertas |
| 3 - Estoque/Full | 8-9 | Gestao estoque, integracao ML Full, armazenagem |
| 4 - Marketing | 10 | Mercado Ads, ROI real, promocoes |
| 5 - Multi-Marketplace | 11-14 | Amazon, Shopee, sync multi-canal |
| 6 - Pos-Venda | 15 | Mensageria unificada, reclamacoes, templates |
| 7 - Go-Live | 16-17 | Seguranca, polish, documentacao |

**Total estimado: ~17 semanas (4 meses)**

---

## Dependencias Externas

| Item | Quando precisa | Acao necessaria |
|------|---------------|-----------------|
| Conta vendedor ML | Fase 0 | Criar em mercadolivre.com.br |
| App no DevCenter ML | Fase 0 | Registrar em developers.mercadolivre.com.br |
| CNPJ / MEI | Antes de vender | Necessario para NF-e e ML Coleta |
| Conta Amazon Seller | Fase 5 | Registrar em sellercentral.amazon.com.br |
| Conta Shopee Seller | Fase 5 | Registrar em seller.shopee.com.br |
| Dominio perushop.com.br | Fase 7 | Ja possui |

---

## Marcos de Validacao (Checkpoints)

| Marco | Criterio de aceite |
|-------|-------------------|
| **M1 - Conexao** | Sistema conectado ao ML, importando anuncios existentes |
| **M2 - Primeira venda rastreada** | Pedido recebido via webhook com calculo de lucro |
| **M3 - Dashboard funcional** | Metricas de vendas/lucro visiveis no painel |
| **M4 - Financeiro completo** | Decomposicao total de custos, relatorio exportavel |
| **M5 - Estoque operacional** | Entrada de mercadoria reflete no ML automaticamente |
| **M6 - Multi-marketplace** | Venda na Amazon/Shopee atualiza estoque no ML |
| **M7 - Go-Live** | Sistema em producao, uso diario real |
