import { Injectable, signal, inject } from '@angular/core';
import { ProductVariant, CreateVariantDto, UpdateVariantDto } from '../models/product-variant.model';
import { CategoryService } from './category.service';

function generateId(): string {
  return 'var-' + Math.random().toString(36).substring(2, 10);
}

// Pre-seeded variants (Camiseta product with Cor + Tamanho combos)
const SEED_VARIANTS: ProductVariant[] = [
  { id: 'var-001', productId: '1', sku: 'FN-BT-001-PRETO', attributes: { 'Cor': 'Preto' }, price: null, stock: 15, isActive: true, needsReview: false },
  { id: 'var-002', productId: '1', sku: 'FN-BT-001-BRANCO', attributes: { 'Cor': 'Branco' }, price: null, stock: 12, isActive: true, needsReview: false },
  { id: 'var-003', productId: '1', sku: 'FN-BT-001-AZUL', attributes: { 'Cor': 'Azul' }, price: 199.90, stock: 8, isActive: true, needsReview: false },

  // Product 4 (Smartwatch with Cor variants)
  { id: 'var-004', productId: '4', sku: 'SW-FIT-004-PRETO-P', attributes: { 'Cor': 'Preto', 'Tamanho': 'P' }, price: null, stock: 3, isActive: true, needsReview: false },
  { id: 'var-005', productId: '4', sku: 'SW-FIT-004-PRETO-M', attributes: { 'Cor': 'Preto', 'Tamanho': 'M' }, price: null, stock: 5, isActive: true, needsReview: false },
  { id: 'var-006', productId: '4', sku: 'SW-FIT-004-PRETO-G', attributes: { 'Cor': 'Preto', 'Tamanho': 'G' }, price: 269.90, stock: 2, isActive: true, needsReview: false },
  { id: 'var-007', productId: '4', sku: 'SW-FIT-004-BRANCO-P', attributes: { 'Cor': 'Branco', 'Tamanho': 'P' }, price: null, stock: 0, isActive: true, needsReview: false },
  { id: 'var-008', productId: '4', sku: 'SW-FIT-004-BRANCO-M', attributes: { 'Cor': 'Branco', 'Tamanho': 'M' }, price: null, stock: 4, isActive: true, needsReview: false },
  { id: 'var-009', productId: '4', sku: 'SW-FIT-004-BRANCO-G', attributes: { 'Cor': 'Branco', 'Tamanho': 'G' }, price: 269.90, stock: 1, isActive: true, needsReview: false },
  { id: 'var-010', productId: '4', sku: 'SW-FIT-004-AZUL-P', attributes: { 'Cor': 'Azul', 'Tamanho': 'P' }, price: null, stock: 6, isActive: true, needsReview: false },
  { id: 'var-011', productId: '4', sku: 'SW-FIT-004-AZUL-M', attributes: { 'Cor': 'Azul', 'Tamanho': 'M' }, price: null, stock: 3, isActive: true, needsReview: false },
  { id: 'var-012', productId: '4', sku: 'SW-FIT-004-AZUL-G', attributes: { 'Cor': 'Azul', 'Tamanho': 'G' }, price: 269.90, stock: 0, isActive: true, needsReview: true },
];

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly categoryService = inject(CategoryService);
  private readonly variantsData = signal<ProductVariant[]>([...SEED_VARIANTS]);

  readonly variants = this.variantsData.asReadonly();

  private delay(): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, 300));
  }

  getByProductId(productId: string): ProductVariant[] {
    return this.variantsData().filter(v => v.productId === productId);
  }

  getVariantCount(productId: string): number {
    return this.variantsData().filter(v => v.productId === productId).length;
  }

  hasNeedsReview(productId: string): boolean {
    return this.variantsData().some(v => v.productId === productId && v.needsReview);
  }

  async create(productId: string, dto: CreateVariantDto): Promise<ProductVariant> {
    await this.delay();
    const variant: ProductVariant = {
      id: generateId(),
      productId,
      sku: dto.sku,
      attributes: dto.attributes,
      price: dto.price,
      stock: dto.stock,
      isActive: dto.isActive,
      needsReview: false,
    };
    this.variantsData.update(vars => [...vars, variant]);
    return variant;
  }

  async update(variantId: string, dto: UpdateVariantDto): Promise<ProductVariant | undefined> {
    await this.delay();
    let updated: ProductVariant | undefined;
    this.variantsData.update(vars =>
      vars.map(v => {
        if (v.id === variantId) {
          updated = { ...v, ...dto };
          return updated;
        }
        return v;
      })
    );
    return updated;
  }

  async delete(variantId: string): Promise<boolean> {
    await this.delay();
    this.variantsData.update(vars => vars.filter(v => v.id !== variantId));
    return true;
  }

  async deleteByProductId(productId: string): Promise<void> {
    await this.delay();
    this.variantsData.update(vars => vars.filter(v => v.productId !== productId));
  }

  generateCombinations(
    fields: { name: string; values: string[] }[]
  ): { combinations: Record<string, string>[]; warning: boolean } {
    if (fields.length === 0 || fields.some(f => f.values.length === 0)) {
      return { combinations: [], warning: false };
    }

    const combinations: Record<string, string>[] = [];

    const generate = (index: number, current: Record<string, string>) => {
      if (index === fields.length) {
        combinations.push({ ...current });
        return;
      }
      const field = fields[index];
      for (const value of field.values) {
        current[field.name] = value;
        generate(index + 1, current);
      }
    };

    generate(0, {});

    return {
      combinations,
      warning: combinations.length > 100,
    };
  }

  isSkuUnique(sku: string, excludeId?: string): boolean {
    return !this.variantsData().some(
      v => v.sku.toLowerCase() === sku.toLowerCase() && v.id !== excludeId
    );
  }

  flagForReview(categoryId: string): number {
    const descendantIds = this.categoryService.getDescendantIds(categoryId);
    const allCategoryIds = [categoryId, ...descendantIds];

    // In a real app, we'd look up products by category and flag their variants
    // For mock purposes, flag all variants that have attributes matching category fields
    let count = 0;
    this.variantsData.update(vars =>
      vars.map(v => {
        // Simple mock: flag variants that aren't already flagged
        if (!v.needsReview && Object.keys(v.attributes).length > 0) {
          count++;
          return { ...v, needsReview: true };
        }
        return v;
      })
    );
    return count;
  }

  flagForReviewByProduct(productId: string): void {
    this.variantsData.update(vars =>
      vars.map(v => {
        if (v.productId === productId) {
          return { ...v, needsReview: true };
        }
        return v;
      })
    );
  }

  clearReviewFlag(productId: string): void {
    this.variantsData.update(vars =>
      vars.map(v => {
        if (v.productId === productId && v.needsReview) {
          return { ...v, needsReview: false };
        }
        return v;
      })
    );
  }

  getAffectedProductCount(categoryId: string): number {
    // Mock: return count of products that have variants
    const productIds = new Set(this.variantsData().map(v => v.productId));
    return productIds.size;
  }

  /** Ensure a default variant exists for a product without variants */
  ensureDefaultVariant(productId: string, sku: string, stock: number = 0): ProductVariant {
    const existing = this.getByProductId(productId);
    if (existing.length > 0) {
      return existing[0];
    }

    const variant: ProductVariant = {
      id: generateId(),
      productId,
      sku,
      attributes: {},
      price: null,
      stock,
      isActive: true,
      needsReview: false,
    };
    this.variantsData.update(vars => [...vars, variant]);
    return variant;
  }
}
