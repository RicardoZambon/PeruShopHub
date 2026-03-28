# PeruShopHub — Landing Page Brief

> Ultima atualizacao: 2026-03-28
> Status: Planejamento — implementacao futura

---

## Objetivo

Criar uma landing page / site marketing para o PeruShopHub em duas fases:

1. **Pre-MVP**: Pagina de waitlist para coletar interesse de vendedores
2. **Pos-Billing (Fase 6+)**: Site completo de marketing com planos, precos, demo, cases

---

## Fase 1 — Waitlist (Pre-MVP)

### Objetivo
Validar interesse e construir lista de early adopters para o beta fechado.

### Conteudo Essencial

1. **Hero Section**
   - Headline: "Descubra quanto voce realmente lucra em cada venda"
   - Sub: "O primeiro hub de marketplace que decompoe automaticamente todos os custos — comissao, frete, taxas, impostos, embalagem — e mostra seu lucro liquido real por pedido."
   - CTA: "Quero participar do beta"

2. **Problema**
   - Stat: "89% dos vendedores de marketplace vendem no prejuizo sem saber"
   - Mostrar cenario atual: Bling + planilhas + calculadora = fragmentacao
   - Visual: decomposicao de uma venda (preco - comissao - frete - taxa - custo = lucro)

3. **Solucao**
   - Screenshots/mockups do dashboard de lucratividade
   - Destaque: "Automatico. Sem planilhas. Dados reais da API do marketplace."

4. **Waitlist Form**
   - Nome, email, faturamento mensal (faixas), marketplace principal
   - Mensagem de confirmacao: "Voce esta na fila! Vamos te avisar quando o beta abrir."

5. **Footer**
   - Links: Privacidade, Termos (placeholder)
   - Social: Instagram, LinkedIn

### Decisoes Tecnicas (a definir)
- Stack: Pode ser uma pagina estatica simples (HTML/CSS) ou Next.js/Astro
- Formulario: Pode usar Typeform, Google Forms embutido, ou proprio com backend simples
- Hosting: Vercel, Netlify, ou no proprio dominio perushop.com.br
- Analytics: Google Analytics ou Plausible

---

## Fase 2 — Site Completo (Pos-Fase 6)

### Paginas

1. **Home** — Hero + problema + solucao + planos + CTA
2. **Precos** — Tabela de planos detalhada + FAQ de billing
3. **Funcionalidades** — Tour pelas features com screenshots
4. **Cases / Depoimentos** — Resultados dos beta testers
5. **Blog** — SEO content sobre lucratividade em marketplace
6. **Contato / Suporte** — Chat, email, FAQ

### Conteudo de Marketing (a desenvolver)

- **Calculadora interativa**: "Insira o preco do seu produto e veja quanto voce realmente lucra no Mercado Livre" — ferramenta gratuita que gera leads
- **Comparativo**: "PeruShopHub vs Bling vs Ideris" — transparente sobre o que cada um faz
- **Conteudo educativo**: "Como calcular o lucro real de cada venda no ML" — SEO + autoridade

---

## Identidade Visual

Usar o design system existente do PeruShopHub:
- Primary: `#1A237E` (dark blue)
- Accent: `#FF6F00` (orange)
- Fontes: Inter (UI) + Roboto Mono (dados financeiros)
- Suportar light + dark mode

---

## Timeline

| Fase | Quando | Dependencia |
|------|--------|-------------|
| Waitlist (Fase 1) | Antes do MVP (Fase 5) | Dominio + design basico |
| Site completo (Fase 2) | Com lancamento billing (Fase 6) | Planos definidos + beta feedback |

---

## Notas

Este documento e um brief inicial. O design detalhado e implementacao serao planejados separadamente quando a fase correspondente for priorizada. A landing page de waitlist pode ser um projeto paralelo simples que nao depende do desenvolvimento principal do produto.
