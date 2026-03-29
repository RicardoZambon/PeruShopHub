# Guia de Deploy — PeruShopHub

## Visão Geral

O deploy é automatizado via GitHub Actions (`.github/workflows/deploy.yml`).
Fluxo: **Build → Push GHCR → SSH → Docker Compose → Health Check**.

## Pré-requisitos no VPS

### 1. Software necessário

```bash
# Docker + Docker Compose
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER

# Certbot (para SSL inicial, depois gerenciado via container)
sudo apt install certbot
```

### 2. Arquivo `.env`

Criar `~/perushophub/.env` no VPS (nunca commitado no repositório):

```env
DOMAIN=seudominio.com
IMAGE_TAG=latest
POSTGRES_PASSWORD=senha-forte-aqui
REDIS_PASSWORD=senha-forte-aqui
JWT_SECRET=segredo-jwt-minimo-32-caracteres-aqui
SENTRY_DSN=https://xxx@sentry.io/xxx
EMAIL_API_KEY=re_xxx
ML_APP_ID=123456
ML_SECRET_KEY=xxx
GITHUB_REPOSITORY_OWNER=RicardoZambon
```

### 3. Secrets no GitHub

Configurar em **Settings → Secrets → Actions**:

| Secret | Descrição |
|--------|-----------|
| `VPS_HOST` | IP ou hostname do VPS |
| `VPS_USER` | Usuário SSH (ex: `deploy`) |
| `VPS_SSH_KEY` | Chave privada SSH (Ed25519 recomendada) |

### 4. GitHub Environment

Criar environment **production** em **Settings → Environments** para proteção adicional (reviewers opcionais).

## Triggers de Deploy

### Tag (automático)

```bash
git tag v1.0.0
git push origin v1.0.0
```

### Manual (workflow_dispatch)

**Actions → Deploy to VPS → Run workflow** — pode especificar a tag da imagem.

## SSL/TLS com Let's Encrypt

### Certificado inicial

```bash
# No VPS, primeira vez (antes do nginx SSL estar ativo):
cd ~/perushophub

# Subir nginx em modo HTTP-only temporariamente:
docker compose -f docker-compose.prod.yml up -d web

# Gerar certificado:
docker compose -f docker-compose.prod.yml run --rm certbot \
  certonly --webroot -w /var/www/certbot -d seudominio.com

# Reiniciar nginx para ativar SSL:
docker compose -f docker-compose.prod.yml restart web
```

### Renovação automática

O container `certbot` renova automaticamente a cada 12h. Para renovação manual:

```bash
docker compose -f docker-compose.prod.yml run --rm certbot renew
docker compose -f docker-compose.prod.yml restart web
```

## Rollback

### Opção 1: Reverter para tag anterior

```bash
# No VPS:
cd ~/perushophub
export IMAGE_TAG=v1.0.0  # tag anterior que funcionava
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

### Opção 2: Via GitHub Actions

Executar workflow manualmente com a tag da versão anterior.

### Opção 3: Imagens Docker locais

```bash
# Ver imagens disponíveis no VPS:
docker images | grep perushophub

# Usar uma imagem local específica (editar .env):
IMAGE_TAG=sha-abc1234
docker compose -f docker-compose.prod.yml up -d
```

## Verificação pós-deploy

```bash
# Health check
curl https://seudominio.com/health

# Status dos containers
docker compose -f docker-compose.prod.yml ps

# Logs
docker compose -f docker-compose.prod.yml logs --tail=50

# Logs de um serviço específico
docker compose -f docker-compose.prod.yml logs api --tail=100 -f
```

## Estrutura de Arquivos no VPS

```
~/perushophub/
├── .env                      # Variáveis de ambiente (não commitado)
├── docker-compose.prod.yml   # Copiado pelo deploy
└── docker/
    └── nginx-prod.conf       # Copiado pelo deploy
```

## Backup do Banco

Ver `Docs/guides/Backup.md` (US-082) para backup automatizado do PostgreSQL.

## Troubleshooting

### Container não inicia

```bash
docker compose -f docker-compose.prod.yml logs <service>
```

### Erro de certificado SSL

```bash
# Verificar certificado:
docker compose -f docker-compose.prod.yml run --rm certbot certificates

# Forçar renovação:
docker compose -f docker-compose.prod.yml run --rm certbot renew --force-renewal
docker compose -f docker-compose.prod.yml restart web
```

### Banco não conecta

```bash
# Verificar se db está saudável:
docker compose -f docker-compose.prod.yml ps db

# Testar conexão:
docker compose -f docker-compose.prod.yml exec db pg_isready -U perushophub
```
