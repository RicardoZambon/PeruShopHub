import { Component, input, output, signal, computed, HostListener, ElementRef, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, ChevronDown, ChevronRight, Search } from 'lucide-angular';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchSubject = new Subject<string>();

  readonly chevronDownIcon = ChevronDown;
  readonly chevronRightIcon = ChevronRight;
  readonly searchIcon = Search;

  readonly categories = input<Category[]>([]);
  readonly value = input<string | null>(null);
  readonly valueChange = output<string>();

  isOpen = signal(false);
  searchQuery = signal('');
  expandedIds = signal<Set<string>>(new Set());
  focusedIndex = signal(-1);
  readonly searchResults = signal<Category[] | null>(null);

  constructor() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe(async (query) => {
      if (!query.trim()) {
        this.searchResults.set(null);
        return;
      }
      try {
        const results = await this.categoryService.search(query);
        this.searchResults.set(results);
      } catch {
        this.searchResults.set(null);
      }
    });
  }

  /** Get breadcrumb for selected category */
  selectedBreadcrumb = computed(() => {
    if (!this.value()) return '';
    return this.categoryService.getBreadcrumb(this.value()!).join(' > ');
  });

  /** The effective tree: API search results when searching, or the full input tree */
  private effectiveCategories = computed((): Category[] => {
    const apiResults = this.searchResults();
    if (apiResults !== null && this.searchQuery().trim()) {
      return this.buildTreeFromFlat(apiResults);
    }
    return this.categories();
  });

  private buildTreeFromFlat(flat: Category[]): Category[] {
    const map = new Map<string, Category>();
    const roots: Category[] = [];

    for (const cat of flat) {
      map.set(cat.id, { ...cat, children: [] });
    }

    for (const cat of map.values()) {
      if (cat.parentId && map.has(cat.parentId)) {
        map.get(cat.parentId)!.children.push(cat);
      } else if (!cat.parentId || !map.has(cat.parentId)) {
        roots.push(cat);
      }
    }

    return roots;
  }

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

    flatten(this.effectiveCategories(), 0);
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
    this.searchResults.set(null);
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
    this.focusedIndex.set(-1);
    this.searchSubject.next(value);
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
