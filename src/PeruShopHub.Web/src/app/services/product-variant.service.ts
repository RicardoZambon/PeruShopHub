import { Injectable, signal, computed } from '@angular/core';
import {
  ProductVariant,
  CreateVariantDto,
  UpdateVariantDto,
  DEFAULT_VARIANT_COSTS,
  DEFAULT_VARIANT_SHIPPING,
} from '../models/product-variant.model';

export interface CombinationResult {
  combinations: Record<string, string>[];
  warning: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly variantsSignal = signal<ProductVariant[]>(SEED_VARIANTS);

  /** All variants across all products */
  readonly variants = this.variantsSignal.asReadonly();

  /** Total variant count */
  readonly totalCount = computed(() => this.variantsSignal().length);

  // ---------------------------------------------------------------------------
  // CRUD
  // ---------------------------------------------------------------------------

  getByProductId(productId: string): ProductVariant[] {
    return this.variantsSignal().filter(v => v.productId === productId);
  }

  async create(productId: string, dto: CreateVariantDto): Promise<ProductVariant> {
    await this.delay();
    const variant: ProductVariant = {
      id: this.generateId(),
      productId,
      sku: dto.sku,
      attributes: { ...dto.attributes },
      price: dto.price,
      stock: dto.stock,
      isActive: dto.isActive,
      needsReview: false,
      costs: { ...DEFAULT_VARIANT_COSTS },
      shipping: { ...DEFAULT_VARIANT_SHIPPING },
    };
    this.variantsSignal.update(list => [...list, variant]);
    return variant;
  }

  async update(variantId: string, dto: UpdateVariantDto): Promise<ProductVariant | undefined> {
    await this.delay();
    let updated: ProductVariant | undefined;
    this.variantsSignal.update(list =>
      list.map(v => {
        if (v.id !== variantId) return v;
        updated = { ...v, ...dto };
        return updated;
      }),
    );
    return updated;
  }

  async delete(variantId: string): Promise<boolean> {
    await this.delay();
    const before = this.variantsSignal().length;
    this.variantsSignal.update(list => list.filter(v => v.id !== variantId));
    return this.variantsSignal().length < before;
  }

  // ---------------------------------------------------------------------------
  // Combination generator
  // ---------------------------------------------------------------------------

  generateCombinations(fields: { name: string; values: string[] }[]): CombinationResult {
    // Filter out fields with no values
    const activeFields = fields.filter(f => f.values.length > 0);
    if (activeFields.length === 0) {
      return { combinations: [], warning: false };
    }

    // Cartesian product
    const combinations: Record<string, string>[] = [{}];
    for (const field of activeFields) {
      const newCombinations: Record<string, string>[] = [];
      for (const existing of combinations) {
        for (const value of field.values) {
          newCombinations.push({ ...existing, [field.name]: value });
        }
      }
      combinations.length = 0;
      combinations.push(...newCombinations);
    }

    return {
      combinations,
      warning: combinations.length > 100,
    };
  }

  // ---------------------------------------------------------------------------
  // SKU validation
  // ---------------------------------------------------------------------------

  isSkuUnique(sku: string, excludeId?: string): boolean {
    return !this.variantsSignal().some(
      v => v.sku.toLowerCase() === sku.toLowerCase() && v.id !== excludeId,
    );
  }

  // ---------------------------------------------------------------------------
  // Review flags
  // ---------------------------------------------------------------------------

  /**
   * Flag all variants for products in the given category (and descendants)
   * as needing review. In a real app, this would query products by category.
   * For mock purposes, we flag variants that belong to pre-seeded product IDs.
   */
  flagForReview(categoryId: string): number {
    // Mock: map known categories to product IDs for demo purposes
    const categoryProductMap: Record<string, string[]> = {
      'cat-fones': ['1'],
      'cat-audio': ['1', '5'],
      'cat-eletronicos': ['1', '4', '5'],
      'cat-cabos': ['5'],
      'cat-moda': ['4'],
    };

    const productIds = categoryProductMap[categoryId] ?? [];
    let count = 0;

    this.variantsSignal.update(list =>
      list.map(v => {
        if (productIds.includes(v.productId) && !v.needsReview) {
          count++;
          return { ...v, needsReview: true };
        }
        return v;
      }),
    );

    return count;
  }

  flagForReviewByProduct(productId: string): void {
    this.variantsSignal.update(list =>
      list.map(v =>
        v.productId === productId ? { ...v, needsReview: true } : v,
      ),
    );
  }

  clearReviewFlag(productId: string): void {
    this.variantsSignal.update(list =>
      list.map(v =>
        v.productId === productId ? { ...v, needsReview: false } : v,
      ),
    );
  }

  getAffectedProductCount(categoryId: string): number {
    // Mock: return count based on known seed data
    const categoryProductMap: Record<string, string[]> = {
      'cat-fones': ['1'],
      'cat-audio': ['1', '5'],
      'cat-eletronicos': ['1', '4', '5'],
      'cat-cabos': ['5'],
      'cat-moda': ['4'],
    };

    const productIds = new Set(categoryProductMap[categoryId] ?? []);
    return productIds.size;
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private generateId(): string {
    return 'var-' + Math.random().toString(36).substring(2, 10);
  }

  private delay(ms = 300): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// =============================================================================
// Seed data — Products 1, 4, 5 with variants matching numeric product IDs
// =============================================================================

const SEED_VARIANTS: ProductVariant[] = [
  // Product 1: Fone Bluetooth TWS Pro Max (category: cat-fones) — Voltagem variants
  { id: 'var-001', productId: '1', sku: 'FN-BT-001-110V',  attributes: { Voltagem: '110V' },                    price: null,   stock: 15, isActive: true,  needsReview: false, costs: { custoAquisicao: 62.00 }, shipping: { peso: 0.25, altura: 8, largura: 12, comprimento: 15 } },
  { id: 'var-002', productId: '1', sku: 'FN-BT-001-220V',  attributes: { Voltagem: '220V' },                    price: null,   stock: 20, isActive: true,  needsReview: false, costs: { custoAquisicao: 62.00 }, shipping: { peso: 0.25, altura: 8, largura: 12, comprimento: 15 } },
  { id: 'var-003', productId: '1', sku: 'FN-BT-001-BIV',   attributes: { Voltagem: 'Bivolt' },                  price: 199.90, stock: 10, isActive: true,  needsReview: false, costs: { custoAquisicao: 65.00 }, shipping: { peso: 0.25, altura: 8, largura: 12, comprimento: 15 } },

  // Product 4: Smartwatch Fitness Band Pro — Cor x Tamanho variants
  { id: 'var-004', productId: '4', sku: 'SW-FIT-004-P-PRETO',   attributes: { Cor: 'Preto',  Tamanho: 'P' },    price: null,   stock: 5,  isActive: true,  needsReview: true, costs: { custoAquisicao: 95.00 }, shipping: { peso: 0.15, altura: 5, largura: 8, comprimento: 10 } },
  { id: 'var-005', productId: '4', sku: 'SW-FIT-004-M-PRETO',   attributes: { Cor: 'Preto',  Tamanho: 'M' },    price: null,   stock: 3,  isActive: true,  needsReview: true, costs: { custoAquisicao: 95.00 }, shipping: { peso: 0.16, altura: 5, largura: 8, comprimento: 10 } },
  { id: 'var-006', productId: '4', sku: 'SW-FIT-004-G-PRETO',   attributes: { Cor: 'Preto',  Tamanho: 'G' },    price: 269.90, stock: 2,  isActive: true,  needsReview: true, costs: { custoAquisicao: 98.00 }, shipping: { peso: 0.17, altura: 5, largura: 8, comprimento: 10 } },
  { id: 'var-007', productId: '4', sku: 'SW-FIT-004-P-BRANCO',  attributes: { Cor: 'Branco', Tamanho: 'P' },    price: null,   stock: 4,  isActive: true,  needsReview: true, costs: { custoAquisicao: 95.00 }, shipping: { peso: 0.15, altura: 5, largura: 8, comprimento: 10 } },
  { id: 'var-008', productId: '4', sku: 'SW-FIT-004-M-BRANCO',  attributes: { Cor: 'Branco', Tamanho: 'M' },    price: null,   stock: 0,  isActive: false, needsReview: true, costs: { custoAquisicao: 95.00 }, shipping: { peso: 0.16, altura: 5, largura: 8, comprimento: 10 } },
  { id: 'var-009', productId: '4', sku: 'SW-FIT-004-G-BRANCO',  attributes: { Cor: 'Branco', Tamanho: 'G' },    price: 269.90, stock: 1,  isActive: true,  needsReview: true, costs: { custoAquisicao: 98.00 }, shipping: { peso: 0.17, altura: 5, largura: 8, comprimento: 10 } },

  // Product 5: Cabo HDMI 2.1 4K 2m (category: cat-cabos) — Comprimento variants
  { id: 'var-010', productId: '5', sku: 'CB-HDMI-005-1M',  attributes: { Comprimento: '1m' },                   price: 29.90,  stock: 50, isActive: true,  needsReview: false, costs: { custoAquisicao: 8.50 },  shipping: { peso: 0.05, altura: 2, largura: 5, comprimento: 15 } },
  { id: 'var-011', productId: '5', sku: 'CB-HDMI-005-2M',  attributes: { Comprimento: '2m' },                   price: 39.90,  stock: 35, isActive: true,  needsReview: false, costs: { custoAquisicao: 10.00 }, shipping: { peso: 0.07, altura: 2, largura: 5, comprimento: 20 } },
  { id: 'var-012', productId: '5', sku: 'CB-HDMI-005-3M',  attributes: { Comprimento: '3m' },                   price: 49.90,  stock: 20, isActive: true,  needsReview: false, costs: { custoAquisicao: 12.50 }, shipping: { peso: 0.09, altura: 2, largura: 5, comprimento: 25 } },
];
