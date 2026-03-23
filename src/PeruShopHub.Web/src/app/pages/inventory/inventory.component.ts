import { Component, signal, computed, HostListener, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { InventoryService } from '../../services/inventory.service';
import type { InventoryItem, StockMovement } from '../../services/inventory.service';

type InventoryTab = 'visao-geral' | 'movimentacoes' | 'estoque-full';
type MovementType = 'Entrada' | 'Saída' | 'Ajuste';

interface ProductOption {
  sku: string;
  nome: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, KpiCardComponent, SkeletonComponent, BadgeComponent, BrlCurrencyPipe],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);

  activeTab = signal<InventoryTab>('visao-geral');
  loading = signal(true);
  movementTypeFilter = signal<MovementType | 'all'>('all');

  // Mutable inventory and movements data
  inventoryData = signal<InventoryItem[]>([]);
  movements = signal<StockMovement[]>([]);

  // Modal state
  entryModalOpen = signal(false);
  entryForm: FormGroup;
  productSearch = signal('');
  showProductDropdown = signal(false);

  // Product options derived from real inventory data
  productOptions = computed<ProductOption[]>(() =>
    this.inventoryData().map(r => ({ sku: r.sku, nome: r.productName }))
  );

  filteredProducts = computed(() => {
    const query = this.productSearch().toLowerCase();
    const options = this.productOptions();
    if (!query) return options;
    return options.filter(p =>
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
    const totalUnits = data.reduce((s, r) => s + r.totalStock, 0);
    const critical = data.filter(r => r.available <= 5).length;
    const stockValue = data.reduce((s, r) => s + r.stockValue, 0);
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
  }

  ngOnInit(): void {
    this.loadInventory();
    this.loadMovements();
  }

  private loadInventory(): void {
    this.loading.set(true);
    this.inventoryService.getInventory().subscribe({
      next: (data) => {
        this.inventoryData.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  private loadMovements(): void {
    this.inventoryService.getMovements({ page: 1, pageSize: 50 }).subscribe({
      next: (result) => {
        this.movements.set(result.items);
      },
    });
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
    if (tab === 'movimentacoes') {
      this.loadMovementsWithFilters();
    }
  }

  private loadMovementsWithFilters(): void {
    const filter = this.movementTypeFilter();
    this.inventoryService.getMovements({
      type: filter === 'all' ? undefined : filter,
      page: 1,
      pageSize: 50,
    }).subscribe({
      next: (result) => {
        this.movements.set(result.items);
      },
    });
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
    const quantidade = val.quantidade;
    const notaFiscal = val.notaFiscal || '';
    const observacao = val.observacao || '';

    const reason = notaFiscal ? `Reposição NF ${notaFiscal}` : (observacao || 'Entrada manual');

    this.inventoryService.adjust({
      productId: sku,
      variantId: sku,
      quantity: quantidade,
      reason,
    }).subscribe({
      next: () => {
        this.closeEntryModal();
        // Reload data from server
        this.loadInventory();
        this.loadMovements();
      },
      error: () => {
        // Keep modal open on error so user can retry
      },
    });
  }

  getMovementTypeVariant(tipo: MovementType): BadgeVariant {
    switch (tipo) {
      case 'Entrada': return 'success';
      case 'Saída': return 'danger';
      case 'Ajuste': return 'primary';
    }
  }

  getRowClass(row: InventoryItem): string {
    if (row.available === 0) return 'inv-table__row--zero';
    if (row.available <= 5) return 'inv-table__row--low';
    return '';
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  setMovementFilter(type: MovementType | 'all'): void {
    this.movementTypeFilter.set(type);
    this.loadMovementsWithFilters();
  }
}
