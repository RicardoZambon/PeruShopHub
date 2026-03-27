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
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/categories`;

  // Local cache of loaded categories (flat)
  private readonly categoriesData = signal<Category[]>([]);

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

    // Sort children alphabetically
    const sortChildren = (cats: Category[]): void => {
      cats.sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'));
      for (const cat of cats) {
        sortChildren(cat.children);
      }
    };

    sortChildren(roots);
    return roots;
  }

  // ── Category HTTP methods ──

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

  async search(query: string): Promise<Category[]> {
    const params = new HttpParams().set('q', query);
    const categories = await firstValueFrom(
      this.http.get<Category[]>(`${this.baseUrl}/search`, { params }),
    );
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

  async update(id: string, dto: UpdateCategoryDto & { version: number }): Promise<Category> {
    const updated = await firstValueFrom(
      this.http.put<Category>(`${this.baseUrl}/${id}`, dto),
    );
    this.categoriesData.update((cats) =>
      cats.map((c) => (c.id === id ? { ...updated, children: [] } : c))
    );
    return updated;
  }

  async delete(id: string): Promise<boolean> {
    try {
      await firstValueFrom(
        this.http.delete<void>(`${this.baseUrl}/${id}`),
      );
      this.categoriesData.update((cats) => cats.filter((c) => c.id !== id));
      return true;
    } catch {
      return false;
    }
  }

  // ── Variation Fields (backend-persisted) ──

  async getVariationFields(categoryId: string): Promise<VariationField[]> {
    return firstValueFrom(
      this.http.get<VariationField[]>(`${this.baseUrl}/${categoryId}/variation-fields`),
    );
  }

  async getInheritedVariationFields(categoryId: string): Promise<InheritedVariationField[]> {
    return firstValueFrom(
      this.http.get<InheritedVariationField[]>(
        `${this.baseUrl}/${categoryId}/variation-fields/inherited`
      ),
    );
  }

  async addVariationField(
    categoryId: string,
    dto: CreateVariationFieldDto
  ): Promise<VariationField> {
    return firstValueFrom(
      this.http.post<VariationField>(
        `${this.baseUrl}/${categoryId}/variation-fields`, dto
      ),
    );
  }

  async updateVariationField(
    categoryId: string,
    fieldId: string,
    dto: UpdateVariationFieldDto
  ): Promise<VariationField> {
    return firstValueFrom(
      this.http.put<VariationField>(
        `${this.baseUrl}/${categoryId}/variation-fields/${fieldId}`, dto
      ),
    );
  }

  async deleteVariationField(categoryId: string, fieldId: string): Promise<boolean> {
    try {
      await firstValueFrom(
        this.http.delete<void>(
          `${this.baseUrl}/${categoryId}/variation-fields/${fieldId}`
        ),
      );
      return true;
    } catch {
      return false;
    }
  }

  // ── Helpers ──

  getBreadcrumb(categoryId: string): string[] {
    const ancestors = this.getAncestors(categoryId);
    const current = this.categoriesData().find((c) => c.id === categoryId);
    const names = ancestors.map((a) => a.name);
    if (current) names.push(current.name);
    return names;
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
