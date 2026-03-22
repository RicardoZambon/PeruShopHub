import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule } from 'lucide-angular';
import { FolderTree, FolderPlus, ChevronLeft } from 'lucide-angular';
import { CategoryService } from '../../services/category.service';

@Component({
  selector: 'app-categorias',
  standalone: true,
  imports: [CommonModule, LucideAngularModule],
  templateUrl: './categorias.component.html',
  styleUrl: './categorias.component.scss',
})
export class CategoriasComponent {
  private readonly categoryService = inject(CategoryService);

  /** Icons */
  readonly folderTreeIcon = FolderTree;
  readonly folderPlusIcon = FolderPlus;
  readonly chevronLeftIcon = ChevronLeft;

  /** Currently selected category ID */
  readonly selectedCategoryId = signal<string | null>(null);

  /** Mobile view mode — tree or detail panel */
  readonly mobileView = signal<'tree' | 'detail'>('tree');

  /** Category tree from service */
  readonly categoryTree = this.categoryService.categoryTree;

  /** Total category count */
  readonly totalCount = this.categoryService.totalCount;

  /** Selected category object */
  readonly selectedCategory = computed(() => {
    const id = this.selectedCategoryId();
    if (!id) return null;
    return this.categoryService.categories().find(c => c.id === id) ?? null;
  });

  /** Breadcrumb path for selected category */
  readonly selectedBreadcrumb = computed(() => {
    const id = this.selectedCategoryId();
    if (!id) return [];
    return this.categoryService.getBreadcrumb(id);
  });

  selectCategory(id: string): void {
    this.selectedCategoryId.set(id);
    this.mobileView.set('detail');
  }

  backToTree(): void {
    this.mobileView.set('tree');
  }
}
