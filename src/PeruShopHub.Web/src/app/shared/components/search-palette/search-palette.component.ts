import {
  Component,
  inject,
  signal,
  computed,
  output,
  HostListener,
  ElementRef,
  AfterViewInit,
  ViewChild,
  OnDestroy,
} from '@angular/core';
import { Router } from '@angular/router';
import {
  LucideAngularModule,
  Search,
  X,
  Package,
  ShoppingCart,
  Users,
  type LucideIconData,
} from 'lucide-angular';
import { SearchService, type SearchResult } from '../../../services/search.service';
import { Subscription } from 'rxjs';

interface SearchGroup {
  type: 'pedido' | 'produto' | 'cliente';
  label: string;
  icon: LucideIconData;
  results: SearchResult[];
}

const MAX_PER_GROUP = 3;

const TYPE_CONFIG: Record<SearchResult['type'], { label: string; icon: LucideIconData }> = {
  pedido: { label: 'Pedidos', icon: ShoppingCart },
  produto: { label: 'Produtos', icon: Package },
  cliente: { label: 'Clientes', icon: Users },
};

@Component({
  selector: 'app-search-palette',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './search-palette.component.html',
  styleUrl: './search-palette.component.scss',
})
export class SearchPaletteComponent implements AfterViewInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly searchService = inject(SearchService);
  readonly closed = output<void>();

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  readonly searchIcon = Search;
  readonly closeIcon = X;

  readonly query = signal('');
  readonly activeIndex = signal(-1);
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private searchSubscription: Subscription | null = null;

  readonly apiResults = signal<SearchResult[]>([]);

  readonly groups = computed<SearchGroup[]>(() => {
    const results = this.apiResults();
    if (results.length === 0) return [];

    const grouped = new Map<SearchResult['type'], SearchResult[]>();
    for (const r of results) {
      const existing = grouped.get(r.type) || [];
      if (existing.length < MAX_PER_GROUP) {
        existing.push(r);
        grouped.set(r.type, existing);
      }
    }

    const groups: SearchGroup[] = [];
    const typeOrder: SearchResult['type'][] = ['pedido', 'produto', 'cliente'];
    for (const type of typeOrder) {
      const items = grouped.get(type);
      if (items && items.length > 0) {
        const config = TYPE_CONFIG[type];
        groups.push({ type, label: config.label, icon: config.icon, results: items });
      }
    }

    return groups;
  });

  readonly flatResults = computed<SearchResult[]>(() => {
    return this.groups().flatMap((g) => g.results);
  });

  readonly hasQuery = computed(() => this.query().trim().length > 0);
  readonly noResults = computed(() => this.hasQuery() && this.flatResults().length === 0);

  ngAfterViewInit(): void {
    setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
    this.searchSubscription?.unsubscribe();
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.activeIndex.set(-1);

    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
    this.debounceTimer = setTimeout(() => {
      this.performSearch(value);
    }, 300);
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('palette-backdrop')) {
      this.closed.emit();
    }
  }

  navigateToResult(result: SearchResult): void {
    this.router.navigateByUrl(result.route);
    this.closed.emit();
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.closed.emit();
  }

  @HostListener('document:keydown.arrowdown', ['$event'])
  onArrowDown(event: Event): void {
    event.preventDefault();
    const max = this.flatResults().length;
    if (max === 0) return;
    this.activeIndex.update((i) => (i + 1) % max);
  }

  @HostListener('document:keydown.arrowup', ['$event'])
  onArrowUp(event: Event): void {
    event.preventDefault();
    const max = this.flatResults().length;
    if (max === 0) return;
    this.activeIndex.update((i) => (i - 1 + max) % max);
  }

  @HostListener('document:keydown.enter')
  onEnter(): void {
    const idx = this.activeIndex();
    const results = this.flatResults();
    if (idx >= 0 && idx < results.length) {
      this.navigateToResult(results[idx]);
    }
  }

  isActive(result: SearchResult): boolean {
    const idx = this.flatResults().indexOf(result);
    return idx === this.activeIndex();
  }

  private performSearch(query: string): void {
    const trimmed = query.trim();
    if (!trimmed) {
      this.apiResults.set([]);
      return;
    }

    this.searchSubscription?.unsubscribe();
    this.searchSubscription = this.searchService.search(trimmed).subscribe({
      next: (results) => this.apiResults.set(results),
      error: (err) => {
        console.error('Search failed:', err);
        this.apiResults.set([]);
      },
    });
  }
}
