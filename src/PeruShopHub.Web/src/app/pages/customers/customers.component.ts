import { Component, signal, computed, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, X, ShoppingBag, Mail, Phone, User } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import { RelativeDatePipe } from '../../shared/pipes/relative-date.pipe';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';

interface CustomerOrder {
  id: string;
  data: string;
  valor: number;
  status: OrderStatus;
}

interface Customer {
  id: string;
  nome: string;
  nickname: string;
  email: string;
  totalPedidos: number;
  totalGasto: number;
  ultimaCompra: string;
  orders: CustomerOrder[];
}

const MOCK_CUSTOMERS: Customer[] = [
  {
    id: 'C001', nome: 'Maria Silva Santos', nickname: 'MARI.SILVA', email: 'maria.s***@gmail.com',
    totalPedidos: 12, totalGasto: 4589.80, ultimaCompra: '2026-03-22T10:30:00',
    orders: [
      { id: '2087654321', data: '2026-03-22', valor: 389.80, status: 'Entregue' },
      { id: '2087654290', data: '2026-03-15', valor: 259.90, status: 'Entregue' },
      { id: '2087654245', data: '2026-03-01', valor: 479.70, status: 'Entregue' },
      { id: '2087654200', data: '2026-02-18', valor: 159.90, status: 'Entregue' },
      { id: '2087654150', data: '2026-02-05', valor: 329.90, status: 'Entregue' },
    ]
  },
  {
    id: 'C002', nome: 'João Pedro Oliveira', nickname: 'JOAOPEDRO_OLI', email: 'joao.p***@hotmail.com',
    totalPedidos: 8, totalGasto: 3210.50, ultimaCompra: '2026-03-21T14:20:00',
    orders: [
      { id: '2087654320', data: '2026-03-21', valor: 259.90, status: 'Enviado' },
      { id: '2087654280', data: '2026-03-10', valor: 449.90, status: 'Entregue' },
      { id: '2087654230', data: '2026-02-25', valor: 189.90, status: 'Entregue' },
      { id: '2087654180', data: '2026-02-10', valor: 539.90, status: 'Entregue' },
      { id: '2087654120', data: '2026-01-28', valor: 299.90, status: 'Cancelado' },
    ]
  },
  {
    id: 'C003', nome: 'Ana Carolina Ferreira', nickname: 'ANA_CAROL_F', email: 'ana.c***@outlook.com',
    totalPedidos: 15, totalGasto: 6780.20, ultimaCompra: '2026-03-20T09:15:00',
    orders: [
      { id: '2087654315', data: '2026-03-20', valor: 719.60, status: 'Entregue' },
      { id: '2087654270', data: '2026-03-08', valor: 349.90, status: 'Entregue' },
      { id: '2087654220', data: '2026-02-22', valor: 199.90, status: 'Devolvido' },
      { id: '2087654170', data: '2026-02-08', valor: 589.90, status: 'Entregue' },
      { id: '2087654110', data: '2026-01-25', valor: 419.90, status: 'Entregue' },
    ]
  },
  {
    id: 'C004', nome: 'Carlos Eduardo Lima', nickname: 'CARLOSEDU_L', email: 'carlos.e***@gmail.com',
    totalPedidos: 3, totalGasto: 879.70, ultimaCompra: '2026-03-19T16:45:00',
    orders: [
      { id: '2087654318', data: '2026-03-19', valor: 129.90, status: 'Pago' },
      { id: '2087654260', data: '2026-03-05', valor: 449.90, status: 'Entregue' },
      { id: '2087654210', data: '2026-02-20', valor: 299.90, status: 'Entregue' },
      { id: '2087654160', data: '2026-02-06', valor: 0, status: 'Cancelado' },
      { id: '2087654100', data: '2026-01-22', valor: 0, status: 'Cancelado' },
    ]
  },
  {
    id: 'C005', nome: 'Fernanda Costa Souza', nickname: 'FECOSTA_SZ', email: 'fernanda.c***@yahoo.com',
    totalPedidos: 22, totalGasto: 9450.30, ultimaCompra: '2026-03-22T08:00:00',
    orders: [
      { id: '2087654317', data: '2026-03-22', valor: 239.80, status: 'Entregue' },
      { id: '2087654250', data: '2026-03-03', valor: 189.90, status: 'Entregue' },
      { id: '2087654195', data: '2026-02-18', valor: 659.90, status: 'Entregue' },
      { id: '2087654140', data: '2026-02-02', valor: 129.90, status: 'Entregue' },
      { id: '2087654090', data: '2026-01-20', valor: 819.90, status: 'Entregue' },
    ]
  },
  {
    id: 'C006', nome: 'Rafael Almeida Gomes', nickname: 'RAFA_GOMES', email: 'rafael.a***@gmail.com',
    totalPedidos: 6, totalGasto: 2340.60, ultimaCompra: '2026-03-18T11:30:00',
    orders: [
      { id: '2087654316', data: '2026-03-18', valor: 189.90, status: 'Entregue' },
      { id: '2087654240', data: '2026-02-28', valor: 349.90, status: 'Entregue' },
      { id: '2087654185', data: '2026-02-14', valor: 499.90, status: 'Entregue' },
      { id: '2087654130', data: '2026-01-30', valor: 269.90, status: 'Enviado' },
      { id: '2087654080', data: '2026-01-18', valor: 159.90, status: 'Entregue' },
    ]
  },
  {
    id: 'C007', nome: 'Juliana Ribeiro Martins', nickname: 'JU_RIBEIRO', email: 'juliana.r***@gmail.com',
    totalPedidos: 4, totalGasto: 1560.40, ultimaCompra: '2026-03-17T13:20:00',
    orders: [
      { id: '2087654313', data: '2026-03-17', valor: 349.80, status: 'Entregue' },
      { id: '2087654235', data: '2026-02-27', valor: 419.90, status: 'Entregue' },
      { id: '2087654175', data: '2026-02-12', valor: 299.90, status: 'Entregue' },
      { id: '2087654115', data: '2026-01-26', valor: 490.80, status: 'Devolvido' },
      { id: '2087654060', data: '2026-01-14', valor: 0, status: 'Cancelado' },
    ]
  },
  {
    id: 'C008', nome: 'Lucas Mendes Pereira', nickname: 'LUCAS_MP', email: 'lucas.m***@outlook.com',
    totalPedidos: 1, totalGasto: 49.90, ultimaCompra: '2026-03-16T15:50:00',
    orders: [
      { id: '2087654314', data: '2026-03-16', valor: 49.90, status: 'Devolvido' },
      { id: '2087654225', data: '2026-02-24', valor: 0, status: 'Cancelado' },
      { id: '2087654165', data: '2026-02-09', valor: 0, status: 'Cancelado' },
      { id: '2087654105', data: '2026-01-24', valor: 0, status: 'Cancelado' },
      { id: '2087654050', data: '2026-01-12', valor: 49.90, status: 'Devolvido' },
    ]
  },
  {
    id: 'C009', nome: 'Beatriz Rodrigues Nunes', nickname: 'BIANUNES_RN', email: 'beatriz.r***@gmail.com',
    totalPedidos: 9, totalGasto: 4120.70, ultimaCompra: '2026-03-15T10:10:00',
    orders: [
      { id: '2087654309', data: '2026-03-15', valor: 409.80, status: 'Entregue' },
      { id: '2087654255', data: '2026-03-04', valor: 289.90, status: 'Entregue' },
      { id: '2087654190', data: '2026-02-16', valor: 579.90, status: 'Entregue' },
      { id: '2087654135', data: '2026-02-01', valor: 349.90, status: 'Entregue' },
      { id: '2087654075', data: '2026-01-17', valor: 199.90, status: 'Entregue' },
    ]
  },
  {
    id: 'C010', nome: 'Thiago Nascimento Barbosa', nickname: 'THIAGO_NB', email: 'thiago.n***@hotmail.com',
    totalPedidos: 5, totalGasto: 1890.40, ultimaCompra: '2026-03-14T17:30:00',
    orders: [
      { id: '2087654307', data: '2026-03-14', valor: 339.80, status: 'Entregue' },
      { id: '2087654248', data: '2026-03-02', valor: 199.90, status: 'Pago' },
      { id: '2087654183', data: '2026-02-15', valor: 449.90, status: 'Entregue' },
      { id: '2087654125', data: '2026-01-29', valor: 289.90, status: 'Entregue' },
      { id: '2087654070', data: '2026-01-16', valor: 610.90, status: 'Entregue' },
    ]
  },
];

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, BadgeComponent, EmptyStateComponent, DataTableComponent, RelativeDatePipe],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss',
})
export class CustomersComponent {
  readonly searchIcon = Search;
  readonly closeIcon = X;
  readonly bagIcon = ShoppingBag;
  readonly mailIcon = Mail;
  readonly phoneIcon = Phone;
  readonly userIcon = User;

  readonly searchQuery = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly sortColumn = signal<'totalGasto' | 'totalPedidos' | 'ultimaCompra'>('totalGasto');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

  // Slide-over state
  readonly selectedCustomer = signal<Customer | null>(null);
  readonly slideOverOpen = signal(false);

  readonly filteredCustomers = computed(() => {
    let customers = [...MOCK_CUSTOMERS];
    const query = this.searchQuery().toLowerCase();

    if (query) {
      customers = customers.filter(
        c => c.nome.toLowerCase().includes(query) ||
             c.nickname.toLowerCase().includes(query) ||
             c.email.toLowerCase().includes(query)
      );
    }

    const col = this.sortColumn();
    const dir = this.sortDirection();
    customers.sort((a, b) => {
      let cmp = 0;
      if (col === 'totalGasto') cmp = a.totalGasto - b.totalGasto;
      else if (col === 'totalPedidos') cmp = a.totalPedidos - b.totalPedidos;
      else if (col === 'ultimaCompra') cmp = a.ultimaCompra.localeCompare(b.ultimaCompra);
      return dir === 'desc' ? -cmp : cmp;
    });

    return customers;
  });

  constructor(private elementRef: ElementRef) {
    setTimeout(() => this.loading.set(false), 600);
  }

  @HostListener('document:keydown.escape')
  onEscKey(): void {
    if (this.slideOverOpen()) {
      this.closeSlideOver();
    }
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

  onSort(column: 'totalGasto' | 'totalPedidos' | 'ultimaCompra'): void {
    if (this.sortColumn() === column) {
      this.sortDirection.set(this.sortDirection() === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortColumn.set(column);
      this.sortDirection.set('desc');
    }
  }

  getSortIndicator(column: string): string {
    if (this.sortColumn() !== column) return '';
    return this.sortDirection() === 'asc' ? ' ↑' : ' ↓';
  }

  onRowClick(customer: Customer): void {
    this.selectedCustomer.set(customer);
    this.slideOverOpen.set(true);
  }

  closeSlideOver(): void {
    this.slideOverOpen.set(false);
    setTimeout(() => this.selectedCustomer.set(null), 200);
  }

  onBackdropClick(): void {
    this.closeSlideOver();
  }
}
