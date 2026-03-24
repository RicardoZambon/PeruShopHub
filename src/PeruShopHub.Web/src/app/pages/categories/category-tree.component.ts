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
  @Input() loading = false;

  @Output() selectCategory = new EventEmitter<string>();
  @Output() addCategory = new EventEmitter<void>();

  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  readonly searchIcon = Search;
  readonly folderPlusIcon = FolderPlus;

  readonly searchQuery = signal('');
  private readonly categoriesSignal = signal<Category[]>([]);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['externalSearchQuery']) {
      this.searchQuery.set(changes['externalSearchQuery'].currentValue ?? '');
    }
    if (changes['categories']) {
      this.categoriesSignal.set(changes['categories'].currentValue ?? []);
    }
  }

  readonly filteredTree = computed(() => {
    const cats = this.categoriesSignal();
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return cats;
    return this.filterTree(cats, query);
  });

  readonly hasCategories = computed(() => {
    const cats = this.categoriesSignal();
    return cats && cats.length > 0;
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

  onRootDrop(_event: CdkDragDrop<Category[]>): void {
    // Categories are sorted alphabetically — drag reordering disabled
  }

  onChildReorder(_event: { categoryId: string; newParentId: string | null; newIndex: number }): void {
    // Categories are sorted alphabetically — drag reordering disabled
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
