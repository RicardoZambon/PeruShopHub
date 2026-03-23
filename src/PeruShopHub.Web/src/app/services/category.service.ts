import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
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

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/categories';

  // Local cache of loaded categories (flat)
  private readonly categoriesData = signal<Category[]>([]);

  // Variation fields remain frontend-only
  private readonly variationFieldsData = signal<VariationField[]>([]);

  // Computed tree structure built from cached flat list
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

  // ── HTTP methods ──

  async getAll(): Promise<Category[]> {
    const categories = await firstValueFrom(
      this.http.get<Category[]>(this.baseUrl),
    );
    this.categoriesData.set(categories);
    return categories;
  }

  async getChildren(parentId?: string | null): Promise<Category[]> {
    let params = new HttpParams();
    if (parentId != null) {
      params = params.set('parentId', parentId);
    }
    const categories = await firstValueFrom(
      this.http.get<Category[]>(this.baseUrl, { params }),
    );
    // Merge into local cache (upsert)
    this.mergeIntoCache(categories);
    return categories;
  }

  async getTree(): Promise<Category[]> {
    await this.getAll();
    return this.categoryTree();
  }

  async getById(id: string): Promise<Category | undefined> {
    try {
      const category = await firstValueFrom(
        this.http.get<Category>(`${this.baseUrl}/${id}`),
      );
      this.mergeIntoCache([category]);
      return category;
    } catch {
      return undefined;
    }
  }

  async create(dto: CreateCategoryDto): Promise<Category> {
    const created = await firstValueFrom(
      this.http.post<Category>(this.baseUrl, dto),
    );
    this.categoriesData.update((cats) => [...cats, { ...created, children: [] }]);
    return created;
  }

  async update(id: string, dto: UpdateCategoryDto): Promise<Category | undefined> {
    try {
      const updated = await firstValueFrom(
        this.http.put<Category>(`${this.baseUrl}/${id}`, dto),
      );
      this.categoriesData.update((cats) =>
        cats.map((c) => (c.id === id ? { ...updated, children: [] } : c))
      );
      return updated;
    } catch {
      return undefined;
    }
  }

  async delete(id: string): Promise<boolean> {
    try {
      await firstValueFrom(
        this.http.delete<void>(`${this.baseUrl}/${id}`),
      );
      this.categoriesData.update((cats) => cats.filter((c) => c.id !== id));
      // Also remove associated variation fields
      this.variationFieldsData.update((fields) =>
        fields.filter((f) => f.categoryId !== id)
      );
      return true;
    } catch {
      return false;
    }
  }

  // ── Variation fields (frontend-only) ──

  getVariationFields(categoryId: string): VariationField[] {
    return this.variationFieldsData().filter((f) => f.categoryId === categoryId);
  }

  getInheritedVariationFields(categoryId: string): InheritedVariationField[] {
    const result: InheritedVariationField[] = [];
    const ancestors = this.getAncestors(categoryId);

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

  getAllVariationFieldsForCategory(categoryId: string): InheritedVariationField[] {
    const inherited = this.getInheritedVariationFields(categoryId);
    const own = this.getVariationFields(categoryId);
    const category = this.categoriesData().find(c => c.id === categoryId);
    const ownAsInherited: InheritedVariationField[] = own.map(f => ({
      ...f,
      inheritedFrom: category?.name ?? '',
      inheritedFromId: categoryId,
    }));
    return [...inherited, ...ownAsInherited];
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

  // ── Private helpers ──

  private mergeIntoCache(categories: Category[]): void {
    this.categoriesData.update((existing) => {
      const map = new Map(existing.map((c) => [c.id, c]));
      for (const cat of categories) {
        map.set(cat.id, { ...cat, children: [] });
      }
      return Array.from(map.values());
    });
  }
}
