import { Component, Input, Output, EventEmitter, signal, computed, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CdkDragDrop, CdkDropList, CdkDrag, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { LucideAngularModule, Search, FolderPlus } from 'lucide-angular';
import { CategoryTreeNodeComponent } from './category-tree-node.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { CategoryService } from '../../services/category.service';
import { ToastService } from '../../services/toast.service';
import type { Category } from '../../models/category.model';

@Component({
  selector: 'app-category-tree',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LucideAngularModule,
    CategoryTreeNodeComponent,
    EmptyStateComponent,
    CdkDropList,
    CdkDrag,
    CdkDragPlaceholder,
  ],
  templateUrl: './category-tree.component.html',
  styleUrl: './category-tree.component.scss',
})
export class CategoryTreeComponent implements OnChanges {
  @Input({ required: true }) categories!: Category[];
  @Input() selectedId: string | null = null;
  @Input() externalSearchQuery = '';

  @Output() selectCategory = new EventEmitter<string>();
  @Output() addCategory = new EventEmitter<void>();

  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  readonly searchIcon = Search;
  readonly folderPlusIcon = FolderPlus;

  readonly searchQuery = signal('');

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['externalSearchQuery']) {
      this.searchQuery.set(changes['externalSearchQuery'].currentValue ?? '');
    }
  }

  readonly filteredTree = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return this.categories;
    return this.filterTree(this.categories, query);
  });

  readonly hasCategories = computed(() => {
    return this.categories && this.categories.length > 0;
  });

  readonly isDragDisabled = computed(() => {
    return this.searchQuery().trim().length > 0;
  });

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
  }

  onSelectCategory(id: string): void {
    this.selectCategory.emit(id);
  }

  onAddCategory(): void {
    this.addCategory.emit();
  }

  async onRootDrop(event: CdkDragDrop<Category[]>): Promise<void> {
    if (event.previousIndex !== event.currentIndex) {
      const items = [...this.filteredTree()];
      const [moved] = items.splice(event.previousIndex, 1);
      items.splice(event.currentIndex, 0, moved);
      const orderedIds = items.map((c) => c.id);
      await this.categoryService.reorderCategories(null, orderedIds);
      this.toast.show('Categorias reordenadas', 'success');
    }
  }

  async onChildReorder(event: { categoryId: string; newParentId: string | null; newIndex: number }): Promise<void> {
    // Re-order within a parent
    const siblings = this.categoryService.allCategories()
      .filter((c) => c.parentId === event.newParentId)
      .sort((a, b) => a.order - b.order);

    const currentIndex = siblings.findIndex((c) => c.id === event.categoryId);
    if (currentIndex === -1) return;

    const items = [...siblings];
    const [moved] = items.splice(currentIndex, 1);
    items.splice(event.newIndex, 0, moved);
    const orderedIds = items.map((c) => c.id);
    await this.categoryService.reorderCategories(event.newParentId, orderedIds);
    this.toast.show('Categorias reordenadas', 'success');
  }

  private filterTree(categories: Category[], query: string): Category[] {
    const result: Category[] = [];

    for (const cat of categories) {
      const nameMatches = cat.name.toLowerCase().includes(query);
      const filteredChildren = this.filterTree(cat.children, query);

      if (nameMatches || filteredChildren.length > 0) {
        result.push({
          ...cat,
          children: nameMatches ? cat.children : filteredChildren,
        });
      }
    }

    return result;
  }
}
