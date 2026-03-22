import { Component, Input, Output, EventEmitter, signal, computed, HostListener, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, ChevronDown, ChevronRight, Search } from 'lucide-angular';
import { Category } from '../../models/category.model';
import { CategoryService } from '../../services/category.service';

interface FlatNode {
  category: Category;
  depth: number;
  hasChildren: boolean;
  expanded: boolean;
}

@Component({
  selector: 'app-tree-select',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './tree-select.component.html',
  styleUrl: './tree-select.component.scss',
})
export class TreeSelectComponent {
  private readonly el = inject(ElementRef);
  private readonly categoryService = inject(CategoryService);

  readonly chevronDownIcon = ChevronDown;
  readonly chevronRightIcon = ChevronRight;
  readonly searchIcon = Search;

  @Input() categories: Category[] = [];
  @Input() value: string | null = null;
  @Output() valueChange = new EventEmitter<string>();

  isOpen = signal(false);
  searchQuery = signal('');
  expandedIds = signal<Set<string>>(new Set());
  focusedIndex = signal(-1);

  /** Get breadcrumb for selected category */
  selectedBreadcrumb = computed(() => {
    if (!this.value) return '';
    return this.categoryService.getBreadcrumb(this.value).join(' > ');
  });

  /** Flatten the tree for rendering, respecting expanded state and search */
  flatNodes = computed((): FlatNode[] => {
    const query = this.searchQuery().toLowerCase();
    const expanded = this.expandedIds();
    const nodes: FlatNode[] = [];

    const matchesSearch = (cat: Category): boolean => {
      if (!query) return true;
      if (cat.name.toLowerCase().includes(query)) return true;
      return cat.children.some(c => matchesSearch(c));
    };

    const flatten = (cats: Category[], depth: number) => {
      for (const cat of cats) {
        if (query && !matchesSearch(cat)) continue;

        const hasChildren = cat.children.length > 0;
        const isExpanded = query
          ? hasChildren && cat.children.some(c => matchesSearch(c))
          : expanded.has(cat.id);

        nodes.push({
          category: cat,
          depth,
          hasChildren,
          expanded: isExpanded,
        });

        if (isExpanded && hasChildren) {
          flatten(cat.children, depth + 1);
        }
      }
    };

    flatten(this.categories, 0);
    return nodes;
  });

  toggleOpen(): void {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      this.searchQuery.set('');
      this.focusedIndex.set(-1);
    }
  }

  close(): void {
    this.isOpen.set(false);
    this.searchQuery.set('');
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.focusedIndex.set(-1);
  }

  toggleExpand(nodeId: string, event: Event): void {
    event.stopPropagation();
    this.expandedIds.update(set => {
      const next = new Set(set);
      if (next.has(nodeId)) {
        next.delete(nodeId);
      } else {
        next.add(nodeId);
      }
      return next;
    });
  }

  selectNode(node: FlatNode): void {
    this.valueChange.emit(node.category.id);
    this.close();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target)) {
      this.close();
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    if (!this.isOpen()) return;

    const nodes = this.flatNodes();

    switch (event.key) {
      case 'Escape':
        this.close();
        event.preventDefault();
        break;
      case 'ArrowDown':
        this.focusedIndex.update(i => Math.min(i + 1, nodes.length - 1));
        event.preventDefault();
        break;
      case 'ArrowUp':
        this.focusedIndex.update(i => Math.max(i - 1, 0));
        event.preventDefault();
        break;
      case 'Enter':
        const idx = this.focusedIndex();
        if (idx >= 0 && idx < nodes.length) {
          this.selectNode(nodes[idx]);
        }
        event.preventDefault();
        break;
    }
  }
}
