import { Component, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { BrlCurrencyPipe } from '../../shared/pipes';

type InventoryTab = 'visao-geral' | 'movimentacoes' | 'estoque-full';
type MovementType = 'Entrada' | 'Saída' | 'Ajuste';

interface InventoryRow {
  sku: string;
  produto: string;
  estoqueTotal: number;
  reservado: number;
  disponivel: number;
  syncStatus: 'synced' | 'pending' | 'error';
  ultimaSync: string;
}

interface MovementRow {
  data: string;
  sku: string;
  produto: string;
  tipo: MovementType;
  quantidade: number;
  custoUnitario?: number;
  motivo: string;
  usuario: string;
}

interface ProductOption {
  sku: string;
  nome: string;
}

const INITIAL_INVENTORY: InventoryRow[] = [
  { sku: 'PSH-001', produto: 'Fone Bluetooth TWS Pro', estoqueTotal: 145, reservado: 12, disponivel: 133, syncStatus: 'synced', ultimaSync: '2026-03-22 14:32' },
  { sku: 'PSH-002', produto: 'Capa iPhone 15 Silicone', estoqueTotal: 320, reservado: 28, disponivel: 292, syncStatus: 'synced', ultimaSync: '2026-03-22 14:30' },
  { sku: 'PSH-003', produto: 'Carregador USB-C 65W', estoqueTotal: 67, reservado: 5, disponivel: 62, syncStatus: 'pending', ultimaSync: '2026-03-22 13:15' },
  { sku: 'PSH-004', produto: 'Suporte Notebook Alumínio', estoqueTotal: 8, reservado: 3, disponivel: 5, syncStatus: 'synced', ultimaSync: '2026-03-22 14:28' },
  { sku: 'PSH-005', produto: 'Película Galaxy S24 Ultra', estoqueTotal: 450, reservado: 35, disponivel: 415, syncStatus: 'synced', ultimaSync: '2026-03-22 14:31' },
  { sku: 'PSH-006', produto: 'Hub USB-C 7 em 1', estoqueTotal: 3, reservado: 2, disponivel: 1, syncStatus: 'error', ultimaSync: '2026-03-22 10:05' },
  { sku: 'PSH-007', produto: 'Mouse Gamer RGB 12000DPI', estoqueTotal: 22, reservado: 4, disponivel: 18, syncStatus: 'synced', ultimaSync: '2026-03-22 14:29' },
  { sku: 'PSH-008', produto: 'Cabo HDMI 2.1 3m', estoqueTotal: 0, reservado: 0, disponivel: 0, syncStatus: 'synced', ultimaSync: '2026-03-22 14:20' },
  { sku: 'PSH-009', produto: 'Teclado Mecânico Compact', estoqueTotal: 5, reservado: 5, disponivel: 0, syncStatus: 'pending', ultimaSync: '2026-03-22 12:40' },
  { sku: 'PSH-010', produto: 'Webcam Full HD 1080p', estoqueTotal: 89, reservado: 7, disponivel: 82, syncStatus: 'synced', ultimaSync: '2026-03-22 14:33' },
];

const INITIAL_MOVEMENTS: MovementRow[] = [
  { data: '2026-03-22 14:10', sku: 'PSH-001', produto: 'Fone Bluetooth TWS Pro', tipo: 'Saída', quantidade: -2, motivo: 'Venda #MLB-48291', usuario: 'Sistema' },
  { data: '2026-03-22 13:45', sku: 'PSH-005', produto: 'Película Galaxy S24 Ultra', tipo: 'Saída', quantidade: -5, motivo: 'Venda #MLB-48290', usuario: 'Sistema' },
  { data: '2026-03-22 12:30', sku: 'PSH-002', produto: 'Capa iPhone 15 Silicone', tipo: 'Entrada', quantidade: 100, custoUnitario: 12.50, motivo: 'Reposição NF 4521', usuario: 'Carlos Silva' },
  { data: '2026-03-22 11:20', sku: 'PSH-003', produto: 'Carregador USB-C 65W', tipo: 'Saída', quantidade: -1, motivo: 'Venda #MLB-48289', usuario: 'Sistema' },
  { data: '2026-03-22 10:55', sku: 'PSH-007', produto: 'Mouse Gamer RGB 12000DPI', tipo: 'Ajuste', quantidade: -3, motivo: 'Inventário — avaria', usuario: 'Ana Costa' },
  { data: '2026-03-22 09:30', sku: 'PSH-006', produto: 'Hub USB-C 7 em 1', tipo: 'Saída', quantidade: -1, motivo: 'Venda #MLB-48288', usuario: 'Sistema' },
  { data: '2026-03-21 17:40', sku: 'PSH-004', produto: 'Suporte Notebook Alumínio', tipo: 'Entrada', quantidade: 10, custoUnitario: 42.00, motivo: 'Reposição NF 4520', usuario: 'Carlos Silva' },
  { data: '2026-03-21 16:15', sku: 'PSH-001', produto: 'Fone Bluetooth TWS Pro', tipo: 'Saída', quantidade: -3, motivo: 'Venda #MLB-48287', usuario: 'Sistema' },
  { data: '2026-03-21 15:00', sku: 'PSH-008', produto: 'Cabo HDMI 2.1 3m', tipo: 'Saída', quantidade: -8, motivo: 'Venda #MLB-48286', usuario: 'Sistema' },
  { data: '2026-03-21 14:20', sku: 'PSH-009', produto: 'Teclado Mecânico Compact', tipo: 'Ajuste', quantidade: 2, motivo: 'Inventário — correção contagem', usuario: 'Ana Costa' },
  { data: '2026-03-21 12:10', sku: 'PSH-010', produto: 'Webcam Full HD 1080p', tipo: 'Entrada', quantidade: 50, custoUnitario: 85.00, motivo: 'Reposição NF 4519', usuario: 'Carlos Silva' },
  { data: '2026-03-21 10:30', sku: 'PSH-005', produto: 'Película Galaxy S24 Ultra', tipo: 'Saída', quantidade: -12, motivo: 'Venda #MLB-48285', usuario: 'Sistema' },
  { data: '2026-03-20 18:00', sku: 'PSH-002', produto: 'Capa iPhone 15 Silicone', tipo: 'Saída', quantidade: -7, motivo: 'Venda #MLB-48284', usuario: 'Sistema' },
  { data: '2026-03-20 15:45', sku: 'PSH-003', produto: 'Carregador USB-C 65W', tipo: 'Entrada', quantidade: 30, custoUnitario: 35.90, motivo: 'Reposição NF 4518', usuario: 'Carlos Silva' },
  { data: '2026-03-20 11:20', sku: 'PSH-007', produto: 'Mouse Gamer RGB 12000DPI', tipo: 'Saída', quantidade: -2, motivo: 'Venda #MLB-48283', usuario: 'Sistema' },
];

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, KpiCardComponent, SkeletonComponent, BadgeComponent, BrlCurrencyPipe],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent {
  activeTab = signal<InventoryTab>('visao-geral');
  loading = signal(true);
  movementTypeFilter = signal<MovementType | 'all'>('all');

  // Mutable inventory and movements data
  inventoryData = signal<InventoryRow[]>([...INITIAL_INVENTORY]);
  movements = signal<MovementRow[]>([...INITIAL_MOVEMENTS]);

  // Modal state
  entryModalOpen = signal(false);
  entryForm: FormGroup;
  productSearch = signal('');
  showProductDropdown = signal(false);

  // Product options for dropdown
  readonly productOptions: ProductOption[] = INITIAL_INVENTORY.map(r => ({ sku: r.sku, nome: r.produto }));

  filteredProducts = computed(() => {
    const query = this.productSearch().toLowerCase();
    if (!query) return this.productOptions;
    return this.productOptions.filter(p =>
      p.nome.toLowerCase().includes(query) || p.sku.toLowerCase().includes(query)
    );
  });

  tabs: { key: InventoryTab; label: string; disabled?: boolean }[] = [
    { key: 'visao-geral', label: 'Visão Geral' },
    { key: 'movimentacoes', label: 'Movimentações' },
    { key: 'estoque-full', label: 'Estoque Full', disabled: true },
  ];

  kpis = computed(() => {
    const data = this.inventoryData();
    const totalSkus = data.length;
    const totalUnits = data.reduce((s, r) => s + r.estoqueTotal, 0);
    const critical = data.filter(r => r.disponivel <= 5).length;
    const stockValue = data.reduce((s, r) => s + r.estoqueTotal * (45 + Math.random() * 80), 0);
    return [
      { label: 'Total SKUs', value: `${totalSkus}`, change: 2.0, changeLabel: 'vs mês anterior' },
      { label: 'Unidades em Estoque', value: totalUnits.toLocaleString('pt-BR'), change: 8.5, changeLabel: 'vs mês anterior' },
      { label: 'Itens Críticos', value: `${critical}`, change: 25.0, changeLabel: 'vs mês anterior', invertColors: true },
      { label: 'Valor em Estoque', value: `R$ ${Math.round(stockValue).toLocaleString('pt-BR')}`, change: 5.3, changeLabel: 'vs mês anterior' },
    ];
  });

  filteredMovements = computed(() => {
    const filter = this.movementTypeFilter();
    const all = this.movements();
    if (filter === 'all') return all;
    return all.filter(m => m.tipo === filter);
  });

  // Recent movements for the overview tab (latest 5)
  recentMovements = computed(() => {
    return this.movements().slice(0, 5);
  });

  constructor(private fb: FormBuilder) {
    this.entryForm = this.fb.group({
      produto: ['', Validators.required],
      produtoSku: [''],
      quantidade: [null, [Validators.required, Validators.min(1)]],
      custoUnitario: [null],
      notaFiscal: [''],
      observacao: [''],
    });

    setTimeout(() => this.loading.set(false), 600);
  }

  @HostListener('document:keydown.escape')
  onEscKey(): void {
    if (this.entryModalOpen()) {
      this.closeEntryModal();
    }
  }

  selectTab(tab: InventoryTab): void {
    const tabDef = this.tabs.find(t => t.key === tab);
    if (tabDef?.disabled) return;
    this.activeTab.set(tab);
  }

  // Entry modal methods
  openEntryModal(): void {
    this.entryForm.reset();
    this.productSearch.set('');
    this.showProductDropdown.set(false);
    this.entryModalOpen.set(true);
  }

  closeEntryModal(): void {
    this.entryModalOpen.set(false);
  }

  onModalBackdropClick(): void {
    this.closeEntryModal();
  }

  onProductSearchInput(value: string): void {
    this.productSearch.set(value);
    this.showProductDropdown.set(true);
  }

  selectProduct(product: ProductOption): void {
    this.productSearch.set(`${product.nome} (${product.sku})`);
    this.entryForm.patchValue({ produto: product.nome, produtoSku: product.sku });
    this.showProductDropdown.set(false);
  }

  onProductInputFocus(): void {
    this.showProductDropdown.set(true);
  }

  onProductInputBlur(): void {
    // Delay to allow click on dropdown item
    setTimeout(() => this.showProductDropdown.set(false), 200);
  }

  saveEntry(): void {
    if (this.entryForm.invalid) {
      this.entryForm.markAllAsTouched();
      return;
    }

    const val = this.entryForm.value;
    const sku = val.produtoSku;
    const produto = val.produto;
    const quantidade = val.quantidade;
    const custoUnitario = val.custoUnitario || undefined;
    const notaFiscal = val.notaFiscal || '';
    const observacao = val.observacao || '';

    const now = new Date();
    const dateStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;

    const motivo = notaFiscal ? `Reposição NF ${notaFiscal}` : (observacao || 'Entrada manual');

    // Add movement to top of list
    const newMovement: MovementRow = {
      data: dateStr,
      sku,
      produto,
      tipo: 'Entrada',
      quantidade,
      custoUnitario,
      motivo,
      usuario: 'Operador',
    };
    this.movements.update(m => [newMovement, ...m]);

    // Update inventory stock
    this.inventoryData.update(rows => {
      return rows.map(row => {
        if (row.sku === sku) {
          return {
            ...row,
            estoqueTotal: row.estoqueTotal + quantidade,
            disponivel: row.disponivel + quantidade,
          };
        }
        return row;
      });
    });

    this.closeEntryModal();
  }

  getSyncStatusLabel(status: string): string {
    switch (status) {
      case 'synced': return 'Sincronizado';
      case 'pending': return 'Pendente';
      case 'error': return 'Erro';
      default: return status;
    }
  }

  getSyncStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'synced': return 'success';
      case 'pending': return 'warning';
      case 'error': return 'danger';
      default: return 'neutral';
    }
  }

  getMovementTypeVariant(tipo: MovementType): BadgeVariant {
    switch (tipo) {
      case 'Entrada': return 'success';
      case 'Saída': return 'danger';
      case 'Ajuste': return 'primary';
    }
  }

  getRowClass(row: InventoryRow): string {
    if (row.disponivel === 0) return 'inv-table__row--zero';
    if (row.disponivel <= 5) return 'inv-table__row--low';
    return '';
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  setMovementFilter(type: MovementType | 'all'): void {
    this.movementTypeFilter.set(type);
  }
}
