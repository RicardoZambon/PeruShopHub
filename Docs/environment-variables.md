# Variáveis de Ambiente — PeruShopHub

## Variáveis Obrigatórias (Produção)

Estas variáveis **devem** ser configuradas no arquivo `.env` do VPS.

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `DOMAIN` | Domínio do servidor | `perushophub.com.br` |
| `IMAGE_TAG` | Tag das imagens Docker | `latest` ou SHA do commit |
| `POSTGRES_PASSWORD` | Senha do PostgreSQL | `openssl rand -base64 32` |
| `REDIS_PASSWORD` | Senha do Redis | `openssl rand -base64 32` |
| `JWT_SECRET` | Secret para tokens JWT (mín. 32 caracteres) | `openssl rand -base64 48` |

## Variáveis Opcionais

| Variável | Descrição | Default |
|----------|-----------|---------|
| `SENTRY_DSN` | DSN do Sentry para tracking de erros | _(desabilitado)_ |
| `EMAIL_API_KEY` | API key do serviço de e-mail | _(desabilitado)_ |
| `ML_APP_ID` | App ID do Mercado Livre | _(desabilitado)_ |
| `ML_SECRET_KEY` | Secret Key do Mercado Livre | _(desabilitado)_ |
| `ALERT_WEBHOOK` | Webhook Slack/Discord para alertas de monitoramento | _(desabilitado)_ |
| `GITHUB_REPOSITORY_OWNER` | Owner do repositório (para registry de imagens) | `ricardozambon` |

## Configuração da API (`appsettings.json`)

Estas configurações podem ser sobrescritas via variáveis de ambiente usando o formato `Section__Key`.

### Conexões

| Configuração | Variável de Ambiente | Default (Dev) |
|-------------|---------------------|---------------|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | `Host=localhost;Port=5432;Database=perushophub;Username=perushophub;Password=perushophub_secret` |
| `ConnectionStrings:Redis` | `ConnectionStrings__Redis` | `localhost:6379` |

### JWT / Autenticação

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `Jwt:Secret` | `Jwt__Secret` | _(obrigatório, mín. 32 chars)_ |
| `Jwt:Issuer` | `Jwt__Issuer` | `PeruShopHub` |
| `Jwt:Audience` | `Jwt__Audience` | `PeruShopHub` |
| `Jwt:AccessTokenExpirationMinutes` | `Jwt__AccessTokenExpirationMinutes` | `15` |
| `Jwt:RefreshTokenExpirationDays` | `Jwt__RefreshTokenExpirationDays` | `7` |

### Rate Limiting

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `RateLimiting:RequestsPerMinute` | `RateLimiting__RequestsPerMinute` | `100` |
| `RateLimiting:AuthMaxAttemptsPerMinute` | `RateLimiting__AuthMaxAttemptsPerMinute` | `5` |
| `RateLimiting:AuthWindowSeconds` | `RateLimiting__AuthWindowSeconds` | `60` |

### CORS

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | `http://localhost:4200` |

### Mercado Livre

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `Marketplaces:MercadoLivre:ClientId` | `Marketplaces__MercadoLivre__ClientId` | _(vazio)_ |
| `Marketplaces:MercadoLivre:ClientSecret` | `Marketplaces__MercadoLivre__ClientSecret` | _(vazio)_ |
| `Marketplaces:MercadoLivre:RedirectUri` | `Marketplaces__MercadoLivre__RedirectUri` | `http://localhost:4200/oauth-callback` |
| `Webhooks:MercadoLivre:AllowedIPs` | `Webhooks__MercadoLivre__AllowedIPs__0` | _(vazio)_ |

### Health Checks

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `HealthChecks:EvaluationIntervalSeconds` | `HealthChecks__EvaluationIntervalSeconds` | `30` |

## Configuração do Worker (`appsettings.json`)

| Configuração | Variável de Ambiente | Default |
|-------------|---------------------|---------|
| `Workers:StockAlert:IntervalMinutes` | `Workers__StockAlert__IntervalMinutes` | `15` |
| `Workers:NotificationCleanup:IntervalHours` | `Workers__NotificationCleanup__IntervalHours` | `24` |
| `Workers:NotificationCleanup:RetentionDays` | `Workers__NotificationCleanup__RetentionDays` | `30` |

## Variáveis do Container de Backup

| Variável | Descrição | Default |
|----------|-----------|---------|
| `PGHOST` | Host do PostgreSQL | `db` |
| `PGPORT` | Porta do PostgreSQL | `5432` |
| `PGDATABASE` | Nome do banco | `perushophub` |
| `PGUSER` | Usuário do banco | `perushophub` |
| `PGPASSWORD` | Senha do banco | _(obrigatório)_ |
| `BACKUP_DIR` | Diretório de backups no container | `/backups` |
| `REDIS_HOST` | Host do Redis | `redis` |
| `REDIS_PORT` | Porta do Redis | `6379` |
| `REDIS_PASSWORD` | Senha do Redis | _(obrigatório)_ |
| `HEALTH_URL` | URL do health check da API | `http://api:5000/health` |
| `DISK_THRESHOLD_PERCENT` | Limite de uso de disco para alerta | `80` |
| `REDIS_THRESHOLD_PERCENT` | Limite de uso de memória Redis para alerta | `80` |
| `ALERT_WEBHOOK` | URL do webhook para alertas | _(desabilitado)_ |

### Backup Offsite (Opcional)

| Variável | Descrição |
|----------|-----------|
| `OFFSITE_BUCKET` | Bucket S3 (ex: `s3://perushophub-backups`) |
| `OFFSITE_ENDPOINT` | Endpoint S3 (ex: `https://s3.us-west-001.backblazeb2.com`) |
| `AWS_ACCESS_KEY_ID` | Chave de acesso AWS/S3 |
| `AWS_SECRET_ACCESS_KEY` | Secret de acesso AWS/S3 |

## GitHub Actions Secrets

Estes secrets devem ser configurados no repositório GitHub para deploy automático:

| Secret | Descrição |
|--------|-----------|
| `VPS_HOST` | IP ou hostname do servidor de produção |
| `VPS_USER` | Usuário SSH com permissão Docker |
| `VPS_SSH_KEY` | Chave SSH privada (Ed25519 ou RSA) |
