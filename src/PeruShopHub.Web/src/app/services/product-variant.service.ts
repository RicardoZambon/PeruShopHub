import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
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
  private readonly http = inject(HttpClient);
  private readonly variantsSignal = signal<ProductVariant[]>([]);

  /** All variants across all products */
  readonly variants = this.variantsSignal.asReadonly();

  /** Total variant count */
  readonly totalCount = computed(() => this.variantsSignal().length);

  // ---------------------------------------------------------------------------
  // CRUD
  // ---------------------------------------------------------------------------

  async getByProductId(productId: string): Promise<ProductVariant[]> {
    const variants = await firstValueFrom(
      this.http.get<ProductVariant[]>(`/api/products/${productId}/variants`),
    );
    // Update local cache
    this.variantsSignal.update((list) => {
      const otherProducts = list.filter((v) => v.productId !== productId);
      return [...otherProducts, ...variants];
    });
    return variants;
  }

  async create(productId: string, dto: CreateVariantDto): Promise<ProductVariant> {
    const variant = await firstValueFrom(
      this.http.post<ProductVariant>(`/api/products/${productId}/variants`, dto),
    );
    this.variantsSignal.update((list) => [...list, variant]);
    return variant;
  }

  async update(variantId: string, dto: UpdateVariantDto): Promise<ProductVariant | undefined> {
    const updated = await firstValueFrom(
      this.http.put<ProductVariant>(`/api/variants/${variantId}`, dto),
    );
    this.variantsSignal.update((list) =>
      list.map((v) => (v.id === variantId ? updated : v)),
    );
    return updated;
  }

  async delete(variantId: string): Promise<boolean> {
    try {
      await firstValueFrom(
        this.http.delete<void>(`/api/variants/${variantId}`),
      );
      this.variantsSignal.update((list) => list.filter((v) => v.id !== variantId));
      return true;
    } catch {
      return false;
    }
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

  flagForReview(categoryId: string): number {
    // In a real implementation, this would call an API endpoint
    let count = 0;
    this.variantsSignal.update(list =>
      list.map(v => {
        if (!v.needsReview) {
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
    const productIds = new Set(this.variantsSignal().map(v => v.productId));
    return productIds.size;
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  getCachedByProductId(productId: string): ProductVariant[] {
    return this.variantsSignal().filter(v => v.productId === productId);
  }
}
