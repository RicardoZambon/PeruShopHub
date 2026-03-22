# Mercado Livre - Modelos de Venda

## Modelo 1: Mercado Envios Full (Logistica do ML)

### Como funciona

O vendedor envia o estoque para os centros de distribuicao (CDs) do Mercado Livre. Quando uma venda ocorre, o ML faz a separacao, embalagem, emissao da NF-e e envio automaticamente.

### Requisitos

- **CNPJ ativo** (MEI aceito)
- Certificado digital para emissao de NF-e
- Conta com reputacao em bom estado
- Produtos dentro dos limites: **max 25kg**, nenhum lado > 120cm, soma dos lados <= 260cm

### Processo de Inbound (envio ao CD)

1. Criar remessa agendada no painel do Mercado Envios Full
2. Emitir NF-e de "Remessa para Deposito Fechado"
3. Embalar conforme diretrizes do ML (embalagem primaria adequada, etiquetas legiveis, codigos de barras visiveis)
4. Enviar ao CD indicado pelo ML
5. Equipe do CD recebe, inspeciona e registra o inventario
6. Anuncio recebe o **selo "Full"** (etiqueta azul)

### Notas Fiscais envolvidas

- **NF-e de Remessa (Inbound)**: emitida pelo vendedor ao enviar estoque ao CD
- **NF-e de Venda**: emitida automaticamente pelo ML quando ocorre uma venda
- **NF-e de Retorno Simbolico**: emitida pelo ML quando necessario (devolucoes, retorno ao vendedor)

### Custos especificos do Full

| Tamanho | Dimensoes max. | Armazenagem/dia |
|---------|----------------|-----------------|
| Pequeno | 12x15x25 cm | R$ 0,007 |
| Medio | 28x36x51 cm | R$ 0,015 |
| Grande | 60x60x70 cm | R$ 0,050 |
| Extra-grande | Acima | R$ 0,107 |

- Armazenagem prolongada (>2 meses) gera custo mensal adicional
- Itens fora das especificacoes nao retirados em 20 dias podem ser descartados (com custo)
- Retirada de estoque do CD tem custo (~5% de aumento em 2026)
- Divergencia entre estoque declarado e recebido gera penalidade

### Produtos proibidos no Full

- Pneus, baterias e pilhas de litio
- Materiais perigosos (inflamaveis, explosivos, toxicos, aerossois, corrosivos)
- Joias com pedras/metais preciosos
- Celulares sem IMEI visivel
- Medicamentos (humano e veterinario)
- Lentes de contato e oculos com grau
- Produtos falsificados ou sem autorizacao do fabricante

### Restricoes adicionais

- Alimentos, suplementos e cosmeticos: validade minima de **120 dias** ao enviar ao CD
- Datas de validade manuscritas nao sao aceitas
- Produtos que exigem refrigeracao nao sao aceitos
- Certificacoes regulatorias obrigatorias (Anvisa, Anatel, INMETRO) quando aplicavel

### Vantagens

- Entrega **mesmo dia / dia seguinte** em grandes centros
- **Selo Full** (etiqueta azul) = maior visibilidade e melhor ranqueamento no algoritmo
- NF-e emitida automaticamente pelo ML
- Frete gratis acima de R$ 79 (subsidiado parcialmente pelo ML)
- Vendedor foca apenas em sourcing, marketing e precificacao
- Reducao de reclamacoes por atraso (ML assume responsabilidade)

### Desvantagens

- Custos de armazenagem acumulam diariamente (produtos de baixo giro ficam onerosos)
- Sem personalizacao de embalagem (sem brindes, encartes, embalagem de marca)
- Penalidades por divergencia de estoque
- Risco de descarte de itens fora de especificacao
- Itens volumetricos tem custos significativamente maiores
- Dependencia total da plataforma para logistica
- Custo de retirada de estoque caso queira remover produtos do CD

---

## Modelo 2: Estoque Proprio + Envio pelo Vendedor

### Cadastro

- Aceita **CPF** (limite R$ 12.000/mes) ou **CNPJ**
- CNPJ necessario para Coleta e programa Mercado Lider
- MEI aceito com ressalvas de limite de faturamento
- Vendedor novo precisa completar **10 vendas em 365 dias** para classificacao de reputacao
- Programa **Decola** oferece suporte especial para novos vendedores

### Modalidades de envio

| Modalidade | Como funciona | Requisitos |
|------------|--------------|------------|
| **Coleta** | ML coleta no endereco do vendedor | CNPJ + area de cobertura |
| **Agencias/Places** | Vendedor leva ao ponto de despacho | Nenhum especifico |
| **Flex** | Vendedor entrega no mesmo dia (selo "Chega hoje") | Reputacao amarela+, area de cobertura |
| **Coleta Rapida** | ML coleta e entrega no mesmo dia (lancado ago/2025) | Area de cobertura |
| **Envio Proprio** | Transportadora/Correios por conta propria | Autorizacao previa do ML |

### Fluxo de processamento de pedidos

1. Notificacao de venda na plataforma/e-mail
2. Separar, embalar e rotular o produto (tempo de preparo configuravel)
3. Imprimir etiqueta de envio gerada pelo ML
4. Despachar conforme modalidade (coleta, agencia, flex ou proprio)
5. Rastreamento em tempo real para vendedor e comprador
6. Confirmacao de entrega pelo comprador
7. Liberacao do pagamento no Mercado Pago (~48h apos entrega)

### Vantagens

- Zero custo de armazenagem
- Controle total da operacao
- Personalizacao de embalagem (brindes, encartes, experiencia de unboxing)
- Menor capital imobilizado
- Ideal para validacao do negocio e produtos sazonais

### Desvantagens

- Prazo de entrega mais lento e variavel
- Menor destaque nos resultados de busca (sem selo Full)
- Operacao logistica e responsabilidade do vendedor
- Reputacao depende de nao atrasar (min 90% no prazo)

---

## Comissoes (ambos os modelos)

### Por tipo de anuncio

| Tipo | Comissao | Parcelamento |
|------|----------|-------------|
| **Gratis** | 0% | Nao |
| **Classico** | 10% a 14% | Nao |
| **Premium** | 15% a 19% (+5% sobre Classico) | Ate 10-12x sem juros |

O anuncio **Gratis** tem limites: 5 novos/20 usados por ano, max 10 anuncios simultaneos.

### Comissao por categoria (exemplos)

| Categoria | Classico | Premium |
|-----------|----------|---------|
| Eletronicos, Audio e Video | 13% | 18% |
| Informatica (Celulares, Notebooks) | 11% | 16% |
| Eletrodomesticos | 11% | 16% |
| Calcados, Roupas e Bolsas | 14% | 19% |
| Beleza e Cuidado Pessoal | 14% | 19% |
| Casa, Moveis e Decoracao | 11,5% | 16,5% |
| Brinquedos e Hobbies | 11,5% | 16,5% |
| Pet Shop | 12,5% | 17,5% |
| Acessorios para Veiculos | 12% | 17% |
| Esportes e Fitness | 14% | 19% |

### Custo fixo (produtos abaixo de R$ 79)

| Faixa de Preco | Custo Fixo |
|----------------|-----------|
| Ate R$ 12,50 | Metade do preco do produto |
| R$ 12,50 a R$ 29 | R$ 6,25 |
| R$ 29 a R$ 50 | R$ 6,50 |
| R$ 50 a R$ 79 | R$ 6,75 |
| Acima de R$ 79 | Sem custo fixo |

**Mudanca marco/2026**: custo fixo sera substituido por **custo operacional variavel** baseado em peso, dimensoes e preco.

### Frete gratis - divisao de custos

| Faixa de Preco | Quem paga |
|----------------|-----------|
| Acima de R$ 79 | Frete gratis obrigatorio; custo parcial recai sobre o vendedor |
| R$ 19 a R$ 78,99 | Plataforma arca integralmente |
| Abaixo de R$ 19 | Vendedor assume 100% |

Vendedores com **reputacao verde** podem obter ate **70% de desconto** nos custos de frete.

---

## Sistema de Reputacao

O termometro vai de **vermelho** (pior) a **verde escuro** (melhor). Criterios:

- **Taxa de reclamacoes**: verde < 3%, amarelo < 7%
- **Cancelamentos** feitos pelo vendedor
- **Atrasos nos envios**: minimo 90% no prazo

Periodo de calculo:
- 60+ vendas em 60 dias: ultimos 60 dias
- Menos de 60 vendas: ultimos 365 dias

Penalidades por baixa reputacao: reducao de visibilidade, perda de descontos de frete, aumento de custos, desativacao de anuncios.

---

## Comparativo Direto

| Aspecto | Full | Envio Proprio |
|---------|------|---------------|
| Prazo de entrega | Mesmo dia / dia seguinte | Variavel (2-7 dias) |
| Destaque nos anuncios | Alto (selo Full) | Menor |
| Taxa de conversao | Alta | Media |
| Custo de armazenagem | Sim (diario) | Zero |
| Controle operacional | Baixo (ML gerencia) | Total |
| Personalizacao | Nenhuma | Total |
| Capital imobilizado | Maior (estoque no CD) | Menor |
| Escalabilidade | Alta | Depende da equipe |
| Ideal para | Alto giro, boa margem, itens pequenos/medios | Validacao, frageis, personalizados, margem apertada |

### Exemplo numerico (produto R$ 159,90)

| Item | Full | Envio Proprio |
|------|------|---------------|
| Comissao + taxas | R$ 25,00 | R$ 25,00 |
| Impostos | R$ 8,00 | R$ 8,00 |
| CMV | R$ 72,00 | R$ 72,00 |
| Custo logistico | R$ 18,00 | R$ 24,00 |
| Armazenagem (25 dias) | R$ 0,38 | R$ 0,00 |
| **Lucro por unidade** | **~R$ 36,52** | **~R$ 30,90** |

---

## Estrategia Recomendada

A melhor abordagem e **hibrida, decidida por SKU**:

1. **Comecar com envio proprio** para validar o negocio e entender a demanda
2. **Migrar para Full** os SKUs com alto giro, boa margem e tamanho pequeno/medio
3. **Manter envio proprio** para itens volumosos, sazonais, personalizados ou de margem apertada
4. **Usar Flex** quando houver demanda local concentrada

### Formula de decisao por SKU

```
Lucro Full = Preco de Venda - Comissao ML - Impostos - CMV - Custo Logistico Full - Armazenagem Esperada
Lucro Proprio = Preco de Venda - Comissao ML - Impostos - CMV - Custo Logistico Proprio
```

Se o ciclo medio de estoque for superior a ~120 dias, a armazenagem prolongada do Full tende a eliminar a vantagem.

---

## Fontes

- [Mercado Envios Full: Vale a Pena? 2026 - GoSmarter](https://gosmarter.com.br/mercado-envios-full-vale-a-pena-2026/)
- [Termos e Condicoes do Envios Full - Mercado Livre](https://www.mercadolivre.com.br/ajuda/Termos-e-condi%C3%A7%C3%B5es-do-MercadoEnvios-Full_2982)
- [Quanto custa vender em cada categoria 2026 - Koncili](https://www.koncili.com/blog/categorias-do-mercado-livre/)
- [Taxas Mercado Livre 2025 - Arcos Scale](https://blog.arcosscale.com.br/guia-completo-taxas-mercado-livre-2025/)
- [Produtos proibidos no Full - Mercado Livre](https://www.mercadolivre.com.br/ajuda/5200)
- [Como os envios funcionam - Central de Vendedores](https://vendedores.mercadolivre.com.br/nota/como-os-envios-do-mercado-livre-funcionam)
- [Quanto custa vender um produto - Mercado Livre](https://www.mercadolivre.com.br/ajuda/quanto-custa-vender-um-produto_1338)
- [Reputacao como vendedor - Mercado Livre](https://www.mercadolivre.com.br/ajuda/como-funciona-a-reputacao-como-vendedor_1382)
- [Custos Mercado Livre 2026 - JoomPulse](https://blog.joompulse.com/2026/02/12/custos-mercado-livre-o-que-muda-para-sellers-2026/)
- [Full em 2026: o que muda - Estoquee](https://estoquee.com.br/blog/processos/full-em-2026-o-que-muda-em-mercado-livre-amazon-e-shopee-e-como-preparar-sua-operacao-agora/)
