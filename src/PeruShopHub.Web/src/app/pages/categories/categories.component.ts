import { Component, signal, computed, inject, OnInit, OnDestroy, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, FolderTree, FolderPlus, Search } from 'lucide-angular';
import { PageHeaderComponent, SearchInputComponent } from '../../shared/components';
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
    PageHeaderComponent,
    SearchInputComponent,
    CategoryTreeComponent,
    CategoryDetailComponent,
    CategoryFormDialogComponent,
  ],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent implements OnInit, OnDestroy {
  readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly el = inject(ElementRef);

  readonly folderTreeIcon = FolderTree;
  readonly folderPlusIcon = FolderPlus;
  readonly searchIcon = Search;

  // State
  readonly searchQuery = signal('');
  readonly selectedCategoryId = signal<string | null>(null);
  readonly mobileView = signal<'tree' | 'detail'>('tree');
  readonly showCreateDialog = signal(false);
  readonly dialogCategory = signal<Category | null>(null);
  readonly loading = signal(true);
  readonly detailLoading = signal(false);

  // Resize handle state
  readonly treeWidth = signal<number>(
    parseInt(localStorage.getItem('categories-tree-width') ?? '300', 10)
  );
  readonly resizing = signal(false);

  // Request cancellation
  private detailAbort: AbortController | null = null;

  // Computed
  readonly categoryTree = this.categoryService.categoryTree;
  readonly selectedCategory = signal<Category | null>(null);

  readonly totalCategories = computed(() => {
    return this.categoryService.allCategories().length;
  });

  readonly totalProducts = this.categoryService.totalProductCount;

  // ── Lifecycle ──

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    try {
      await this.categoryService.getAll();
    } catch {
      this.toast.show('Erro ao carregar categorias', 'danger');
    } finally {
      this.loading.set(false);
    }
  }

  ngOnDestroy(): void {
    this.detailAbort?.abort();
  }

  // ── Tree events ──

  async onSelectCategory(id: string): Promise<void> {
    // Cancel any in-flight detail request
    this.detailAbort?.abort();
    this.detailAbort = new AbortController();
    const signal = this.detailAbort.signal;

    this.selectedCategoryId.set(id);
    this.selectedCategory.set(null);
    this.detailLoading.set(true);
    this.mobileView.set('detail');

    try {
      const detail = await this.categoryService.getById(id);
      if (signal.aborted) return;
      if (detail) {
        this.selectedCategory.set(detail);
      }
    } finally {
      if (!signal.aborted) {
        this.detailLoading.set(false);
      }
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  onAddCategory(): void {
    this.dialogCategory.set(null);
    this.showCreateDialog.set(true);
  }

  // ── Detail events ──

  onCategoryUpdated(category: Category): void {
    this.selectedCategoryId.set(category.id);
    this.selectedCategory.set(category);
  }

  onCategoryDeleted(id: string): void {
    const deleted = this.categoryService.allCategories().find((c) => c.id === id);
    if (deleted?.parentId) {
      this.selectedCategoryId.set(deleted.parentId);
      this.onSelectCategory(deleted.parentId);
    } else {
      this.selectedCategoryId.set(null);
      this.selectedCategory.set(null);
      this.detailLoading.set(false);
    }
    this.mobileView.set('tree');
  }

  onBackToTree(): void {
    this.mobileView.set('tree');
  }

  // ── Resize handle ──

  onResizeStart(event: MouseEvent): void {
    event.preventDefault();
    this.resizing.set(true);

    const contentEl = this.el.nativeElement.querySelector('.categorias-page__content') as HTMLElement;
    const contentLeft = contentEl.getBoundingClientRect().left;

    document.body.style.userSelect = 'none';
    document.body.style.cursor = 'col-resize';

    const onMouseMove = (e: MouseEvent) => {
      const newWidth = Math.min(450, Math.max(200, e.clientX - contentLeft));
      this.treeWidth.set(newWidth);
    };

    const onMouseUp = () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      document.body.style.userSelect = '';
      document.body.style.cursor = '';
      this.resizing.set(false);
      localStorage.setItem('categories-tree-width', String(this.treeWidth()));
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  // ── Dialog events ──

  onDialogSaved(category: Category): void {
    this.showCreateDialog.set(false);
    this.selectedCategoryId.set(category.id);
    this.onSelectCategory(category.id);
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
}
