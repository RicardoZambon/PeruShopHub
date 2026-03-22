export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  attributes: Record<string, string>;  // field name -> value
  price: number | null;                // null = use base price
  stock: number;
  isActive: boolean;
  needsReview: boolean;
}

export type CreateVariantDto = Pick<ProductVariant, 'sku' | 'attributes' | 'price' | 'stock' | 'isActive'>;
export type UpdateVariantDto = Partial<CreateVariantDto & { needsReview: boolean }>;
