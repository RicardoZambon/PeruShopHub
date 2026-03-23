import { Component, signal, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search } from 'lucide-angular';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import { RelativeDatePipe } from '../../shared/pipes/relative-date.pipe';
import { CustomerService } from '../../services/customer.service';
import type { CustomerListItem } from '../../services/customer.service';

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
  readonly filteredCustomers = signal<CustomerListItem[]>([]);

  private readonly customerService = inject(CustomerService);

  constructor(private router: Router) {
    effect(() => {
      const search = this.searchQuery();
      const sortCol = this.sortColumn();
      const sortDir = this.sortDirection();
      this.loadCustomers(search, sortCol, sortDir);
    });
  }

  private async loadCustomers(search: string, sortBy: string, sortDirection: 'asc' | 'desc'): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.customerService.list({
        search: search || undefined,
        sortBy,
        sortDirection,
      });
      this.filteredCustomers.set(response.items);
      this.hasData.set(response.totalCount > 0 || !!search);
    } catch {
      this.filteredCustomers.set([]);
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

  onRowClick(customer: CustomerListItem): void {
    this.router.navigate(['/clientes', customer.id]);
  }
}
