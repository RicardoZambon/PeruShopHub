import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, FolderPlus } from 'lucide-angular';
import { CategoryTreeNodeComponent } from './category-tree-node.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
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
  ],
  templateUrl: './category-tree.component.html',
  styleUrl: './category-tree.component.scss',
})
export class CategoryTreeComponent {
  @Input({ required: true }) categories!: Category[];
  @Input() selectedId: string | null = null;

  @Output() selectCategory = new EventEmitter<string>();
  @Output() addCategory = new EventEmitter<void>();

  readonly searchIcon = Search;
  readonly folderPlusIcon = FolderPlus;

  readonly searchQuery = signal('');

  readonly filteredTree = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) return this.categories;
    return this.filterTree(this.categories, query);
  });

  readonly hasCategories = computed(() => {
    return this.categories && this.categories.length > 0;
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
