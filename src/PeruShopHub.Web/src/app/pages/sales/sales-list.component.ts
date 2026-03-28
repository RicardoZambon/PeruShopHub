import { Component, signal, computed, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, ShoppingCart, Eye } from 'lucide-angular';
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
import { ButtonComponent } from '../../shared/components/button/button.component';
import { formatBrl as formatBrlUtil, formatDateShort, getOrderStatusVariant } from '../../shared/utils';
import { OrderService, type OrderListItem } from '../../services/order.service';
import { FinanceService } from '../../services/finance.service';
import { ToastService } from '../../services/toast.service';
import { firstValueFrom } from 'rxjs';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';

@Component({
  selector: 'app-sales-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataGridComponent, GridCellDirective, GridCardDirective, BadgeComponent, EmptyStateComponent, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent, ButtonComponent],
  templateUrl: './sales-list.component.html',
  styleUrl: './sales-list.component.scss',
})
export class SalesListComponent implements OnInit {
  private readonly orderService = inject(OrderService);
  private readonly financeService = inject(FinanceService);
  private readonly toastService = inject(ToastService);

  readonly searchIcon = Search;
  readonly cartIcon = ShoppingCart;
  readonly eyeIcon = Eye;

  @ViewChild('grid') gridRef!: DataGridComponent;

  readonly statusOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos' },
    { value: 'Pago', label: 'Pago' },
    { value: 'Enviado', label: 'Enviado' },
    { value: 'Entregue', label: 'Entregue' },
    { value: 'Cancelado', label: 'Cancelado' },
    { value: 'Devolvido', label: 'Devolvido' },
  ];

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | OrderStatus>('Todos');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly hasMore = signal(true);
  readonly totalCount = signal(0);

  readonly orders = signal<OrderListItem[]>([]);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('asc');

  readonly gridColumns: GridColumn[] = [
    { key: 'id', label: 'ID', width: '100px' },
    { key: 'date', label: 'Data', sortable: true },
    { key: 'buyer', label: 'Comprador', sortable: true },
    { key: 'itemCount', label: 'Itens', align: 'center' },
    { key: 'total', label: 'Valor', align: 'right', sortable: true },
    { key: 'profit', label: 'Lucro', align: 'right', sortable: true },
    { key: 'status', label: 'Status' },
  ];

  readonly gridData = computed(() => {
    return this.orders().map(o => ({
      ...o,
      id: o.id,
      date: o.date,
      buyer: o.buyer,
      itemCount: o.itemCount,
      total: o.total,
      profit: o.profit,
      status: o.status,
    }));
  });

  constructor(public router: Router) {}

  ngOnInit(): void {
    this.loadOrders(true);
  }

  async loadOrders(reset = false): Promise<void> {
    if (reset) {
      this.currentPage.set(1);
      this.orders.set([]);
      this.hasMore.set(true);
    }

    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.orderService.list({
        search: this.searchQuery() || undefined,
        status: this.statusFilter() !== 'Todos' ? this.statusFilter() : undefined,
        dateFrom: this.dateFrom() || undefined,
        dateTo: this.dateTo() || undefined,
        page: this.currentPage(),
        pageSize: this.pageSize(),
      }));

      if (reset) {
        this.orders.set(response.items as OrderListItem[]);
      } else {
        this.orders.update(prev => [...prev, ...response.items as OrderListItem[]]);
      }

      const totalLoaded = this.orders().length;
      this.hasMore.set(totalLoaded < response.totalCount);
      this.totalCount.set(response.totalCount);
      this.hasData.set(totalLoaded > 0 || this.searchQuery().length > 0 || this.statusFilter() !== 'Todos' || !!this.dateFrom() || !!this.dateTo());
    } catch {
      if (reset) {
        this.orders.set([]);
      }
      this.hasMore.set(false);
      this.hasData.set(false);
    } finally {
      this.loading.set(false);
    }
  }

  formatBrl = formatBrlUtil;

  formatDate = formatDateShort;

  getStatusVariant = getOrderStatusVariant;

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter.set(value as 'Todos' | OrderStatus);
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onDateFromChange(event: Event): void {
    this.dateFrom.set((event.target as HTMLInputElement).value);
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onDateToChange(event: Event): void {
    this.dateTo.set((event.target as HTMLInputElement).value);
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onSort(event: GridSortEvent): void {
    this.sortBy.set(event.direction ? event.column : null);
    this.sortDirection.set(event.direction ?? 'asc');
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onLoadMore(): void {
    this.currentPage.update(p => p + 1);
    this.loadOrders(false);
  }

  onRowClick(row: Record<string, any>): void {
    this.router.navigate(['/vendas', row['id']]);
  }

  onExportPdf(): void {
    const dateFrom = this.dateFrom() || undefined;
    const dateTo = this.dateTo() || undefined;

    this.financeService.exportOrdersPdf(dateFrom, dateTo).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `vendas_${new Date().toISOString().split('T')[0]}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.toastService.show('Erro ao gerar PDF', 'danger');
      },
    });
  }
}
