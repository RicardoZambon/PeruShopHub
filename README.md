# PeruShopHub

[![CI](https://github.com/RicardoZambon/PeruShopHub/actions/workflows/ci.yml/badge.svg)](https://github.com/RicardoZambon/PeruShopHub/actions/workflows/ci.yml)

Sistema centralizado de gestão multi-marketplace focado em **rastreamento real de lucratividade por venda**.

## O que é

O PeruShopHub calcula o lucro líquido real de cada venda considerando todos os custos: comissão do marketplace, taxas fixas, frete real, fulfillment, publicidade, impostos, custo do produto, embalagem e absorção de cupons. Começa com integração ao Mercado Livre e é arquitetado para expandir para Amazon, Shopee e outros.

## Arquitetura

```
┌─────────────────────────────────────────────────┐
│                    Nginx                        │
│              (Reverse Proxy + SSL)              │
└───────────┬───────────────────┬─────────────────┘
            │                   │
    ┌───────▼───────┐   ┌──────▼──────┐
    │   Angular 17  │   │  SignalR Hub │
    │   (Frontend)  │   │ (Real-time)  │
    └───────────────┘   └──────┬───────┘
                               │
                    ┌──────────▼──────────┐
                    │   ASP.NET Core 9    │
                    │      (Web API)      │
                    └─────┬─────┬────────┘
                          │     │
              ┌───────────┤     ├───────────┐
              │           │     │           │
      ┌───────▼──┐ ┌─────▼──┐ ┌▼────────┐ ┌▼──────────┐
      │PostgreSQL│ │ Redis  │ │ Worker  │ │  Backup   │
      │   16     │ │   7    │ │ (.NET)  │ │ (pg_dump) │
      └──────────┘ └────────┘ └─────────┘ └───────────┘
```

**Padrão:** Monolito Modular com Adapter Pattern por marketplace.

| Camada | Tecnologia |
|--------|-----------|
| Backend | C# / ASP.NET Core 9, EF Core 8, PostgreSQL 16 |
| Cache/Fila | Redis 7 |
| Frontend | Angular 17+, Angular Material |
| Auth | JWT + Refresh Token |
| Deploy | Docker Compose, GitHub Actions, Nginx |

## Quick Start (Desenvolvimento)

```bash
# 1. Clonar
git clone https://github.com/RicardoZambon/PeruShopHub.git
cd PeruShopHub

# 2. Subir todos os serviços (API, Worker, Angular, PostgreSQL, Redis, Nginx)
docker compose up -d

# 3. Acessar
# Frontend: http://localhost
# API:      http://localhost/api
# Health:   http://localhost/health
```

**Ou rodar individualmente:**

```bash
# Backend
dotnet run --project src/PeruShopHub.API

# Worker
dotnet run --project src/PeruShopHub.Worker

# Frontend
cd src/PeruShopHub.Web && ng serve
```

## Testes

```bash
# Todos os testes
dotnet test

# Apenas unit tests
dotnet test tests/PeruShopHub.UnitTests

# Apenas integration tests
dotnet test tests/PeruShopHub.IntegrationTests

# Teste específico
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Deploy (Produção)

```bash
# 1. Configurar variáveis de ambiente
cp .env.example .env
nano .env  # preencher as variáveis obrigatórias

# 2. Subir com compose de produção
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

# 3. Configurar SSL
docker compose -f docker-compose.prod.yml run --rm certbot \
  certonly --webroot -w /var/www/certbot -d seudominio.com
```

O deploy automatizado via GitHub Actions é acionado por push de tags `v*` ou manualmente via `workflow_dispatch`.

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [Deployment](Docs/deployment.md) | Setup do VPS, comandos Docker, deploy via CI/CD |
| [Environment Variables](Docs/environment-variables.md) | Todas as variáveis com descrições e defaults |
| [Troubleshooting](Docs/troubleshooting.md) | Problemas comuns e soluções |
| [Backup & Restore](Docs/backup-restore.md) | Procedimentos de backup e restauração |
| [Architecture](Docs/Architecture.md) | Arquitetura técnica, modelo de dados, convenções |
| [Design System](Docs/PeruShopHub-Design-System.md) | Tokens, componentes, responsividade |
| [Roadmap](Docs/ROADMAP.md) | Progresso, fases, próximos passos |
| [Security Audit](Docs/Security-Audit.md) | Auditoria de segurança e remediações |

### Guias

| Guia | Descrição |
|------|-----------|
| [Mercado Livre API](Docs/guides/Mercado-Livre-API.md) | OAuth, Items, Orders, Shipping, Webhooks |
| [Financial Model](Docs/guides/Financial-Model.md) | Decomposição de custos, motor de lucratividade |
| [Stock Management](Docs/guides/Stock-Management.md) | Inventário, sincronização ML, reconciliação |
| [Multi-Tenancy](Docs/guides/Multi-Tenancy.md) | Banco compartilhado, filtros, middleware |
| [Authentication](Docs/guides/Authentication.md) | JWT, refresh tokens, RBAC |

## Estrutura do Projeto

```
src/
├── PeruShopHub.Core/            # Domínio: entidades, interfaces, value objects
├── PeruShopHub.Infrastructure/  # Adaptadores, persistência, HTTP clients
├── PeruShopHub.Application/     # Casos de uso, serviços
├── PeruShopHub.API/             # Controllers, webhooks, SignalR hubs
├── PeruShopHub.Worker/          # Jobs em background
└── PeruShopHub.Web/             # Frontend Angular
tests/
├── PeruShopHub.UnitTests/
└── PeruShopHub.IntegrationTests/
docker/
├── Dockerfile.api / .worker / .web / .backup
├── nginx.conf / nginx-prod.conf
└── postgresql.conf
```

## Licença

Proprietário — todos os direitos reservados.
