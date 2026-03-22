import { Injectable, signal, computed } from '@angular/core';
import {
  Category,
  VariationField,
  InheritedVariationField,
  CreateCategoryDto,
  UpdateCategoryDto,
  CreateVariationFieldDto,
  UpdateVariationFieldDto,
} from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly categoriesSignal = signal<Category[]>(SEED_CATEGORIES);
  private readonly variationFieldsSignal = signal<VariationField[]>(SEED_VARIATION_FIELDS);

  /** Flat list of all categories */
  readonly categories = this.categoriesSignal.asReadonly();

  /** Flat list of all variation fields */
  readonly variationFields = this.variationFieldsSignal.asReadonly();

  /** Hierarchical tree built from flat list */
  readonly categoryTree = computed<Category[]>(() => {
    const flat = this.categoriesSignal();
    return this.buildTree(flat);
  });

  /** Total category count */
  readonly totalCount = computed(() => this.categoriesSignal().length);

  // ---------------------------------------------------------------------------
  // Category CRUD
  // ---------------------------------------------------------------------------

  async getAll(): Promise<Category[]> {
    await this.delay();
    return this.categoriesSignal();
  }

  async getTree(): Promise<Category[]> {
    await this.delay();
    return this.categoryTree();
  }

  async getById(id: string): Promise<Category | undefined> {
    await this.delay();
    return this.categoriesSignal().find(c => c.id === id);
  }

  async create(dto: CreateCategoryDto): Promise<Category> {
    await this.delay();
    const now = new Date().toISOString();
    const siblings = this.categoriesSignal().filter(c => c.parentId === dto.parentId);
    const category: Category = {
      id: this.generateId(),
      name: dto.name,
      slug: this.slugify(dto.name),
      parentId: dto.parentId,
      children: [],
      icon: null,
      isActive: dto.isActive,
      productCount: 0,
      order: siblings.length,
      createdAt: now,
      updatedAt: now,
    };
    this.categoriesSignal.update(list => [...list, category]);
    return category;
  }

  async update(id: string, dto: UpdateCategoryDto): Promise<Category | undefined> {
    await this.delay();
    let updated: Category | undefined;
    this.categoriesSignal.update(list =>
      list.map(c => {
        if (c.id !== id) return c;
        updated = {
          ...c,
          ...dto,
          slug: dto.name ? this.slugify(dto.name) : c.slug,
          updatedAt: new Date().toISOString(),
        };
        return updated;
      }),
    );
    return updated;
  }

  async delete(id: string): Promise<boolean> {
    await this.delay();
    const category = this.categoriesSignal().find(c => c.id === id);
    if (!category) return false;

    // Block deletion if has children
    const hasChildren = this.categoriesSignal().some(c => c.parentId === id);
    if (hasChildren) return false;

    this.categoriesSignal.update(list => list.filter(c => c.id !== id));
    // Also remove variation fields belonging to this category
    this.variationFieldsSignal.update(list => list.filter(f => f.categoryId !== id));
    return true;
  }

  // ---------------------------------------------------------------------------
  // Tree operations
  // ---------------------------------------------------------------------------

  async reorderCategories(parentId: string | null, orderedIds: string[]): Promise<void> {
    await this.delay();
    this.categoriesSignal.update(list =>
      list.map(c => {
        if (c.parentId !== parentId) return c;
        const idx = orderedIds.indexOf(c.id);
        return idx >= 0 ? { ...c, order: idx, updatedAt: new Date().toISOString() } : c;
      }),
    );
  }

  async moveCategory(categoryId: string, newParentId: string | null): Promise<boolean> {
    await this.delay();
    // Prevent circular reference — cannot move to self or descendants
    if (newParentId === categoryId) return false;
    if (newParentId && this.isDescendant(categoryId, newParentId)) return false;

    this.categoriesSignal.update(list =>
      list.map(c =>
        c.id === categoryId
          ? { ...c, parentId: newParentId, updatedAt: new Date().toISOString() }
          : c,
      ),
    );
    return true;
  }

  getBreadcrumb(categoryId: string): string[] {
    const flat = this.categoriesSignal();
    const crumbs: string[] = [];
    let current = flat.find(c => c.id === categoryId);
    while (current) {
      crumbs.unshift(current.name);
      current = current.parentId ? flat.find(c => c.id === current!.parentId) : undefined;
    }
    return crumbs;
  }

  // ---------------------------------------------------------------------------
  // Variation fields
  // ---------------------------------------------------------------------------

  getVariationFields(categoryId: string): VariationField[] {
    return this.variationFieldsSignal()
      .filter(f => f.categoryId === categoryId)
      .sort((a, b) => a.order - b.order);
  }

  getInheritedVariationFields(categoryId: string): InheritedVariationField[] {
    const flat = this.categoriesSignal();
    const ancestors: Category[] = [];
    let current = flat.find(c => c.id === categoryId);

    // Walk up ancestors (exclude self)
    if (current?.parentId) {
      let parent = flat.find(c => c.id === current!.parentId);
      while (parent) {
        ancestors.unshift(parent);
        parent = parent.parentId ? flat.find(c => c.id === parent!.parentId) : undefined;
      }
    }

    const inherited: InheritedVariationField[] = [];
    for (const ancestor of ancestors) {
      const fields = this.getVariationFields(ancestor.id);
      for (const field of fields) {
        inherited.push({
          ...field,
          inheritedFrom: ancestor.name,
          inheritedFromId: ancestor.id,
        });
      }
    }
    return inherited;
  }

  async addVariationField(categoryId: string, dto: CreateVariationFieldDto): Promise<VariationField> {
    await this.delay();
    const existing = this.getVariationFields(categoryId);
    const field: VariationField = {
      id: this.generateId(),
      categoryId,
      name: dto.name,
      type: dto.type,
      options: dto.options,
      required: dto.required,
      order: existing.length,
    };
    this.variationFieldsSignal.update(list => [...list, field]);
    return field;
  }

  async updateVariationField(fieldId: string, dto: UpdateVariationFieldDto): Promise<VariationField | undefined> {
    let updated: VariationField | undefined;
    await this.delay();
    this.variationFieldsSignal.update(list =>
      list.map(f => {
        if (f.id !== fieldId) return f;
        updated = { ...f, ...dto };
        return updated;
      }),
    );
    return updated;
  }

  async deleteVariationField(fieldId: string, categoryId: string): Promise<{ deleted: boolean; affectedProducts: number }> {
    await this.delay();
    this.variationFieldsSignal.update(list => list.filter(f => f.id !== fieldId));
    // In a real app, would call ProductVariantService.flagForReview()
    // For mock, return a simulated count
    const affectedProducts = this.getAffectedProductCount(categoryId);
    return { deleted: true, affectedProducts };
  }

  getAffectedProductCount(categoryId: string): number {
    // Count products in this category and descendants
    const descendants = this.getDescendantIds(categoryId);
    const allIds = [categoryId, ...descendants];
    return this.categoriesSignal()
      .filter(c => allIds.includes(c.id))
      .reduce((sum, c) => sum + c.productCount, 0);
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  private buildTree(flat: Category[]): Category[] {
    const map = new Map<string, Category>();
    const roots: Category[] = [];

    // Clone each category with empty children
    for (const cat of flat) {
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
    const sortChildren = (nodes: Category[]): void => {
      nodes.sort((a, b) => a.order - b.order);
      for (const node of nodes) {
        sortChildren(node.children);
      }
    };
    sortChildren(roots);

    return roots;
  }

  private isDescendant(ancestorId: string, potentialDescendantId: string): boolean {
    const flat = this.categoriesSignal();
    let current = flat.find(c => c.id === potentialDescendantId);
    while (current) {
      if (current.parentId === ancestorId) return true;
      current = current.parentId ? flat.find(c => c.id === current!.parentId) : undefined;
    }
    return false;
  }

  private getDescendantIds(categoryId: string): string[] {
    const flat = this.categoriesSignal();
    const ids: string[] = [];
    const collect = (parentId: string) => {
      for (const c of flat) {
        if (c.parentId === parentId) {
          ids.push(c.id);
          collect(c.id);
        }
      }
    };
    collect(categoryId);
    return ids;
  }

  private generateId(): string {
    return 'cat-' + Math.random().toString(36).substring(2, 10);
  }

  private slugify(name: string): string {
    return name
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/(^-|-$)/g, '');
  }

  private delay(ms = 300): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// =============================================================================
// Seed data
// =============================================================================

const SEED_CATEGORIES: Category[] = [
  // Root categories
  { id: 'eletronicos',      name: 'Eletrônicos',       slug: 'eletronicos',       parentId: null,            children: [], icon: null, isActive: true, productCount: 45, order: 0, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-06-01T14:30:00Z' },
  { id: 'informatica',      name: 'Informática',       slug: 'informatica',       parentId: null,            children: [], icon: null, isActive: true, productCount: 28, order: 1, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-05-20T09:00:00Z' },
  { id: 'moda',             name: 'Moda',              slug: 'moda',              parentId: null,            children: [], icon: null, isActive: true, productCount: 32, order: 2, createdAt: '2025-01-15T10:00:00Z', updatedAt: '2025-06-10T11:00:00Z' },
  { id: 'casa-decoracao',   name: 'Casa e Decoração',  slug: 'casa-e-decoracao',  parentId: null,            children: [], icon: null, isActive: true, productCount: 15, order: 3, createdAt: '2025-02-01T08:00:00Z', updatedAt: '2025-05-15T16:00:00Z' },
  { id: 'esportes',         name: 'Esportes',          slug: 'esportes',          parentId: null,            children: [], icon: null, isActive: true, productCount: 12, order: 4, createdAt: '2025-02-01T08:00:00Z', updatedAt: '2025-04-20T10:00:00Z' },
  { id: 'beleza-saude',     name: 'Beleza e Saúde',    slug: 'beleza-e-saude',    parentId: null,            children: [], icon: null, isActive: true, productCount: 8,  order: 5, createdAt: '2025-02-01T08:00:00Z', updatedAt: '2025-05-10T12:00:00Z' },

  // Eletrônicos children
  { id: 'celulares',        name: 'Celulares',         slug: 'celulares',         parentId: 'eletronicos',   children: [], icon: null, isActive: true, productCount: 18, order: 0, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-06-01T14:30:00Z' },
  { id: 'audio',            name: 'Áudio',             slug: 'audio',             parentId: 'eletronicos',   children: [], icon: null, isActive: true, productCount: 12, order: 1, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-05-28T09:00:00Z' },

  // Áudio children (3rd level)
  { id: 'fones',            name: 'Fones',             slug: 'fones',             parentId: 'audio',         children: [], icon: null, isActive: true, productCount: 8,  order: 0, createdAt: '2025-02-10T10:00:00Z', updatedAt: '2025-05-28T09:00:00Z' },
  { id: 'cabos',            name: 'Cabos',             slug: 'cabos',             parentId: 'audio',         children: [], icon: null, isActive: true, productCount: 3,  order: 1, createdAt: '2025-02-10T10:00:00Z', updatedAt: '2025-04-15T11:00:00Z' },
  { id: 'caixas-de-som',    name: 'Caixas de Som',     slug: 'caixas-de-som',     parentId: 'audio',         children: [], icon: null, isActive: true, productCount: 5,  order: 2, createdAt: '2025-02-10T10:00:00Z', updatedAt: '2025-05-01T14:00:00Z' },

  // Informática children
  { id: 'notebooks',        name: 'Notebooks',         slug: 'notebooks',         parentId: 'informatica',   children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-05-20T09:00:00Z' },
  { id: 'perifericos',      name: 'Periféricos',       slug: 'perifericos',       parentId: 'informatica',   children: [], icon: null, isActive: true, productCount: 14, order: 1, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-05-18T15:00:00Z' },

  // Periféricos children (3rd level)
  { id: 'teclados',         name: 'Teclados',          slug: 'teclados',          parentId: 'perifericos',   children: [], icon: null, isActive: true, productCount: 7,  order: 0, createdAt: '2025-03-01T10:00:00Z', updatedAt: '2025-05-18T15:00:00Z' },
  { id: 'mouses',           name: 'Mouses',            slug: 'mouses',            parentId: 'perifericos',   children: [], icon: null, isActive: true, productCount: 6,  order: 1, createdAt: '2025-03-01T10:00:00Z', updatedAt: '2025-05-10T08:00:00Z' },

  // Moda children
  { id: 'masculina',        name: 'Masculina',         slug: 'masculina',         parentId: 'moda',          children: [], icon: null, isActive: true, productCount: 14, order: 0, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-06-10T11:00:00Z' },
  { id: 'feminina',         name: 'Feminina',          slug: 'feminina',          parentId: 'moda',          children: [], icon: null, isActive: true, productCount: 18, order: 1, createdAt: '2025-01-20T10:00:00Z', updatedAt: '2025-06-08T16:00:00Z' },

  // Feminina children (3rd level)
  { id: 'camisetas',        name: 'Camisetas',         slug: 'camisetas',         parentId: 'feminina',      children: [], icon: null, isActive: true, productCount: 10, order: 0, createdAt: '2025-03-15T10:00:00Z', updatedAt: '2025-06-08T16:00:00Z' },
  { id: 'calcas',           name: 'Calças',            slug: 'calcas',            parentId: 'feminina',      children: [], icon: null, isActive: true, productCount: 8,  order: 1, createdAt: '2025-03-15T10:00:00Z', updatedAt: '2025-05-25T13:00:00Z' },
];

const SEED_VARIATION_FIELDS: VariationField[] = [
  // Voltagem on Eletrônicos (inherited by all electronics)
  { id: 'vf-voltagem',      categoryId: 'eletronicos', name: 'Voltagem',     type: 'select', options: ['110V', '220V', 'Bivolt'],                required: true,  order: 0 },
  // Cor on Moda (inherited by all fashion)
  { id: 'vf-cor',           categoryId: 'moda',        name: 'Cor',          type: 'select', options: ['Preto', 'Branco', 'Azul', 'Vermelho'],   required: true,  order: 0 },
  // Tamanho on Camisetas
  { id: 'vf-tamanho',       categoryId: 'camisetas',   name: 'Tamanho',      type: 'select', options: ['P', 'M', 'G', 'GG'],                    required: true,  order: 0 },
  // Comprimento on Cabos
  { id: 'vf-comprimento',   categoryId: 'cabos',       name: 'Comprimento',  type: 'text',   options: [],                                        required: true,  order: 0 },
];
