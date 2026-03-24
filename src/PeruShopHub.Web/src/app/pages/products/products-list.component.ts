import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus, Edit, Package } from 'lucide-angular';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { MarginBadgeComponent } from '../../shared/components/margin-badge/margin-badge.component';
import { ProductService, Product } from '../../services/product.service';
import type { DataTableColumn, SortEvent, PageEvent } from '../../shared/components/data-table/data-table.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type ProductStatus = 'Ativo' | 'Pausado' | 'Encerrado';
type FilterStatus = 'Todos' | ProductStatus | 'Revisão';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataTableComponent, BadgeComponent, EmptyStateComponent, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent, MarginBadgeComponent],
  templateUrl: './products-list.component.html',
  styleUrl: './products-list.component.scss',
})
export class ProductsListComponent implements OnInit {
  private readonly productService = inject(ProductService);

  readonly plusIcon = Plus;
  readonly editIcon = Edit;
  readonly packageIcon = Package;

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

  readonly products = signal<Product[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  readonly columns: DataTableColumn[] = [
    { key: 'foto', label: 'Foto', align: 'center' },
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'sku', label: 'SKU', format: 'mono' },
    { key: 'preco', label: 'Preço', align: 'right', format: 'currency', sortable: true },
    { key: 'estoque', label: 'Estoque', align: 'right', sortable: true },
    { key: 'variantes', label: 'Variantes', align: 'center' },
    { key: 'status', label: 'Status' },
    { key: 'margem', label: 'Margem', align: 'right', sortable: true },
    { key: 'acoes', label: 'Ações', align: 'center' },
  ];

  readonly filteredProducts = computed(() => {
    return this.products();
  });

  readonly tableData = computed(() => {
    return this.products().map(p => ({
      id: p.id,
      foto: p.imageUrl ?? '',
      nome: p.name,
      sku: p.sku,
      preco: p.price,
      estoque: p.stock,
      status: p.status,
      margem: p.margin,
      variantCount: p.variantCount,
      needsReview: p.needsReview,
      precoFormatted: this.formatBrl(p.price),
      margemFormatted: `${(p.margin ?? 0).toFixed(1)}%`,
    }));
  });

  constructor(public router: Router) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  async loadProducts(): Promise<void> {
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
      this.products.set(result.items);
      this.totalCount.set(result.totalCount);
      this.hasData.set(result.items.length > 0 || this.searchQuery().length > 0 || this.statusFilter() !== 'Todos');
    } catch {
      this.products.set([]);
      this.totalCount.set(0);
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
    this.currentPage.set(1);
    this.loadProducts();
  }

  onStatusChange(value: string): void {
    this.statusFilter.set(value as FilterStatus);
    this.currentPage.set(1);
    this.loadProducts();
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

  onSort(event: SortEvent): void {
    this.sortBy.set(event.column);
    this.sortDirection.set(event.direction);
    this.loadProducts();
  }

  onPageChange(event: PageEvent): void {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
    this.loadProducts();
  }
}
