# Backup e Restore — PeruShopHub

## Visão Geral

O sistema de backup roda no container `backup` e executa automaticamente:

| Operação | Horário | Descrição |
|----------|---------|-----------|
| Backup completo | 03:00 UTC diário | `pg_dump` compactado com gzip |
| Verificação de restore | 04:00 UTC domingos | Restaura em banco temporário e valida |
| Monitoramento de idade | A cada hora | Alerta se backup > 25 horas |
| Monitoramento de sistema | A cada 5 min | Disco, Redis, saúde da API |

## Backups Automáticos

### O que é incluído

- **Banco de dados PostgreSQL** — dump completo via `pg_dump`
- **Retenção** — backups antigos são removidos automaticamente conforme política

### Verificar backups existentes

```bash
# Listar backups
docker compose -f docker-compose.prod.yml exec backup ls -lah /backups/

# Verificar tamanho total
docker compose -f docker-compose.prod.yml exec backup du -sh /backups/
```

### Verificar logs de backup

```bash
docker compose -f docker-compose.prod.yml logs backup --tail=50
```

## Backup Manual

### Banco de dados

```bash
# Backup completo
docker compose -f docker-compose.prod.yml exec db \
  pg_dump -U perushophub -d perushophub -Fc -f /tmp/manual-backup.dump

# Copiar para o host
docker compose -f docker-compose.prod.yml cp db:/tmp/manual-backup.dump ./manual-backup.dump
```

### Volumes Docker

```bash
# Listar volumes
docker volume ls | grep perushophub

# Backup do volume de uploads
docker run --rm \
  -v perushophub_uploads:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/uploads-backup.tar.gz -C /data .

# Backup do volume de data protection keys
docker run --rm \
  -v perushophub_dataprotection:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/dataprotection-backup.tar.gz -C /data .
```

## Procedimento de Restore

### 1. Parar a aplicação

```bash
docker compose -f docker-compose.prod.yml stop api worker
```

### 2. Restaurar o banco de dados

**A partir de um dump compactado (.gz):**

```bash
# Descompactar se necessário
gunzip backup-file.sql.gz

# Dropar e recriar o banco
docker compose -f docker-compose.prod.yml exec db \
  psql -U perushophub -c "DROP DATABASE IF EXISTS perushophub;"
docker compose -f docker-compose.prod.yml exec db \
  psql -U perushophub -c "CREATE DATABASE perushophub OWNER perushophub;"

# Restaurar
docker compose -f docker-compose.prod.yml exec -T db \
  psql -U perushophub -d perushophub < backup-file.sql
```

**A partir de um dump custom format (.dump):**

```bash
# Copiar dump para o container
docker compose -f docker-compose.prod.yml cp manual-backup.dump db:/tmp/

# Restaurar (--clean dropa objetos antes de recriar)
docker compose -f docker-compose.prod.yml exec db \
  pg_restore -U perushophub -d perushophub --clean --if-exists /tmp/manual-backup.dump
```

### 3. Restaurar volumes (se necessário)

```bash
# Restaurar uploads
docker run --rm \
  -v perushophub_uploads:/data \
  -v $(pwd):/backup \
  alpine sh -c "rm -rf /data/* && tar xzf /backup/uploads-backup.tar.gz -C /data"
```

### 4. Reiniciar a aplicação

```bash
docker compose -f docker-compose.prod.yml start api worker

# Verificar saúde
curl -f http://localhost/health
```

## Verificação de Backup

O sistema executa verificação automática todo domingo às 04:00 UTC:

1. Cria um banco temporário
2. Restaura o backup mais recente
3. Executa queries de validação (contagem de tabelas, integridade)
4. Remove o banco temporário
5. Envia alerta via webhook se a verificação falhar

### Verificação manual

```bash
# Executar script de verificação
docker compose -f docker-compose.prod.yml exec backup /scripts/verify-restore.sh
```

## Backup Offsite (Opcional)

Para enviar backups para armazenamento externo (S3/Backblaze B2), configure as variáveis:

```env
OFFSITE_BUCKET=s3://perushophub-backups
OFFSITE_ENDPOINT=https://s3.us-west-001.backblazeb2.com
AWS_ACCESS_KEY_ID=sua-chave
AWS_SECRET_ACCESS_KEY=seu-secret
```

Os backups serão enviados automaticamente após cada backup diário.

## Alertas

Se `ALERT_WEBHOOK` estiver configurado, o sistema envia alertas para:

- Falha no backup diário
- Falha na verificação de restore
- Backup com mais de 25 horas de idade
- Uso de disco acima do threshold (padrão 80%)
- Uso de memória Redis acima do threshold (padrão 80%)
- Falha no health check da API

### Configurar webhook no Slack

1. Crie um [Incoming Webhook](https://api.slack.com/messaging/webhooks) no Slack
2. Configure `ALERT_WEBHOOK=https://hooks.slack.com/services/...` no `.env`
3. Reinicie o container de backup

### Configurar webhook no Discord

1. Nas configurações do canal, vá em **Integrações > Webhooks**
2. Crie um webhook e copie a URL
3. Configure `ALERT_WEBHOOK=https://discord.com/api/webhooks/...` no `.env`
4. Reinicie o container de backup
