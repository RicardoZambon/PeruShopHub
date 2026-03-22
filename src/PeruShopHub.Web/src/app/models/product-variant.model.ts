export interface VariantCosts {
  custoAquisicao: number | null;   // null = use base product cost
  custoEmbalagem: number | null;   // null = use base product cost
}

export interface VariantShipping {
  peso: number | null;             // kg — null = use base product value
  altura: number | null;           // cm
  largura: number | null;          // cm
  comprimento: number | null;      // cm
  freteGratis: boolean | null;     // null = use base product value
}

export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  attributes: Record<string, string>;  // field name -> value
  price: number | null;                // null = use base price
  costs: VariantCosts;
  shipping: VariantShipping;
  stock: number;
  isActive: boolean;
  needsReview: boolean;
}

export const DEFAULT_VARIANT_COSTS: VariantCosts = {
  custoAquisicao: null,
  custoEmbalagem: null,
};

export const DEFAULT_VARIANT_SHIPPING: VariantShipping = {
  peso: null,
  altura: null,
  largura: null,
  comprimento: null,
  freteGratis: null,
};

export type CreateVariantDto = Pick<ProductVariant, 'sku' | 'attributes' | 'price' | 'costs' | 'shipping' | 'stock' | 'isActive'>;
export type UpdateVariantDto = Partial<CreateVariantDto & { needsReview: boolean }>;
