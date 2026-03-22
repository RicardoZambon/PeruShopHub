import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search } from 'lucide-angular';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import { RelativeDatePipe } from '../../shared/pipes/relative-date.pipe';

interface Customer {
  id: string;
  nome: string;
  nickname: string;
  email: string;
  totalPedidos: number;
  totalGasto: number;
  ultimaCompra: string;
}

const MOCK_CUSTOMERS: Customer[] = [
  {
    id: 'C001', nome: 'Maria Silva Santos', nickname: 'MARI.SILVA', email: 'maria.s***@gmail.com',
    totalPedidos: 12, totalGasto: 4589.80, ultimaCompra: '2026-03-22T10:30:00',
  },
  {
    id: 'C002', nome: 'João Pedro Oliveira', nickname: 'JOAOPEDRO_OLI', email: 'joao.p***@hotmail.com',
    totalPedidos: 8, totalGasto: 3210.50, ultimaCompra: '2026-03-21T14:20:00',
  },
  {
    id: 'C003', nome: 'Ana Carolina Ferreira', nickname: 'ANA_CAROL_F', email: 'ana.c***@outlook.com',
    totalPedidos: 15, totalGasto: 6780.20, ultimaCompra: '2026-03-20T09:15:00',
  },
  {
    id: 'C004', nome: 'Carlos Eduardo Lima', nickname: 'CARLOSEDU_L', email: 'carlos.e***@gmail.com',
    totalPedidos: 3, totalGasto: 879.70, ultimaCompra: '2026-03-19T16:45:00',
  },
  {
    id: 'C005', nome: 'Fernanda Costa Souza', nickname: 'FECOSTA_SZ', email: 'fernanda.c***@yahoo.com',
    totalPedidos: 22, totalGasto: 9450.30, ultimaCompra: '2026-03-22T08:00:00',
  },
  {
    id: 'C006', nome: 'Rafael Almeida Gomes', nickname: 'RAFA_GOMES', email: 'rafael.a***@gmail.com',
    totalPedidos: 6, totalGasto: 2340.60, ultimaCompra: '2026-03-18T11:30:00',
  },
  {
    id: 'C007', nome: 'Juliana Ribeiro Martins', nickname: 'JU_RIBEIRO', email: 'juliana.r***@gmail.com',
    totalPedidos: 4, totalGasto: 1560.40, ultimaCompra: '2026-03-17T13:20:00',
  },
  {
    id: 'C008', nome: 'Lucas Mendes Pereira', nickname: 'LUCAS_MP', email: 'lucas.m***@outlook.com',
    totalPedidos: 1, totalGasto: 49.90, ultimaCompra: '2026-03-16T15:50:00',
  },
  {
    id: 'C009', nome: 'Beatriz Rodrigues Nunes', nickname: 'BIANUNES_RN', email: 'beatriz.r***@gmail.com',
    totalPedidos: 9, totalGasto: 4120.70, ultimaCompra: '2026-03-15T10:10:00',
  },
  {
    id: 'C010', nome: 'Thiago Nascimento Barbosa', nickname: 'THIAGO_NB', email: 'thiago.n***@hotmail.com',
    totalPedidos: 5, totalGasto: 1890.40, ultimaCompra: '2026-03-14T17:30:00',
  },
];

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, EmptyStateComponent, DataTableComponent, RelativeDatePipe],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss',
})
export class CustomersComponent {
  readonly searchIcon = Search;

  readonly searchQuery = signal('');
  readonly loading = signal(true);
  readonly hasData = signal(true);
  readonly sortColumn = signal<'totalGasto' | 'totalPedidos' | 'ultimaCompra'>('totalGasto');
  readonly sortDirection = signal<'asc' | 'desc'>('desc');

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

  constructor(private router: Router) {
    setTimeout(() => this.loading.set(false), 600);
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
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
    return this.sortDirection() === 'asc' ? ' \u2191' : ' \u2193';
  }

  onRowClick(customer: Customer): void {
    this.router.navigate(['/clientes', customer.id]);
  }
}
