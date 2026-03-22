# PRD: Product Form Redesign

## Introduction

The product edit/create form needs UX improvements for consistency, field ordering, and a new media gallery. This PRD covers field removals, reordering, tab renames, a simplified save flow, and a split-panel layout with a media gallery (images + video) on the right side.

**Scope:** Frontend only (Angular). Mock data. Current branch `ralph/ui-ux-design-system`.

---

## Goals

- Improve consistency with the product list page (title alignment, layout)
- Simplify the form by removing unused/premature fields (Tipo de anúncio, Custo embalagem, Condição, Frete grátis)
- Add a media gallery panel (up to 9 images + 1 video) visible across all tabs
- Unify save actions into a single "Salvar" button
- Apply the same changes to the variant expandable detail rows

---

## User Stories

### US-045: Page header and layout consistency

**Description:** As a user, I want the product form to have a consistent header layout matching the product list page.

**Acceptance Criteria:**
- [ ] Page title ("Novo Produto" or "Editar Produto: [name]") aligned to the left, matching product list page header
- [ ] Back arrow button on the left of the title (existing behavior, keep)
- [ ] Form takes left side (~60%) of the page, media gallery takes right side (~40%) on desktop
- [ ] Mobile (<768px): gallery stacks above the form as a horizontal scrollable strip
- [ ] Gallery panel is visible and persistent across all tabs (Informações Básicas, Preço e Custos, Dimensões, Variações)

### US-046: Informações Básicas tab — field reordering and cleanup

**Description:** As a user, I want the basic info tab to have fields in a logical order without unnecessary fields.

**Acceptance Criteria:**
- [ ] Field order: **SKU**, **Título do anúncio** (with char counter), **Categoria** (tree-select), **Descrição** (textarea, 8 rows minimum), **Fornecedor** (moved from Preço e Custos tab)
- [ ] **Remove:** Condição field (Novo/Usado radio buttons) — removed entirely from form and FormGroup
- [ ] Descrição textarea has `rows="8"` (was `rows="5"`)

### US-047: Preço e Custos tab — simplification

**Description:** As a user, I want a simplified pricing tab focused on sale price and acquisition cost.

**Acceptance Criteria:**
- [ ] Fields: **Preço de venda** (R$ prefix, required), **Custo de aquisição** (R$ prefix, optional)
- [ ] **Remove:** Custo embalagem field — removed from form and FormGroup
- [ ] **Remove:** Tipo de anúncio dropdown — removed from form and FormGroup
- [ ] **Remove:** Fornecedor field (moved to Informações Básicas)
- [ ] Margin calculator simplified: `margem = (precoVenda - custoAquisicao) / precoVenda * 100` — no more commission calculation
- [ ] Keep margin color coding: green ≥20%, yellow 10-19%, red <10%

### US-048: Envio tab renamed to Dimensões

**Description:** As a user, I want the shipping tab to focus on physical dimensions only.

**Acceptance Criteria:**
- [ ] Tab renamed from "Envio" to "Dimensões"
- [ ] Fields: **Peso (kg)**, **Altura (cm)**, **Largura (cm)**, **Comprimento (cm)** — in a 4-column grid (existing)
- [ ] **Remove:** Frete grátis toggle — removed from form and FormGroup
- [ ] **Remove:** Frete grátis hint text

### US-049: Bottom bar — single save button

**Description:** As a user, I want a single save action instead of choosing between draft and publish.

**Acceptance Criteria:**
- [ ] Replace "Salvar Rascunho" + "Publicar" buttons with a single **"Salvar"** button (accent style)
- [ ] Save button uses the existing save icon (`Save` from lucide)
- [ ] Remove the publish (`Send`) icon import if unused
- [ ] Loading state: "Salvando..." with spinner
- [ ] Cancel button remains on the left

### US-050: Media gallery panel

**Description:** As a user, I want to manage product images and a video in a gallery panel next to the form.

**Acceptance Criteria:**
- [ ] Gallery panel on the right side of the form, visible across all tabs
- [ ] **Images:** Up to 9 image slots, displayed as a grid (3x3 on desktop, 3x2 on tablet)
- [ ] First image slot is marked as "Principal" (featured/main image)
- [ ] Each slot shows: placeholder with camera icon when empty, thumbnail when filled
- [ ] Add image: click empty slot opens a mock file picker (for now, just cycles through placeholder colors/patterns)
- [ ] Remove image: hover shows X button overlay on thumbnail
- [ ] **Drag to reorder:** images are reorderable via drag-and-drop (Angular CDK)
- [ ] Dragged image shows `--shadow-md` elevation, drop target shows dashed border
- [ ] **Video slot:** Separate section above/below image grid
- [ ] Video input: YouTube URL text field with preview thumbnail
- [ ] Video preview: shows YouTube embed thumbnail with play icon overlay
- [ ] Video remove: X button to clear
- [ ] **Video specs reference** (shown as hint text): "YouTube URL. Para Clips: MP4/MOV, 10-60s, vertical 9:16, até 280 MB"
- [ ] Image specs reference (hint text): "JPG/PNG, 500-1920px, até 10 MB, fundo branco recomendado"
- [ ] Gallery card: `--surface` background, `--neutral-200` border, `--radius-lg` border-radius
- [ ] Mobile (<768px): gallery collapses into a horizontal scrollable strip at top of page, with a "Mídia" section header

### US-051: Variant detail — costs and shipping cleanup

**Description:** As a user, when I expand a variant's detail row, I want the fields to match the simplified product form.

**Acceptance Criteria:**
- [ ] Rename expandable section "Custos" → "Preço e custos"
- [ ] Fields under "Preço e custos": **Preço de venda** (the existing per-variant price, moved here from the table row), **Custo de aquisição**
- [ ] **Remove:** Custo embalagem field from variant costs
- [ ] Rename expandable section "Envio" → "Dimensões"
- [ ] Fields under "Dimensões": **Peso (kg)**, **Altura (cm)**, **Largura (cm)**, **Comprimento (cm)**
- [ ] **Remove:** Frete grátis toggle from variant shipping
- [ ] Update `VariantCosts` interface: remove `custoEmbalagem` field
- [ ] Update `VariantShipping` interface: remove `freteGratis` field
- [ ] Variant table columns: attribute columns + **Estoque** + **SKU** + **Ativo** + **Ações** (price moved to expandable row)
- [ ] Bulk actions: only "Definir preço para todos" remains (stock bulk removed)

---

## Functional Requirements

- **FR-1:** Product form uses a two-panel layout: form (left ~60%) + gallery (right ~40%)
- **FR-2:** Gallery panel persists across all tab changes
- **FR-3:** Images limited to 9, plus 1 video slot (YouTube URL)
- **FR-4:** Images are reorderable via drag-and-drop; first image is always "Principal"
- **FR-5:** Margin calculation simplified to `(precoVenda - custoAquisicao) / precoVenda * 100`
- **FR-6:** Single save button replaces draft + publish flow
- **FR-7:** Removed fields: Condição, Custo embalagem, Tipo de anúncio, Frete grátis (both product and variant level)
- **FR-8:** Fornecedor moved from Preço e Custos to Informações Básicas
- **FR-9:** Variant price field moved from table column to expandable detail row under "Preço e custos"

---

## Non-Goals

- Actual image/video upload to a server or Mercado Livre API
- Image cropping or editing
- Clips direct upload (future feature)
- Variant-level images (each variant having its own gallery)

---

## Design Considerations

### Desktop Layout (≥1024px)
```
┌────────────────────────────────────────────────────────┐
│  ← Editar Produto: Fone Bluetooth                      │
├────────────────────────────────────────────────────────┤
│  Básico │ Preço │ Dimensões │ Variações                │
├──────────────────────────┬─────────────────────────────┤
│  [Form fields]           │  🎬 Vídeo                   │
│                          │  ┌─────────────────┐        │
│  SKU: [FN-BT-001]       │  │ YouTube URL...   │        │
│  Título: [Fone BT...]   │  └─────────────────┘        │
│  Categoria: [Áudio >...] │                             │
│  Descrição: [........]   │  📷 Imagens (3/9)           │
│  [...............]       │  ┌────┬────┬────┐           │
│  [...............]       │  │ ★1 │  2 │  3 │           │
│  Fornecedor: [........]  │  ├────┼────┼────┤           │
│                          │  │  + │  + │  + │           │
│                          │  ├────┼────┼────┤           │
│                          │  │  + │  + │  + │           │
│                          │  └────┴────┴────┘           │
│                          │  JPG/PNG, 500-1920px        │
├──────────────────────────┴─────────────────────────────┤
│  Cancelar                                    [Salvar]  │
└────────────────────────────────────────────────────────┘
```

### Mobile Layout (<768px)
```
┌─────────────────────────┐
│  ← Editar Produto       │
├─────────────────────────┤
│  Mídia                   │
│  [★1] [2] [3] [+] → scroll
├─────────────────────────┤
│  ▼ Informações Básicas   │
│  [Form fields...]        │
│  ▼ Preço e Custos        │
│  ...                     │
├─────────────────────────┤
│  Cancelar      [Salvar]  │
└─────────────────────────┘
```

### Components
- New: `media-gallery.component.ts/html/scss` in `pages/products/`
- Modified: `product-form.component.ts/html/scss`
- Modified: `variant-manager.component.ts/html/scss`
- Modified: `product-variant.model.ts` (remove `custoEmbalagem` from costs, `freteGratis` from shipping)

---

## Technical Considerations

- Angular CDK DragDrop for image reordering (already a dependency from category tree)
- YouTube thumbnail: extract from URL using `https://img.youtube.com/vi/{VIDEO_ID}/mqdefault.jpg`
- Mock images: use colored placeholder divs with numbers (no actual image files needed)
- Gallery state managed locally in the form component via signals
- `HostListener` for paste event to support pasting YouTube URLs

---

## Open Questions

None — all resolved in conversation.
