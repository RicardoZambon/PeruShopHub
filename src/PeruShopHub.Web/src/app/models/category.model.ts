export interface Category {
  id: string;
  name: string;
  slug: string;
  parentId: string | null;
  children: Category[];
  icon: string | null;
  isActive: boolean;
  productCount: number;
  order: number;
  createdAt: string;
  updatedAt: string;
}

export interface VariationField {
  id: string;
  categoryId: string;
  name: string;
  type: 'text' | 'select';
  options: string[];   // used when type === 'select'
  required: boolean;
  order: number;
}

export interface InheritedVariationField extends VariationField {
  inheritedFrom: string;       // category name
  inheritedFromId: string;     // category id
}

export interface CreateCategoryDto {
  name: string;
  slug: string;
  parentId: string | null;
  icon: string | null;
  order: number;
}
export interface UpdateCategoryDto {
  name?: string;
  slug?: string;
  parentId?: string | null;
  icon?: string | null;
  isActive?: boolean;
  order?: number;
}
export type CreateVariationFieldDto = Pick<VariationField, 'name' | 'type' | 'options' | 'required'>;
export type UpdateVariationFieldDto = Partial<CreateVariationFieldDto & { order: number }>;
