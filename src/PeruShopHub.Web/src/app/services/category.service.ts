import { Injectable, signal, computed, inject } from '@angular/core';
import {
  Category,
  VariationField,
  InheritedVariationField,
  CreateCategoryDto,
  UpdateCategoryDto,
  CreateVariationFieldDto,
  UpdateVariationFieldDto,
} from '../models/category.model';

function generateId(): string {
  return 'cat-' + Math.random().toString(36).substring(2, 10);
}

function generateFieldId(): string {
  return 'fld-' + Math.random().toString(36).substring(2, 10);
}

function slugify(name: string): string {
  return name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/(^-|-$)/g, '');
}

const now = new Date().toISOString();

// Pre-seeded flat categories
const SEED_CATEGORIES: Category[] = [
  // Root categories
  { id: 'cat-eletronicos', name: 'Eletrônicos', slug: 'eletronicos', parentId: null, children: [], icon: null, isActive: true, productCount: 45, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-informatica', name: 'Informática', slug: 'informatica', parentId: null, children: [], icon: null, isActive: true, productCount: 28, order: 1, createdAt: now, updatedAt: now },
  { id: 'cat-moda', name: 'Moda', slug: 'moda', parentId: null, children: [], icon: null, isActive: true, productCount: 32, order: 2, createdAt: now, updatedAt: now },
  { id: 'cat-casa', name: 'Casa e Decoração', slug: 'casa-e-decoracao', parentId: null, children: [], icon: null, isActive: true, productCount: 15, order: 3, createdAt: now, updatedAt: now },
  { id: 'cat-esportes', name: 'Esportes', slug: 'esportes', parentId: null, children: [], icon: null, isActive: true, productCount: 10, order: 4, createdAt: now, updatedAt: now },
  { id: 'cat-beleza', name: 'Beleza e Saúde', slug: 'beleza-e-saude', parentId: null, children: [], icon: null, isActive: true, productCount: 8, order: 5, createdAt: now, updatedAt: now },

  // Eletrônicos children
  { id: 'cat-celulares', name: 'Celulares', slug: 'celulares', parentId: 'cat-eletronicos', children: [], icon: null, isActive: true, productCount: 18, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-audio', name: 'Áudio', slug: 'audio', parentId: 'cat-eletronicos', children: [], icon: null, isActive: true, productCount: 12, order: 1, createdAt: now, updatedAt: now },

  // Áudio children
  { id: 'cat-fones', name: 'Fones', slug: 'fones', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 8, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-cabos', name: 'Cabos', slug: 'cabos', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 3, order: 1, createdAt: now, updatedAt: now },
  { id: 'cat-caixas-som', name: 'Caixas de Som', slug: 'caixas-de-som', parentId: 'cat-audio', children: [], icon: null, isActive: true, productCount: 5, order: 2, createdAt: now, updatedAt: now },

  // Informática children
  { id: 'cat-notebooks', name: 'Notebooks', slug: 'notebooks', parentId: 'cat-informatica', children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-perifericos', name: 'Periféricos', slug: 'perifericos', parentId: 'cat-informatica', children: [], icon: null, isActive: true, productCount: 14, order: 1, createdAt: now, updatedAt: now },

  // Periféricos children
  { id: 'cat-teclados', name: 'Teclados', slug: 'teclados', parentId: 'cat-perifericos', children: [], icon: null, isActive: true, productCount: 6, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-mouses', name: 'Mouses', slug: 'mouses', parentId: 'cat-perifericos', children: [], icon: null, isActive: true, productCount: 8, order: 1, createdAt: now, updatedAt: now },

  // Moda children
  { id: 'cat-masculina', name: 'Masculina', slug: 'masculina', parentId: 'cat-moda', children: [], icon: null, isActive: true, productCount: 14, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-feminina', name: 'Feminina', slug: 'feminina', parentId: 'cat-moda', children: [], icon: null, isActive: true, productCount: 18, order: 1, createdAt: now, updatedAt: now },

  // Feminina children
  { id: 'cat-camisetas', name: 'Camisetas', slug: 'camisetas', parentId: 'cat-feminina', children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: now, updatedAt: now },
  { id: 'cat-calcas', name: 'Calças', slug: 'calcas', parentId: 'cat-feminina', children: [], icon: null, isActive: true, productCount: 8, order: 1, createdAt: now, updatedAt: now },
];

// Pre-seeded variation fields
const SEED_VARIATION_FIELDS: VariationField[] = [
  { id: 'fld-voltagem', categoryId: 'cat-eletronicos', name: 'Voltagem', type: 'select', options: ['110V', '220V', 'Bivolt'], required: true, order: 0 },
  { id: 'fld-cor', categoryId: 'cat-moda', name: 'Cor', type: 'select', options: ['Preto', 'Branco', 'Azul', 'Vermelho'], required: true, order: 0 },
  { id: 'fld-tamanho', categoryId: 'cat-camisetas', name: 'Tamanho', type: 'select', options: ['P', 'M', 'G', 'GG'], required: true, order: 0 },
  { id: 'fld-comprimento', categoryId: 'cat-cabos', name: 'Comprimento', type: 'text', options: [], required: false, order: 0 },
];

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly categoriesData = signal<Category[]>([...SEED_CATEGORIES]);
  private readonly variationFieldsData = signal<VariationField[]>([...SEED_VARIATION_FIELDS]);

  /** Flat list of all categories */
  readonly categories = this.categoriesData.asReadonly();

  /** Tree-structured categories (top-level with nested children) */
  readonly categoryTree = computed<Category[]>(() => {
    return this.buildTree(this.categoriesData());
  });

  private buildTree(flatList: Category[]): Category[] {
    const map = new Map<string, Category>();
    const roots: Category[] = [];

    // Create copies with empty children
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

  private delay(): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, 300));
  }

  async getAll(): Promise<Category[]> {
    await this.delay();
    return this.categoriesData();
  }

  async getTree(): Promise<Category[]> {
    await this.delay();
    return this.categoryTree();
  }

  async getById(id: string): Promise<Category | undefined> {
    await this.delay();
    return this.categoriesData().find(c => c.id === id);
  }

  async create(dto: CreateCategoryDto): Promise<Category> {
    await this.delay();
    const siblings = this.categoriesData().filter(c => c.parentId === dto.parentId);
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
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    this.categoriesData.update(cats => [...cats, newCat]);
    return newCat;
  }

  async update(id: string, dto: UpdateCategoryDto): Promise<Category | undefined> {
    await this.delay();
    let updated: Category | undefined;
    this.categoriesData.update(cats =>
      cats.map(c => {
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
    await this.delay();
    const cat = this.categoriesData().find(c => c.id === id);
    if (!cat) return false;
    const hasChildren = this.categoriesData().some(c => c.parentId === id);
    if (hasChildren) return false;

    this.categoriesData.update(cats => cats.filter(c => c.id !== id));
    // Also remove variation fields for this category
    this.variationFieldsData.update(fields => fields.filter(f => f.categoryId !== id));
    return true;
  }

  getVariationFields(categoryId: string): VariationField[] {
    return this.variationFieldsData()
      .filter(f => f.categoryId === categoryId)
      .sort((a, b) => a.order - b.order);
  }

  getInheritedVariationFields(categoryId: string): InheritedVariationField[] {
    const result: InheritedVariationField[] = [];
    const ancestors = this.getAncestorChain(categoryId);

    // Walk from root to leaf (excluding the category itself)
    for (const ancestor of ancestors) {
      const fields = this.getVariationFields(ancestor.id);
      for (const field of fields) {
        result.push({
          ...field,
          inheritedFrom: ancestor.name,
          inheritedFromId: ancestor.id,
        });
      }
    }

    // Also include own fields (tagged as own)
    const ownFields = this.getVariationFields(categoryId);
    for (const field of ownFields) {
      const cat = this.categoriesData().find(c => c.id === categoryId);
      result.push({
        ...field,
        inheritedFrom: cat?.name || '',
        inheritedFromId: categoryId,
      });
    }

    return result;
  }

  /** Get all variation fields for a category and all its ancestors (for product form) */
  getAllVariationFieldsForCategory(categoryId: string): InheritedVariationField[] {
    return this.getInheritedVariationFields(categoryId);
  }

  private getAncestorChain(categoryId: string): Category[] {
    const chain: Category[] = [];
    let current = this.categoriesData().find(c => c.id === categoryId);

    while (current?.parentId) {
      const parent = this.categoriesData().find(c => c.id === current!.parentId);
      if (parent) {
        chain.unshift(parent);
        current = parent;
      } else {
        break;
      }
    }

    return chain;
  }

  getBreadcrumb(categoryId: string): string[] {
    const chain = this.getAncestorChain(categoryId);
    const cat = this.categoriesData().find(c => c.id === categoryId);
    if (cat) {
      chain.push(cat);
    }
    return chain.map(c => c.name);
  }

  async addVariationField(categoryId: string, dto: CreateVariationFieldDto): Promise<VariationField> {
    await this.delay();
    const existing = this.getVariationFields(categoryId);
    const field: VariationField = {
      id: generateFieldId(),
      categoryId,
      name: dto.name,
      type: dto.type,
      options: dto.options,
      required: dto.required,
      order: existing.length,
    };
    this.variationFieldsData.update(fields => [...fields, field]);
    return field;
  }

  async updateVariationField(fieldId: string, dto: UpdateVariationFieldDto): Promise<VariationField | undefined> {
    await this.delay();
    let updated: VariationField | undefined;
    this.variationFieldsData.update(fields =>
      fields.map(f => {
        if (f.id === fieldId) {
          updated = { ...f, ...dto };
          return updated;
        }
        return f;
      })
    );
    return updated;
  }

  async deleteVariationField(fieldId: string, categoryId: string): Promise<boolean> {
    await this.delay();
    this.variationFieldsData.update(fields => fields.filter(f => f.id !== fieldId));
    return true;
  }

  async reorderCategories(parentId: string | null, orderedIds: string[]): Promise<void> {
    await this.delay();
    this.categoriesData.update(cats =>
      cats.map(c => {
        const idx = orderedIds.indexOf(c.id);
        if (idx >= 0 && c.parentId === parentId) {
          return { ...c, order: idx, updatedAt: new Date().toISOString() };
        }
        return c;
      })
    );
  }

  async moveCategory(categoryId: string, newParentId: string | null): Promise<void> {
    await this.delay();
    this.categoriesData.update(cats =>
      cats.map(c => {
        if (c.id === categoryId) {
          return { ...c, parentId: newParentId, updatedAt: new Date().toISOString() };
        }
        return c;
      })
    );
  }

  /** Check if moving category to newParentId would create a circular reference */
  wouldCreateCircular(categoryId: string, newParentId: string | null): boolean {
    if (!newParentId) return false;
    if (categoryId === newParentId) return true;

    let current = this.categoriesData().find(c => c.id === newParentId);
    while (current?.parentId) {
      if (current.parentId === categoryId) return true;
      current = this.categoriesData().find(c => c.id === current!.parentId);
    }
    return false;
  }

  /** Get all descendant category IDs */
  getDescendantIds(categoryId: string): string[] {
    const ids: string[] = [];
    const collect = (parentId: string) => {
      for (const cat of this.categoriesData()) {
        if (cat.parentId === parentId) {
          ids.push(cat.id);
          collect(cat.id);
        }
      }
    };
    collect(categoryId);
    return ids;
  }
}
