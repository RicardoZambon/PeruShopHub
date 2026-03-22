import { Component, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, FolderTree } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';
import { ToastService } from '../../services/toast.service';
import { CategoryTreeComponent } from './category-tree.component';
import { CategoryDetailComponent } from './category-detail.component';
import { CategoryFormDialogComponent } from './category-form-dialog.component';
import type { Category } from '../../models/category.model';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule,
    LucideAngularModule,
    CategoryTreeComponent,
    CategoryDetailComponent,
    CategoryFormDialogComponent,
  ],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent {
  readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  readonly folderTreeIcon = FolderTree;

  // State
  readonly selectedCategoryId = signal<string | null>(null);
  readonly mobileView = signal<'tree' | 'detail'>('tree');
  readonly showCreateDialog = signal(false);
  readonly dialogCategory = signal<Category | null>(null);

  // Computed
  readonly categoryTree = this.categoryService.categoryTree;

  readonly selectedCategory = computed<Category | null>(() => {
    const id = this.selectedCategoryId();
    if (!id) return null;
    return this.findCategoryById(this.categoryTree(), id) ?? null;
  });

  readonly totalCategories = computed(() => {
    return this.categoryService.allCategories().length;
  });

  readonly totalProducts = this.categoryService.totalProductCount;

  // ── Tree events ──

  onSelectCategory(id: string): void {
    this.selectedCategoryId.set(id);
    this.mobileView.set('detail');
  }

  onAddCategory(): void {
    this.dialogCategory.set(null);
    this.showCreateDialog.set(true);
  }

  // ── Detail events ──

  onCategoryUpdated(category: Category): void {
    // Tree auto-updates via signals
    this.selectedCategoryId.set(category.id);
  }

  onCategoryDeleted(id: string): void {
    // Select parent or null
    const deleted = this.categoryService.allCategories().find((c) => c.id === id);
    if (deleted?.parentId) {
      this.selectedCategoryId.set(deleted.parentId);
    } else {
      this.selectedCategoryId.set(null);
    }
    this.mobileView.set('tree');
  }

  onBackToTree(): void {
    this.mobileView.set('tree');
  }

  // ── Dialog events ──

  onDialogSaved(category: Category): void {
    this.showCreateDialog.set(false);
    this.selectedCategoryId.set(category.id);
    this.mobileView.set('detail');
    this.toast.show(
      this.dialogCategory() ? 'Categoria atualizada com sucesso' : 'Categoria criada com sucesso',
      'success'
    );
  }

  onDialogClosed(): void {
    this.showCreateDialog.set(false);
    this.dialogCategory.set(null);
  }

  // ── Helpers ──

  private findCategoryById(tree: Category[], id: string): Category | undefined {
    for (const cat of tree) {
      if (cat.id === id) return cat;
      if (cat.children.length > 0) {
        const found = this.findCategoryById(cat.children, id);
        if (found) return found;
      }
    }
    return undefined;
  }
}
