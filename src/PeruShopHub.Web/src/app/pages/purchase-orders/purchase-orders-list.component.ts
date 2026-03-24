import { Component, signal, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, Plus, ShoppingCart } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { SearchInputComponent } from '../../shared/components/search-input/search-input.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { PageSkeletonComponent } from '../../shared/components/page-skeleton/page-skeleton.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PurchaseOrderService, type PurchaseOrderListItem } from '../../services/purchase-order.service';
import { firstValueFrom } from 'rxjs';

type POStatus = 'Rascunho' | 'Recebido' | 'Cancelado';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, BrlCurrencyPipe, PageHeaderComponent, SearchInputComponent, SelectDropdownComponent, PageSkeletonComponent],
  templateUrl: './purchase-orders-list.component.html',
  styleUrl: './purchase-orders-list.component.scss',
})
export class PurchaseOrdersListComponent {
  readonly searchIcon = Search;
  readonly plusIcon = Plus;
  readonly cartIcon = ShoppingCart;

  readonly statusOptions: SelectOption[] = [
    { value: 'Todos', label: 'Todos os status' },
    { value: 'Rascunho', label: 'Rascunho' },
    { value: 'Recebido', label: 'Recebido' },
    { value: 'Cancelado', label: 'Cancelado' },
  ];

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | POStatus>('Todos');
  readonly loading = signal(true);
  readonly orders = signal<PurchaseOrderListItem[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  private readonly poService = inject(PurchaseOrderService);

  constructor(public router: Router) {
    effect(() => {
      const search = this.searchQuery();
      const status = this.statusFilter();
      const page = this.currentPage();
      const size = this.pageSize();
      this.loadOrders(search, status, page, size);
    });
  }

  private async loadOrders(search: string, status: string, page: number, pageSize: number): Promise<void> {
    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.poService.list({
        supplier: search || undefined,
        status: status !== 'Todos' ? status : undefined,
        page,
        pageSize,
      }));
      this.orders.set(response.items);
      this.totalCount.set(response.totalCount);
    } catch {
      this.orders.set([]);
      this.totalCount.set(0);
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
    this.currentPage.set(1);
    this.searchQuery.set(value);
  }

  onStatusChange(event: Event): void {
    this.currentPage.set(1);
    this.statusFilter.set((event.target as HTMLSelectElement).value as 'Todos' | POStatus);
  }

  onStatusFilterChange(value: string): void {
    this.currentPage.set(1);
    this.statusFilter.set(value as 'Todos' | POStatus);
  }

  onRowClick(order: PurchaseOrderListItem): void {
    this.router.navigate(['/compras', order.id]);
  }

  onNewOrder(): void {
    this.router.navigate(['/compras/novo']);
  }
}
