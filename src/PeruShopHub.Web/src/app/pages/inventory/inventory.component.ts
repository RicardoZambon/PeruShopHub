import { Component, signal, computed, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TabBarComponent, type TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { DialogComponent } from '../../shared/components/dialog/dialog.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import {
  DataGridComponent,
  GridCellDirective,
  GridCardDirective,
  GridColumn,
  GridSortEvent,
} from '../../shared/components/data-grid/data-grid.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { InventoryService } from '../../services/inventory.service';
import type { InventoryItem, StockMovement, InventoryQueryParams } from '../../services/inventory.service';
import { firstValueFrom } from 'rxjs';

type InventoryTab = 'visao-geral' | 'movimentacoes' | 'estoque-full';
type MovementType = 'Entrada' | 'Saída' | 'Ajuste';

interface ProductOption {
  sku: string;
  nome: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, KpiCardComponent, SkeletonComponent, BadgeComponent, BrlCurrencyPipe, PageHeaderComponent, TabBarComponent, SelectDropdownComponent, DialogComponent, FormFieldComponent, FormActionsComponent, DataGridComponent, GridCellDirective, GridCardDirective],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);

  @ViewChild('movGrid') movGridRef!: DataGridComponent;
  @ViewChild('invGrid') invGridRef!: DataGridComponent;

  activeTab = signal<InventoryTab>('visao-geral');
  loading = signal(true);
  movementTypeFilter = signal<MovementType | 'all'>('all');

  // Mutable inventory and movements data
  inventoryData = signal<InventoryItem[]>([]);
  movements = signal<StockMovement[]>([]);

  // Inventory grid state (for infinite scroll in overview tab)
  invLoading = signal(false);
  invHasMore = signal(true);
  invCurrentPage = signal(1);
  readonly invPageSize = signal(20);
  invSortBy = signal('productName');
  invSortDir = signal<'asc' | 'desc'>('asc');
  invSearch = signal('');

  readonly invGridColumns: GridColumn[] = [
    { key: 'sku', label: 'SKU', sortable: true },
    { key: 'productName', label: 'Produto', sortable: true },
    { key: 'totalStock', label: 'Estoque Total', align: 'right', sortable: true },
    { key: 'reserved', label: 'Reservado', align: 'right', sortable: true },
    { key: 'available', label: 'Disponível', align: 'right', sortable: true },
    { key: 'unitCost', label: 'Custo Unit.', align: 'right', sortable: true },
    { key: 'stockValue', label: 'Valor Estoque', align: 'right', sortable: true },
  ];

  readonly invGridData = computed(() => {
    return this.inventoryData().map(item => ({
      ...item,
    }));
  });

  // Movements grid state (for infinite scroll in movimentações tab)
  movLoading = signal(false);
  movHasMore = signal(true);
  movCurrentPage = signal(1);
  readonly movPageSize = signal(20);

  readonly movGridColumns: GridColumn[] = [
    { key: 'data', label: 'Data', sortable: true },
    { key: 'produto', label: 'Produto', sortable: true },
    { key: 'tipo', label: 'Tipo' },
    { key: 'quantidade', label: 'Quantidade', align: 'right' },
    { key: 'custoUnitario', label: 'Custo Unitário', align: 'right' },
    { key: 'motivo', label: 'Observação' },
  ];

  readonly movGridData = computed(() => {
    return this.movements().map(m => ({
      ...m,
    }));
  });

  // Modal state
  entryModalOpen = signal(false);
  readonly saving = signal(false);
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

  tabs: TabItem[] = [
    { key: 'visao-geral', label: 'Visão Geral' },
    { key: 'movimentacoes', label: 'Movimentações' },
    { key: 'estoque-full', label: 'Estoque Full', disabled: true },
  ];

  movementTypeOptions: SelectOption[] = [
    { value: 'all', label: 'Todos' },
    { value: 'Entrada', label: 'Entrada' },
    { value: 'Saída', label: 'Saída' },
    { value: 'Ajuste', label: 'Ajuste' },
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

  formatMovDate(dateStr: string | null): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return dateStr;
    return date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

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
    this.loadInventoryGrid(true);
    this.loadMovements();
  }

  async loadInventoryGrid(reset = false): Promise<void> {
    if (reset) {
      this.invCurrentPage.set(1);
      this.inventoryData.set([]);
      this.invHasMore.set(true);
    }

    this.invLoading.set(true);
    if (reset) this.loading.set(true);
    try {
      const result = await firstValueFrom(this.inventoryService.getInventory({
        page: this.invCurrentPage(),
        pageSize: this.invPageSize(),
        search: this.invSearch() || undefined,
        sortBy: this.invSortBy(),
        sortDir: this.invSortDir(),
      }));

      if (reset) {
        this.inventoryData.set(result.items);
      } else {
        this.inventoryData.update(prev => [...prev, ...result.items]);
      }

      const totalLoaded = this.inventoryData().length;
      this.invHasMore.set(totalLoaded < result.totalCount);
    } catch {
      if (reset) {
        this.inventoryData.set([]);
      }
      this.invHasMore.set(false);
    } finally {
      this.invLoading.set(false);
      this.loading.set(false);
    }
  }

  onInvLoadMore(): void {
    this.invCurrentPage.update(p => p + 1);
    this.loadInventoryGrid(false);
  }

  onInvSort(event: GridSortEvent): void {
    if (event.direction) {
      this.invSortBy.set(event.column);
      this.invSortDir.set(event.direction);
    } else {
      this.invSortBy.set('productName');
      this.invSortDir.set('asc');
    }
    this.loadInventoryGrid(true);
    this.invGridRef?.scrollToTop();
  }

  private loadMovements(): void {
    this.inventoryService.getMovements({ page: 1, pageSize: 50 }).subscribe({
      next: (result) => {
        this.movements.set(result.items);
      },
    });
  }

  selectTab(tab: InventoryTab): void {
    const tabDef = this.tabs.find(t => t.key === tab);
    if (tabDef?.disabled) return;
    this.activeTab.set(tab);
    if (tab === 'movimentacoes') {
      this.loadMovementsGrid(true);
    }
  }

  async loadMovementsGrid(reset = false): Promise<void> {
    if (reset) {
      this.movCurrentPage.set(1);
      this.movements.set([]);
      this.movHasMore.set(true);
    }

    this.movLoading.set(true);
    try {
      const filter = this.movementTypeFilter();
      const result = await firstValueFrom(this.inventoryService.getMovements({
        type: filter === 'all' ? undefined : filter,
        page: this.movCurrentPage(),
        pageSize: this.movPageSize(),
      }));

      if (reset) {
        this.movements.set(result.items);
      } else {
        this.movements.update(prev => [...prev, ...result.items]);
      }

      const totalLoaded = this.movements().length;
      this.movHasMore.set(totalLoaded < result.totalCount);
    } catch {
      if (reset) {
        this.movements.set([]);
      }
      this.movHasMore.set(false);
    } finally {
      this.movLoading.set(false);
    }
  }

  onMovLoadMore(): void {
    this.movCurrentPage.update(p => p + 1);
    this.loadMovementsGrid(false);
  }

  onMovSort(event: GridSortEvent): void {
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
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

    this.saving.set(true);
    this.inventoryService.adjust({
      productId: sku,
      variantId: sku,
      quantity: quantidade,
      reason,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeEntryModal();
        // Reload data from server
        this.loadInventoryGrid(true);
        this.loadMovements();
      },
      error: () => {
        this.saving.set(false);
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

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  setMovementFilter(type: MovementType | 'all'): void {
    this.movementTypeFilter.set(type);
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
  }
}
