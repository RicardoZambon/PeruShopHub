import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, ShoppingCart, Eye } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';

interface MockOrder {
  id: string;
  data: string;
  comprador: string;
  itens: number;
  valor: number;
  lucro: number;
  status: OrderStatus;
}

const MOCK_ORDERS: MockOrder[] = [
  { id: '2087654321', data: '2026-03-22', comprador: 'Maria Silva Santos', itens: 2, valor: 389.80, lucro: 78.50, status: 'Entregue' },
  { id: '2087654320', data: '2026-03-22', comprador: 'João Pedro Oliveira', itens: 1, valor: 259.90, lucro: 48.60, status: 'Enviado' },
  { id: '2087654319', data: '2026-03-21', comprador: 'Ana Carolina Ferreira', itens: 3, valor: 479.70, lucro: -12.30, status: 'Cancelado' },
  { id: '2087654318', data: '2026-03-21', comprador: 'Carlos Eduardo Lima', itens: 1, valor: 129.90, lucro: 36.20, status: 'Pago' },
  { id: '2087654317', data: '2026-03-20', comprador: 'Fernanda Costa Souza', itens: 2, valor: 239.80, lucro: 52.10, status: 'Entregue' },
  { id: '2087654316', data: '2026-03-20', comprador: 'Rafael Almeida Gomes', itens: 1, valor: 189.90, lucro: 61.50, status: 'Entregue' },
  { id: '2087654315', data: '2026-03-19', comprador: 'Juliana Ribeiro Martins', itens: 4, valor: 719.60, lucro: 142.80, status: 'Enviado' },
  { id: '2087654314', data: '2026-03-19', comprador: 'Lucas Mendes Pereira', itens: 1, valor: 49.90, lucro: 8.30, status: 'Devolvido' },
  { id: '2087654313', data: '2026-03-18', comprador: 'Beatriz Rodrigues Nunes', itens: 2, valor: 349.80, lucro: 87.40, status: 'Entregue' },
  { id: '2087654312', data: '2026-03-18', comprador: 'Thiago Nascimento Barbosa', itens: 1, valor: 299.90, lucro: 55.70, status: 'Pago' },
  { id: '2087654311', data: '2026-03-17', comprador: 'Camila Araújo Cardoso', itens: 3, valor: 569.70, lucro: 98.40, status: 'Entregue' },
  { id: '2087654310', data: '2026-03-17', comprador: 'Diego Moreira Teixeira', itens: 1, valor: 159.90, lucro: -5.20, status: 'Cancelado' },
  { id: '2087654309', data: '2026-03-16', comprador: 'Larissa Freitas Carvalho', itens: 2, valor: 409.80, lucro: 76.90, status: 'Entregue' },
  { id: '2087654308', data: '2026-03-16', comprador: 'Gustavo Pinto Correia', itens: 1, valor: 219.90, lucro: 41.20, status: 'Enviado' },
  { id: '2087654307', data: '2026-03-15', comprador: 'Patricia Lopes Vieira', itens: 2, valor: 339.80, lucro: 63.50, status: 'Entregue' },
];

@Component({
  selector: 'app-vendas-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, DataTableComponent],
  templateUrl: './vendas-list.component.html',
  styleUrl: './vendas-list.component.scss',
})
export class VendasListComponent {
  readonly searchIcon = Search;
  readonly cartIcon = ShoppingCart;
  readonly eyeIcon = Eye;

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | OrderStatus>('Todos');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);

  readonly filteredOrders = computed(() => {
    let orders = [...MOCK_ORDERS];
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();
    const from = this.dateFrom();
    const to = this.dateTo();

    if (query) {
      orders = orders.filter(
        o => o.id.includes(query) || o.comprador.toLowerCase().includes(query)
      );
    }

    if (status !== 'Todos') {
      orders = orders.filter(o => o.status === status);
    }

    if (from) {
      orders = orders.filter(o => o.data >= from);
    }

    if (to) {
      orders = orders.filter(o => o.data <= to);
    }

    return orders;
  });

  constructor(public router: Router) {
    setTimeout(() => this.loading.set(false), 600);
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr + 'T12:00:00');
    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getStatusVariant(status: OrderStatus): BadgeVariant {
    switch (status) {
      case 'Pago': return 'primary';
      case 'Enviado': return 'warning';
      case 'Entregue': return 'success';
      case 'Cancelado': return 'danger';
      case 'Devolvido': return 'neutral';
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

  onRowClick(order: MockOrder): void {
    this.router.navigate(['/vendas', order.id]);
  }
}
