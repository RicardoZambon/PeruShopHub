# PeruShopHub - Design System & Visual Specification

Documento de referencia completa para implementacao visual do PeruShopHub. Complementa o PRD de UI/UX (`tasks/prd-ui-ux-design.md`) com especificacoes detalhadas para os 3 screens criticos e padroes de componentes reutilizaveis.

---

## 1. Design Tokens

### 1.1 Cores — Tema Claro

```scss
:root {
  // Primaria (Azul escuro — confianca, profissionalismo)
  --primary: #1A237E;
  --primary-hover: #283593;
  --primary-light: #E8EAF6;

  // Accent (Laranja — acao, destaque, energia)
  --accent: #FF6F00;
  --accent-hover: #E65100;
  --accent-light: #FFF3E0;

  // Semanticas
  --success: #2E7D32;
  --success-light: #E8F5E9;
  --warning: #F57F17;
  --warning-light: #FFFDE7;
  --danger: #C62828;
  --danger-light: #FFEBEE;

  // Neutras
  --neutral-900: #212121;
  --neutral-700: #616161;
  --neutral-500: #9E9E9E;
  --neutral-300: #E0E0E0;
  --neutral-100: #F5F5F5;
  --neutral-50: #FAFAFA;

  // Superficies
  --surface: #FFFFFF;
  --page-bg: #F5F5F5;

  // Overlay
  --overlay: rgba(0, 0, 0, 0.5);
}
```

### 1.2 Cores — Tema Escuro

```scss
:root[data-theme="dark"] {
  --primary: #7986CB;
  --primary-hover: #9FA8DA;
  --primary-light: #1A237E;

  --accent: #FFB74D;
  --accent-hover: #FFA726;
  --accent-light: #3E2723;

  --success: #66BB6A;
  --success-light: #1B3A1B;
  --warning: #FFF176;
  --warning-light: #3E3A00;
  --danger: #EF5350;
  --danger-light: #3E1111;

  --neutral-900: #FAFAFA;
  --neutral-700: #BDBDBD;
  --neutral-500: #757575;
  --neutral-300: #424242;
  --neutral-100: #303030;
  --neutral-50: #252525;

  --surface: #1E1E1E;
  --page-bg: #121212;

  --overlay: rgba(0, 0, 0, 0.7);
}
```

### 1.3 Tipografia

**Fontes**: Inter (UI principal), Roboto Mono (dados financeiros, IDs, SKUs)

Carregar via Google Fonts:
```
Inter: 400, 500, 600, 700
Roboto Mono: 400, 500
```

| Token | Tamanho | Peso | Line Height | Uso |
|-------|---------|------|-------------|-----|
| heading-1 | 24px | 700 | 32px | Titulos de pagina |
| heading-2 | 20px | 600 | 28px | Titulos de secao/card |
| heading-3 | 16px | 600 | 24px | Subsecoes |
| body | 14px | 400 | 20px | Texto padrao |
| body-small | 12px | 400 | 16px | Captions, texto auxiliar |
| label | 12px | 500 | 16px | Labels de formulario, overlines |
| mono | 13px (Roboto Mono) | 400 | 18px | SKUs, IDs, valores monetarios |
| metric-large | 32px | 700 | 40px | KPIs grandes no dashboard |
| metric-medium | 20px | 600 | 28px | Metricas em cards |

### 1.4 Espacamento

Base: 4px. Escala: 4, 8, 12, 16, 20, 24, 32, 40, 48, 64.

| Contexto | Desktop | Tablet | Mobile |
|----------|---------|--------|--------|
| Padding da pagina | 24px | 16px | 12px |
| Gap entre cards | 16px | 16px | 12px |
| Padding interno de card | 16px | 16px | 12px |
| Gap entre secoes | 24px | 20px | 16px |
| Padding de celula (tabela) | 12px h / 8px v | 8px h / 6px v | 8px h / 6px v |

### 1.5 Elevacao e Bordas

```scss
--shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05);   // Cards em repouso
--shadow-md: 0 4px 6px rgba(0, 0, 0, 0.07);    // Cards hover, dropdowns
--shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1);   // Modais, dialogs

--radius-sm: 4px;     // Botoes, inputs, badges
--radius-md: 8px;     // Cards, modais
--radius-lg: 12px;    // Containers grandes
--radius-full: 9999px; // Avatares, pills
```

### 1.6 Breakpoints

| Nome | Min Width | Comportamento |
|------|-----------|---------------|
| mobile | 0 | Sidebar overlay, cards em 1 coluna, tabelas viram cards |
| mobile-landscape | 480px | Cards em 2 colunas |
| tablet | 768px | Sidebar colapsada, tabelas com scroll horizontal |
| desktop | 1024px | Sidebar expandida, layout completo |
| desktop-lg | 1280px | Layout otimizado, mais colunas visiveis |
| desktop-xl | 1536px | Espaco extra para graficos e tabelas largas |

---

## 2. Componentes Padrao

### 2.1 Botoes

| Variante | Background | Texto | Borda | Uso |
|----------|-----------|-------|-------|-----|
| Primary | `--primary` | white | none | Acoes principais (Salvar, Confirmar) |
| Accent | `--accent` | white | none | CTAs de destaque (Novo Produto, Publicar) |
| Outline | transparent | `--primary` | `--primary` 1px | Acoes secundarias (Cancelar, Exportar) |
| Ghost | transparent | `--neutral-700` | none | Acoes terciarias (Cancelar em modais) |
| Danger | `--danger` | white | none | Acoes destrutivas (Excluir, Desconectar) |

Todos os botoes:
- Height: 36px (sm), 40px (md), 44px (lg)
- Padding horizontal: 16px (sm), 20px (md), 24px (lg)
- Border radius: `--radius-sm`
- Font: 14px / 500
- Transicao hover: 150ms ease
- Estado disabled: opacity 0.5, cursor not-allowed
- Estado loading: texto substitudo por spinner (16px), largura fixa

### 2.2 Badges / Tags

| Variante | Background | Texto | Uso |
|----------|-----------|-------|-----|
| Success | `--success-light` | `--success` | Ativo, Entregue, Conectado, Synced |
| Warning | `--warning-light` | `--warning` | Pausado, Pendente, Estoque baixo |
| Danger | `--danger-light` | `--danger` | Erro, Cancelado, Desconectado, Margem negativa |
| Primary | `--primary-light` | `--primary` | Pago, Em transito |
| Neutral | `--neutral-100` | `--neutral-700` | Encerrado, Devolvido |
| Accent | `--accent-light` | `--accent` | Novo, Destaque |

Formato: pill (border-radius full), padding 4px 8px, font body-small / 500.

### 2.3 Cards

```
┌─────────────────────────────────┐
│  Card Title           [Action]  │  ← heading-3, padding-bottom 12px
│─────────────────────────────────│  ← border-bottom: 1px neutral-300
│                                 │
│  Card content                   │  ← padding 16px
│                                 │
└─────────────────────────────────┘
```

- Background: `--surface`
- Border: 1px `--neutral-300` (light) ou none com shadow (elevado)
- Border-radius: `--radius-md`
- Shadow: `--shadow-sm` (em repouso)
- Hover (se clicavel): `--shadow-md`, translateY(-1px)

### 2.4 KPI Card

```
┌─────────────────────────────┐
│  Label (body-small, neutral-700)  │
│  R$ 12.345,67 (metric-large)     │
│  ▲ 12,3%  vs periodo anterior    │
│    (body-small, success/danger)   │
└─────────────────────────────┘
```

- Largura flexivel (grid column)
- Icone opcional a esquerda do valor
- Seta de variacao: `▲` verde (positivo bom), `▼` vermelho (negativo ruim)
- Contexto invertido para custos: `▲` vermelho (custo subiu = ruim)

### 2.5 Tabelas

**Cabecalho**: background `--neutral-100`, texto `--label` uppercase, sortable columns com icone de seta.

**Linhas**: altura minima 48px, hover background `--neutral-50`, alternancia de cor desabilitada (clean look).

**Celulas especiais**:
- Valores monetarios: alinhamento a direita, font `--mono`
- Porcentagens: alinhamento a direita
- Status: badge centralizado
- Acoes: icones (edit, view, delete) com tooltip, alinhamento a direita

**Paginacao**: barra inferior com: "Mostrando X-Y de Z", seletor de tamanho (10/25/50), botoes prev/next com numeros de pagina.

**Estado vazio**: celula unica colspan, icone + mensagem + CTA opcional.

**Estado loading**: skeleton rows (6 linhas de retangulos animados).

### 2.6 Formularios

- Labels acima do campo (nao floating label — melhor para data density)
- Campos required marcados com asterisco vermelho
- Erro inline: texto `--danger` abaixo do campo, borda do campo muda para `--danger`
- Campos desabilitados: background `--neutral-100`, texto `--neutral-500`
- Campos monetarios: prefixo "R$" fixo a esquerda dentro do campo
- Campos de busca: icone de lupa a esquerda
- Selects: seta dropdown a direita, opcoes em dropdown com busca para listas longas

### 2.7 Modais / Dialogs

- Overlay: `--overlay` sobre toda a pagina
- Container: `--surface`, `--radius-md`, `--shadow-lg`, max-width 480px (sm) / 640px (md) / 800px (lg)
- Header: titulo heading-2, botao X de fechar a direita
- Body: padding 24px, scroll independente se conteudo longo
- Footer: botoes alinhados a direita, gap 12px entre botoes
- Animacao: fade in (150ms) + scale(0.95 → 1)
- Fechar: clicar fora, tecla Esc, botao X

### 2.8 Toast / Snackbar

- Posicao: top-right, 24px de margem
- Largura: 360px (desktop), 100% - 24px (mobile)
- Variantes: success (green left border), warning, danger, info (primary)
- Conteudo: icone + titulo + descricao opcional + botao X
- Auto-dismiss: 5 segundos
- Stack: toasts empilham verticalmente com gap 8px, max 3 visiveis
- Animacao: slide-in da direita (200ms)

### 2.9 Slide-over Panel

- Aparece da direita
- Largura: 400px (desktop), 100% (mobile)
- Overlay: `--overlay`
- Conteudo: header com titulo + X, body com scroll
- Usado para: notificacoes, perfil de cliente, detalhes rapidos
- Animacao: translateX(100% → 0) (200ms)

---

## 3. Especificacao Detalhada — Tela Dashboard

### 3.1 Hierarquia de Informacao

```
1. Barra de periodo (contexto temporal)
2. KPI cards (metricas-chave de performance)
3. Graficos (tendencias e distribuicao)
4. Tabelas de ranking (produtos top/bottom)
5. Acoes pendentes (call-to-action imediato)
```

### 3.2 Layout Grid

```
Desktop (>=1024px):
┌──────────────────────────────────────────────────────┐
│  [Hoje] [7 dias] [30 dias] [Personalizado ▼]        │
├────────────┬────────────┬────────────┬───────────────┤
│  Vendas    │  Receita   │  Lucro     │  Margem       │
│  127       │  R$ 18.4k  │  R$ 4.2k   │  22,8%        │
│  ▲ 15,3%   │  ▲ 8,7%    │  ▼ 2,1%    │  ▼ 1,3pp      │
├────────────┴────────────┼────────────┴───────────────┤
│                         │                            │
│  Vendas e Lucro (Line)  │  Distribuicao Custos       │
│  [Chart 60% width]      │  (Donut) [Chart 40% width] │
│                         │                            │
├─────────────────────────┼────────────────────────────┤
│  Top 5 Mais Lucrativos  │  Top 5 Menos Lucrativos    │
│  (Table)                │  (Table)                   │
├─────────────────────────┴────────────────────────────┤
│  Acoes Pendentes                                     │
│  [🔴 3 Perguntas] [🟡 5 Pedidos pendentes] [⚠️ 2 Alertas] │
└──────────────────────────────────────────────────────┘

Tablet (768-1023px):
- KPIs: 2x2 grid
- Graficos: empilhados (full width cada)
- Tabelas: empilhadas

Mobile (<768px):
- KPIs: 1 coluna, cards compactos (metric-medium em vez de metric-large)
- Graficos: full width, altura reduzida (200px)
- Tabelas: 3 colunas visiveis (Produto, Lucro, Margem)
- Acoes pendentes: badges horizontais com scroll
```

### 3.3 Interacoes

- **Periodo selector**: botoes de opcoes rapidas + botao "Personalizado" que abre date range picker
- **KPI cards**: hover mostra tooltip com valor absoluto detalhado
- **Graficos**: tooltips no hover com valores exatos, legendas clicaveis para toggle de series
- **Tabelas de ranking**: row click navega para detalhe do produto
- **Acoes pendentes**: click em cada badge navega para o respectivo filtro (ex: "Perguntas sem resposta")

### 3.4 Estados

| Estado | Comportamento |
|--------|---------------|
| **Loading** | Skeleton: 4 cards retangulares + 2 areas de grafico + linhas de tabela |
| **Vazio (sem dados)** | Ilustracao centralizada: "Conecte seu Mercado Livre para comecar" + botao "Conectar Marketplace" |
| **Vazio (periodo sem vendas)** | Metricas mostram "0" e "--", graficos mostram eixos sem dados, tabelas mostram "Nenhuma venda neste periodo" |
| **Erro** | Card de erro no lugar do componente que falhou, restante carrega normal |
| **Parcial** | Se um endpoint falha, mostra error state para aquele componente, demais funcionam |

### 3.5 Dados do Grafico "Vendas e Lucro"

- **Tipo**: Line chart (Chart.js)
- **Eixo X**: datas do periodo selecionado (agrupamento automatico: dias para <30d, semanas para 30-90d, meses para >90d)
- **Eixo Y esquerdo**: valores em BRL (Receita, Lucro)
- **Linha 1**: Receita Bruta — cor `--primary`, solid
- **Linha 2**: Lucro Liquido — cor `--success`, dashed
- **Area**: preenchimento sutil abaixo de cada linha (opacity 0.1)
- **Tooltip**: data + Receita: R$ X + Lucro: R$ Y + Margem: Z%
- **Legenda**: acima do grafico, horizontal, clicavel para toggle

### 3.6 Dados do Grafico "Distribuicao de Custos"

- **Tipo**: Doughnut chart (Chart.js)
- **Segmentos**: Comissao ML (`#5C6BC0`), Frete (`#42A5F5`), Custo Produto (`#66BB6A`), Embalagem (`#FFA726`), Impostos (`#EF5350`), Armazenagem (`#AB47BC`), Advertising (`#26C6DA`), Outros (`#BDBDBD`)
- **Centro**: "Total Custos" + valor em BRL
- **Tooltip**: categoria + valor + porcentagem
- **Legenda**: abaixo do grafico, 2 colunas

---

## 4. Especificacao Detalhada — Tela Detalhe do Pedido

### 4.1 Layout

```
Desktop (>=1024px):
┌──────────────────────────────────────────────────────┐
│  ← Voltar    Pedido #2087654321    [Entregue ✓]      │
│              15 mar 2026, 14:32     [ML icon]         │
├──────────────────────────┬───────────────────────────┤
│                          │                           │
│  ITENS DO PEDIDO         │  COMPRADOR                │
│  ┌─────────────────────┐ │  Nome: Maria Silva        │
│  │ [img] Capinha iPhone │ │  Nickname: MARIA_S123     │
│  │ SKU: CAP-IPH15-BLK  │ │  Email: m***@gmail.com    │
│  │ Var: Preto           │ │  Tel: (11) 9****-5678     │
│  │ 1x R$ 69,00         │ │  Pedidos: 3 | Total: R$   │
│  └─────────────────────┘ │  287,00                   │
│                          │                           │
├──────────────────────────┤  ENVIO                    │
│                          │  Rastreio: AB123456789BR  │
│  DECOMPOSICAO DE CUSTOS  │  [copiar]                 │
│  ========================│  Carrier: Correios        │
│                          │  Tipo: [Full badge]       │
│  [===Barra visual=====] │                           │
│                          │  ○ Criado — 15/03 14:32   │
│  Comissao ML    R$ 11,04 │  ○ Pago — 15/03 14:33    │
│  Taxa fixa       R$ 0,00 │  ● Enviado — 15/03 18:00 │
│  Frete          R$ 12,00 │  ● Entregue — 17/03 10:20│
│  Tx pagamento    R$ 3,20 │                           │
│  Custo produto  R$ 12,00 │  PAGAMENTO                │
│  Embalagem       R$ 2,00 │  [💳 Visa] 3x R$ 23,00   │
│  Impostos        R$ 3,80 │  Status: Aprovado         │
│  ─────────────────────── │  Total: R$ 69,00          │
│  TOTAL CUSTOS   R$ 44,04 │                           │
│  ═══════════════════════ │                           │
│  LUCRO LIQUIDO  R$ 24,96 │                           │
│  Margem: 36,2%           │                           │
│                          │                           │
│  [+ Adicionar custo]     │                           │
├──────────────────────────┴───────────────────────────┤
│  TIMELINE                                            │
│  ● 17/03 10:20 — Entregue ao destinatario           │
│  ○ 16/03 08:15 — Em transito — CD Osasco            │
│  ○ 15/03 18:00 — Coletado pelo transportador        │
│  ○ 15/03 14:33 — Pagamento aprovado                 │
│  ○ 15/03 14:32 — Pedido criado                      │
├──────────────────────────────────────────────────────┤
│  [▸ Dados brutos da API (JSON)]   ← collapsible     │
└──────────────────────────────────────────────────────┘

Mobile:
- Secoes empilhadas em coluna unica
- Ordem: Header → Itens → Custos → Envio → Pagamento → Comprador → Timeline
- Barra visual de custos simplificada (apenas numeros)
```

### 4.2 Barra Visual de Custos (Stacked Bar)

Barra horizontal de largura total representando a receita bruta. Cada segmento proporcional ao custo:

```
|▓▓▓▓▓▓ Comissao ▓▓▓▓ Frete ▓▓▓▓▓▓▓▓ Produto ▓▓ Embal ▓ Tax ░░░ Lucro ░░░|
```

- Segmentos de custo: cores solidas com labels dentro (se cabe) ou tooltip
- Segmento de lucro: cor `--success` com hachura ou opacidade diferente
- Altura: 32px
- Border-radius: `--radius-sm`
- Hover em segmento: tooltip com nome + valor + percentual

### 4.3 Tabela de Custos

| Coluna | Largura | Alinhamento | Formato |
|--------|---------|-------------|---------|
| Categoria | flex | left | texto |
| Valor | 120px | right | `--mono`, BRL |
| % Receita | 80px | right | `--mono`, 1 decimal |
| Fonte | 80px | center | badge colorido |

Badges de Fonte:
- API: `--primary` badge
- Manual: `--warning` badge
- Calculado: `--success` badge

Linha de total: bold, borda superior dupla, background `--neutral-100`.
Linha de lucro: bold, font metric-medium, cor condicional (success/danger).

### 4.4 Timeline de Envio

- Linha vertical a esquerda (2px, `--neutral-300`)
- Circulos nos pontos: 8px, preenchidos para eventos passados (`--primary`), vazio para futuros
- Ultimo evento (atual): circulo maior (12px) com cor do status (success para entregue, primary para em transito)
- Texto: data + hora (mono, neutral-700) + descricao (body)

---

## 5. Especificacao Detalhada — Tela Financeiro

### 5.1 Sub-navegacao

Tabs horizontais abaixo do titulo da pagina:
```
[Resumo] [Lucratividade por SKU] [Conciliacao] [Curva ABC]
```
Active tab: borda inferior 2px `--primary`, texto `--primary`.

### 5.2 Aba Resumo — Layout

```
Desktop:
┌──────────────────────────────────────────────────────┐
│  Financeiro     [Periodo ▼]   [PDF] [Excel]          │
│  [Resumo] [Lucratividade] [Conciliacao] [Curva ABC]  │
├──────────┬──────────┬──────────┬─────────┬───────────┤
│ Receita  │ Custos   │ Lucro    │ Margem  │ Ticket    │
│ R$ 18.4k │ R$ 14.2k │ R$ 4.2k  │ 22,8%   │ R$ 145    │
│ ▲ 8,7%   │ ▲ 12,3%  │ ▼ 2,1%   │ ▼ 1,3pp │ ▲ 3,2%    │
├──────────┴──────────┴──────────┴─────────┴───────────┤
│                                                      │
│  Receita vs Lucro (Grouped Bar Chart)                │
│  [dia ▼]                                             │
│  ████ Receita  ████ Lucro                            │
│                                                      │
├──────────────────────────┬───────────────────────────┤
│                          │                           │
│  Evolucao da Margem      │  Custos por Categoria     │
│  (Line Chart)            │  (Horizontal Stacked Bar) │
│  ─ ─ ─ 15% min ─ ─ ─    │                           │
│                          │                           │
└──────────────────────────┴───────────────────────────┘
```

### 5.3 Aba Lucratividade por SKU

```
┌───────────────────────────────────────────────────────────────────────────────────┐
│ SKU       │ Produto     │ Vendas │ Receita  │ CMV      │ Comissoes │ Frete   │...│
├───────────┼─────────────┼────────┼──────────┼──────────┼───────────┼─────────┤   │
│ CAP-IPH15 │ Capinha iPh │ 127    │ R$ 8.763 │ R$ 1.524 │ R$ 1.402  │ R$ 876  │   │
│           │ [▸ expand]  │        │          │          │           │         │   │
│           │  Jan: ████  │        │          │          │           │         │   │
│           │  Fev: █████ │        │          │          │           │         │   │
│           │  Mar: ███   │        │          │          │           │         │   │
├───────────┼─────────────┼────────┼──────────┼──────────┼───────────┼─────────┤   │
│ ORG-ACR01 │ Organizador │ 43     │ R$ 3.827 │ R$ 1.290 │ R$ 632    │ R$ 459  │   │
└───────────┴─────────────┴────────┴──────────┴──────────┴───────────┴─────────┘   │
                                                          ... │ Lucro   │ Margem │
                                                              │ R$ 2.145│ 24,5%  │
                                                              │         │ [████] │
                                                              │ R$   987│ 25,8%  │
                                                              │         │ [█████]│
```

- Ultima coluna "Margem": barra de progresso horizontal colorida (verde >20%, amarelo 10-20%, vermelho <10%)
- Expandir linha: mostra sparkline de lucro mensal dos ultimos 6 meses
- Scroll horizontal com colunas fixas (SKU + Produto ficam fixas a esquerda)

### 5.4 Aba Conciliacao

```
┌────────────┬──────────────┬──────────────┬────────────┬──────────┐
│ Periodo    │ Esperado     │ Depositado   │ Diferenca  │ Status   │
├────────────┼──────────────┼──────────────┼────────────┼──────────┤
│ 01-15/Mar  │ R$ 8.432,10  │ R$ 8.432,10  │ R$ 0,00    │ [OK ✓]   │
│ 16-28/Fev  │ R$ 12.876,50 │ R$ 12.654,30 │ -R$ 222,20 │ [⚠ Div] │
│  [▸ expand: detalhes por pedido com divergencias destacadas]      │
└────────────┴──────────────┴──────────────┴────────────┴──────────┘
```

- Rows com divergencia: background `--warning-light`
- Badge "Divergencia": `--warning`
- Expand mostra tabela interna: Order ID, Esperado, Recebido, Diferenca

### 5.5 Aba Curva ABC

```
Desktop:
┌──────────────────────────────────────────────┐
│  [Bar chart horizontal: produtos por lucro]  │
│  ────────── Linha cumulativa 80%/95% ─────── │
│  Zona A ████████████████████                 │
│  Zona B ████████                             │
│  Zona C ████                                 │
├──────┬───────────┬────────┬────────┬─────────┤
│ Rank │ Produto   │ Lucro  │ % Tot  │ Class.  │
├──────┼───────────┼────────┼────────┼─────────┤
│ 1    │ Capinha   │ R$ 2.1k│ 28,3%  │ [A ✓]   │
│ 2    │ Pelicula  │ R$ 1.8k│ 24,1%  │ [A ✓]   │
│ 3    │ Fone BT   │ R$ 1.2k│ 16,1%  │ [A ✓]   │
│ 4    │ Organiz.  │ R$ 0.9k│ 12,1%  │ [B]     │
│ ...  │           │        │        │         │
└──────┴───────────┴────────┴────────┴─────────┘
```

Badges de classificacao:
- A (top 80% do lucro): `--success` badge
- B (proximos 15%): `--warning` badge
- C (ultimos 5%): `--danger` badge

---

## 6. Padroes de Interacao

### 6.1 Formatacao de Valores

| Tipo | Formato | Exemplo | Font |
|------|---------|---------|------|
| Moeda BRL | R$ #.###,## | R$ 1.234,56 | `--mono` |
| Moeda BRL grande | R$ ##,#k ou R$ #,#M | R$ 18,4k | `--mono` |
| Porcentagem | ##,#% | 22,8% | `--mono` |
| Pontos percentuais | ##,#pp | 1,3pp | `--mono` |
| Data recente | "ha Xh", "ha Xd" | ha 2h | body-small |
| Data absoluta | DD mmm YYYY, HH:MM | 15 mar 2026, 14:30 | body-small |
| SKU / ID | As-is, uppercase | CAP-IPH15-BLK | `--mono` |
| Order ID | # + numero | #2087654321 | `--mono` |

### 6.2 Cores Condicionais para Valores

| Contexto | Positivo (bom) | Negativo (ruim) |
|----------|---------------|-----------------|
| Lucro | `--success` | `--danger` |
| Margem % | `--success` (>=20%), `--warning` (10-19%), `--danger` (<10%) | |
| Variacao de receita/vendas | `--success` (subiu) | `--danger` (desceu) |
| Variacao de custos | `--danger` (subiu) | `--success` (desceu) |
| Estoque | normal | `--warning` (<min), `--danger` (0) |

### 6.3 Loading Pattern

Toda pagina segue o padrao:
1. **Imediato**: shell (sidebar + header) renderizado, area de conteudo mostra skeleton
2. **API retorna**: skeleton substituido por dados reais com fade-in sutil (100ms)
3. **Erro**: skeleton substituido por error card com "Tentar novamente"

Skeleton shapes devem imitar o layout real (mesmas dimensoes de cards, linhas de tabela, areas de grafico).

### 6.4 Navegacao por Teclado

| Atalho | Acao |
|--------|------|
| `Ctrl+K` / `Cmd+K` | Abrir busca global |
| `Esc` | Fechar modal/slide-over/busca |
| `Tab` | Navegar entre elementos interativos |
| `Enter` | Ativar botao/link focado |
| `Arrow keys` | Navegar resultados de busca, opcoes de dropdown |

---

## 7. Iconografia

Usar uma biblioteca consistente. Recomendacoes:
- **Lucide Icons** (open source, clean, 24px grid) — ou
- **Material Symbols** (se Angular Material for escolhido)

Icones devem usar `currentColor` para herdar cor do contexto. Tamanhos padrao: 16px (inline), 20px (botoes), 24px (navegacao).

### Icones por Contexto de Navegacao

| Secao | Icone sugerido |
|-------|---------------|
| Dashboard | `layout-dashboard` |
| Produtos | `package` |
| Vendas | `shopping-cart` |
| Perguntas | `message-circle` |
| Clientes | `users` |
| Financeiro | `trending-up` |
| Estoque | `clipboard-list` |
| Configuracoes | `settings` |
| Notificacoes | `bell` |
| Busca | `search` |
| Tema | `sun` / `moon` |

---

## 8. Identidade Visual

### Logo

- **Simbolo**: Silhueta estilizada de Shih Tzu (referencia ao Pepperoni) integrada com icone de hub/conexao
- **Logotipo**: "PeruShopHub" em Inter Bold, "Peru" em `--primary`, "Shop" em `--accent`, "Hub" em `--neutral-700`
- **Variantes**: completo (simbolo + logotipo), simbolo apenas (para sidebar colapsada e favicon), monocromatico (para fundos coloridos)

### Tagline

"Gestao inteligente de marketplaces"

### Favicon

Simbolo do Shih Tzu simplificado, 32x32px, com fundo `--primary`.
