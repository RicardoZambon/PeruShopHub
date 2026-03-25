import { Component, signal, computed, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search } from 'lucide-angular';
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  GridColumn,
  GridSortEvent,
} from '../../shared/components/data-grid/data-grid.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { RelativeDatePipe } from '../../shared/pipes/relative-date.pipe';
import { CustomerService, type CustomerListItem } from '../../services/customer.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataGridComponent, GridCellDirective, GridCardDirective, EmptyStateComponent, PageHeaderComponent, SearchInputComponent, RelativeDatePipe],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss',
})
export class CustomersComponent implements OnInit {
  readonly searchIcon = Search;

  @ViewChild('grid') gridRef!: DataGridComponent;

  readonly searchQuery = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly hasMore = signal(true);

  readonly customers = signal<CustomerListItem[]>([]);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>('totalGasto');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  private readonly customerService = inject(CustomerService);

  readonly gridColumns: GridColumn[] = [
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'nickname', label: 'Nickname' },
    { key: 'email', label: 'Email' },
    { key: 'totalPedidos', label: 'Total Pedidos', align: 'right', sortable: true },
    { key: 'totalGasto', label: 'Total Gasto', align: 'right', sortable: true },
    { key: 'ultimaCompra', label: 'Última Compra', sortable: true },
  ];

  readonly gridData = computed(() => {
    return this.customers().map(c => ({
      ...c,
    }));
  });

  constructor(public router: Router) {}

  ngOnInit(): void {
    this.loadCustomers(true);
  }

  async loadCustomers(reset = false): Promise<void> {
    if (reset) {
      this.currentPage.set(1);
      this.customers.set([]);
      this.hasMore.set(true);
    }

    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.customerService.list({
        search: this.searchQuery() || undefined,
        sortBy: this.sortBy() ?? undefined,
        sortDir: this.sortDirection(),
        page: this.currentPage(),
        pageSize: this.pageSize(),
      }));

      if (reset) {
        this.customers.set(response.items as CustomerListItem[]);
      } else {
        this.customers.update(prev => [...prev, ...response.items as CustomerListItem[]]);
      }

      const totalLoaded = this.customers().length;
      this.hasMore.set(totalLoaded < response.totalCount);
      this.hasData.set(totalLoaded > 0 || this.searchQuery().length > 0);
    } catch {
      if (reset) {
        this.customers.set([]);
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

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.loadCustomers(true);
    this.gridRef?.scrollToTop();
  }

  onSort(event: GridSortEvent): void {
    this.sortBy.set(event.direction ? event.column : null);
    this.sortDirection.set(event.direction ?? 'asc');
    this.loadCustomers(true);
    this.gridRef?.scrollToTop();
  }

  onLoadMore(): void {
    this.currentPage.update(p => p + 1);
    this.loadCustomers(false);
  }

  onRowClick(row: Record<string, any>): void {
    this.router.navigate(['/clientes', row['id']]);
  }
}
