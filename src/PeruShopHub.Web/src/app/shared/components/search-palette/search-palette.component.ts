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

interface SearchResult {
  type: 'pedido' | 'produto' | 'cliente';
  id: string;
  primary: string;
  secondary: string;
  route: string;
}

interface SearchGroup {
  type: 'pedido' | 'produto' | 'cliente';
  label: string;
  icon: LucideIconData;
  results: SearchResult[];
}

const MOCK_PRODUCTS: SearchResult[] = [
  { type: 'produto', id: 'p1', primary: 'Fone Bluetooth TWS Pro', secondary: 'SKU: FBT-001', route: '/produtos/1' },
  { type: 'produto', id: 'p2', primary: 'Capinha iPhone 15 Silicone', secondary: 'SKU: CIP-015', route: '/produtos/2' },
  { type: 'produto', id: 'p3', primary: 'Carregador USB-C 65W', secondary: 'SKU: CRG-065', route: '/produtos/3' },
  { type: 'produto', id: 'p4', primary: 'Pelicula Vidro Samsung S24', secondary: 'SKU: PVS-024', route: '/produtos/4' },
  { type: 'produto', id: 'p5', primary: 'Suporte Celular Veicular', secondary: 'SKU: SCV-001', route: '/produtos/5' },
  { type: 'produto', id: 'p6', primary: 'Cabo HDMI 2.1 4K 2m', secondary: 'SKU: CHD-021', route: '/produtos/6' },
  { type: 'produto', id: 'p7', primary: 'Mouse Gamer RGB 12000dpi', secondary: 'SKU: MGR-120', route: '/produtos/7' },
  { type: 'produto', id: 'p8', primary: 'Teclado Mecanico Compacto', secondary: 'SKU: TMC-001', route: '/produtos/8' },
  { type: 'produto', id: 'p9', primary: 'Webcam Full HD 1080p', secondary: 'SKU: WFH-108', route: '/produtos/9' },
  { type: 'produto', id: 'p10', primary: 'Hub USB-C 7 em 1', secondary: 'SKU: HUB-007', route: '/produtos/10' },
];

const MOCK_ORDERS: SearchResult[] = [
  { type: 'pedido', id: 'o1', primary: 'Pedido #2087654321', secondary: '15 mar 2026', route: '/vendas/2087654321' },
  { type: 'pedido', id: 'o2', primary: 'Pedido #2087654322', secondary: '14 mar 2026', route: '/vendas/2087654322' },
  { type: 'pedido', id: 'o3', primary: 'Pedido #2087654323', secondary: '14 mar 2026', route: '/vendas/2087654323' },
  { type: 'pedido', id: 'o4', primary: 'Pedido #2087654324', secondary: '13 mar 2026', route: '/vendas/2087654324' },
  { type: 'pedido', id: 'o5', primary: 'Pedido #2087654325', secondary: '12 mar 2026', route: '/vendas/2087654325' },
  { type: 'pedido', id: 'o6', primary: 'Pedido #2087654326', secondary: '11 mar 2026', route: '/vendas/2087654326' },
  { type: 'pedido', id: 'o7', primary: 'Pedido #2087654327', secondary: '10 mar 2026', route: '/vendas/2087654327' },
  { type: 'pedido', id: 'o8', primary: 'Pedido #2087654328', secondary: '09 mar 2026', route: '/vendas/2087654328' },
  { type: 'pedido', id: 'o9', primary: 'Pedido #2087654329', secondary: '08 mar 2026', route: '/vendas/2087654329' },
  { type: 'pedido', id: 'o10', primary: 'Pedido #2087654330', secondary: '07 mar 2026', route: '/vendas/2087654330' },
  { type: 'pedido', id: 'o11', primary: 'Pedido #2087654331', secondary: '06 mar 2026', route: '/vendas/2087654331' },
  { type: 'pedido', id: 'o12', primary: 'Pedido #2087654332', secondary: '05 mar 2026', route: '/vendas/2087654332' },
  { type: 'pedido', id: 'o13', primary: 'Pedido #2087654333', secondary: '04 mar 2026', route: '/vendas/2087654333' },
  { type: 'pedido', id: 'o14', primary: 'Pedido #2087654334', secondary: '03 mar 2026', route: '/vendas/2087654334' },
  { type: 'pedido', id: 'o15', primary: 'Pedido #2087654335', secondary: '02 mar 2026', route: '/vendas/2087654335' },
];

const MOCK_CUSTOMERS: SearchResult[] = [
  { type: 'cliente', id: 'c1', primary: 'Maria Silva', secondary: 'maria.silva@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c2', primary: 'Joao Santos', secondary: 'joao.santos@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c3', primary: 'Ana Oliveira', secondary: 'ana.oliveira@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c4', primary: 'Carlos Pereira', secondary: 'carlos.pereira@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c5', primary: 'Fernanda Costa', secondary: 'fernanda.costa@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c6', primary: 'Pedro Almeida', secondary: 'pedro.almeida@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c7', primary: 'Lucia Ferreira', secondary: 'lucia.ferreira@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c8', primary: 'Rafael Souza', secondary: 'rafael.souza@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c9', primary: 'Beatriz Lima', secondary: 'beatriz.lima@email.com', route: '/clientes' },
  { type: 'cliente', id: 'c10', primary: 'Marcos Rodrigues', secondary: 'marcos.rodrigues@email.com', route: '/clientes' },
];

const MAX_PER_GROUP = 3;

@Component({
  selector: 'app-search-palette',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './search-palette.component.html',
  styleUrl: './search-palette.component.scss',
})
export class SearchPaletteComponent implements AfterViewInit, OnDestroy {
  private readonly router = inject(Router);
  readonly closed = output<void>();

  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  readonly searchIcon = Search;
  readonly closeIcon = X;

  readonly query = signal('');
  readonly activeIndex = signal(-1);
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private readonly debouncedQuery = signal('');

  readonly groups = computed<SearchGroup[]>(() => {
    const q = this.debouncedQuery().toLowerCase().trim();
    if (!q) return [];

    const groups: SearchGroup[] = [];

    const matchedOrders = MOCK_ORDERS.filter(
      (r) => r.primary.toLowerCase().includes(q) || r.secondary.toLowerCase().includes(q),
    ).slice(0, MAX_PER_GROUP);
    if (matchedOrders.length > 0) {
      groups.push({ type: 'pedido', label: 'Pedidos', icon: ShoppingCart, results: matchedOrders });
    }

    const matchedProducts = MOCK_PRODUCTS.filter(
      (r) => r.primary.toLowerCase().includes(q) || r.secondary.toLowerCase().includes(q),
    ).slice(0, MAX_PER_GROUP);
    if (matchedProducts.length > 0) {
      groups.push({ type: 'produto', label: 'Produtos', icon: Package, results: matchedProducts });
    }

    const matchedCustomers = MOCK_CUSTOMERS.filter(
      (r) => r.primary.toLowerCase().includes(q) || r.secondary.toLowerCase().includes(q),
    ).slice(0, MAX_PER_GROUP);
    if (matchedCustomers.length > 0) {
      groups.push({ type: 'cliente', label: 'Clientes', icon: Users, results: matchedCustomers });
    }

    return groups;
  });

  readonly flatResults = computed<SearchResult[]>(() => {
    return this.groups().flatMap((g) => g.results);
  });

  readonly hasQuery = computed(() => this.debouncedQuery().trim().length > 0);
  readonly noResults = computed(() => this.hasQuery() && this.flatResults().length === 0);

  ngAfterViewInit(): void {
    setTimeout(() => this.searchInput?.nativeElement.focus(), 0);
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.query.set(value);
    this.activeIndex.set(-1);

    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
    }
    this.debounceTimer = setTimeout(() => {
      this.debouncedQuery.set(value);
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
}
