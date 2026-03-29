# Guia de Deploy — PeruShopHub

## Pré-requisitos

| Requisito | Versão Mínima |
|-----------|---------------|
| VPS (Ubuntu/Debian) | Ubuntu 22.04+ |
| Docker | 24.0+ |
| Docker Compose | v2.20+ |
| RAM | 2 GB (mínimo), 4 GB (recomendado) |
| Disco | 20 GB SSD |
| Domínio | Apontando para o IP do VPS |

## Arquitetura dos Containers

```
┌─────────────┐     ┌─────────────┐
│   Certbot    │     │    Web       │ ← Nginx (porta 80/443)
│  (SSL certs) │     │  (Angular)   │
└──────┬───────┘     └──────┬───────┘
       │                    │
       │              ┌─────▼──────┐
       └──────────────│    API     │ ← ASP.NET Core (porta 5000)
                      │  (.NET 9)  │
                      └─────┬──────┘
                            │
              ┌─────────────┼─────────────┐
              │             │             │
        ┌─────▼──────┐ ┌───▼────┐ ┌──────▼──────┐
        │ PostgreSQL  │ │ Redis  │ │   Worker    │
        │    16       │ │   7    │ │  (.NET 9)   │
        └─────────────┘ └────────┘ └─────────────┘
                                   ┌─────────────┐
                                   │   Backup    │
                                   │  (pg_dump)  │
                                   └─────────────┘
```

## Setup Inicial no VPS

### 1. Instalar Docker

```bash
# Atualizar pacotes
sudo apt update && sudo apt upgrade -y

# Instalar Docker
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Instalar Docker Compose plugin
sudo apt install docker-compose-plugin -y

# Verificar instalação
docker --version
docker compose version
```

### 2. Clonar o Repositório

```bash
cd /opt
sudo git clone https://github.com/RicardoZambon/PeruShopHub.git
cd PeruShopHub
```

### 3. Configurar Variáveis de Ambiente

Crie o arquivo `.env` na raiz do projeto (nunca commite este arquivo):

```bash
cp .env.example .env
nano .env
```

Preencha as variáveis obrigatórias. Veja [environment-variables.md](environment-variables.md) para detalhes completos.

**Gerar senhas seguras:**

```bash
# Senha para PostgreSQL e Redis
openssl rand -base64 32

# JWT Secret (mínimo 32 caracteres)
openssl rand -base64 48
```

### 4. Configurar SSL (Let's Encrypt)

```bash
# Primeira execução: obter certificado
docker compose -f docker-compose.prod.yml run --rm certbot \
  certonly --webroot -w /var/www/certbot -d seudominio.com

# Verificar certificado
docker compose -f docker-compose.prod.yml run --rm certbot certificates
```

O container `certbot` renova automaticamente a cada 12 horas.

### 5. Deploy

```bash
# Baixar imagens e iniciar
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

# Verificar saúde dos serviços
docker compose -f docker-compose.prod.yml ps
curl -f http://localhost/health
```

## Deploy via GitHub Actions

O deploy automatizado é configurado em `.github/workflows/deploy.yml`.

### Secrets Necessários no GitHub

| Secret | Descrição |
|--------|-----------|
| `VPS_HOST` | IP ou hostname do servidor |
| `VPS_USER` | Usuário SSH no servidor |
| `VPS_SSH_KEY` | Chave SSH privada para autenticação |

### Trigger

- **Automático:** push de tag `v*` (ex: `v1.0.0`)
- **Manual:** via `workflow_dispatch` no GitHub Actions

### Fluxo do Deploy

1. Build das imagens Docker (API, Worker, Web)
2. Push para GitHub Container Registry (`ghcr.io`)
3. SSH para o VPS
4. Pull das novas imagens
5. `docker compose up -d` com o compose de produção
6. Health check: 30 tentativas com 5s de intervalo
7. **Rollback automático** em caso de falha

## Comandos Docker Úteis

```bash
# Ver logs de um serviço
docker compose -f docker-compose.prod.yml logs -f api
docker compose -f docker-compose.prod.yml logs -f worker

# Reiniciar um serviço
docker compose -f docker-compose.prod.yml restart api

# Parar tudo
docker compose -f docker-compose.prod.yml down

# Atualizar imagens manualmente
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d

# Executar migrations manualmente
docker compose -f docker-compose.prod.yml exec api \
  dotnet PeruShopHub.API.dll -- migrate

# Acessar o banco de dados
docker compose -f docker-compose.prod.yml exec db \
  psql -U perushophub -d perushophub

# Verificar uso de disco dos volumes
docker system df -v
```

## Configuração do PostgreSQL

O arquivo `docker/postgresql.conf` otimiza o PostgreSQL para a VPS:

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| `shared_buffers` | 256 MB | ~25% da RAM disponível |
| `effective_cache_size` | 768 MB | ~75% da RAM |
| `work_mem` | 4 MB | Memória por operação de sort |
| `maintenance_work_mem` | 64 MB | Para VACUUM, CREATE INDEX |
| `wal_buffers` | 16 MB | Buffer de WAL |

Também habilita logging de queries lentas (>1s) e monitoramento de autovacuum.

## Configuração do Nginx

O Nginx em produção (`docker/nginx-prod.conf`) inclui:

- **HTTPS** com TLS 1.2/1.3 via Let's Encrypt
- **HTTP/2** habilitado
- **HSTS** com max-age de 2 anos
- **Headers de segurança** (CSP, X-Frame-Options, etc.)
- **Gzip** para text, CSS, JS, JSON, XML, SVG
- **Cache** de 1 ano para assets estáticos
- **WebSocket** proxy para SignalR (`/hubs/`)
- **Upload** máximo de 20 MB
- **Log rotation** via logrotate (14 dias de retenção)

## Monitoramento

O container `backup` executa monitoramento automático:

| Verificação | Intervalo | Alerta |
|-------------|-----------|--------|
| Saúde da API | 5 min | Webhook se `/health` falhar |
| Uso de disco | 5 min | Webhook se > 80% |
| Memória Redis | 5 min | Webhook se > 80% |
| Idade do backup | 1 hora | Webhook se > 25h sem backup |

Alertas são enviados para o webhook configurado em `ALERT_WEBHOOK` (Slack/Discord).
