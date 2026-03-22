import { Component, Input, inject, signal, computed, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus, Trash2, AlertTriangle } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';
import { ProductVariantService } from '../../services/product-variant.service';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { BrlCurrencyPipe } from '../../shared/pipes/brl-currency.pipe';
import type { InheritedVariationField } from '../../models/category.model';
import type { ProductVariant } from '../../models/product-variant.model';

interface FieldValues {
  field: InheritedVariationField;
  selectedValues: string[];
  chipInput: string;
}

interface EditableVariant extends ProductVariant {
  skuError: string | null;
}

@Component({
  selector: 'app-variant-manager',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, BrlCurrencyPipe],
  templateUrl: './variant-manager.component.html',
  styleUrl: './variant-manager.component.scss',
})
export class VariantManagerComponent implements OnChanges {
  private readonly categoryService = inject(CategoryService);
  private readonly variantService = inject(ProductVariantService);

  readonly plusIcon = Plus;
  readonly trashIcon = Trash2;
  readonly alertIcon = AlertTriangle;

  @Input() categoryId: string | null = null;
  @Input() productSku: string = 'PROD';
  @Input() productId: string = '';

  variationFields = signal<InheritedVariationField[]>([]);
  fieldValues = signal<FieldValues[]>([]);
  variants = signal<EditableVariant[]>([]);
  bulkPrice = signal<number | null>(null);
  bulkStock = signal<number | null>(null);
  combinationWarning = signal(false);
  isDefaultVariant = signal(false);

  activeVariantCount = computed(() =>
    this.variants().filter(v => v.isActive).length
  );

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['categoryId'] || changes['productId']) {
      this.loadFields();
      this.loadExistingVariants();
    }
  }

  private loadFields(): void {
    if (!this.categoryId) {
      this.variationFields.set([]);
      this.fieldValues.set([]);
      return;
    }

    const fields = this.categoryService.getAllVariationFieldsForCategory(this.categoryId);
    this.variationFields.set(fields);

    this.fieldValues.set(
      fields.map(f => ({
        field: f,
        selectedValues: [],
        chipInput: '',
      }))
    );
  }

  private loadExistingVariants(): void {
    if (!this.productId) {
      this.variants.set([]);
      this.isDefaultVariant.set(false);
      return;
    }

    const existing = this.variantService.getByProductId(this.productId);
    if (existing.length === 1 && Object.keys(existing[0].attributes).length === 0) {
      this.isDefaultVariant.set(true);
    } else {
      this.isDefaultVariant.set(false);
    }
    this.variants.set(existing.map(v => ({ ...v, skuError: null })));
  }

  // --- Chip input for text fields ---
  onChipInputKeydown(event: KeyboardEvent, fieldIndex: number): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      const fv = this.fieldValues()[fieldIndex];
      const value = fv.chipInput.trim();
      if (value && !fv.selectedValues.includes(value)) {
        this.fieldValues.update(arr => {
          const next = [...arr];
          next[fieldIndex] = {
            ...fv,
            selectedValues: [...fv.selectedValues, value],
            chipInput: '',
          };
          return next;
        });
      }
    }
  }

  updateChipInput(fieldIndex: number, value: string): void {
    this.fieldValues.update(arr => {
      const next = [...arr];
      next[fieldIndex] = { ...next[fieldIndex], chipInput: value };
      return next;
    });
  }

  removeChip(fieldIndex: number, chipValue: string): void {
    this.fieldValues.update(arr => {
      const next = [...arr];
      const fv = next[fieldIndex];
      next[fieldIndex] = {
        ...fv,
        selectedValues: fv.selectedValues.filter(v => v !== chipValue),
      };
      return next;
    });
  }

  // --- Select field toggles ---
  toggleSelectOption(fieldIndex: number, option: string): void {
    this.fieldValues.update(arr => {
      const next = [...arr];
      const fv = next[fieldIndex];
      const has = fv.selectedValues.includes(option);
      next[fieldIndex] = {
        ...fv,
        selectedValues: has
          ? fv.selectedValues.filter(v => v !== option)
          : [...fv.selectedValues, option],
      };
      return next;
    });
  }

  isOptionSelected(fieldIndex: number, option: string): boolean {
    return this.fieldValues()[fieldIndex]?.selectedValues.includes(option) ?? false;
  }

  // --- Generate combinations ---
  generateCombinations(): void {
    const fields = this.fieldValues()
      .filter(fv => fv.selectedValues.length > 0)
      .map(fv => ({ name: fv.field.name, values: fv.selectedValues }));

    if (fields.length === 0) return;

    const result = this.variantService.generateCombinations(fields);
    this.combinationWarning.set(result.warning);

    const newVariants: EditableVariant[] = result.combinations.map((attrs, idx) => {
      const skuParts = Object.values(attrs).map(v =>
        v.toUpperCase().replace(/\s+/g, '-').substring(0, 6)
      );
      const sku = `${this.productSku}-${skuParts.join('-')}`.toUpperCase();

      return {
        id: 'new-' + Math.random().toString(36).substring(2, 8),
        productId: this.productId,
        sku,
        attributes: { ...attrs },
        price: null,
        stock: 0,
        isActive: true,
        needsReview: false,
        skuError: null,
      };
    });

    this.variants.set(newVariants);
    this.isDefaultVariant.set(false);
  }

  // --- Variant editing ---
  updateVariantPrice(index: number, value: string): void {
    this.variants.update(arr => {
      const next = [...arr];
      next[index] = { ...next[index], price: value ? parseFloat(value) : null };
      return next;
    });
  }

  updateVariantStock(index: number, value: string): void {
    this.variants.update(arr => {
      const next = [...arr];
      next[index] = { ...next[index], stock: parseInt(value, 10) || 0 };
      return next;
    });
  }

  updateVariantSku(index: number, value: string): void {
    this.variants.update(arr => {
      const next = [...arr];
      const isUnique = this.variantService.isSkuUnique(value, next[index].id);
      next[index] = {
        ...next[index],
        sku: value,
        skuError: isUnique ? null : 'SKU já está em uso',
      };
      return next;
    });
  }

  toggleVariantActive(index: number): void {
    this.variants.update(arr => {
      const next = [...arr];
      next[index] = { ...next[index], isActive: !next[index].isActive };
      return next;
    });
  }

  deleteVariant(index: number): void {
    this.variants.update(arr => arr.filter((_, i) => i !== index));
  }

  // --- Bulk actions ---
  applyBulkPrice(): void {
    const price = this.bulkPrice();
    if (price === null || price === undefined) return;
    this.variants.update(arr =>
      arr.map(v => ({ ...v, price }))
    );
  }

  applyBulkStock(): void {
    const stock = this.bulkStock();
    if (stock === null || stock === undefined) return;
    this.variants.update(arr =>
      arr.map(v => ({ ...v, stock }))
    );
  }

  updateBulkPrice(value: string): void {
    this.bulkPrice.set(value ? parseFloat(value) : null);
  }

  updateBulkStock(value: string): void {
    this.bulkStock.set(value ? parseInt(value, 10) : null);
  }

  // --- Helpers ---
  getVariantFieldNames(): string[] {
    if (this.variants().length === 0) return [];
    return Object.keys(this.variants()[0].attributes);
  }

  getVariantRowClass(variant: EditableVariant): string {
    if (variant.needsReview) return 'variant-row--review';
    if (variant.stock === 0) return 'variant-row--danger';
    if (variant.stock > 0 && variant.stock <= 5) return 'variant-row--warning';
    return '';
  }
}
