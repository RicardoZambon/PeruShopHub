import { Component, Input, signal, computed, inject, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  LucideAngularModule,
  ChevronDown,
  ChevronUp,
  Trash2,
  Plus,
  DollarSign,
  Shuffle,
  AlertTriangle,
  X,
} from 'lucide-angular';
import { ToggleSwitchComponent } from '../../shared/components/toggle-switch/toggle-switch.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import {
  ProductVariant,
  DEFAULT_VARIANT_COSTS,
  DEFAULT_VARIANT_SHIPPING,
} from '../../models/product-variant.model';
import { ProductVariantService } from '../../services/product-variant.service';
import { CategoryService } from '../../services/category.service';
import { ConfirmDialogService } from '../../shared/components';
import type { InheritedVariationField } from '../../models/category.model';

@Component({
  selector: 'app-variant-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, ToggleSwitchComponent, ButtonComponent],
  templateUrl: './variant-manager.component.html',
  styleUrl: './variant-manager.component.scss',
})
export class VariantManagerComponent implements OnChanges, OnInit {
  private variantService = inject(ProductVariantService);
  private categoryService = inject(CategoryService);
  private confirmDialog = inject(ConfirmDialogService);

  readonly chevronDownIcon = ChevronDown;
  readonly chevronUpIcon = ChevronUp;
  readonly trashIcon = Trash2;
  readonly plusIcon = Plus;
  readonly dollarIcon = DollarSign;
  readonly shuffleIcon = Shuffle;
  readonly alertIcon = AlertTriangle;
  readonly closeIcon = X;

  @Input() productId = '';
  @Input() categoryId = '';  // Accepts category ID (e.g., 'cat-fones') or category name (e.g., 'Áudio')
  @Input() productSku = 'PROD';

  private readonly variantsSignal = signal<ProductVariant[]>([]);
  variants = this.variantsSignal.asReadonly();

  ngOnInit(): void {
    this.loadVariants();
  }

  expandedRows = signal<Set<string>>(new Set());
  bulkPrice = signal<number | null>(null);

  // --- Variation field creation flow ---
  variationFields = signal<InheritedVariationField[]>([]);
  selectedValues = signal<Record<string, string[]>>({});
  textInputValues = signal<Record<string, string>>({});
  combinationWarning = signal(false);
  generating = signal(false);

  attributeKeys = computed(() => {
    const vs = this.variants();
    if (vs.length === 0) return [];
    const keys = new Set<string>();
    vs.forEach(v => Object.keys(v.attributes).forEach(k => keys.add(k)));
    return Array.from(keys);
  });

  combinationCount = computed(() => {
    const selected = this.selectedValues();
    const activeFields = Object.entries(selected).filter(([, vals]) => vals.length > 0);
    if (activeFields.length === 0) return 0;
    return activeFields.reduce((acc, [, vals]) => acc * vals.length, 1);
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId']) {
      this.loadVariationFields();
    }
    if (changes['productId']) {
      this.loadVariants();
    }
  }

  private async loadVariants(): Promise<void> {
    if (!this.productId) return;
    try {
      const variants = await this.variantService.getByProductId(this.productId);
      this.variantsSignal.set(variants);
    } catch {
      this.variantsSignal.set([]);
    }
  }

  private resolveCategoryId(input: string): string | null {
    if (!input) return null;

    // Try direct ID match first (e.g., 'cat-fones')
    const allCats = this.categoryService.allCategories();
    const byId = allCats.find(c => c.id === input);
    if (byId) return byId.id;

    // Fall back to name match (e.g., 'Áudio')
    const byName = allCats.find(c => c.name === input);
    if (byName) return byName.id;

    return null;
  }

  private async loadVariationFields(): Promise<void> {
    const resolvedId = this.resolveCategoryId(this.categoryId);
    if (!resolvedId) {
      this.variationFields.set([]);
      this.selectedValues.set({});
      this.textInputValues.set({});
      return;
    }

    try {
      const [own, inherited] = await Promise.all([
        this.categoryService.getVariationFields(resolvedId),
        this.categoryService.getInheritedVariationFields(resolvedId),
      ]);
      // Map own fields to InheritedVariationField shape for uniform handling
      const ownAsInherited: InheritedVariationField[] = own.map(f => ({
        ...f,
        inheritedFrom: '',
        inheritedFromId: resolvedId,
      }));
      const fields = [...inherited, ...ownAsInherited];
      this.variationFields.set(fields);

      // Initialize selected values map
      const selected: Record<string, string[]> = {};
      for (const field of fields) {
        selected[field.name] = [];
      }
      this.selectedValues.set(selected);
      this.textInputValues.set({});
    } catch {
      this.variationFields.set([]);
    }
  }

  // --- Field selection methods ---

  isOptionSelected(fieldName: string, option: string): boolean {
    return (this.selectedValues()[fieldName] ?? []).includes(option);
  }

  toggleOption(fieldName: string, option: string): void {
    const current = { ...this.selectedValues() };
    const values = [...(current[fieldName] ?? [])];
    const idx = values.indexOf(option);
    if (idx >= 0) {
      values.splice(idx, 1);
    } else {
      values.push(option);
    }
    current[fieldName] = values;
    this.selectedValues.set(current);
    this.combinationWarning.set(false);
  }

  getTextInputValue(fieldName: string): string {
    return this.textInputValues()[fieldName] ?? '';
  }

  updateTextInput(fieldName: string, value: string): void {
    const current = { ...this.textInputValues() };
    current[fieldName] = value;
    this.textInputValues.set(current);
  }

  addTextChip(fieldName: string): void {
    const value = (this.textInputValues()[fieldName] ?? '').trim();
    if (!value) return;

    const current = { ...this.selectedValues() };
    const values = [...(current[fieldName] ?? [])];
    if (!values.includes(value)) {
      values.push(value);
      current[fieldName] = values;
      this.selectedValues.set(current);
    }

    const inputs = { ...this.textInputValues() };
    inputs[fieldName] = '';
    this.textInputValues.set(inputs);
    this.combinationWarning.set(false);
  }

  onTextKeydown(event: KeyboardEvent, fieldName: string): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addTextChip(fieldName);
    }
  }

  removeTextChip(fieldName: string, value: string): void {
    const current = { ...this.selectedValues() };
    const values = (current[fieldName] ?? []).filter(v => v !== value);
    current[fieldName] = values;
    this.selectedValues.set(current);
    this.combinationWarning.set(false);
  }

  getSelectedValuesForField(fieldName: string): string[] {
    return this.selectedValues()[fieldName] ?? [];
  }

  // --- Generate combinations ---

  async generateCombinations(): Promise<void> {
    const selected = this.selectedValues();
    const fields = Object.entries(selected)
      .filter(([, vals]) => vals.length > 0)
      .map(([name, values]) => ({ name, values }));

    if (fields.length === 0) return;

    const result = this.variantService.generateCombinations(fields);

    if (result.warning) {
      this.combinationWarning.set(true);
      return;
    }

    this.generating.set(true);

    const baseSku = this.productSku || 'PROD';
    let index = this.variants().length;

    for (const combo of result.combinations) {
      index++;
      const skuParts = Object.values(combo)
        .map(v => v.toUpperCase().replace(/\s+/g, '').substring(0, 6));
      const sku = `${baseSku}-${skuParts.join('-')}`;

      await this.variantService.create(this.productId, {
        sku,
        attributes: combo,
        price: null,
        stock: 0,
        isActive: true,
      });
    }

    this.generating.set(false);
    await this.loadVariants();

    // Clear selections after generation
    const cleared: Record<string, string[]> = {};
    for (const field of this.variationFields()) {
      cleared[field.name] = [];
    }
    this.selectedValues.set(cleared);
    this.textInputValues.set({});
    this.combinationWarning.set(false);
  }

  forceGenerateCombinations(): void {
    this.combinationWarning.set(false);
    // Temporarily bypass warning for the next call
    const selected = this.selectedValues();
    const fields = Object.entries(selected)
      .filter(([, vals]) => vals.length > 0)
      .map(([name, values]) => ({ name, values }));

    if (fields.length === 0) return;

    this.generating.set(true);

    const baseSku = this.productSku || 'PROD';
    const result = this.variantService.generateCombinations(fields);
    let index = this.variants().length;

    const createAll = async () => {
      for (const combo of result.combinations) {
        index++;
        const skuParts = Object.values(combo)
          .map(v => v.toUpperCase().replace(/\s+/g, '').substring(0, 6));
        const sku = `${baseSku}-${skuParts.join('-')}`;

        await this.variantService.create(this.productId, {
          sku,
          attributes: combo,
          price: null,
          stock: 0,
          isActive: true,
        });
      }

      this.generating.set(false);
      await this.loadVariants();
      const cleared: Record<string, string[]> = {};
      for (const field of this.variationFields()) {
        cleared[field.name] = [];
      }
      this.selectedValues.set(cleared);
      this.textInputValues.set({});
    };

    createAll();
  }

  // --- Existing methods ---

  toggleExpand(variantId: string): void {
    const current = new Set(this.expandedRows());
    if (current.has(variantId)) {
      current.delete(variantId);
    } else {
      current.add(variantId);
    }
    this.expandedRows.set(current);
  }

  isExpanded(variantId: string): boolean {
    return this.expandedRows().has(variantId);
  }

  async updateVariantPrice(variantId: string, price: number | null): Promise<void> {
    await this.variantService.update(variantId, { price });
    this.variantsSignal.update(list => list.map(v => v.id === variantId ? { ...v, price } : v));
  }

  async updateVariantStock(variantId: string, stock: number): Promise<void> {
    await this.variantService.update(variantId, { stock });
    this.variantsSignal.update(list => list.map(v => v.id === variantId ? { ...v, stock } : v));
  }

  async updateVariantSku(variantId: string, sku: string): Promise<void> {
    await this.variantService.update(variantId, { sku });
    this.variantsSignal.update(list => list.map(v => v.id === variantId ? { ...v, sku } : v));
  }

  async updateVariantActive(variantId: string, isActive: boolean): Promise<void> {
    await this.variantService.update(variantId, { isActive });
    this.variantsSignal.update(list => list.map(v => v.id === variantId ? { ...v, isActive } : v));
  }

  async updateVariantCostAquisicao(variant: ProductVariant, value: number | null): Promise<void> {
    const costs = { ...(variant.costs ?? DEFAULT_VARIANT_COSTS), custoAquisicao: value };
    await this.variantService.update(variant.id, { costs });
    this.variantsSignal.update(list => list.map(v => v.id === variant.id ? { ...v, costs } : v));
  }

  async updateVariantShippingField(
    variant: ProductVariant,
    field: 'peso' | 'altura' | 'largura' | 'comprimento',
    value: number | null,
  ): Promise<void> {
    const shipping = { ...(variant.shipping ?? DEFAULT_VARIANT_SHIPPING), [field]: value };
    await this.variantService.update(variant.id, { shipping });
    this.variantsSignal.update(list => list.map(v => v.id === variant.id ? { ...v, shipping } : v));
  }

  async deleteVariant(variantId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Excluir variante',
      message: 'Tem certeza que deseja excluir esta variante?',
      confirmLabel: 'Excluir',
      variant: 'danger',
    });
    if (!confirmed) return;
    try {
      await this.variantService.delete(variantId);
      this.confirmDialog.done();
      this.variantsSignal.update(list => list.filter(v => v.id !== variantId));
    } catch {
      this.confirmDialog.done();
    }
  }

  updateBulkPrice(value: string): void {
    const num = parseFloat(value);
    this.bulkPrice.set(isNaN(num) ? null : num);
  }

  async applyBulkPrice(): Promise<void> {
    const price = this.bulkPrice();
    if (price === null) return;
    const vs = this.variants();
    for (const v of vs) {
      await this.variantService.update(v.id, { price });
    }
    this.variantsSignal.update(list => list.map(v => ({ ...v, price })));
    this.bulkPrice.set(null);
  }

  formatCurrency(value: number | null | undefined): string {
    if (value == null) return '-';
    return `R$ ${value.toFixed(2).replace('.', ',')}`;
  }
}
