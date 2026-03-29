# Troubleshooting — PeruShopHub

## Problemas Comuns

### API não inicia

**Sintoma:** Container `api` reinicia em loop ou retorna erro 502.

**Verificar logs:**
```bash
docker compose -f docker-compose.prod.yml logs api --tail=50
```

**Causas comuns:**

1. **JWT Secret muito curto** — Deve ter no mínimo 32 caracteres.
   ```
   System.ArgumentOutOfRangeException: JWT secret must be at least 32 characters
   ```
   **Solução:** Gere um novo secret: `openssl rand -base64 48`

2. **PostgreSQL não está pronto** — A API depende do health check do banco.
   ```bash
   docker compose -f docker-compose.prod.yml logs db --tail=20
   ```
   **Solução:** Aguarde o banco iniciar ou verifique a senha no `.env`.

3. **Redis inacessível** — Verifique se a senha bate com o `.env`.
   ```bash
   docker compose -f docker-compose.prod.yml exec redis redis-cli -a $REDIS_PASSWORD ping
   ```

### Erro 502 Bad Gateway

**Sintoma:** Nginx retorna 502 ao acessar a aplicação.

**Causas:**
- Container `api` não está rodando
- API não está escutando na porta 5000

**Solução:**
```bash
# Verificar se a API está respondendo internamente
docker compose -f docker-compose.prod.yml exec web curl -f http://api:5000/health

# Verificar se todos os containers estão rodando
docker compose -f docker-compose.prod.yml ps
```

### Erro de CORS

**Sintoma:** Console do browser mostra `Access-Control-Allow-Origin` bloqueado.

**Solução:** Configure `Cors:AllowedOrigins` no ambiente da API:
```bash
# No docker-compose.prod.yml ou via .env
Cors__AllowedOrigins__0=https://seudominio.com
```

### Migrations não aplicadas

**Sintoma:** Erros 500 com `relation "XXX" does not exist`.

**Solução:** A API aplica migrations automaticamente ao iniciar. Se necessário, force manualmente:
```bash
docker compose -f docker-compose.prod.yml exec api \
  dotnet PeruShopHub.API.dll -- migrate
```

### Certificado SSL não renova

**Sintoma:** HTTPS para de funcionar após ~90 dias.

**Verificar:**
```bash
docker compose -f docker-compose.prod.yml run --rm certbot certificates
```

**Renovar manualmente:**
```bash
docker compose -f docker-compose.prod.yml run --rm certbot renew
docker compose -f docker-compose.prod.yml restart web
```

### Redis fica sem memória

**Sintoma:** Alertas de `REDIS_THRESHOLD_PERCENT` ou erros `OOM command not allowed`.

**Solução:**
```bash
# Verificar uso de memória
docker compose -f docker-compose.prod.yml exec redis redis-cli -a $REDIS_PASSWORD info memory

# Limpar cache manualmente (dados de sessão serão perdidos)
docker compose -f docker-compose.prod.yml exec redis redis-cli -a $REDIS_PASSWORD FLUSHDB
```

### WebSocket/SignalR não conecta

**Sintoma:** Dashboard não atualiza em tempo real, console mostra erros de WebSocket.

**Causas:**
- Nginx não configurado para WebSocket upgrade
- Firewall bloqueando conexões long-lived

**Solução:** Verifique que o Nginx está usando o config de produção (`nginx-prod.conf`) que inclui headers de WebSocket upgrade para `/hubs/`.

### Backup não executa

**Sintoma:** Nenhum arquivo de backup em `/backups` ou alertas de backup antigo.

**Verificar:**
```bash
# Logs do container de backup
docker compose -f docker-compose.prod.yml logs backup --tail=50

# Verificar cron está rodando
docker compose -f docker-compose.prod.yml exec backup crontab -l

# Listar backups existentes
docker compose -f docker-compose.prod.yml exec backup ls -la /backups/
```

### Disco cheio

**Sintoma:** Alertas de disco ou containers não iniciam.

**Solução:**
```bash
# Verificar uso de disco
df -h

# Limpar imagens Docker não utilizadas
docker image prune -a

# Limpar logs antigos do Docker
docker system prune --volumes

# Verificar tamanho dos backups
docker compose -f docker-compose.prod.yml exec backup du -sh /backups/*
```

### Rate limit atingido na autenticação

**Sintoma:** HTTP 429 ao tentar login.

**Causa:** Mais de 5 tentativas de login por minuto do mesmo IP.

**Solução:** Aguardar 60 segundos. Se necessário, ajustar os limites:
```
RateLimiting__AuthMaxAttemptsPerMinute=10
RateLimiting__AuthWindowSeconds=60
```

## Verificações de Saúde

### Health Check da API

```bash
curl -f http://localhost/health
```

Retorna status dos componentes:
- **PostgreSQL** — conexão com o banco
- **Redis** — conexão com o cache
- **Disk space** — espaço em disco disponível

### Verificar todos os serviços

```bash
docker compose -f docker-compose.prod.yml ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
```

Todos devem mostrar status `Up` ou `Up (healthy)`.

## Logs

### Localização dos logs

| Serviço | Como acessar |
|---------|-------------|
| API | `docker compose logs api` |
| Worker | `docker compose logs worker` |
| Nginx | Volume `nginx_logs` ou `docker compose logs web` |
| PostgreSQL | `docker compose logs db` |
| Backup | `docker compose logs backup` |

### Filtrar logs por período

```bash
docker compose -f docker-compose.prod.yml logs --since="2h" api
docker compose -f docker-compose.prod.yml logs --since="2024-01-01" --until="2024-01-02" api
```

### Logs em tempo real

```bash
docker compose -f docker-compose.prod.yml logs -f api worker
```

## Rollback de Deploy

Se um deploy causar problemas, volte para a versão anterior:

```bash
# Identificar a tag anterior
docker images ghcr.io/*/perushophub-api --format "{{.Tag}}"

# Alterar IMAGE_TAG no .env para a tag anterior
nano .env

# Recriar containers
docker compose -f docker-compose.prod.yml up -d
```

O workflow de deploy do GitHub Actions já inclui rollback automático quando o health check falha.
