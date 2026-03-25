import { Component, signal, computed, inject, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus } from 'lucide-angular';
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  GridColumn,
  GridSortEvent,
} from '../../shared/components/data-grid/data-grid.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PurchaseOrderService, type PurchaseOrderListItem } from '../../services/purchase-order.service';
import { firstValueFrom } from 'rxjs';

type POStatus = 'Rascunho' | 'Recebido' | 'Cancelado';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataGridComponent, GridCellDirective, GridCardDirective, BadgeComponent, BrlCurrencyPipe, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent],
  templateUrl: './purchase-orders-list.component.html',
  styleUrl: './purchase-orders-list.component.scss',
})
export class PurchaseOrdersListComponent implements OnInit {
  readonly plusIcon = Plus;

  @ViewChild('grid') gridRef!: DataGridComponent;

  readonly statusOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos os status' },
    { value: 'Rascunho', label: 'Rascunho' },
    { value: 'Recebido', label: 'Recebido' },
    { value: 'Cancelado', label: 'Cancelado' },
  ];

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | POStatus>('Todos');
  readonly loading = signal(true);
  readonly hasMore = signal(true);
  readonly totalCount = signal(0);
  readonly orders = signal<PurchaseOrderListItem[]>([]);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly sortBy = signal<string | null>(null);
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  private readonly poService = inject(PurchaseOrderService);

  readonly gridColumns: GridColumn[] = [
    { key: 'supplier', label: 'Fornecedor', sortable: true },
    { key: 'status', label: 'Status' },
    { key: 'itemCount', label: 'Itens', align: 'right' },
    { key: 'total', label: 'Total', align: 'right', sortable: true },
    { key: 'createdAt', label: 'Data Criação', sortable: true },
    { key: 'receivedAt', label: 'Recebido em' },
  ];

  readonly gridData = computed(() => {
    return this.orders().map(o => ({
      ...o,
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
      const response = await firstValueFrom(this.poService.list({
        supplier: this.searchQuery() || undefined,
        status: this.statusFilter() !== 'Todos' ? this.statusFilter() : undefined,
        page: this.currentPage(),
        pageSize: this.pageSize(),
      }));

      if (reset) {
        this.orders.set(response.items);
      } else {
        this.orders.update(prev => [...prev, ...response.items]);
      }

      const totalLoaded = this.orders().length;
      this.hasMore.set(totalLoaded < response.totalCount);
      this.totalCount.set(response.totalCount);
    } catch {
      if (reset) {
        this.orders.set([]);
      }
      this.hasMore.set(false);
    } finally {
      this.loading.set(false);
    }
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Rascunho': return 'neutral';
      case 'Recebido': return 'success';
      case 'Cancelado': return 'danger';
      default: return 'neutral';
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
    this.loadOrders(true);
    this.gridRef?.scrollToTop();
  }

  onStatusFilterChange(value: string): void {
    this.statusFilter.set(value as 'Todos' | POStatus);
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
    this.router.navigate(['/compras', row['id']]);
  }

  onNewOrder(): void {
    this.router.navigate(['/compras/novo']);
  }
}
