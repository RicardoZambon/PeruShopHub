export interface ProductVariant {
  id: string;
  productId: string;
  sku: string;
  attributes: Record<string, string>;  // field name -> value
  price: number | null;                // null = use base price
  stock: number;
  isActive: boolean;
  needsReview: boolean;
  costs?: VariantCosts;
  shipping?: VariantShipping;
}

export interface VariantCosts {
  custoAquisicao: number | null;
}

export interface VariantShipping {
  peso: number | null;
  altura: number | null;
  largura: number | null;
  comprimento: number | null;
}

export const DEFAULT_VARIANT_COSTS: VariantCosts = {
  custoAquisicao: null,
};

export const DEFAULT_VARIANT_SHIPPING: VariantShipping = {
  peso: null,
  altura: null,
  largura: null,
  comprimento: null,
};

export type CreateVariantDto = Pick<ProductVariant, 'sku' | 'attributes' | 'price' | 'stock' | 'isActive'>;
export type UpdateVariantDto = Partial<CreateVariantDto & { needsReview: boolean; costs: VariantCosts; shipping: VariantShipping }>;
