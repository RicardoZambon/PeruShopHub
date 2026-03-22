import { Injectable, signal, inject } from '@angular/core';
import type {
  ProductVariant,
  CreateVariantDto,
  UpdateVariantDto,
} from '../models/product-variant.model';
import { CategoryService } from './category.service';

function generateId(): string {
  return Math.random().toString(36).substring(2, 11);
}

function delay(ms = 300): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Mock product-category associations
const PRODUCT_CATEGORIES: Record<string, string> = {
  'prod-1': 'cat-camisetas',
  'prod-2': 'cat-camisetas',
  'prod-3': 'cat-cabos',
};

// Pre-seeded variants for a Camiseta product
const SEED_VARIANTS: ProductVariant[] = [
  { id: 'var-1', productId: 'prod-1', sku: 'CAM-001-P-PRETO', attributes: { Cor: 'Preto', Tamanho: 'P' }, price: 49.90, stock: 15, isActive: true, needsReview: false },
  { id: 'var-2', productId: 'prod-1', sku: 'CAM-001-M-PRETO', attributes: { Cor: 'Preto', Tamanho: 'M' }, price: 49.90, stock: 23, isActive: true, needsReview: false },
  { id: 'var-3', productId: 'prod-1', sku: 'CAM-001-G-PRETO', attributes: { Cor: 'Preto', Tamanho: 'G' }, price: 54.90, stock: 8, isActive: true, needsReview: false },
  { id: 'var-4', productId: 'prod-1', sku: 'CAM-001-P-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'P' }, price: null, stock: 12, isActive: true, needsReview: false },
  { id: 'var-5', productId: 'prod-1', sku: 'CAM-001-M-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'M' }, price: null, stock: 18, isActive: true, needsReview: false },
  { id: 'var-6', productId: 'prod-1', sku: 'CAM-001-G-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'G' }, price: 54.90, stock: 5, isActive: true, needsReview: false },

  // Cable product variants
  { id: 'var-7', productId: 'prod-3', sku: 'CB-HDMI-1M-110V', attributes: { Comprimento: '1m', Voltagem: '110V' }, price: 29.90, stock: 30, isActive: true, needsReview: false },
  { id: 'var-8', productId: 'prod-3', sku: 'CB-HDMI-2M-110V', attributes: { Comprimento: '2m', Voltagem: '110V' }, price: 39.90, stock: 20, isActive: true, needsReview: false },
  { id: 'var-9', productId: 'prod-3', sku: 'CB-HDMI-1M-220V', attributes: { Comprimento: '1m', Voltagem: '220V' }, price: 29.90, stock: 25, isActive: true, needsReview: false },
];

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly categoryService = inject(CategoryService);
  private readonly variantsData = signal<ProductVariant[]>([...SEED_VARIANTS]);
  private readonly productCategoryMap = signal<Record<string, string>>({ ...PRODUCT_CATEGORIES });

  async getByProductId(productId: string): Promise<ProductVariant[]> {
    await delay();
    return this.variantsData().filter((v) => v.productId === productId);
  }

  async create(productId: string, dto: CreateVariantDto): Promise<ProductVariant> {
    await delay();
    const variant: ProductVariant = {
      id: generateId(),
      productId,
      sku: dto.sku,
      attributes: { ...dto.attributes },
      price: dto.price,
      stock: dto.stock,
      isActive: dto.isActive,
      needsReview: false,
    };

    this.variantsData.update((variants) => [...variants, variant]);
    return variant;
  }

  async update(variantId: string, dto: UpdateVariantDto): Promise<ProductVariant | undefined> {
    await delay();
    let updated: ProductVariant | undefined;
    this.variantsData.update((variants) =>
      variants.map((v) => {
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
    await delay();
    const exists = this.variantsData().some((v) => v.id === variantId);
    if (!exists) return false;

    this.variantsData.update((variants) =>
      variants.filter((v) => v.id !== variantId)
    );
    return true;
  }

  generateCombinations(
    fields: { name: string; values: string[] }[]
  ): { combinations: Record<string, string>[]; warning: boolean } {
    if (fields.length === 0 || fields.some((f) => f.values.length === 0)) {
      return { combinations: [], warning: false };
    }

    const combinations: Record<string, string>[] = [];

    const generate = (index: number, current: Record<string, string>): void => {
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
      (v) => v.sku === sku && v.id !== excludeId
    );
  }

  async flagForReview(categoryId: string): Promise<number> {
    await delay(100);
    const descendantIds = this.categoryService.getDescendantIds(categoryId);
    const affectedCategoryIds = [categoryId, ...descendantIds];

    // Find all products in affected categories
    const affectedProductIds: string[] = [];
    const map = this.productCategoryMap();
    for (const [productId, catId] of Object.entries(map)) {
      if (affectedCategoryIds.includes(catId)) {
        affectedProductIds.push(productId);
      }
    }

    // Flag all variants for those products
    let count = 0;
    this.variantsData.update((variants) =>
      variants.map((v) => {
        if (affectedProductIds.includes(v.productId) && !v.needsReview) {
          count++;
          return { ...v, needsReview: true };
        }
        return v;
      })
    );

    return count;
  }

  async flagForReviewByProduct(productId: string): Promise<void> {
    await delay(100);
    this.variantsData.update((variants) =>
      variants.map((v) => {
        if (v.productId === productId) {
          return { ...v, needsReview: true };
        }
        return v;
      })
    );
  }

  async clearReviewFlag(productId: string): Promise<void> {
    await delay(100);
    this.variantsData.update((variants) =>
      variants.map((v) => {
        if (v.productId === productId) {
          return { ...v, needsReview: false };
        }
        return v;
      })
    );
  }

  getAffectedProductCount(categoryId: string): number {
    const descendantIds = this.categoryService.getDescendantIds(categoryId);
    const affectedCategoryIds = [categoryId, ...descendantIds];

    const affectedProductIds = new Set<string>();
    const map = this.productCategoryMap();
    for (const [productId, catId] of Object.entries(map)) {
      if (affectedCategoryIds.includes(catId)) {
        affectedProductIds.add(productId);
      }
    }

    // Count variants for those products
    return this.variantsData().filter((v) =>
      affectedProductIds.has(v.productId)
    ).length;
  }

  async ensureDefaultVariant(productId: string, sku: string): Promise<ProductVariant> {
    const existing = this.variantsData().filter((v) => v.productId === productId);
    if (existing.length > 0) {
      return existing[0];
    }

    const variant: ProductVariant = {
      id: generateId(),
      productId,
      sku,
      attributes: {},
      price: null,
      stock: 0,
      isActive: true,
      needsReview: false,
    };

    this.variantsData.update((variants) => [...variants, variant]);
    return variant;
  }
}
