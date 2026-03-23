import { Component, signal, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, ShoppingCart, Eye } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { OrderService, type OrderListItem } from '../../services/order.service';
import { firstValueFrom } from 'rxjs';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';

@Component({
  selector: 'app-sales-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, DataTableComponent],
  templateUrl: './sales-list.component.html',
  styleUrl: './sales-list.component.scss',
})
export class SalesListComponent {
  readonly searchIcon = Search;
  readonly cartIcon = ShoppingCart;
  readonly eyeIcon = Eye;

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | OrderStatus>('Todos');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly filteredOrders = signal<OrderListItem[]>([]);

  private readonly orderService = inject(OrderService);

  constructor(public router: Router) {
    effect(() => {
      // Track all filter signals to trigger reload
      const search = this.searchQuery();
      const status = this.statusFilter();
      const from = this.dateFrom();
      const to = this.dateTo();
      this.loadOrders(search, status, from, to);
    });
  }

  private async loadOrders(search: string, status: string, dateFrom: string, dateTo: string): Promise<void> {
    this.loading.set(true);
    try {
      const response = await firstValueFrom(this.orderService.list({
        search: search || undefined,
        status: status !== 'Todos' ? status : undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
      }));
      this.filteredOrders.set(response.items as OrderListItem[]);
      this.hasData.set(response.totalCount > 0 || !!search || status !== 'Todos' || !!dateFrom || !!dateTo);
    } catch {
      this.filteredOrders.set([]);
      this.hasData.set(false);
    } finally {
      this.loading.set(false);
    }
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr + 'T12:00:00');
    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Pago': return 'primary';
      case 'Enviado': return 'warning';
      case 'Entregue': return 'success';
      case 'Cancelado': return 'danger';
      case 'Devolvido': return 'neutral';
      default: return 'neutral';
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  onStatusChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value as 'Todos' | OrderStatus);
  }

  onDateFromChange(event: Event): void {
    this.dateFrom.set((event.target as HTMLInputElement).value);
  }

  onDateToChange(event: Event): void {
    this.dateTo.set((event.target as HTMLInputElement).value);
  }

  onRowClick(order: OrderListItem): void {
    this.router.navigate(['/vendas', order.id]);
  }
}
