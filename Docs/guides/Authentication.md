# Autenticacao e Autorizacao

Guia tecnico sobre o sistema de autenticacao e autorizacao do PeruShopHub.

## Visao Geral

O PeruShopHub usa **JWT (JSON Web Tokens)** com esquema de access token + refresh token. A autenticacao e stateless (sem sessao no servidor), com refresh token rotation para seguranca adicional.

---

## Tokens

### Access Token

| Propriedade | Valor |
|-------------|-------|
| Algoritmo | HMAC-SHA256 |
| Duracao | 15 minutos (configuravel via `Jwt:AccessTokenExpirationMinutes`) |
| Formato | JWT padrao |

### Refresh Token

| Propriedade | Valor |
|-------------|-------|
| Geracao | 64 bytes aleatorios via `RandomNumberGenerator` |
| Duracao | 7 dias (configuravel via `Jwt:RefreshTokenExpirationDays`) |
| Armazenamento | Coluna `RefreshToken` na tabela `SystemUsers` |
| Rotacao | Cada refresh gera um novo refresh token (o anterior e invalidado) |

### Claims do JWT

| Claim | Descricao |
|-------|-----------|
| `sub` (NameIdentifier) | ID do usuario (GUID) |
| `email` | E-mail do usuario |
| `name` | Nome do usuario |
| `is_super_admin` | `"true"` ou `"false"` |
| `tenant_id` | ID do tenant ativo (se tiver) |
| `tenant_role` | Role no tenant ativo |
| `tenant_name` | Nome do tenant ativo |
| `role` | Role do usuario (para `[Authorize(Roles)]`) |

Se o usuario e super-admin, a claim `role` inclui tambem `"SuperAdmin"`.

---

## Fluxos

### Registro

`POST /api/auth/register`

Request:
```json
{
  "shopName": "Minha Loja",
  "name": "Joao Silva",
  "email": "joao@email.com",
  "password": "minhasenha123"
}
```

Fluxo:
1. Valida todos os campos (nome, email, senha >= 8 caracteres)
2. Verifica unicidade do email
3. Gera slug a partir do nome da loja (com desacentuacao)
4. Cria `Tenant` + `SystemUser` + `TenantUser` (role = Owner)
5. Hash da senha com BCrypt
6. Gera access token + refresh token
7. Salva refresh token no usuario
8. Retorna `AuthResponse`

Validacao retorna **todos** os erros de uma vez (nao falha no primeiro):

```json
{
  "errors": {
    "Email": ["E-mail ja esta em uso."],
    "Password": ["Senha deve ter no minimo 8 caracteres."]
  }
}
```

### Login

`POST /api/auth/login`

Request:
```json
{
  "email": "joao@email.com",
  "password": "minhasenha123"
}
```

Fluxo:
1. Busca usuario por email (inclui memberships e tenants)
2. Verifica senha com `BCrypt.Verify`
3. Seleciona primeiro tenant ativo do usuario
4. Gera access token com claims do tenant
5. Gera novo refresh token
6. Atualiza `LastLogin`
7. Retorna `AuthResponse`

Se credenciais invalidas: retorna 401 com mensagem generica ("E-mail ou senha incorretos").

### Refresh Token

`POST /api/auth/refresh`

Request:
```json
{
  "refreshToken": "base64string..."
}
```

Fluxo:
1. Busca usuario pelo refresh token
2. Verifica se token nao expirou (`RefreshTokenExpiresAt`)
3. Gera novo access token + novo refresh token (rotation)
4. Invalida refresh token anterior
5. Retorna `AuthResponse`

### Logout

`POST /api/auth/logout` (autenticado)

Limpa `RefreshToken` e `RefreshTokenExpiresAt` do usuario, invalidando o refresh token.

### Me

`GET /api/auth/me` (autenticado)

Retorna dados do usuario atual extraidos do JWT (sem consulta ao banco):

```json
{
  "id": "guid",
  "name": "Joao Silva",
  "email": "joao@email.com",
  "tenantRole": "Owner",
  "tenantId": "guid",
  "tenantName": "Minha Loja",
  "isSuperAdmin": false
}
```

### Switch Tenant

`POST /api/auth/switch-tenant` (autenticado)

Permite trocar de tenant sem re-login. Gera novo JWT com claims do novo tenant. Ver detalhes em `Multi-Tenancy.md`.

### Change Password

`POST /api/auth/change-password` (autenticado)

Delega para `UserService.ChangePasswordAsync`. Requer senha atual para validacao.

---

## Frontend (Angular)

### AuthService

Servico central de autenticacao:
- Armazena tokens em `localStorage`
- Fornece `currentUser` signal
- Metodos: `login()`, `register()`, `refresh()`, `logout()`, `switchTenant()`

### AuthInterceptor

HTTP interceptor que:
1. Injeta header `Authorization: Bearer {token}` em todas as requests
2. Intercepta respostas 401:
   - Tenta refresh do token
   - Se refresh sucede: repete a request original com novo token
   - Se refresh falha: redireciona para `/login`

### AuthGuard

Protege todas as rotas exceto `/login` e `/register`:
- Verifica se existe token valido
- Se nao autenticado → redireciona para `/login`
- Aplicado como `canActivate` nas rotas

### TenantGuard

Complementa o AuthGuard verificando se um tenant ativo esta selecionado. Ver detalhes em `Multi-Tenancy.md`.

### SuperAdminGuard

Protege rotas exclusivas de super-admin (ex: pagina de administracao de tenants).

### UnsavedChangesGuard

Guard generico que previne navegacao quando ha mudancas nao salvas em formularios.

---

## Autorizacao por Role

### Atributos de Autorizacao (Backend)

O backend usa os atributos `[Authorize]` do ASP.NET Core:

```csharp
// Qualquer usuario autenticado
[Authorize]

// Apenas Admin
[Authorize(Roles = "Admin")]

// Admin ou Manager
[Authorize(Roles = "Admin,Manager")]

// Apenas SuperAdmin
[Authorize(Roles = "SuperAdmin")]
```

### Matriz de Permissoes

| Recurso | Viewer | Manager | Admin | Owner | SuperAdmin |
|---------|--------|---------|-------|-------|------------|
| Ver dashboard | Sim | Sim | Sim | Sim | Sim |
| Ver produtos/pedidos | Sim | Sim | Sim | Sim | Sim |
| Criar/editar produtos | Nao | Sim | Sim | Sim | Sim |
| Excluir produtos | Nao | Sim | Sim | Sim | Sim |
| Configuracoes | Nao | Nao | Sim | Sim | Sim |
| Gestao de usuarios | Nao | Nao | Sim | Sim | Sim |
| Gestao de tenant | Nao | Nao | Nao | Sim | Sim |
| Admin de tenants | Nao | Nao | Nao | Nao | Sim |

---

## Hashing de Senha

O sistema usa **BCrypt.Net-Next** para hashing de senhas:

```csharp
// Hash ao criar/alterar
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

// Verificacao ao logar
BCrypt.Net.BCrypt.Verify(inputPassword, user.PasswordHash);
```

BCrypt inclui salt automaticamente e e resistente a ataques de forca bruta (work factor configuravel).

---

## Gestao de Usuarios

### Endpoints

| Metodo | Rota | Descricao | Role |
|--------|------|-----------|------|
| GET | `/api/settings/users` | Listar usuarios do tenant | Admin |
| GET | `/api/settings/users/{id}` | Detalhes de usuario | Admin |
| POST | `/api/settings/users` | Criar usuario no tenant | Admin |
| PUT | `/api/settings/users/{id}` | Editar usuario | Admin |
| DELETE | `/api/settings/users/{id}` | Remover usuario do tenant | Admin |
| POST | `/api/settings/users/{id}/reset-password` | Reset de senha (admin) | Admin |
| POST | `/api/auth/change-password` | Alterar propria senha | Qualquer |

### Fluxo de Criacao

1. Admin cria usuario com nome, email, role
2. Sistema gera senha temporaria ou admin define senha
3. Cria `SystemUser` (se nao existe) + `TenantUser` com role especificado
4. Usuario pode alterar senha no primeiro login

---

## SignalR — Autenticacao

O SignalR (WebSocket) nao suporta headers HTTP customizados. O token JWT e passado via query string:

```typescript
// Frontend
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/notifications', {
    accessTokenFactory: () => this.authService.getToken()
  })
  .build();
```

O middleware do SignalR extrai o token da query string e o adiciona ao contexto de autenticacao antes do processamento do hub.

---

## Super-Admin

### Conceito

O flag `IsSuperAdmin` na entidade `SystemUser` concede privilegios especiais:

- **Bypass de query filters**: ve dados de todos os tenants
- **Impersonacao**: pode atuar como admin de qualquer tenant via header `X-Tenant-Id`
- **Admin de tenants**: acesso a pagina de gestao de tenants

### Seguranca

- O flag so e setado diretamente no banco (nao ha endpoint para se autopromover)
- JWT inclui claim `is_super_admin` para decisoes no frontend
- Backend sempre valida via claims, nunca confia apenas no frontend

---

## Configuracao

As configuracoes JWT ficam em `appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "chave-secreta-minimo-32-caracteres",
    "Issuer": "PeruShopHub",
    "Audience": "PeruShopHub",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

**Importante**: em producao, a chave secreta deve ser armazenada em variavel de ambiente ou secret manager, nunca commitada no repositorio.

---

## Arquivos Relevantes

| Arquivo | Descricao |
|---------|-----------|
| `src/PeruShopHub.API/Controllers/AuthController.cs` | Controller de autenticacao (login, register, refresh, switch-tenant, logout, me, change-password) |
| `src/PeruShopHub.Application/DTOs/Auth/AuthDtos.cs` | DTOs: LoginRequest, RegisterRequest, RefreshRequest, AuthResponse, UserDto |
| `src/PeruShopHub.Core/Entities/SystemUser.cs` | Entidade de usuario (com RefreshToken, IsSuperAdmin) |
| `src/PeruShopHub.Core/Entities/TenantUser.cs` | Tabela de juncao usuario-tenant com role |
| `src/PeruShopHub.Application/Services/UserService.cs` | CRUD de usuarios e change-password |
| `src/PeruShopHub.Application/Services/IUserService.cs` | Interface do servico |
| `src/PeruShopHub.Web/src/app/services/auth.service.ts` | Servico Angular de autenticacao |
| `src/PeruShopHub.Web/src/app/interceptors/auth.interceptor.ts` | Interceptor HTTP (Bearer token, refresh on 401) |
| `src/PeruShopHub.Web/src/app/guards/auth.guard.ts` | Guard de autenticacao |
| `src/PeruShopHub.Web/src/app/guards/super-admin.guard.ts` | Guard de super-admin |
| `src/PeruShopHub.Web/src/app/guards/tenant.guard.ts` | Guard de tenant ativo |
