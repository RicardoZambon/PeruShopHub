# PeruShopHub — Modelo de Precos & Estrategia de Monetizacao

> Ultima atualizacao: 2026-03-28

---

## Posicionamento

**"O primeiro hub que mostra seu lucro real por venda"**

Plataforma unica para gestao completa de loja em marketplaces. O vendedor gerencia tudo dentro do PeruShopHub (produtos, estoque, custos, vendas) e a plataforma sincroniza automaticamente com os marketplaces conectados.

**Diferencial central**: Decomposicao automatica de lucratividade por venda — nenhum concorrente oferece isso.

---

## Publico-Alvo

**Perfil primario**: Vendedores pequenos faturando R$10k-50k/mes no Mercado Livre.

| Caracteristica | Detalhe |
|----------------|---------|
| Faturamento | R$10k-50k/mes |
| Marketplaces | 1-2 (ML principal, expandindo para Shopee) |
| Produtos | 50-500 SKUs |
| Equipe | Solo ou 1-2 pessoas |
| Ferramentas atuais | Bling + planilhas + calculadora manual |
| Dor principal | Suspeitam que estao perdendo dinheiro em alguns produtos mas nao conseguem provar |
| Disposicao a pagar | R$100-300/mes por clareza financeira |

Este segmento e subatendido: pequeno demais para ferramentas enterprise, sofisticado demais para ERP basico.

---

## Planos e Precos

### Tabela de Planos

| | Starter | Pro | Business |
|--|---------|-----|----------|
| **Preco mensal** | R$89/mes | R$199/mes | R$449/mes |
| **Preco anual** | R$71/mes | R$159/mes | R$359/mes |
| **Desconto anual** | 20% | 20% | 20% |
| **Pedidos/mes** | 200 | 1.000 | 5.000 |
| **Marketplaces** | 1 | 3 | Ilimitado |
| **Produtos** | 100 | 500 | Ilimitado |
| **Usuarios** | 2 | 5 | 15 |
| **Motor de lucratividade** | Decomposicao por venda | + Curva ABC, alertas de margem | + Simulador what-if, insights IA |
| **Relatorios** | Dashboard basico | + Export PDF/Excel, relatorios por email | + Relatorios customizados, acesso API |
| **Regras de preco** | Manual por marketplace | + Baseado em margem-alvo | + Regras em massa, monitoramento |
| **Suporte** | Email | Email + chat | Prioritario + call de onboarding |

### Logica de Trial

O trial nao e baseado em tempo fixo — e baseado em **atividade real**:

1. Novo usuario se registra → recebe acesso **Pro completo**
2. Configura loja, importa produtos, organiza estoque — **sem cobranca**
3. Primeira venda entra no sistema → **timer de 14 dias inicia**
4. Apos 14 dias da primeira venda → plano cai automaticamente para **Starter**
5. Usuario escolhe plano quando quiser; se nao escolher, permanece no Starter

**Racional**: O vendedor experimenta o valor total antes de pagar. Ate a primeira venda, o custo de infra e minimo (sem webhooks, sem calculo financeiro). Os 14 dias pos-venda garantem que ele viu seus dados reais de lucratividade.

### Equipe e Usuarios

Multi-usuario disponivel em todos os planos (com limite por plano). Roles: Owner, Admin, Manager, Viewer. O sistema permite uso solo ou em equipe — nao forca nenhum modelo.

---

## Analise Competitiva

### Mapa de Mercado (Marco 2026)

O mercado brasileiro se divide em 4 categorias:

#### 1. ERPs Leves com Integracao Marketplace

| Player | Preco/mes | NF-e | Lucratividade | Notas |
|--------|-----------|------|---------------|-------|
| **Bling** | R$55-650 | Sim | Basica (receita - custo) | Lider de mercado. 250+ integracoes. Aumentando precos em abril 2026. |
| **Olist Tiny** | R$59-849 | Sim | Relatorios basicos de margem | Rebranded de Tiny. Forte integracao ML. |
| **Eccosys** | A partir de R$720 | Sim | Melhor que a maioria — tem algum rastreio de custos | Foco em alto volume. Mais proximo da nossa visao financeira. |
| **UpSeller** | Gratis (NF-e R$0.01/nota) | Sim | Basica | Newcomer agressivo. 50 CNPJs/conta. |

#### 2. Hubs Puros de Integracao (sem ERP/NF-e)

| Player | Preco/mes | Marketplaces | Notas |
|--------|-----------|-------------|-------|
| **Ideris** | ~R$100-1.124 | 20+ | Mid-to-large. R$0.40/pedido excedente. |
| **Plugg.to** | A partir de R$54 | 80+ | Maior cobertura de marketplaces. |
| **Anymarket** | A partir de R$399 | 30+ | Enterprise. Fixo + por pedido. |

#### 3. Ferramentas Financeiras Nicho (nao sao hubs)

| Player | Foco | Notas |
|--------|------|-------|
| **Preco Certo** | Calculadora de precificacao + indicadores | 89% dos sellers vendem no prejuizo. Nao e hub. |
| **OMIQ** | Gestao ML + calculadora de precos | Lucro por anuncio. ML-only. |
| **Koncili** | Conciliacao financeira com marketplaces | Identifica taxas indevidas. Ferramenta de auditoria. |

#### 4. Enterprise

| Player | Notas |
|--------|-------|
| **VTEX/SkyHub** | R$1.000+/mes. Overkill para nosso target. |

### Gap que o PeruShopHub Preenche

Vendedores hoje precisam de 2-3 ferramentas:

```
Workflow atual do vendedor:
  Bling (R$185/mes)        → ERP, NF-e, estoque basico
  + Ideris (R$300/mes)     → hub marketplace, sync de anuncios
  + Preco Certo (R$??/mes) → analise de lucratividade
  + Planilhas              → rastreio real de custos por venda
  = R$485+/mes + trabalho manual + dados fragmentados

PeruShopHub:
  Plataforma unica         → Hub + motor de lucratividade + alertas
  = Decomposicao automatica via APIs dos marketplaces
  = Sem planilhas, sem entrada manual
```

### Vantagens Competitivas

| Vantagem | vs ERPs | vs Hubs | vs Nicho |
|----------|---------|---------|----------|
| Decomposicao automatica de custos | Nao tem | Nao fazem financeiro | Calculam pre-venda, nao pos-venda real |
| Alertas de margem em tempo real | Nao | Nao | Parcial (so precificacao) |
| Multi-tenant SaaS | Instalacao single-tenant | Single-tenant | Single-tenant |
| UI moderna (Angular + dark mode) | Bling e datado | Varia | OMIQ e decente |
| Simulador what-if | Nao | Nao | Preco Certo tem versao basica |
| Regras de preco por margem-alvo | Nao | Nao | Nao |

### Desvantagens Competitivas (avaliacao honesta)

| Desvantagem | Impacto | Mitigacao |
|-------------|---------|-----------|
| Sem NF-e (ate Fase 7) | Table stakes no Brasil | Integrar via API externa (Focus NFe). Inicialmente sellers mantem Bling para NF-e. |
| Marketplace unico (ML primeiro) | Concorrentes suportam 20-80+ | Adapter pattern pronto. Prioridade: ML → Shopee → Amazon. |
| Sem historico/reputacao | Sellers confiam em marcas estabelecidas | Beta fechado + valor inegavel (mostrar dinheiro que estao perdendo). |
| Sem integracao contabil | Sellers precisam de Bling/Tiny para compliance fiscal | API/export hooks. Posicionar como complemento inicialmente. |

---

## Estrategia de Go-to-Market

### MVP (Pos-Fase 5)

**Modelo**: Beta fechado, convite apenas.

1. Selecionar 10-20 vendedores do perfil-alvo
2. Onboarding pessoal (call 1:1, setup assistido)
3. Iterar baseado em feedback real
4. Sem cobranca durante beta

### Produto (Pos-Fase 6)

**Modelo**: Lancamento publico com billing ativo.

1. Landing page com waitlist → converte para marketing site
2. Trial: Pro completo, cobranca 14 dias apos primeira venda
3. Soft-land para Starter se nao escolher plano
4. Upsell natural quando atingir limites

### Hook de Marketing

> "89% dos vendedores de marketplace vendem no prejuizo. Descubra em 5 minutos quais dos seus produtos estao dando lucro — e quais estao comendo sua margem."

---

## Metricas de Sucesso

| Metrica | Meta MVP | Meta Lancamento |
|---------|----------|-----------------|
| Sellers ativos | 10-20 | 100+ |
| Retencao mensal | >80% | >90% |
| Trial → Pago | N/A (beta gratis) | >30% |
| Tempo ate "aha moment" | <5 min apos primeira venda | <5 min |
| NPS | >50 | >60 |

---

## Referencias

- [Bling - Planos e Precos](https://www.bling.com.br/planos-e-precos)
- [Olist Tiny - Planos](https://tiny.com.br/planos)
- [Eccosys - Planos](https://www.eccosys.com.br/planos)
- [UpSeller - Pricing](https://www.upseller.com/en/pricing/)
- [Ideris - Planos](https://www.ideris.com.br/planos/)
- [Plugg.to - Planos](https://plugg.to/planos/)
- [Anymarket - Planos](https://anymarket.com.br/planos/)
- [Preco Certo](https://precocerto.co/)
- [OMIQ](https://omiq.com.br/)
- [Koncili](https://www.koncili.com/planos/)
