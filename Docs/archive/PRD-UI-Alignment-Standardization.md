# PRD: Alinhamento Visual e Padronização do Design System

**Projeto:** PeruShopHub
**Branch:** `ralph/ui-ux-design-system`
**Data:** 2026-03-22
**Tipo:** Revisão de Design / Padronização
**Fase:** 1 de 3 (Auditoria → Revisão Detalhada → Implementação)

---

## 0. Instruções para o Agente Executor

### Persona
Você deve atuar como um **Designer de Frontend Senior / UI Engineer** especializado em design systems e padronização visual. Sua expertise é em:
- Consistência visual entre telas de aplicações SaaS/admin panels
- Design tokens e sistemas de CSS custom properties
- Angular component architecture e SCSS
- Acessibilidade (WCAG) e suporte a temas (light/dark)
- Tipografia, hierarquia visual e espaçamento

### Diretrizes de Execução
- **Branch obrigatório:** Todo o trabalho DEVE ser feito no branch `ralph/ui-ux-design-system`. Não criar branches novos.
- **PRDs derivados:** Qualquer PRD gerado como output desta fase (Fase 2: Revisão Detalhada, Fase 3: Implementação) DEVE especificar `"branchName": "ralph/ui-ux-design-system"` — o mesmo branch.
- **Foco:** Apenas padronização e alinhamento visual. Não adicionar features novas, não alterar lógica de negócio, não mudar estrutura de rotas.
- **Validação:** Cada correção deve ser verificada com `ng build` (typecheck) e visualmente em ambos os temas (light e dark).

---

## 1. Contexto e Motivação

O PeruShopHub é uma central de administração de loja online conectando múltiplas plataformas (MVP: Mercado Livre). O frontend Angular 21 foi construído por user stories individuais (US-001 a US-033), o que naturalmente gerou **inconsistências visuais entre páginas**. Cada tela foi implementada isoladamente, resultando em divergências de tokens, nomes de classes, padrões de layout e comportamentos de componentes.

Este PRD documenta **todas as inconsistências identificadas** e define as correções necessárias para unificar a identidade visual antes de avançar com novas features.

### Objetivo

Garantir que todas as 8 páginas principais + componentes globais (header, sidebar, toast, tabelas) sigam **uma única linguagem visual consistente**, usando os design tokens definidos em `tokens.css` como fonte única de verdade.

---

## 2. Escopo da Auditoria

### 2.1 Componentes Globais Auditados
- Header (`shared/components/header/`)
- Sidebar (`shared/components/sidebar/`)
- Toast Container (`shared/components/toast-container/`)
- Data Table (`shared/components/data-table/`)
- Search Palette (`shared/components/search-palette/`)
- Notification Panel (`shared/components/notification-panel/`)

### 2.2 Páginas Auditadas
| Página | Caminho | Status |
|--------|---------|--------|
| Dashboard | `pages/dashboard/` | Referência parcial |
| Produtos | `pages/produtos/` | Inconsistente |
| Vendas | `pages/vendas/` | Inconsistente |
| Perguntas | `pages/perguntas/` | Mais inconsistente |
| Clientes | `pages/clientes/` | Inconsistente |
| Financeiro | `pages/financeiro/` | Parcialmente alinhado |
| Estoque | `pages/estoque/` | Parcialmente alinhado |
| Configurações | `pages/configuracoes/` | Parcialmente alinhado |

---

## 3. Problemas Encontrados

### 3.1 [CRÍTICO] Naming de Tokens de Espaçamento

**Problema:** Existem duas convenções de nomes para spacing tokens no projeto — `--space-*` e `--spacing-*`. Apenas `--space-*` está definido em `tokens.css`.

| Convenção | Páginas que usam |
|-----------|-----------------|
| `--space-*` (correto) | Perguntas, Financeiro, Estoque, Dashboard, Configurações, Sidebar, Header |
| `--spacing-*` (não existe em tokens.css) | Clientes, Produtos, Vendas, Data Table |

**Impacto:** Os valores `--spacing-*` caem no fallback do CSS (geralmente 0 ou valor herdado), causando espaçamentos quebrados ou dependentes de valores inline/hardcoded.

**Correção:** Renomear todas as ocorrências de `--spacing-*` para `--space-*` em:
- `clientes.component.scss`
- `produtos-list.component.scss`
- `vendas-list.component.scss`
- `data-table.component.scss`

---

### 3.2 [CRÍTICO] Cores Hardcoded (`#fff`)

**Problema:** Múltiplos componentes usam `color: #fff` em vez de tokens semânticos. Isso quebra no dark theme (texto branco sobre fundo escuro claro).

| Arquivo | Linha(s) | Contexto |
|---------|----------|----------|
| `perguntas.component.scss` | 60, 223 | Badge de contagem, botão enviar |
| `configuracoes.component.scss` | 276 | Botão primário |
| `financeiro.component.scss` | 213 | Botão de período ativo |
| `estoque.component.scss` | 132 | Botão accent |
| `header.component.scss` | 52 | Badge de notificação |
| `dashboard.component.scss` | 362 | Seletor de período ativo |

**Correção:** Criar token `--text-on-primary: #FFFFFF` (light) / `--text-on-primary: #FFFFFF` (dark) em `tokens.css` e substituir todos os `#fff` hardcoded.

---

### 3.3 [ALTO] Tipografia Inconsistente nos Títulos de Página

**Problema:** As páginas usam tokens diferentes para o mesmo nível de título:

| Token usado | Páginas |
|------------|---------|
| `--text-2xl` | Perguntas, Produtos |
| `--h2-size` | Financeiro, Estoque |
| `--heading-1-size` (correto via tokens.css) | Nenhuma usa diretamente |

**Nenhum desses tokens existe em `tokens.css`.** Os tokens reais são `--heading-1-size: 24px`, `--heading-2-size: 20px`, etc.

**Correção:** Padronizar todos os títulos de página para usar `--heading-1-size` / `--heading-1-weight` / `--heading-1-height`. Adicionar tokens auxiliares se necessário:
```css
--text-2xl: var(--heading-1-size);  /* alias para compatibilidade */
--h2-size: var(--heading-2-size);   /* alias para compatibilidade */
```
Ou preferencialmente, migrar todos para os tokens nativos.

---

### 3.4 [ALTO] Sistema de Botões Fragmentado

**Problema:** Cada página reimplementa seus próprios estilos de botão com naming inconsistente:

| Classe | Onde |
|--------|------|
| `.btn--primary` | Configurações |
| `.btn--outline` | Financeiro |
| `.btn--accent` | Estoque |
| `.btn--secondary` | Configurações |
| `.btn--danger-outline` | Configurações |
| `.btn-reply`, `.btn-cancel`, `.btn-send` | Perguntas |
| `.btn-text` | Configurações |

**Correção:** Criar componente `ButtonComponent` compartilhado OU definir classes globais de botão em `styles.scss`:

```scss
/* Sistema de botões padronizado */
.btn { /* base */ }
.btn--primary { /* fundo primary, texto branco */ }
.btn--secondary { /* fundo neutral-100, texto neutral-700 */ }
.btn--accent { /* fundo accent, texto branco */ }
.btn--outline { /* borda neutral-300, fundo transparente */ }
.btn--danger { /* fundo danger, texto branco */ }
.btn--ghost { /* sem fundo, sem borda, hover sutil */ }
.btn--sm, .btn--md, .btn--lg { /* tamanhos */ }
```

---

### 3.5 [ALTO] Tabelas sem Padrão Unificado

**Problema:** Cada página cria suas próprias classes de tabela:

| Classe | Página |
|--------|--------|
| `.produtos-table` | Produtos |
| `.vendas-table` | Vendas |
| `.clientes-table` | Clientes |
| `.inv-table`, `.mov-table` | Estoque |
| `.sku-table`, `.conciliacao-table`, `.abc-table` | Financeiro |
| `.users-table` | Configurações |
| `.product-table` | Dashboard |
| `.data-table` | Componente compartilhado (não usado em todas as páginas) |

**Problema adicional:** O `DataTableComponent` já existe como componente compartilhado mas nem todas as páginas o utilizam. Algumas criam tabelas inline no HTML da própria página.

**Correção:**
1. Migrar todas as tabelas para usar o `DataTableComponent` compartilhado
2. Se tabelas inline forem necessárias, criar estilos globais `.table`, `.table__head`, `.table__row`, `.table__cell` em `styles.scss`
3. Padronizar: uppercase com `letter-spacing: 0.05em` nos headers OU font-weight 600 sem uppercase — escolher UM estilo

---

### 3.6 [MÉDIO] Focus States Inconsistentes

**Problema:** Inputs têm estilos de focus diferentes:

| Componente | Box-shadow no focus |
|------------|-------------------|
| Perguntas | `0 0 0 3px rgba(26, 35, 126, 0.1)` |
| Estoque | `0 0 0 2px rgba(26, 35, 126, 0.15)` |
| Configurações | `0 0 0 2px rgba(26, 35, 126, 0.15)` |
| Dashboard | `0 0 0 2px rgba(26, 35, 126, 0.15)` |
| Financeiro | `0 0 0 2px rgba(26, 35, 126, 0.15)` |

**Correção:** Criar token de focus ring em `tokens.css`:
```css
--focus-ring: 0 0 0 2px rgba(26, 35, 126, 0.15);
--focus-ring-dark: 0 0 0 2px rgba(121, 134, 203, 0.3);
```
E usar `box-shadow: var(--focus-ring)` em todos os inputs.

---

### 3.7 [MÉDIO] Shadow Tokens Misturados

**Problema:** O header usa `var(--elevation-3)` que não existe em `tokens.css`. Os tokens reais são `--shadow-sm`, `--shadow-md`, `--shadow-lg`.

**Correção:** Substituir `--elevation-3` por `--shadow-md` no `header.component.scss`.

---

### 3.8 [MÉDIO] Border Radius Hardcoded

**Problema:** Alguns componentes usam valores hardcoded em vez de tokens:

| Local | Valor | Deveria ser |
|-------|-------|-------------|
| `perguntas.component.scss` linha 58 | `border-radius: 10px` | `var(--radius-full)` |
| `header.component.scss` linha 50 | `border-radius: 8px` | `var(--radius-md)` |

**Correção:** Substituir todos os border-radius hardcoded pelos tokens correspondentes.

---

### 3.9 [MÉDIO] Página Perguntas com Max-Width Diferente

**Problema:** A página de Perguntas tem `max-width: 900px` enquanto todas as outras páginas usam largura completa.

**Correção:** Avaliar se o layout de chat/perguntas realmente precisa de max-width. Se sim, documentar como exceção intencional. Se não, remover a restrição para consistência.

---

### 3.10 [MÉDIO] Font Family Hardcoded vs Tokens

**Problema:** Alguns componentes usam `font-family: 'Roboto Mono', monospace` ou `font-family: 'Inter', sans-serif` em vez dos tokens `var(--font-mono)` e `var(--font-ui)`.

| Arquivo | Uso hardcoded |
|---------|--------------|
| `perguntas.component.scss` | `'Roboto Mono', monospace` |
| `clientes.component.scss` | `'Roboto Mono', monospace` |
| `header.component.scss` | `'Inter', sans-serif` |

**Correção:** Substituir todas as ocorrências por `var(--font-mono)` e `var(--font-ui)`.

---

### 3.11 [MÉDIO] Empty State Inconsistente

**Problema:** Padrões diferentes de empty state:
- **Componente `app-empty-state`:** Clientes, Produtos, Vendas, Dashboard
- **Inline (HTML direto):** Perguntas
- **Texto simples:** Financeiro, Estoque, Configurações

**Correção:** Migrar todas as páginas para usar o `EmptyStateComponent` compartilhado.

---

### 3.12 [BAIXO] Padrão de Page Header Inconsistente

**Problema:** Cada página estrutura seu header de forma diferente:

| Página | Classe do header | Estrutura |
|--------|-----------------|-----------|
| Dashboard | `.dashboard__title` | Flex com seletor de período |
| Financeiro | `.financeiro__title` + `__header` | Separado com botões de ação |
| Perguntas | `.page-title` | Simples, sem BEM |
| Clientes | `.clientes__title` | BEM notation |
| Produtos | `.produtos__title` | BEM notation |

**Correção:** Definir um padrão único de page header:
```html
<div class="page-header">
  <div class="page-header__title-group">
    <h1 class="page-header__title">Título</h1>
    <p class="page-header__subtitle">Descrição opcional</p>
  </div>
  <div class="page-header__actions">
    <!-- Botões de ação -->
  </div>
</div>
```

---

### 3.13 [BAIXO] Card Padding Inconsistente

**Problema:** Cards usam padding de tokens diferentes:

| Token | Páginas |
|-------|---------|
| `--space-5` (20px) | Perguntas |
| `--card-padding-desktop` (16px) | Configurações, Financeiro, Estoque, Dashboard |

**Correção:** Padronizar todas as cards para usar `--card-padding-desktop` / `--card-padding-tablet` / `--card-padding-mobile`.

---

### 3.14 [BAIXO] Mobile Card Pattern Variado

**Problema:** A transformação tabela→card no mobile é implementada de formas diferentes:
- Clientes, Produtos, Vendas: `div.cards` separado com `display: none` no desktop
- Data Table component: `.data-table__cards`
- Configurações: `.users-cards`

**Correção:** Se usando o `DataTableComponent` em todas as páginas, o pattern fica unificado automaticamente.

---

## 4. Plano de Execução (User Stories para PRD de Implementação)

### Fase 1: Tokens e Fundação (Prioridade Máxima)

| ID | Título | Escopo |
|----|--------|--------|
| FIX-001 | Adicionar tokens ausentes em `tokens.css` | Adicionar `--text-on-primary`, `--focus-ring`, aliases de tipografia. Remover referências a tokens inexistentes |
| FIX-002 | Corrigir `--spacing-*` → `--space-*` em todos os arquivos | Renomear em clientes, produtos, vendas, data-table |
| FIX-003 | Substituir `#fff` hardcoded por `--text-on-primary` | 7 arquivos afetados |
| FIX-004 | Substituir font-family hardcoded por tokens | `var(--font-mono)` e `var(--font-ui)` |
| FIX-005 | Corrigir border-radius e shadow tokens hardcoded | `--elevation-3` → `--shadow-md`, radius hardcoded → tokens |

### Fase 2: Componentes Globais (Prioridade Alta)

| ID | Título | Escopo |
|----|--------|--------|
| FIX-006 | Criar sistema de botões global em `styles.scss` | `.btn` base + variantes (primary, secondary, accent, outline, danger, ghost) + tamanhos (sm, md, lg) |
| FIX-007 | Criar classe global de page-header | `.page-header` com title-group e actions |
| FIX-008 | Criar estilos globais de form-field | `.form-field`, `.form-field__label`, `.form-field__input` com focus ring padronizado |
| FIX-009 | Padronizar focus states com `--focus-ring` token | Aplicar em todos os inputs, selects, textareas |

### Fase 3: Migração por Página (Prioridade Média)

| ID | Título | Escopo |
|----|--------|--------|
| FIX-010 | Padronizar Dashboard | Migrar para page-header global, sistema de botões, tokens corretos |
| FIX-011 | Padronizar Produtos | Migrar para tokens corretos, page-header, sistema de botões |
| FIX-012 | Padronizar Vendas | Migrar para tokens corretos, page-header, sistema de botões |
| FIX-013 | Padronizar Perguntas | Corrigir max-width, migrar para page-header, empty-state, botões globais, tokens |
| FIX-014 | Padronizar Clientes | Migrar para tokens corretos, page-header, sistema de botões |
| FIX-015 | Padronizar Financeiro | Migrar para page-header global, sistema de botões, tokens de tipografia |
| FIX-016 | Padronizar Estoque | Migrar para page-header global, sistema de botões |
| FIX-017 | Padronizar Configurações | Migrar para page-header global, form-field global |

### Fase 4: Refinamento (Prioridade Baixa)

| ID | Título | Escopo |
|----|--------|--------|
| FIX-018 | Migrar empty states para `EmptyStateComponent` | Perguntas, Financeiro, Estoque, Configurações |
| FIX-019 | Unificar table header style | Escolher: uppercase + letter-spacing OU semibold sem uppercase |
| FIX-020 | Revisar card padding para consistência | Padronizar `--card-padding-*` em todas as páginas |

---

## 5. Critérios de Aceite Globais

Cada FIX deve atender:

1. **Zero valores hardcoded** — cores, espaçamentos, tipografia, radius e shadows devem usar tokens de `tokens.css`
2. **Dark theme funcional** — a correção não pode quebrar o tema escuro
3. **Responsivo** — manter comportamento mobile/tablet/desktop existente
4. **Typecheck passa** — `ng build` sem erros
5. **Sem regressão visual** — a página deve parecer igual ou melhor após a correção (as inconsistências são sutis, não radicais)

---

## 6. Arquivos Chave para Referência

| Arquivo | Papel |
|---------|-------|
| `src/styles/tokens.css` | Fonte única de verdade para todos os tokens |
| `src/styles/mixins.scss` | Breakpoints responsivos |
| `src/styles.scss` | Estilos globais (onde adicionar botões, page-header, form-field) |
| `src/app/shared/components/` | Componentes reutilizáveis existentes |

---

## 7. Fora de Escopo

- Mudanças na paleta de cores (primary, accent, etc.)
- Novos componentes de UI (além dos globais de padronização)
- Integração com APIs backend
- Novas funcionalidades de negócio
- Mudanças na estrutura de rotas ou navegação
- Redesign de páginas (apenas padronização do que já existe)

---

## 8. Próximos Passos

> **IMPORTANTE:** Todas as fases abaixo DEVEM usar o branch `ralph/ui-ux-design-system`. Nenhum branch novo deve ser criado.

1. **Este PRD (Fase 1)** → Entregue para revisão ✅
2. **PRD 2 — Revisão Detalhada (Fase 2)** → Validação completa por equipe de design-frontend. Todos os 20 FIX items confirmados (98% accuracy). ✅ Concluído 2026-03-22.
3. **PRD 3 — Implementação (Fase 3)** → PRD final com user stories executáveis: `Docs/PRD-UI-Implementation-Final.md`. Branch: `ralph/ui-ux-design-system`. ✅ Concluído 2026-03-22.
4. **Implementação Fases 1 & 2** → US-FIX-001 a US-FIX-009 implementados e revisados. 22 arquivos alterados, 427 inserções, 207 remoções. ✅ Concluído 2026-03-22. Fases 3 & 4 (migração por página e refinamento) pendentes.

---

## Apêndice A: Inventário Completo de Tokens Ausentes

Tokens referenciados no código mas **não definidos** em `tokens.css`:

| Token Referenciado | Onde | Token Correto |
|-------------------|------|---------------|
| `--spacing-1` a `--spacing-6` | Clientes, Produtos, Vendas, Data Table | `--space-1` a `--space-6` |
| `--text-2xl` | Perguntas, Produtos | `--heading-1-size` (24px) |
| `--text-xl` | Vários | `--heading-2-size` (20px) |
| `--text-lg` | Vários | `--heading-3-size` (16px) |
| `--text-sm` | Vários | `--body-small-size` (12px) |
| `--text-xs` | Clientes, Produtos | `--body-small-size` (12px) |
| `--h2-size` | Financeiro, Estoque | `--heading-2-size` (20px) |
| `--elevation-3` | Header | `--shadow-md` |
| `--text-on-primary` | Não existe (precisa criar) | — |
| `--focus-ring` | Não existe (precisa criar) | — |

## Apêndice B: Mapa de Identidade Visual Alvo

```
┌─────────────────────────────────────────────────────────────┐
│ HEADER (56px, fixed, surface bg, shadow-sm bottom)          │
│ [≡] [Logo]     [🔍 Buscar... Ctrl+K]     [●] [🔔] [☀] [👤]│
├────────┬────────────────────────────────────────────────────┤
│SIDEBAR │ CONTENT AREA                                       │
│256/64px│                                                    │
│        │ ┌─ page-header ─────────────────────────────────┐  │
│ Dashboard│ │ H1 Título da Página    [Btn] [Btn]           │  │
│ Produtos │ └──────────────────────────────────────────────┘  │
│ Vendas   │                                                    │
│ Perguntas│ ┌─ Filtros / Search bar ─────────────────────┐  │
│ Clientes │ └────────────────────────────────────────────┘  │
│ Financ.  │                                                    │
│ Estoque  │ ┌─ Content (table / cards / charts) ────────┐  │
│ Config.  │ │                                             │  │
│          │ │                                             │  │
│          │ └─────────────────────────────────────────────┘  │
│          │                                                    │
│          │ ┌─ Pagination ───────────────────────────────┐  │
│          │ └────────────────────────────────────────────┘  │
├────────┴────────────────────────────────────────────────────┤
│ TOAST (top-right, max 3, auto-dismiss 5s)                   │
└─────────────────────────────────────────────────────────────┘
```

### Hierarquia Tipográfica Padrão

| Elemento | Token | Peso | Tamanho |
|----------|-------|------|---------|
| Título da página (H1) | `--heading-1-*` | 700 | 24px |
| Subtítulo de seção (H2) | `--heading-2-*` | 600 | 20px |
| Título de card (H3) | `--heading-3-*` | 600 | 16px |
| Corpo de texto | `--body-*` | 400 | 14px |
| Labels / Captions | `--label-*` | 500 | 12px |
| Valores monetários | `--font-mono` + `--mono-*` | 400 | 13px |
| Métricas grandes (KPI) | `--metric-large-*` | 700 | 32px |

### Palette de Botões

| Variante | Background | Texto | Borda | Uso |
|----------|-----------|-------|-------|-----|
| Primary | `--primary` | `--text-on-primary` | none | Ação principal da página |
| Secondary | `--neutral-100` | `--neutral-700` | none | Ações secundárias |
| Accent | `--accent` | `--text-on-primary` | none | CTAs de destaque |
| Outline | transparent | `--neutral-700` | `--neutral-300` | Ações terciárias |
| Danger | `--danger` | `--text-on-primary` | none | Ações destrutivas |
| Ghost | transparent | `--primary` | none | Links/ações inline |
