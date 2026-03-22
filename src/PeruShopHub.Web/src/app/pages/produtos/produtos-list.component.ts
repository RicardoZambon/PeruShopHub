import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Plus, Search, Edit, Package } from 'lucide-angular';
import { DataTableComponent } from '../../shared/components/data-table/data-table.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import type { DataTableColumn, SortEvent, PageEvent } from '../../shared/components/data-table/data-table.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type ProductStatus = 'Ativo' | 'Pausado' | 'Encerrado';

interface MockProduct {
  id: number;
  foto: string;
  nome: string;
  sku: string;
  preco: number;
  estoque: number;
  status: ProductStatus;
  margem: number;
}

const MOCK_PRODUCTS: MockProduct[] = [
  { id: 1, foto: '', nome: 'Fone Bluetooth TWS Pro Max', sku: 'FN-BT-001', preco: 189.90, estoque: 45, status: 'Ativo', margem: 32.5 },
  { id: 2, foto: '', nome: 'Capinha iPhone 15 Silicone Premium', sku: 'CP-IP15-002', preco: 49.90, estoque: 230, status: 'Ativo', margem: 45.2 },
  { id: 3, foto: '', nome: 'Carregador USB-C 65W GaN', sku: 'CR-USC-003', preco: 129.90, estoque: 78, status: 'Ativo', margem: 28.1 },
  { id: 4, foto: '', nome: 'Smartwatch Fitness Band Pro', sku: 'SW-FIT-004', preco: 259.90, estoque: 12, status: 'Ativo', margem: 18.7 },
  { id: 5, foto: '', nome: 'Cabo HDMI 2.1 4K 2m', sku: 'CB-HDMI-005', preco: 39.90, estoque: 0, status: 'Pausado', margem: 8.3 },
  { id: 6, foto: '', nome: 'Mouse Gamer RGB 12000dpi', sku: 'MS-GM-006', preco: 149.90, estoque: 56, status: 'Ativo', margem: 25.4 },
  { id: 7, foto: '', nome: 'Teclado Mecânico Compacto 60%', sku: 'TC-MEC-007', preco: 299.90, estoque: 8, status: 'Ativo', margem: 22.0 },
  { id: 8, foto: '', nome: 'Suporte Notebook Alumínio Ajustável', sku: 'SP-NB-008', preco: 89.90, estoque: 34, status: 'Ativo', margem: 15.6 },
  { id: 9, foto: '', nome: 'Webcam Full HD 1080p Autofoco', sku: 'WC-FHD-009', preco: 199.90, estoque: 0, status: 'Encerrado', margem: 5.2 },
  { id: 10, foto: '', nome: 'Hub USB-C 7 em 1 Dock Station', sku: 'HB-USC-010', preco: 219.90, estoque: 22, status: 'Ativo', margem: 31.8 },
];

@Component({
  selector: 'app-produtos-list',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, DataTableComponent, BadgeComponent, EmptyStateComponent],
  templateUrl: './produtos-list.component.html',
  styleUrl: './produtos-list.component.scss',
})
export class ProdutosListComponent {
  readonly plusIcon = Plus;
  readonly searchIcon = Search;
  readonly editIcon = Edit;
  readonly packageIcon = Package;

  readonly searchQuery = signal('');
  readonly statusFilter = signal<'Todos' | ProductStatus>('Todos');
  readonly loading = signal(true);
  readonly hasData = signal(true);

  readonly columns: DataTableColumn[] = [
    { key: 'foto', label: 'Foto', align: 'center' },
    { key: 'nome', label: 'Nome', sortable: true },
    { key: 'sku', label: 'SKU', format: 'mono' },
    { key: 'preco', label: 'Preço', align: 'right', format: 'currency', sortable: true },
    { key: 'estoque', label: 'Estoque', align: 'right', sortable: true },
    { key: 'status', label: 'Status' },
    { key: 'margem', label: 'Margem', align: 'right', sortable: true },
    { key: 'acoes', label: 'Ações', align: 'center' },
  ];

  readonly filteredProducts = computed(() => {
    let products = [...MOCK_PRODUCTS];
    const query = this.searchQuery().toLowerCase();
    const status = this.statusFilter();

    if (query) {
      products = products.filter(
        p => p.nome.toLowerCase().includes(query) || p.sku.toLowerCase().includes(query)
      );
    }

    if (status !== 'Todos') {
      products = products.filter(p => p.status === status);
    }

    return products;
  });

  readonly tableData = computed(() => {
    return this.filteredProducts().map(p => ({
      ...p,
      precoFormatted: this.formatBrl(p.preco),
      margemFormatted: `${p.margem.toFixed(1)}%`,
    }));
  });

  constructor(public router: Router) {
    setTimeout(() => this.loading.set(false), 600);
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  getMarginClass(margin: number): string {
    if (margin >= 20) return 'margin--success';
    if (margin >= 10) return 'margin--warning';
    return 'margin--danger';
  }

  getStatusVariant(status: ProductStatus): BadgeVariant {
    switch (status) {
      case 'Ativo': return 'success';
      case 'Pausado': return 'warning';
      case 'Encerrado': return 'danger';
    }
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  onStatusChange(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value as 'Todos' | ProductStatus);
  }

  onRowClick(row: Record<string, any>): void {
    this.router.navigate(['/produtos', row['id']]);
  }

  onNewProduct(): void {
    this.router.navigate(['/produtos/novo']);
  }

  onSort(event: SortEvent): void {
    // Mock: client-side sort for demo
    console.log('Sort:', event);
  }

  onPageChange(event: PageEvent): void {
    console.log('Page:', event);
  }
}
