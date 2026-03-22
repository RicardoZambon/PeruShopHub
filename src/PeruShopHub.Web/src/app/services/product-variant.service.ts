import { Injectable, signal, inject } from '@angular/core';
import type {
  ProductVariant,
  CreateVariantDto,
  UpdateVariantDto,
} from '../models/product-variant.model';
import { DEFAULT_VARIANT_COSTS, DEFAULT_VARIANT_SHIPPING } from '../models/product-variant.model';
import { CategoryService } from './category.service';

function generateId(): string {
  return Math.random().toString(36).substring(2, 11);
}

function delay(ms = 300): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Mock product-category associations
// IDs match MOCK_PRODUCTS in products-list (1-10) and product-form (1)
const PRODUCT_CATEGORIES: Record<string, string> = {
  '1': 'cat-fones',
  '4': 'cat-camisetas',
  '5': 'cat-cabos',
};

const DC = { ...DEFAULT_VARIANT_COSTS };
const DS = { ...DEFAULT_VARIANT_SHIPPING };

// Pre-seeded variants — IDs match products-list mock data (1-10)
// Product 1: Fone Bluetooth TWS Pro Max (cat-fones, inherits Voltagem from Eletrônicos)
// Product 4: Smartwatch Fitness Band Pro (cat-camisetas for demo — has Cor + Tamanho)
// Product 5: Cabo HDMI 2.1 4K 2m (cat-cabos — has Comprimento + Voltagem)
const SEED_VARIANTS: ProductVariant[] = [
  // Fone variants — different Voltagem
  { id: 'var-1', productId: '1', sku: 'FN-BT-001-110V', attributes: { Voltagem: '110V' }, price: null, costs: { ...DC }, shipping: { ...DS }, stock: 20, isActive: true, needsReview: false },
  { id: 'var-2', productId: '1', sku: 'FN-BT-001-220V', attributes: { Voltagem: '220V' }, price: null, costs: { ...DC }, shipping: { ...DS }, stock: 15, isActive: true, needsReview: false },
  { id: 'var-3', productId: '1', sku: 'FN-BT-001-BIVOLT', attributes: { Voltagem: 'Bivolt' }, price: 199.90, costs: { custoAquisicao: 68.00, custoEmbalagem: null }, shipping: { ...DS, peso: 0.28 }, stock: 10, isActive: true, needsReview: false },

  // Camiseta variants — Cor x Tamanho (different costs + shipping per size)
  { id: 'var-4', productId: '4', sku: 'SW-FIT-P-PRETO', attributes: { Cor: 'Preto', Tamanho: 'P' }, price: 49.90, costs: { ...DC }, shipping: { ...DS, peso: 0.18 }, stock: 15, isActive: true, needsReview: false },
  { id: 'var-5', productId: '4', sku: 'SW-FIT-M-PRETO', attributes: { Cor: 'Preto', Tamanho: 'M' }, price: 49.90, costs: { ...DC }, shipping: { ...DS, peso: 0.20 }, stock: 23, isActive: true, needsReview: false },
  { id: 'var-6', productId: '4', sku: 'SW-FIT-G-PRETO', attributes: { Cor: 'Preto', Tamanho: 'G' }, price: 54.90, costs: { custoAquisicao: 22.00, custoEmbalagem: null }, shipping: { ...DS, peso: 0.22 }, stock: 8, isActive: true, needsReview: false },
  { id: 'var-7', productId: '4', sku: 'SW-FIT-P-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'P' }, price: null, costs: { ...DC }, shipping: { ...DS, peso: 0.18 }, stock: 12, isActive: true, needsReview: false },
  { id: 'var-8', productId: '4', sku: 'SW-FIT-M-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'M' }, price: null, costs: { ...DC }, shipping: { ...DS, peso: 0.20 }, stock: 18, isActive: true, needsReview: false },
  { id: 'var-9', productId: '4', sku: 'SW-FIT-G-BRANCO', attributes: { Cor: 'Branco', Tamanho: 'G' }, price: 54.90, costs: { custoAquisicao: 22.00, custoEmbalagem: null }, shipping: { ...DS, peso: 0.22 }, stock: 5, isActive: true, needsReview: false },

  // Cable variants — different lengths affect weight, dimensions, and cost
  { id: 'var-10', productId: '5', sku: 'CB-HDMI-1M-110V', attributes: { Comprimento: '1m', Voltagem: '110V' }, price: 29.90, costs: { custoAquisicao: 8.50, custoEmbalagem: 1.20 }, shipping: { peso: 0.08, altura: 3, largura: 12, comprimento: 14, freteGratis: null }, stock: 0, isActive: true, needsReview: false },
  { id: 'var-11', productId: '5', sku: 'CB-HDMI-2M-110V', attributes: { Comprimento: '2m', Voltagem: '110V' }, price: 39.90, costs: { custoAquisicao: 12.00, custoEmbalagem: 1.50 }, shipping: { peso: 0.14, altura: 3, largura: 12, comprimento: 22, freteGratis: null }, stock: 0, isActive: false, needsReview: false },
  { id: 'var-12', productId: '5', sku: 'CB-HDMI-1M-220V', attributes: { Comprimento: '1m', Voltagem: '220V' }, price: 29.90, costs: { custoAquisicao: 9.00, custoEmbalagem: 1.20 }, shipping: { peso: 0.09, altura: 3, largura: 12, comprimento: 14, freteGratis: null }, stock: 25, isActive: true, needsReview: false },
];

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly categoryService = inject(CategoryService);
  private readonly variantsData = signal<ProductVariant[]>([...SEED_VARIANTS]);
  private readonly productCategoryMap = signal<Record<string, string>>({ ...PRODUCT_CATEGORIES });

  getByProductId(productId: string): ProductVariant[] {
    return this.variantsData().filter((v) => v.productId === productId);
  }

  deleteByProductId(productId: string): void {
    this.variantsData.update((variants) =>
      variants.filter((v) => v.productId !== productId)
    );
  }

  async create(productId: string, dto: CreateVariantDto): Promise<ProductVariant> {
    await delay();
    const variant: ProductVariant = {
      id: generateId(),
      productId,
      sku: dto.sku,
      attributes: { ...dto.attributes },
      price: dto.price,
      costs: dto.costs ? { ...dto.costs } : { ...DEFAULT_VARIANT_COSTS },
      shipping: dto.shipping ? { ...dto.shipping } : { ...DEFAULT_VARIANT_SHIPPING },
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
      costs: { ...DEFAULT_VARIANT_COSTS },
      shipping: { ...DEFAULT_VARIANT_SHIPPING },
      stock: 0,
      isActive: true,
      needsReview: false,
    };

    this.variantsData.update((variants) => [...variants, variant]);
    return variant;
  }
}
