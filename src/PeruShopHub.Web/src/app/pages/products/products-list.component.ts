import { Component, signal, computed, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus, Edit, Package } from 'lucide-angular';
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  GridColumn,
  GridSortEvent,
} from '../../shared/components/data-grid/data-grid.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { MarginBadgeComponent } from '../../shared/components/margin-badge/margin-badge.component';
import { ProductService, Product } from '../../services/product.service';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type ProductStatus = 'Ativo' | 'Pausado' | 'Encerrado';
type FilterStatus = 'Todos' | ProductStatus | 'Revisão';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataGridComponent, GridCellDirective, GridCardDirective, BadgeComponent, EmptyStateComponent, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent, MarginBadgeComponent],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.scss',
})
export class ProductsListComponent implements OnInit {
  private readonly productService = inject(ProductService);

  readonly plusIcon = Plus;
  readonly editIcon = Edit;
  readonly packageIcon = Package;

  @ViewChild('grid') gridRef!: DataGridComponent;

  readonly statusOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos' },
    { value: 'Ativo', label: 'Ativo' },
    { value: 'Pausado', label: 'Pausado' },
    { value: 'Encerrado', label: 'Encerrado' },
    { value: 'Revisão', label: 'Precisa revisão' },
  ];

  readonly searchQuery = signal('');
  readonly statusFilter = signal<FilterStatus>('Todos');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly hasMore = signal(true);

  readonly products = signal<Product[]>([]);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  readonly gridColumns: GridColumn[] = [
    { key: 'imageUrl', label: 'Foto', align: 'center', width: '56px' },
    { key: 'name', label: 'Nome', sortable: true },
    { key: 'sku', label: 'SKU', cellClass: 'cell-mono' },
    { key: 'price', label: 'Preço', align: 'right', sortable: true },
    { key: 'stock', label: 'Estoque', align: 'right', sortable: true },
    { key: 'variantCount', label: 'Variantes', align: 'center' },
    { key: 'status', label: 'Status' },
    { key: 'margin', label: 'Margem', align: 'right', sortable: true },
    { key: 'actions', label: 'Ações', align: 'center', width: '56px' },
  ];

  readonly gridData = computed(() => {
    return this.products().map(p => ({
      ...p,
      id: p.id,
      imageUrl: p.imageUrl ?? '',
      name: p.name,
      sku: p.sku,
      price: p.price,
      stock: p.stock,
      status: p.status,
      margin: p.margin,
      variantCount: p.variantCount,
      needsReview: p.needsReview,
    }));
  });

  constructor(public router: Router) {}

  ngOnInit(): void {
    this.loadProducts(true);
  }

  async loadProducts(reset = false): Promise<void> {
    if (reset) {
      this.currentPage.set(1);
      this.products.set([]);
      this.hasMore.set(true);
    }

    this.loading.set(true);
    try {
      const result = await this.productService.list({
        page: this.currentPage(),
        pageSize: this.pageSize(),
        search: this.searchQuery() || undefined,
        status: this.statusFilter() === 'Todos' ? undefined : this.statusFilter(),
        sortBy: this.sortBy() ?? undefined,
        sortDirection: this.sortDirection(),
      });

      if (reset) {
        this.products.set(result.items);
      } else {
        this.products.update(prev => [...prev, ...result.items]);
      }

      const totalLoaded = this.products().length;
      this.hasMore.set(totalLoaded < result.totalCount);
      this.hasData.set(totalLoaded > 0 || this.searchQuery().length > 0 || this.statusFilter() !== 'Todos');
    } catch {
      if (reset) {
        this.products.set([]);
      }
      this.hasMore.set(false);
      this.hasData.set(false);
    } finally {
      this.loading.set(false);
    }
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Ativo': return 'success';
      case 'Pausado': return 'warning';
      case 'Encerrado': return 'danger';
      default: return 'neutral';
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.loadProducts(true);
    this.gridRef?.scrollToTop();
  }

  onStatusChange(value: string): void {
    this.statusFilter.set(value as FilterStatus);
    this.loadProducts(true);
    this.gridRef?.scrollToTop();
  }

  onRowClick(row: Record<string, any>): void {
    this.router.navigate(['/produtos', row['id']]);
  }

  onNewProduct(): void {
    this.router.navigate(['/produtos/novo']);
  }

  onVariantClick(event: Event, productId: string | number): void {
    event.stopPropagation();
    this.router.navigate(['/produtos', productId, 'editar'], { queryParams: { tab: 'variacoes' } });
  }

  onSort(event: GridSortEvent): void {
    this.sortBy.set(event.direction ? event.column : null);
    this.sortDirection.set(event.direction ?? 'asc');
    this.loadProducts(true);
    this.gridRef?.scrollToTop();
  }

  onLoadMore(): void {
    this.currentPage.update(p => p + 1);
    this.loadProducts(false);
  }
}
