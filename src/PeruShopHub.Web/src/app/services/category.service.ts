import { Injectable, signal, computed, inject } from '@angular/core';
import type {
  Category,
  VariationField,
  InheritedVariationField,
  CreateCategoryDto,
  UpdateCategoryDto,
  CreateVariationFieldDto,
  UpdateVariationFieldDto,
} from '../models/category.model';

function generateId(): string {
  return Math.random().toString(36).substring(2, 11);
}

function slugify(name: string): string {
  return name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

function delay(ms = 300): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// ── Pre-seeded categories (flat) ──
const SEED_CATEGORIES: Category[] = [
  // Root level
  { id: 'cat-eletronicos', name: 'Eletr\u00f4nicos', slug: 'eletronicos', parentId: null, children: [], icon: null, isActive: true, productCount: 45, order: 0, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-20T14:30:00Z' },
  { id: 'cat-informatica', name: 'Inform\u00e1tica', slug: 'informatica', parentId: null, children: [], icon: null, isActive: true, productCount: 38, order: 1, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-18T09:00:00Z' },
  { id: 'cat-moda', name: 'Moda', slug: 'moda', parentId: null, children: [], icon: null, isActive: true, productCount: 32, order: 2, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-19T11:00:00Z' },
  { id: 'cat-casa', name: 'Casa e Decora\u00e7\u00e3o', slug: 'casa-e-decoracao', parentId: null, children: [], icon: null, isActive: true, productCount: 15, order: 3, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-10T08:00:00Z' },
  { id: 'cat-esportes', name: 'Esportes', slug: 'esportes', parentId: null, children: [], icon: null, isActive: true, productCount: 20, order: 4, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-12T16:00:00Z' },
  { id: 'cat-beleza', name: 'Beleza e Sa\u00fade', slug: 'beleza-e-saude', parentId: null, children: [], icon: null, isActive: true, productCount: 10, order: 5, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-03-15T13:00:00Z' },

  // Eletr\u00f4nicos children
  { id: 'cat-celulares', name: 'Celulares', slug: 'celulares', parentId: 'cat-eletronicos', children: [], icon: null, isActive: true, productCount: 18, order: 0, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-20T14:30:00Z' },
  { id: 'cat-audio', name: '\u00c1udio', slug: 'audio', parentId: 'cat-eletronicos', children: [], icon: null, isActive: true, productCount: 12, order: 1, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-19T10:00:00Z' },

  // \u00c1udio children
  { id: 'cat-fones', name: 'Fones', slug: 'fones', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 8, order: 0, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-18T11:00:00Z' },
  { id: 'cat-cabos', name: 'Cabos', slug: 'cabos', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 3, order: 1, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-17T09:00:00Z' },
  { id: 'cat-caixas-som', name: 'Caixas de Som', slug: 'caixas-de-som', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 5, order: 2, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-16T14:00:00Z' },

  // Inform\u00e1tica children
  { id: 'cat-notebooks', name: 'Notebooks', slug: 'notebooks', parentId: 'cat-informatica', children: [], icon: null, isActive: true, productCount: 15, order: 0, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-18T09:00:00Z' },
  { id: 'cat-perifericos', name: 'Perif\u00e9ricos', slug: 'perifericos', parentId: 'cat-informatica', children: [], icon: null, isActive: true, productCount: 22, order: 1, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-17T15:00:00Z' },

  // Perif\u00e9ricos children
  { id: 'cat-teclados', name: 'Teclados', slug: 'teclados', parentId: 'cat-perifericos', children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-16T12:00:00Z' },
  { id: 'cat-mouses', name: 'Mouses', slug: 'mouses', parentId: 'cat-perifericos', children: [], icon: null, isActive: true, productCount: 12, order: 1, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-15T10:00:00Z' },

  // Moda children
  { id: 'cat-masculina', name: 'Masculina', slug: 'masculina', parentId: 'cat-moda', children: [], icon: null, isActive: true, productCount: 14, order: 0, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-19T11:00:00Z' },
  { id: 'cat-feminina', name: 'Feminina', slug: 'feminina', parentId: 'cat-moda', children: [], icon: null, isActive: true, productCount: 18, order: 1, createdAt: '2025-01-16T10:00:00Z', updatedAt: '2025-03-18T14:00:00Z' },

  // Feminina children
  { id: 'cat-camisetas', name: 'Camisetas', slug: 'camisetas', parentId: 'cat-feminina', children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-17T16:00:00Z' },
  { id: 'cat-calcas', name: 'Cal\u00e7as', slug: 'calcas', parentId: 'cat-feminina', children: [], icon: null, isActive: true, productCount: 8, order: 1, createdAt: '2025-01-17T10:00:00Z', updatedAt: '2025-03-16T11:00:00Z' },
];

// ── Pre-seeded variation fields ──
const SEED_VARIATION_FIELDS: VariationField[] = [
  { id: 'vf-voltagem', categoryId: 'cat-eletronicos', name: 'Voltagem', type: 'select', options: ['110V', '220V', 'Bivolt'], required: true, order: 0 },
  { id: 'vf-cor', categoryId: 'cat-moda', name: 'Cor', type: 'select', options: ['Preto', 'Branco', 'Azul', 'Vermelho'], required: true, order: 0 },
  { id: 'vf-tamanho', categoryId: 'cat-camisetas', name: 'Tamanho', type: 'select', options: ['P', 'M', 'G', 'GG'], required: true, order: 0 },
  { id: 'vf-comprimento', categoryId: 'cat-cabos', name: 'Comprimento', type: 'text', options: [], required: true, order: 0 },
];

@Injectable({ providedIn: 'root' })
export class CategoryService {
  // Flat list of all categories
  private readonly categoriesData = signal<Category[]>([...SEED_CATEGORIES]);

  // Variation fields
  private readonly variationFieldsData = signal<VariationField[]>([...SEED_VARIATION_FIELDS]);

  // Computed tree structure
  readonly categoryTree = computed<Category[]>(() => {
    return this.buildTree(this.categoriesData());
  });

  // All categories flat
  readonly allCategories = computed<Category[]>(() => this.categoriesData());

  // Total product count
  readonly totalProductCount = computed(() =>
    this.categoriesData().reduce((sum, c) => sum + c.productCount, 0)
  );

  private buildTree(flatList: Category[]): Category[] {
    const map = new Map<string, Category>();
    const roots: Category[] = [];

    // Clone each category with empty children
    for (const cat of flatList) {
      map.set(cat.id, { ...cat, children: [] });
    }

    // Build parent-child relationships
    for (const cat of map.values()) {
      if (cat.parentId && map.has(cat.parentId)) {
        map.get(cat.parentId)!.children.push(cat);
      } else if (!cat.parentId) {
        roots.push(cat);
      }
    }

    // Sort children by order
    const sortChildren = (cats: Category[]): void => {
      cats.sort((a, b) => a.order - b.order);
      for (const cat of cats) {
        sortChildren(cat.children);
      }
    };

    sortChildren(roots);
    return roots;
  }

  async getAll(): Promise<Category[]> {
    await delay();
    return this.categoriesData();
  }

  async getTree(): Promise<Category[]> {
    await delay();
    return this.categoryTree();
  }

  async getById(id: string): Promise<Category | undefined> {
    await delay();
    return this.categoriesData().find((c) => c.id === id);
  }

  async create(dto: CreateCategoryDto): Promise<Category> {
    await delay();
    const now = new Date().toISOString();
    const siblings = this.categoriesData().filter((c) => c.parentId === dto.parentId);
    const newCat: Category = {
      id: generateId(),
      name: dto.name,
      slug: slugify(dto.name),
      parentId: dto.parentId,
      children: [],
      icon: null,
      isActive: dto.isActive,
      productCount: 0,
      order: siblings.length,
      createdAt: now,
      updatedAt: now,
    };

    this.categoriesData.update((cats) => [...cats, newCat]);
    return newCat;
  }

  async update(id: string, dto: UpdateCategoryDto): Promise<Category | undefined> {
    await delay();
    let updated: Category | undefined;
    this.categoriesData.update((cats) =>
      cats.map((c) => {
        if (c.id === id) {
          updated = {
            ...c,
            ...dto,
            slug: dto.name ? slugify(dto.name) : c.slug,
            updatedAt: new Date().toISOString(),
          };
          return updated;
        }
        return c;
      })
    );
    return updated;
  }

  async delete(id: string): Promise<boolean> {
    await delay();
    const cat = this.categoriesData().find((c) => c.id === id);
    if (!cat) return false;

    // Check for children
    const hasChildren = this.categoriesData().some((c) => c.parentId === id);
    if (hasChildren) return false;

    this.categoriesData.update((cats) => cats.filter((c) => c.id !== id));

    // Also remove associated variation fields
    this.variationFieldsData.update((fields) =>
      fields.filter((f) => f.categoryId !== id)
    );

    return true;
  }

  getVariationFields(categoryId: string): VariationField[] {
    return this.variationFieldsData().filter((f) => f.categoryId === categoryId);
  }

  getInheritedVariationFields(categoryId: string): InheritedVariationField[] {
    const result: InheritedVariationField[] = [];
    const ancestors = this.getAncestors(categoryId);

    // Walk from root to immediate parent (exclude current category)
    for (const ancestor of ancestors) {
      const fields = this.variationFieldsData().filter(
        (f) => f.categoryId === ancestor.id
      );
      for (const field of fields) {
        result.push({
          ...field,
          inheritedFrom: ancestor.name,
          inheritedFromId: ancestor.id,
        });
      }
    }

    return result;
  }

  getAllVariationFieldsForCategory(categoryId: string): (VariationField | InheritedVariationField)[] {
    const inherited = this.getInheritedVariationFields(categoryId);
    const own = this.getVariationFields(categoryId);
    return [...inherited, ...own];
  }

  getBreadcrumb(categoryId: string): string[] {
    const ancestors = this.getAncestors(categoryId);
    const current = this.categoriesData().find((c) => c.id === categoryId);
    const names = ancestors.map((a) => a.name);
    if (current) names.push(current.name);
    return names;
  }

  private getAncestors(categoryId: string): Category[] {
    const ancestors: Category[] = [];
    let current = this.categoriesData().find((c) => c.id === categoryId);

    while (current?.parentId) {
      const parent = this.categoriesData().find((c) => c.id === current!.parentId);
      if (parent) {
        ancestors.unshift(parent);
        current = parent;
      } else {
        break;
      }
    }

    return ancestors;
  }

  getDescendantIds(categoryId: string): string[] {
    const result: string[] = [];
    const children = this.categoriesData().filter((c) => c.parentId === categoryId);
    for (const child of children) {
      result.push(child.id);
      result.push(...this.getDescendantIds(child.id));
    }
    return result;
  }

  hasChildren(categoryId: string): boolean {
    return this.categoriesData().some((c) => c.parentId === categoryId);
  }

  getChildCount(categoryId: string): number {
    return this.categoriesData().filter((c) => c.parentId === categoryId).length;
  }

  async addVariationField(
    categoryId: string,
    dto: CreateVariationFieldDto
  ): Promise<VariationField> {
    await delay();
    const existing = this.variationFieldsData().filter(
      (f) => f.categoryId === categoryId
    );
    const newField: VariationField = {
      id: generateId(),
      categoryId,
      name: dto.name,
      type: dto.type,
      options: dto.options,
      required: dto.required,
      order: existing.length,
    };

    this.variationFieldsData.update((fields) => [...fields, newField]);
    return newField;
  }

  async updateVariationField(
    fieldId: string,
    dto: UpdateVariationFieldDto
  ): Promise<VariationField | undefined> {
    let updated: VariationField | undefined;
    await delay();
    this.variationFieldsData.update((fields) =>
      fields.map((f) => {
        if (f.id === fieldId) {
          updated = { ...f, ...dto };
          return updated;
        }
        return f;
      })
    );
    return updated;
  }

  async deleteVariationField(fieldId: string): Promise<VariationField | undefined> {
    await delay();
    const field = this.variationFieldsData().find((f) => f.id === fieldId);
    if (!field) return undefined;

    this.variationFieldsData.update((fields) =>
      fields.filter((f) => f.id !== fieldId)
    );
    return field;
  }

  restoreVariationField(field: VariationField): void {
    this.variationFieldsData.update((fields) => [...fields, field]);
  }

  async reorderCategories(parentId: string | null, orderedIds: string[]): Promise<void> {
    await delay();
    this.categoriesData.update((cats) =>
      cats.map((c) => {
        if (c.parentId === parentId) {
          const idx = orderedIds.indexOf(c.id);
          if (idx >= 0) {
            return { ...c, order: idx, updatedAt: new Date().toISOString() };
          }
        }
        return c;
      })
    );
  }

  async moveCategory(categoryId: string, newParentId: string | null): Promise<boolean> {
    await delay();
    // Prevent circular reference
    if (newParentId) {
      const descendants = this.getDescendantIds(categoryId);
      if (descendants.includes(newParentId) || newParentId === categoryId) {
        return false;
      }
    }

    this.categoriesData.update((cats) =>
      cats.map((c) => {
        if (c.id === categoryId) {
          return {
            ...c,
            parentId: newParentId,
            updatedAt: new Date().toISOString(),
          };
        }
        return c;
      })
    );
    return true;
  }
}
