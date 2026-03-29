# Backup & Recovery — PeruShopHub

## Visão Geral

O sistema de backup automatizado realiza dumps diários do PostgreSQL com compressão, aplica política de retenção, sincroniza offsite (S3/B2), e verifica integridade semanalmente.

## Arquitetura

```
┌─────────────┐    pg_dump     ┌───────────────┐    aws s3 sync    ┌────────────┐
│  PostgreSQL  │ ────────────> │  /backups/     │ ───────────────> │  S3 / B2   │
│  (db)        │               │  daily/        │                  │  (offsite) │
└─────────────┘               │  weekly/       │                  └────────────┘
                               │  monthly/      │
                               └───────────────┘
```

### Schedule (UTC)

| Horário | Frequência | Script | Descrição |
|---------|-----------|--------|-----------|
| 03:00 | Diário | `backup.sh` | pg_dump comprimido + retenção + sync offsite |
| 04:00 | Domingo | `verify-restore.sh` | Restore em DB temporário + verificação |
| *:00 | Hora em hora | `monitor.sh` | Alerta se backup > 25h atrás |

### Política de Retenção

| Tipo | Quantidade | Promoção |
|------|-----------|----------|
| Daily | 7 | Sempre |
| Weekly | 4 | Domingos |
| Monthly | 3 | Dia 1 do mês |

## Configuração

### Variáveis de Ambiente (docker-compose.yml)

| Variável | Descrição | Default |
|----------|-----------|---------|
| `PGHOST` | Host do PostgreSQL | `db` |
| `PGPORT` | Porta | `5432` |
| `PGDATABASE` | Database | `perushophub` |
| `PGUSER` | Usuário | `perushophub` |
| `PGPASSWORD` | Senha | — (obrigatório) |
| `BACKUP_DIR` | Diretório local | `/backups` |
| `OFFSITE_BUCKET` | Bucket S3 | — (opcional) |
| `OFFSITE_ENDPOINT` | Endpoint S3-compatible | — (opcional, para B2) |
| `AWS_ACCESS_KEY_ID` | Credencial offsite | — |
| `AWS_SECRET_ACCESS_KEY` | Credencial offsite | — |
| `ALERT_WEBHOOK` | Webhook Slack/Discord | — (opcional) |

### Ativar Sync Offsite (Backblaze B2)

1. Crie um bucket no Backblaze B2
2. Gere Application Key com permissões de leitura/escrita
3. Configure no `docker-compose.yml`:

```yaml
backup:
  environment:
    OFFSITE_BUCKET: s3://nome-do-bucket
    OFFSITE_ENDPOINT: https://s3.us-west-001.backblazeb2.com
    AWS_ACCESS_KEY_ID: "sua-key-id"
    AWS_SECRET_ACCESS_KEY: "sua-secret-key"
```

### Ativar Alertas (Slack)

1. Crie um Incoming Webhook no Slack
2. Configure:

```yaml
backup:
  environment:
    ALERT_WEBHOOK: "https://hooks.slack.com/services/T.../B.../..."
```

## Procedimento de Recovery

### Recovery Completo (Disaster Recovery)

**Cenário**: Servidor perdido, reconstruindo do zero.

```bash
# 1. Subir infraestrutura sem dados
docker compose up -d db redis

# 2. Aguardar PostgreSQL ficar healthy
docker compose exec db pg_isready -U perushophub

# 3. Baixar backup offsite (se local não disponível)
aws s3 cp s3://perushophub-backups/daily/perushophub_YYYYMMDD_HHMMSS.sql.gz ./restore.sql.gz \
  --endpoint-url https://s3.us-west-001.backblazeb2.com

# 4. Restaurar o backup
gunzip -c restore.sql.gz | docker compose exec -T db \
  psql -U perushophub -d perushophub

# 5. Verificar integridade
docker compose exec db psql -U perushophub -d perushophub \
  -c "SELECT schemaname, tablename FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename;"

docker compose exec db psql -U perushophub -d perushophub \
  -c "SELECT 'Tenants' as t, COUNT(*) FROM \"Tenants\"
      UNION ALL SELECT 'Users', COUNT(*) FROM \"Users\"
      UNION ALL SELECT 'Products', COUNT(*) FROM \"Products\"
      UNION ALL SELECT 'Orders', COUNT(*) FROM \"Orders\";"

# 6. Subir restante dos serviços
docker compose up -d
```

### Recovery Parcial (Point-in-Time)

**Cenário**: Restaurar dados de uma data específica.

```bash
# 1. Listar backups disponíveis
ls -la /backups/daily/
ls -la /backups/weekly/
ls -la /backups/monthly/

# 2. Restaurar em banco temporário para inspeção
docker compose exec db createdb -U perushophub perushophub_temp

gunzip -c /backups/daily/perushophub_20260329_030000.sql.gz | \
  docker compose exec -T db psql -U perushophub -d perushophub_temp

# 3. Extrair dados necessários do banco temporário
docker compose exec db psql -U perushophub -d perushophub_temp \
  -c "COPY (SELECT * FROM \"Orders\" WHERE ...) TO STDOUT WITH CSV HEADER;" > extract.csv

# 4. Limpar banco temporário
docker compose exec db dropdb -U perushophub perushophub_temp
```

### Backup Manual (Antes de Manutenção)

```bash
# Executar backup sob demanda
docker compose exec backup /usr/local/bin/backup/backup.sh

# Verificar que foi criado
docker compose exec backup ls -la /backups/daily/
```

### Verificação Manual

```bash
# Executar verificação de restore manualmente
docker compose exec backup /usr/local/bin/backup/verify-restore.sh

# Checar timestamp do último backup bem-sucedido
docker compose exec backup cat /backups/.last_backup_timestamp
docker compose exec backup cat /backups/.last_verify_timestamp
```

## Troubleshooting

### Backup falhou

1. Verifique logs: `docker compose logs backup`
2. Verifique conectividade: `docker compose exec backup pg_isready -h db -U perushophub`
3. Verifique espaço em disco: `docker compose exec backup df -h /backups`
4. Execute manualmente: `docker compose exec backup /usr/local/bin/backup/backup.sh`

### Sync offsite falhou

1. Verifique credenciais AWS/B2
2. Teste conectividade: `docker compose exec backup aws s3 ls ${OFFSITE_BUCKET} --endpoint-url ${OFFSITE_ENDPOINT}`
3. Verifique que o bucket existe e tem permissões corretas

### Monitor alertando

1. Verifique se o cron está rodando: `docker compose exec backup ps aux`
2. Verifique timestamp: `docker compose exec backup cat /backups/.last_backup_timestamp`
3. Se backup não rodou, execute manualmente e investigue logs
