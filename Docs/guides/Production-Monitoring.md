# Monitoramento de Produção — PeruShopHub

## Visão Geral

O PeruShopHub usa uma stack de monitoramento composta por:

- **Health Checks**: endpoints nativos do ASP.NET Core (`/health`, `/health/ready`, `/health/live`, `/health-ui`)
- **Sentry**: rastreamento de erros e performance
- **Serilog**: logs estruturados (JSON) com retenção de 30 dias
- **System Monitor**: script cron que monitora disco, Redis e uptime
- **UptimeRobot**: monitoramento externo de uptime (configuração manual)
- **PostgreSQL**: slow query logging (> 1s)
- **Nginx**: access/error logs com rotação diária (14 dias)

---

## 1. Uptime Externo (UptimeRobot)

### Configuração

1. Criar conta em [uptimerobot.com](https://uptimerobot.com)
2. Adicionar novo monitor:
   - **Tipo**: HTTP(s)
   - **URL**: `https://DOMAIN/health`
   - **Intervalo**: 60 segundos
   - **Timeout**: 30 segundos
3. Configurar alertas:
   - **Email**: email do operador
   - **Telegram** (opcional): criar bot via @BotFather, configurar webhook no UptimeRobot
   - **Slack** (opcional): usar webhook de integração

### Monitors Recomendados

| Monitor | URL | Intervalo | Tipo |
|---------|-----|-----------|------|
| API Health | `https://DOMAIN/health` | 60s | HTTP(s) - keyword "Healthy" |
| API Readiness | `https://DOMAIN/health/ready` | 300s | HTTP(s) |
| Frontend | `https://DOMAIN/` | 300s | HTTP(s) - status 200 |

---

## 2. Sentry — Regras de Alerta

### Configuração Inicial

1. Criar projeto no [sentry.io](https://sentry.io):
   - Plataforma: ASP.NET Core
   - Nome: `perushophub-api`
2. Copiar o DSN para a variável `SENTRY_DSN` no `.env` da VPS

### Regras de Alerta Recomendadas

Criar as seguintes regras em **Sentry > Alerts > Create Alert Rule**:

#### 2.1 Erros Críticos (Imediato)

- **Condição**: Número de eventos > 1 em 5 minutos
- **Filtro**: `level:error OR level:fatal`
- **Ação**: Email + Slack/Telegram
- **Nome**: `[Critical] Erros em Produção`

#### 2.2 Performance — Transações Lentas

- **Tipo**: Transaction Duration
- **Condição**: p95 de transações > 2000ms por 10 minutos
- **Ação**: Email
- **Nome**: `[Perf] Transações Lentas (p95 > 2s)`

#### 2.3 Pico de Erros

- **Condição**: Número de eventos > 50 em 1 hora
- **Ação**: Email + Slack/Telegram
- **Nome**: `[Spike] Pico de Erros`

#### 2.4 Novos Issues

- **Condição**: Nova issue detectada pela primeira vez
- **Ação**: Email
- **Nome**: `[New] Novo Erro Detectado`

### Configuração no Código

O Sentry já está integrado em `Program.cs` com:

```
TracesSampleRate = 0.2 (produção)  /  1.0 (desenvolvimento)
SendDefaultPii = false
MaxBreadcrumbs = 50
```

Erros 5xx são capturados automaticamente pelo `GlobalExceptionFilter`.

---

## 3. System Monitor (Disco + Redis)

O script `scripts/monitoring/system-monitor.sh` roda a cada 5 minutos via cron no container de backup e verifica:

| Check | Threshold | Severidade |
|-------|-----------|------------|
| Uso de disco | >= 80% | warning |
| Memória Redis | >= 80% | warning |
| Health endpoint | HTTP != 200 | critical |
| Redis connectivity | conexão falha | critical |

### Variáveis de Ambiente

```bash
DISK_THRESHOLD_PERCENT=80      # Alerta quando disco >= 80%
REDIS_THRESHOLD_PERCENT=80     # Alerta quando Redis >= 80%
REDIS_HOST=redis
REDIS_PORT=6379
REDIS_PASSWORD=<senha>
HEALTH_URL=http://api:5000/health
ALERT_WEBHOOK=<slack/discord webhook URL>
```

### Canais de Alerta

Os alertas são enviados via webhook compatível com Slack e Discord:

```bash
# Slack
ALERT_WEBHOOK=https://hooks.slack.com/services/T.../B.../xxx

# Discord
ALERT_WEBHOOK=https://discord.com/api/webhooks/.../...
```

---

## 4. PostgreSQL — Slow Query Log

Configurado em `docker/postgresql.conf`:

- Queries > 1 segundo são logadas automaticamente
- Lock waits > 1 segundo logados
- Uso de arquivos temporários logado (indica necessidade de mais `work_mem`)
- Checkpoints e autovacuum logados

### Consultar Slow Queries

```bash
# No container do PostgreSQL
docker compose exec db cat /var/lib/postgresql/data/log/postgresql-*.log | grep "duration:"

# Ou via pg_stat_statements (se habilitado)
SELECT query, calls, mean_exec_time, total_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 20;
```

---

## 5. Nginx — Logs com Rotação

### Formato do Log

```
$remote_addr - $remote_user [$time_local] "$request" $status $body_bytes_sent
"$http_referer" "$http_user_agent" "$http_x_forwarded_for" rt=$request_time
```

### Rotação

Configurada via `docker/logrotate-nginx.conf`:

- Rotação: diária
- Retenção: 14 dias
- Compressão: gzip (com delay de 1 dia)
- Sinal USR1 enviado ao Nginx após rotação

### Consultar Logs

```bash
# Acessar logs do Nginx
docker compose exec web cat /var/log/nginx/access.log

# Filtrar erros 5xx
docker compose exec web grep '" 5[0-9][0-9] ' /var/log/nginx/access.log

# Requests mais lentos
docker compose exec web awk '{print $NF, $7}' /var/log/nginx/access.log | sort -rn | head -20
```

---

## 6. Health Check UI

Acessível em `https://DOMAIN/health-ui` — dashboard visual que mostra:

- Status de cada health check (PostgreSQL, Redis, Disk)
- Histórico de status
- Intervalo de avaliação: 30 segundos (configurável em `appsettings.json`)

---

## 7. Logs Estruturados (Serilog)

### Formato

JSON compacto em `logs/perushophub-*.json` com rotação diária e retenção de 30 dias.

### Campos Enriquecidos

Cada log entry inclui:
- `CorrelationId`: ID único por request (header `X-Correlation-Id`)
- `TenantId`: ID do tenant autenticado
- `UserId`: ID do usuário
- `Endpoint`: path da request
- `StatusCode`: código HTTP da resposta
- `ElapsedMs`: tempo de processamento

### Consultar Logs

```bash
# Último log
docker compose exec api tail -f logs/perushophub-*.json | jq .

# Filtrar erros
docker compose exec api cat logs/perushophub-*.json | jq 'select(.Level == "Error")'

# Requests lentas (> 1s)
docker compose exec api cat logs/perushophub-*.json | jq 'select(.ElapsedMs > 1000)'
```

---

## Checklist de Setup

- [ ] Configurar `SENTRY_DSN` no `.env` da VPS
- [ ] Criar regras de alerta no Sentry (seção 2)
- [ ] Criar monitors no UptimeRobot (seção 1)
- [ ] Configurar `ALERT_WEBHOOK` no `.env` para Slack/Discord
- [ ] Verificar que `docker compose exec db psql -U perushophub -c "SHOW log_min_duration_statement;"` retorna `1000`
- [ ] Verificar `/health-ui` acessível no browser
