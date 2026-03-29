import { Component, signal, computed, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { SkeletonComponent } from '../../shared/components/skeleton/skeleton.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TabPanelsComponent, TabPanelDirective } from '../../shared/components/tab-panels/tab-panels.component';
import type { TabItem } from '../../shared/components/tab-bar/tab-bar.component';
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
import { formatBrl as formatBrlUtil, formatDateShort } from '../../shared/utils';
import { InventoryService } from '../../services/inventory.service';
import type { InventoryItem, StockMovement, InventoryQueryParams, ProductAllocations, VariantAllocations, ReconciliationResult, ReconciliationResultItem, ReconciliationReport, ReconciliationReportDetail, ReconciliationReportItem, FulfillmentStockOverview, ProductFulfillmentStock, FulfillmentStockItem } from '../../services/inventory.service';
import { ProductService } from '../../services/product.service';
import type { Product } from '../../services/product.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import { PricingService } from '../../services/pricing.service';
import type { FulfillmentCompareResult } from '../../services/pricing.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

type InventoryTab = 'visao-geral' | 'movimentacoes' | 'reconciliacao' | 'reconciliacao-ml' | 'estoque-full' | 'simulador-full';
type MovementType = 'Entrada' | 'Saída' | 'Ajuste' | 'Reconciliacao';

interface ProductOption {
  sku: string;
  nome: string;
}

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, ReactiveFormsModule, KpiCardComponent, SkeletonComponent, BadgeComponent, BrlCurrencyPipe, PageHeaderComponent, TabPanelsComponent, TabPanelDirective, SelectDropdownComponent, DialogComponent, FormFieldComponent, FormActionsComponent, DataGridComponent, GridCellDirective, GridCardDirective],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss',
})
export class InventoryComponent implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly productService = inject(ProductService);
  private readonly confirmDialogService = inject(ConfirmDialogService);
  private readonly pricingService = inject(PricingService);
  private readonly router = inject(Router);

  @ViewChild('movGrid') movGridRef!: DataGridComponent;
  @ViewChild('invGrid') invGridRef!: DataGridComponent;

  activeTab = signal<InventoryTab>('visao-geral');
  loading = signal(true);
  movementTypeFilter = signal<MovementType | 'all'>('all');
  movDateFrom = signal('');
  movDateTo = signal('');
  movCreatedBy = signal('');
  exporting = signal(false);

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
  invTotalCount = signal(0);

  readonly invGridColumns: GridColumn[] = [
    { key: 'sku', label: 'SKU', sortable: true },
    { key: 'productName', label: 'Produto', sortable: true },
    { key: 'totalStock', label: 'Estoque Total', align: 'right', sortable: true },
    { key: 'reserved', label: 'Reservado', align: 'right', sortable: true },
    { key: 'available', label: 'Disponível', align: 'right', sortable: true },
    { key: 'unitCost', label: 'Custo Unit.', align: 'right', sortable: true },
    { key: 'stockValue', label: 'Valor Estoque', align: 'right', sortable: true },
    { key: 'actions', label: '', align: 'center' },
  ];

  readonly invGridData = computed(() => {
    return this.inventoryData().map(item => ({
      ...item,
      _rowClass: item.minStock != null && item.totalStock <= item.minStock ? 'inv__row--low-stock' : '',
    }));
  });

  // Movements grid state (for infinite scroll in movimentações tab)
  movLoading = signal(false);
  movHasMore = signal(true);
  movTotalCount = signal(0);
  movCurrentPage = signal(1);
  readonly movPageSize = signal(20);

  readonly movGridColumns: GridColumn[] = [
    { key: 'createdAt', label: 'Data', sortable: true },
    { key: 'productName', label: 'Produto', sortable: true },
    { key: 'type', label: 'Tipo' },
    { key: 'quantity', label: 'Quantidade', align: 'right' },
    { key: 'unitCost', label: 'Custo Unitário', align: 'right' },
    { key: 'reason', label: 'Observação' },
    { key: 'createdBy', label: 'Usuário' },
    { key: 'source', label: 'Origem' },
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
    { key: 'reconciliacao', label: 'Reconciliação' },
    { key: 'reconciliacao-ml', label: 'Reconciliação ML' },
    { key: 'estoque-full', label: 'Estoque Full' },
    { key: 'simulador-full', label: 'Simulador Full' },
  ];

  movementTypeOptions: SelectOption[] = [
    { value: 'all', label: 'Todos' },
    { value: 'Entrada', label: 'Entrada' },
    { value: 'Saída', label: 'Saída' },
    { value: 'Ajuste', label: 'Ajuste' },
    { value: 'Reconciliacao', label: 'Reconciliação' },
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
    return all.filter(m => m.type === filter);
  });

  formatMovDate = formatDateShort;

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
      this.invTotalCount.set(result.totalCount);
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
    } else if (tab === 'reconciliacao') {
      this.loadReconciliationProducts();
    } else if (tab === 'reconciliacao-ml') {
      this.loadMlReports(true);
    } else if (tab === 'estoque-full') {
      this.loadFulfillmentStock();
    } else if (tab === 'simulador-full') {
      this.loadSimuladorProducts();
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
        dateFrom: this.movDateFrom() || undefined,
        dateTo: this.movDateTo() || undefined,
        createdBy: this.movCreatedBy() || undefined,
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
      this.movTotalCount.set(result.totalCount);
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

  getMovementTypeVariant(tipo: string): BadgeVariant {
    switch (tipo) {
      case 'Entrada': return 'success';
      case 'Saída': return 'danger';
      case 'Ajuste': return 'primary';
      case 'Reconciliacao': return 'warning' as BadgeVariant;
      default: return 'primary';
    }
  }

  formatBrl = formatBrlUtil;

  setMovementFilter(type: MovementType | 'all'): void {
    this.movementTypeFilter.set(type);
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
  }

  onMovDateFromChange(event: Event): void {
    this.movDateFrom.set((event.target as HTMLInputElement).value);
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
  }

  onMovDateToChange(event: Event): void {
    this.movDateTo.set((event.target as HTMLInputElement).value);
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
  }

  onMovCreatedByChange(value: string): void {
    this.movCreatedBy.set(value);
    this.loadMovementsGrid(true);
    this.movGridRef?.scrollToTop();
  }

  async exportMovements(): Promise<void> {
    this.exporting.set(true);
    try {
      const filter = this.movementTypeFilter();
      const blob = await firstValueFrom(this.inventoryService.exportMovements({
        type: filter === 'all' ? undefined : filter,
        dateFrom: this.movDateFrom() || undefined,
        dateTo: this.movDateTo() || undefined,
        createdBy: this.movCreatedBy() || undefined,
      }));
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `movimentacoes_${new Date().toISOString().slice(0, 10)}.xlsx`;
      a.click();
      window.URL.revokeObjectURL(url);
    } finally {
      this.exporting.set(false);
    }
  }

  navigateToSource(movement: StockMovement): void {
    if (movement.purchaseOrderId) {
      this.router.navigate(['/compras', movement.purchaseOrderId]);
    } else if (movement.orderId) {
      this.router.navigate(['/vendas', movement.orderId]);
    }
  }

  getMovementTypeLabel(type: string): string {
    const labels: Record<string, string> = {
      'Entrada': 'Entrada',
      'Saída': 'Saída',
      'Ajuste': 'Ajuste',
      'Reconciliacao': 'Reconciliação',
    };
    return labels[type] ?? type;
  }

  // Reconciliation state
  reconLoading = signal(false);
  reconSaving = signal(false);
  reconProducts = signal<Product[]>([]);
  reconCountedQty = signal<Record<string, number | null>>({});
  reconResult = signal<ReconciliationResult | null>(null);

  reconItems = computed(() => {
    const products = this.reconProducts();
    const counted = this.reconCountedQty();
    const items: {
      variantId: string;
      sku: string;
      productName: string;
      systemQty: number;
      countedQty: number | null;
      difference: number | null;
      status: 'under' | 'over' | 'match' | 'pending';
    }[] = [];

    for (const product of products) {
      if (!product.variants || product.variants.length === 0) continue;
      for (const variant of product.variants) {
        const qty = counted[variant.id] ?? null;
        const diff = qty !== null ? qty - (variant.stock ?? 0) : null;
        let status: 'under' | 'over' | 'match' | 'pending' = 'pending';
        if (diff !== null) {
          status = diff < 0 ? 'under' : diff > 0 ? 'over' : 'match';
        }
        items.push({
          variantId: variant.id,
          sku: variant.sku || product.sku,
          productName: product.name,
          systemQty: variant.stock ?? 0,
          countedQty: qty,
          difference: diff,
          status,
        });
      }
    }
    return items;
  });

  reconDiscrepancyCount = computed(() => {
    return this.reconItems().filter(i => i.status === 'under' || i.status === 'over').length;
  });

  reconFilledCount = computed(() => {
    return this.reconItems().filter(i => i.countedQty !== null).length;
  });

  async loadReconciliationProducts(): Promise<void> {
    if (this.reconProducts().length > 0) return; // already loaded
    this.reconLoading.set(true);
    try {
      const result = await this.productService.list({ page: 1, pageSize: 500, status: 'active' });
      this.reconProducts.set(result.items);
    } catch {
      this.reconProducts.set([]);
    } finally {
      this.reconLoading.set(false);
    }
  }

  onReconQtyChange(variantId: string, value: string): void {
    const num = value === '' ? null : parseInt(value, 10);
    if (num !== null && (isNaN(num) || num < 0)) return;
    this.reconCountedQty.update(prev => ({ ...prev, [variantId]: num }));
  }

  resetReconciliation(): void {
    this.reconCountedQty.set({});
    this.reconResult.set(null);
  }

  async submitReconciliation(): Promise<void> {
    const items = this.reconItems().filter(i => i.countedQty !== null);
    if (items.length === 0) return;

    const discrepancies = items.filter(i => i.status !== 'match').length;
    const message = discrepancies > 0
      ? `${items.length} itens verificados, ${discrepancies} com discrepância. O estoque será ajustado automaticamente.`
      : `${items.length} itens verificados, nenhuma discrepância encontrada. Nenhum ajuste será feito.`;

    const confirmed = await this.confirmDialogService.confirm({
      title: 'Confirmar Reconciliação',
      message,
      confirmLabel: 'Reconciliar',
      cancelLabel: 'Cancelar',
      variant: 'warning',
    });

    if (!confirmed) return;

    this.reconSaving.set(true);
    this.confirmDialogService.processing.set(true);
    try {
      const result = await firstValueFrom(this.inventoryService.reconcile({
        items: items.map(i => ({
          variantId: i.variantId,
          countedQuantity: i.countedQty!,
        })),
      }));
      this.reconResult.set(result);
      this.confirmDialogService.done();
      // Refresh inventory data
      this.loadInventoryGrid(true);
      this.loadMovements();
    } catch {
      this.confirmDialogService.done();
    } finally {
      this.reconSaving.set(false);
    }
  }

  // Allocation dialog state
  allocationDialogOpen = signal(false);
  allocationLoading = signal(false);
  allocationSaving = signal<string | null>(null); // variantId:marketplaceId being saved
  allocationData = signal<ProductAllocations | null>(null);
  allocationError = signal('');

  // Tracks edited quantities keyed by "variantId:marketplaceId"
  editedAllocations = signal<Record<string, number>>({});

  openAllocations(item: InventoryItem): void {
    this.allocationDialogOpen.set(true);
    this.allocationData.set(null);
    this.allocationError.set('');
    this.editedAllocations.set({});
    this.loadAllocations(item.productId);
  }

  closeAllocations(): void {
    this.allocationDialogOpen.set(false);
  }

  private async loadAllocations(productId: string): Promise<void> {
    this.allocationLoading.set(true);
    try {
      const data = await firstValueFrom(this.inventoryService.getAllocations(productId));
      this.allocationData.set(data);
    } catch {
      this.allocationError.set('Erro ao carregar alocações');
    } finally {
      this.allocationLoading.set(false);
    }
  }

  allocationKey(variantId: string, marketplaceId: string): string {
    return `${variantId}:${marketplaceId}`;
  }

  onAllocationChange(variantId: string, marketplaceId: string, value: string): void {
    const num = parseInt(value, 10);
    if (isNaN(num) || num < 0) return;
    this.editedAllocations.update(prev => ({
      ...prev,
      [this.allocationKey(variantId, marketplaceId)]: num,
    }));
  }

  getAllocationValue(variant: VariantAllocations, marketplaceId: string): number {
    const key = this.allocationKey(variant.variantId, marketplaceId);
    const edited = this.editedAllocations();
    if (key in edited) return edited[key];
    const existing = variant.allocations.find(a => a.marketplaceId === marketplaceId);
    return existing?.allocatedQuantity ?? 0;
  }

  getVariantTotalAllocated(variant: VariantAllocations): number {
    const marketplaces = this.getMarketplaces();
    return marketplaces.reduce((sum, mp) => sum + this.getAllocationValue(variant, mp), 0);
  }

  getVariantUnallocated(variant: VariantAllocations): number {
    return variant.totalStock - this.getVariantTotalAllocated(variant);
  }

  isOverAllocated(variant: VariantAllocations): boolean {
    return this.getVariantUnallocated(variant) < 0;
  }

  hasUnsavedChanges(variantId: string, marketplaceId: string): boolean {
    const key = this.allocationKey(variantId, marketplaceId);
    return key in this.editedAllocations();
  }

  getMarketplaces(): string[] {
    const data = this.allocationData();
    if (!data) return [];
    const set = new Set<string>();
    for (const v of data.variants) {
      for (const a of v.allocations) {
        set.add(a.marketplaceId);
      }
    }
    // Ensure at least "mercadolivre" is shown
    if (set.size === 0) set.add('mercadolivre');
    return Array.from(set).sort();
  }

  getReservedQuantity(variant: VariantAllocations, marketplaceId: string): number {
    const alloc = variant.allocations.find(a => a.marketplaceId === marketplaceId);
    return alloc?.reservedQuantity ?? 0;
  }

  formatMarketplaceName(id: string): string {
    const names: Record<string, string> = {
      'mercadolivre': 'Mercado Livre',
      'amazon': 'Amazon',
      'shopee': 'Shopee',
      'magalu': 'Magazine Luiza',
    };
    return names[id] ?? id;
  }

  async saveAllocation(variantId: string, marketplaceId: string): Promise<void> {
    const key = this.allocationKey(variantId, marketplaceId);
    const value = this.editedAllocations()[key];
    if (value === undefined) return;

    // Validate: check the variant won't be over-allocated
    const data = this.allocationData();
    if (!data) return;
    const variant = data.variants.find(v => v.variantId === variantId);
    if (variant && this.isOverAllocated(variant)) {
      this.allocationError.set('Alocação excede o estoque total da variante');
      return;
    }

    this.allocationSaving.set(key);
    this.allocationError.set('');
    try {
      await firstValueFrom(this.inventoryService.updateAllocation(variantId, {
        marketplaceId,
        allocatedQuantity: value,
      }));

      // Remove from edited and reload allocations
      this.editedAllocations.update(prev => {
        const copy = { ...prev };
        delete copy[key];
        return copy;
      });
      await this.loadAllocations(data.productId);
    } catch (err: any) {
      const msg = err?.error?.errors
        ? Object.values(err.error.errors).flat().join(', ')
        : 'Erro ao salvar alocação';
      this.allocationError.set(msg as string);
    } finally {
      this.allocationSaving.set(null);
    }
  }

  // ── ML Reconciliation Reports ──────────────────────────────
  mlReports = signal<ReconciliationReport[]>([]);
  mlReportsLoading = signal(false);
  mlReportsTotalCount = signal(0);
  mlReportsPage = signal(1);
  mlReportsHasMore = signal(true);
  mlReportDateFrom = signal('');
  mlReportDateTo = signal('');

  mlSelectedReport = signal<ReconciliationReportDetail | null>(null);
  mlReportDetailLoading = signal(false);

  mlReportGridColumns: GridColumn[] = [
    { key: 'startedAt', label: 'Data' },
    { key: 'status', label: 'Status' },
    { key: 'itemsChecked', label: 'Verificados', align: 'right' },
    { key: 'matches', label: 'OK', align: 'right' },
    { key: 'autoCorrected', label: 'Auto-corrigidos', align: 'right' },
    { key: 'manualReviewRequired', label: 'Revisão Manual', align: 'right' },
    { key: 'actions', label: '', align: 'center' },
  ];

  mlReportItemColumns: GridColumn[] = [
    { key: 'sku', label: 'SKU' },
    { key: 'productName', label: 'Produto' },
    { key: 'localQuantity', label: 'Local', align: 'right' },
    { key: 'marketplaceQuantity', label: 'ML', align: 'right' },
    { key: 'difference', label: 'Diferença', align: 'right' },
    { key: 'resolution', label: 'Resolução' },
  ];

  async loadMlReports(reset = false): Promise<void> {
    if (reset) {
      this.mlReportsPage.set(1);
      this.mlReports.set([]);
      this.mlReportsHasMore.set(true);
      this.mlSelectedReport.set(null);
    }

    this.mlReportsLoading.set(true);
    try {
      const result = await firstValueFrom(this.inventoryService.getReconciliationReports({
        dateFrom: this.mlReportDateFrom() || undefined,
        dateTo: this.mlReportDateTo() || undefined,
        page: this.mlReportsPage(),
        pageSize: 20,
      }));

      if (reset) {
        this.mlReports.set(result.items);
      } else {
        this.mlReports.update(prev => [...prev, ...result.items]);
      }

      this.mlReportsTotalCount.set(result.totalCount);
      this.mlReportsHasMore.set(this.mlReports().length < result.totalCount);
    } catch {
      if (reset) this.mlReports.set([]);
      this.mlReportsHasMore.set(false);
    } finally {
      this.mlReportsLoading.set(false);
    }
  }

  onMlReportsLoadMore(): void {
    this.mlReportsPage.update(p => p + 1);
    this.loadMlReports(false);
  }

  onMlReportDateFromChange(event: Event): void {
    this.mlReportDateFrom.set((event.target as HTMLInputElement).value);
    this.loadMlReports(true);
  }

  onMlReportDateToChange(event: Event): void {
    this.mlReportDateTo.set((event.target as HTMLInputElement).value);
    this.loadMlReports(true);
  }

  async viewMlReportDetail(report: ReconciliationReport): Promise<void> {
    this.mlReportDetailLoading.set(true);
    this.mlSelectedReport.set(null);
    try {
      const detail = await firstValueFrom(this.inventoryService.getReconciliationReportDetail(report.id));
      this.mlSelectedReport.set(detail);
    } catch {
      this.mlSelectedReport.set(null);
    } finally {
      this.mlReportDetailLoading.set(false);
    }
  }

  closeMlReportDetail(): void {
    this.mlSelectedReport.set(null);
  }

  getMlStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Completed': return 'success';
      case 'Running': return 'warning' as BadgeVariant;
      case 'Failed': return 'danger';
      default: return 'primary';
    }
  }

  getMlStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      'Completed': 'Concluído',
      'Running': 'Em andamento',
      'Failed': 'Falhou',
    };
    return labels[status] ?? status;
  }

  getMlResolutionVariant(resolution: string): BadgeVariant {
    switch (resolution) {
      case 'Match': return 'success';
      case 'AutoCorrected': return 'warning' as BadgeVariant;
      case 'ManualReview': return 'danger';
      default: return 'primary';
    }
  }

  getMlResolutionLabel(resolution: string): string {
    const labels: Record<string, string> = {
      'Match': 'OK',
      'AutoCorrected': 'Auto-corrigido',
      'ManualReview': 'Revisão Manual',
    };
    return labels[resolution] ?? resolution;
  }

  // ── Fulfillment Stock (Estoque Full) ──────────────────────
  fullStockLoading = signal(false);
  fullStockData = signal<FulfillmentStockOverview | null>(null);

  async loadFulfillmentStock(): Promise<void> {
    if (this.fullStockData()) return; // already loaded
    this.fullStockLoading.set(true);
    try {
      const data = await firstValueFrom(this.inventoryService.getFulfillmentStock());
      this.fullStockData.set(data);
    } catch {
      this.fullStockData.set(null);
    } finally {
      this.fullStockLoading.set(false);
    }
  }

  async refreshFulfillmentStock(): Promise<void> {
    this.fullStockData.set(null);
    this.fullStockLoading.set(true);
    try {
      const data = await firstValueFrom(this.inventoryService.getFulfillmentStock());
      this.fullStockData.set(data);
    } catch {
      this.fullStockData.set(null);
    } finally {
      this.fullStockLoading.set(false);
    }
  }

  getFullStatusLabel(status: string | null | undefined): string {
    if (!status) return 'N/A';
    const labels: Record<string, string> = {
      'active': 'Ativo',
      'inactive': 'Inativo',
      'error': 'Erro',
    };
    return labels[status] ?? status;
  }

  getFullStatusVariant(status: string | null | undefined): BadgeVariant {
    if (!status) return 'primary';
    switch (status) {
      case 'active': return 'success';
      case 'error': return 'danger';
      default: return 'primary';
    }
  }

  // ── Simulador Full (Full vs Own Shipping) ──────────────
  simProducts = signal<Product[]>([]);
  simProductsLoading = signal(false);
  simSelectedProductId = signal<string | null>(null);
  simLaborCost = signal<number | null>(null);
  simLoading = signal(false);
  simResult = signal<FulfillmentCompareResult | null>(null);
  simError = signal('');

  simProductOptions = computed<SelectOption[]>(() =>
    this.simProducts().map(p => ({ value: p.id, label: `${p.name} (${p.sku})` }))
  );

  async loadSimuladorProducts(): Promise<void> {
    if (this.simProducts().length > 0) return;
    this.simProductsLoading.set(true);
    try {
      const result = await this.productService.list({ page: 1, pageSize: 500, status: 'active' });
      this.simProducts.set(result.items);
    } catch {
      this.simProducts.set([]);
    } finally {
      this.simProductsLoading.set(false);
    }
  }

  onSimProductChange(value: string): void {
    this.simSelectedProductId.set(value);
    this.simResult.set(null);
    this.simError.set('');
  }

  onSimLaborCostChange(event: Event): void {
    const val = (event.target as HTMLInputElement).value;
    this.simLaborCost.set(val ? parseFloat(val) : null);
  }

  async runFulfillmentCompare(): Promise<void> {
    const productId = this.simSelectedProductId();
    if (!productId) return;

    this.simLoading.set(true);
    this.simError.set('');
    this.simResult.set(null);
    try {
      const result = await this.pricingService.fulfillmentCompare({
        productId,
        laborCostPerShipment: this.simLaborCost(),
      });
      this.simResult.set(result);
    } catch (err: any) {
      const msg = err?.error?.message || err?.error?.errors
        ? Object.values(err.error.errors || {}).flat().join(', ')
        : 'Erro ao simular comparação';
      this.simError.set(msg as string);
    } finally {
      this.simLoading.set(false);
    }
  }
}
