# Auditoria de Segurança — PeruShopHub

**Data:** 2026-03-29
**Escopo:** OWASP Top 10 review, configurações de segurança, dependências

---

## Resumo Executivo

| Categoria | Status | Severidade |
|-----------|--------|-----------|
| SQL Injection | ✅ Seguro | — |
| XSS | ✅ Seguro | — |
| CSRF | ✅ N/A (JWT) | — |
| JWT Secret | ✅ Corrigido | Alta → Resolvido |
| HTTPS/HSTS | ✅ Implementado | Alta → Resolvido |
| CORS | ✅ Corrigido | Média → Resolvido |
| Rate Limiting Auth | ✅ Implementado | Alta → Resolvido |
| BCrypt Work Factor | ✅ Corrigido | Baixa → Resolvido |
| Security Headers | ✅ Implementado | Média → Resolvido |
| Dependências | ✅ Sem vulnerabilidades | — |

---

## Achados Detalhados

### 1. SQL Injection — SEGURO

**Severidade:** Nenhuma
**Status:** Todos os acessos a banco usam Entity Framework Core com queries parametrizadas. Nenhum `FromSqlRaw` com input de usuário encontrado. Duas chamadas `ExecuteSqlRawAsync` usam SQL hardcoded (`REFRESH MATERIALIZED VIEW`). Migrations usam dados estáticos.

### 2. XSS — SEGURO

**Severidade:** Nenhuma
**Status:** Angular sanitiza automaticamente bindings de template. Backend retorna JSON (não HTML renderizado). `GlobalExceptionFilter` nunca expõe stack traces ao cliente.

### 3. CSRF — N/A

**Severidade:** N/A
**Status:** Autenticação via JWT no header `Authorization` (não cookies). SPAs com token-based auth são inerentemente protegidas contra CSRF.

### 4. JWT Secret — CORRIGIDO

**Severidade original:** Alta
**Problema:** Secret placeholder em `appsettings.json` ("CHANGE-THIS-TO-A-SECURE-SECRET-AT-LEAST-32-CHARS") sem validação de tamanho.
**Remediação:**
- Validação no startup: secret deve ter ≥ 32 bytes (256 bits)
- App falha ao iniciar se secret for muito curto ou ausente
- Production deve usar variável de ambiente `Jwt__Secret`

### 5. HTTPS/HSTS — IMPLEMENTADO

**Severidade original:** Alta
**Problema:** Sem `UseHttpsRedirection()` nem `UseHsts()` no pipeline.
**Remediação:**
- `UseHsts()` adicionado para ambientes não-Development (max-age=30 dias default)
- `UseHttpsRedirection()` adicionado ao pipeline
- Nginx preparado com header HSTS comentado (ativar quando TLS for configurado)

### 6. CORS — CORRIGIDO

**Severidade original:** Média
**Problema:** Origins hardcoded para `localhost:4200`.
**Remediação:**
- Origins agora configuráveis via `Cors:AllowedOrigins` em appsettings ou variáveis de ambiente
- Default mantém `localhost:4200` para desenvolvimento
- Produção deve configurar o domínio real

### 7. Rate Limiting Auth — IMPLEMENTADO

**Severidade original:** Alta
**Problema:** Endpoints de autenticação (`/login`, `/register`, `/forgot-password`, `/reset-password`) estavam explicitamente excluídos do rate limiting de tenant, permitindo ataques de força bruta ilimitados.
**Remediação:**
- Novo `AuthRateLimitMiddleware` com rate limiting por IP
- Default: 5 tentativas por minuto por endpoint por IP
- Configurável via `RateLimiting:AuthMaxAttemptsPerMinute` e `RateLimiting:AuthWindowSeconds`
- Retorna HTTP 429 com header `Retry-After`
- Implementado antes do authentication no pipeline (protege endpoints anônimos)

### 8. BCrypt Work Factor — CORRIGIDO

**Severidade original:** Baixa
**Problema:** `BCrypt.HashPassword()` chamado sem `workFactor` explícito (default é 11 no BCrypt.Net-Next).
**Remediação:**
- Todas as chamadas agora usam `workFactor: 12` explicitamente
- Arquivos alterados: `AuthController.cs`, `UserService.cs`
- Hashes existentes continuam funcionando (BCrypt armazena o work factor no hash)

### 9. Security Headers — IMPLEMENTADO

**Severidade original:** Média
**Problema:** Nenhum header de segurança configurado no app ou nginx.
**Remediação implementada em ambos API e Nginx:**
- `X-Content-Type-Options: nosniff` — previne MIME sniffing
- `X-Frame-Options: DENY` — previne clickjacking
- `X-XSS-Protection: 1; mode=block` — proteção XSS em browsers legados
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- `Content-Security-Policy` — restringe sources de script, style, font, img, connect
- `server_tokens off` no Nginx — esconde versão do servidor

### 10. Dependências — SEGURO

**Severidade:** Nenhuma
- `dotnet list package --vulnerable`: 0 vulnerabilidades em todos os projetos
- `npm audit --production`: 0 vulnerabilidades
- **Nota:** EF Core tem version mismatch (9.0.1 vs 9.0.14) nos testes — não é segurança, mas deve ser resolvido

---

## Itens Adicionais Positivos (já existentes)

| Feature | Localização |
|---------|------------|
| Token encryption at rest (DPAPI) | `TokenEncryptionService.cs` |
| Sentry header redaction | `Program.cs` (Sentry config) |
| Generic error messages (sem stack trace) | `GlobalExceptionFilter.cs` |
| Password reset: single-use, hashed, 1h expiry | `AuthController.cs` |
| Multi-tenant isolation via JWT claims | `TenantMiddleware.cs` |
| Correlation ID tracking | `CorrelationIdMiddleware.cs` |
| Request/response logging with sensitive header redaction | `RequestLoggingMiddleware.cs` |

---

## Recomendações Futuras (não bloqueantes)

1. **Account lockout** — após N tentativas falhas consecutivas, bloquear conta temporariamente
2. **CAPTCHA** — adicionar em registro e login após tentativas falhas
3. **Password strength** — exigir complexidade além de mínimo 8 caracteres
4. **2FA/MFA** — segundo fator para contas admin
5. **Audit log** — registrar todas as ações administrativas em tabela dedicada
6. **TLS certificates** — configurar Let's Encrypt no Nginx para produção
