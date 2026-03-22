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

export type CreateCategoryDto = Pick<Category, 'name' | 'parentId' | 'isActive'>;
export type UpdateCategoryDto = Partial<Pick<Category, 'name' | 'parentId' | 'isActive' | 'order'>>;
export type CreateVariationFieldDto = Pick<VariationField, 'name' | 'type' | 'options' | 'required'>;
export type UpdateVariationFieldDto = Partial<CreateVariationFieldDto & { order: number }>;
