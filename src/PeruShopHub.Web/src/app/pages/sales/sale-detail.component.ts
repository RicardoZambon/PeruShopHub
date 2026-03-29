import { Component, signal, computed, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import {
  LucideAngularModule, Package, Copy, Truck, CreditCard,
  User, MapPin, Clock, Check, Circle, Plus, Lock, Unlock, Pencil,
  Trash2, X, Search, ChevronDown, RefreshCw
} from 'lucide-angular';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, Plugin } from 'chart.js';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { OrderService, OrderDetail as ApiOrderDetail } from '../../services/order.service';
import { ToastService } from '../../services/toast.service';
import { formatBrl as formatBrlUtil, formatDate as formatDateUtil } from '../../shared/utils';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';
type LogisticType = 'Full' | 'Coleta' | 'Agência';
type CostSource = 'API' | 'Manual' | 'Calculated' | 'Calculado';

interface OrderItem {
  id?: string;
  productId?: string;
  name: string;
  sku: string;
  variation?: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

interface Buyer {
  name: string;
  nickname: string;
  email: string;
  phone: string;
  totalOrders: number;
  totalSpent: number;
}

interface ShippingInfo {
  trackingNumber: string;
  trackingUrl: string;
  carrier: string;
  logisticType: LogisticType;
  shippingStatus: string;
  timeline: TimelineStep[];
}

interface TimelineStep {
  label: string;
  date: string | null;
  completed: boolean;
}

interface PaymentInfo {
  method: string;
  installments: number;
  amount: number;
  status: string;
  statusVariant: BadgeVariant;
}

interface CostItem {
  id?: string;
  category: string;
  categoryKey?: string;
  description?: string;
  value: number;
  color: string;
  source: CostSource;
}

interface OrderDetailView {
  id: string;
  date: string;
  status: OrderStatus;
  statusVariant: BadgeVariant;
  items: OrderItem[];
  buyer: Buyer;
  shipping: ShippingInfo;
  payment: PaymentInfo;
  revenue: number;
  costs: CostItem[];
}

interface Supply {
  id: string;
  name: string;
  unitCost: number;
}

interface SaleSupply {
  supplyId: string;
  name: string;
  quantity: number;
  unitCost: number;
  total: number;
}

const COST_CATEGORIES = [
  { value: 'marketplace_commission', label: 'Comissão marketplace', color: '#5C6BC0' },
  { value: 'fixed_fee', label: 'Taxa fixa', color: '#7986CB' },
  { value: 'shipping_seller', label: 'Frete vendedor', color: '#42A5F5' },
  { value: 'payment_fee', label: 'Taxa pagamento', color: '#64B5F6' },
  { value: 'tax_icms', label: 'ICMS', color: '#EF5350' },
  { value: 'tax_pis_cofins', label: 'PIS/COFINS', color: '#E53935' },
  { value: 'storage_daily', label: 'Armazenagem diária', color: '#AB47BC' },
  { value: 'fulfillment_fee', label: 'Fulfillment', color: '#7E57C2' },
  { value: 'packaging', label: 'Embalagem', color: '#FFA726' },
  { value: 'advertising', label: 'Publicidade', color: '#26C6DA' },
  { value: 'other', label: 'Outros', color: '#BDBDBD' },
];

const MOCK_SUPPLIES: Supply[] = [
  { id: 'sup-001', name: 'Plástico bolha (metro)', unitCost: 1.20 },
  { id: 'sup-002', name: 'Caixa correio M', unitCost: 3.50 },
  { id: 'sup-003', name: 'Caixa correio G', unitCost: 5.80 },
  { id: 'sup-004', name: 'Fita adesiva (rolo)', unitCost: 4.90 },
  { id: 'sup-005', name: 'Envelope plástico segurança M', unitCost: 0.85 },
  { id: 'sup-006', name: 'Etiqueta térmica (un)', unitCost: 0.15 },
  { id: 'sup-007', name: 'Papel kraft (folha)', unitCost: 0.60 },
];

function mapStatusToVariant(status: string): BadgeVariant {
  const map: Record<string, BadgeVariant> = {
    'Pago': 'success',
    'Enviado': 'warning',
    'Entregue': 'success',
    'Cancelado': 'danger',
    'Devolvido': 'danger',
  };
  return map[status] ?? 'neutral';
}

function mapPaymentStatusToVariant(status: string | undefined): BadgeVariant {
  if (!status) return 'neutral';
  const s = status.toLowerCase();
  if (s === 'aprovado' || s === 'approved') return 'success';
  if (s === 'pendente' || s === 'pending') return 'warning';
  if (s === 'rejeitado' || s === 'rejected') return 'danger';
  return 'neutral';
}

function mapCostCategory(category: string): { label: string; key: string; color: string } {
  const found = COST_CATEGORIES.find(c => c.value === category || c.label.toLowerCase() === category.toLowerCase());
  if (found) return { label: found.label, key: found.value, color: found.color };
  return { label: category, key: category, color: '#BDBDBD' };
}

function mapApiToView(api: ApiOrderDetail): OrderDetailView {
  const statusStr = api.status as OrderStatus;

  // Map shipping timeline
  const timeline: TimelineStep[] = (api.shipping?.timeline ?? []).map(t => ({
    label: t.status ?? t.description ?? '',
    date: t.timestamp ? formatDateUtil(t.timestamp, true) : null,
    completed: !!t.timestamp,
  }));

  // Map costs
  const costs: CostItem[] = (api.costs ?? []).map(c => {
    const mapped = mapCostCategory(c.category);
    return {
      id: c.id,
      category: mapped.label,
      categoryKey: mapped.key,
      description: c.description,
      value: c.value,
      color: mapped.color,
      source: c.source as CostSource,
    };
  });

  return {
    id: api.externalOrderId || api.id,
    date: api.orderDate,
    status: statusStr,
    statusVariant: mapStatusToVariant(api.status),
    items: (api.items ?? []).map(item => ({
      id: item.id,
      productId: item.productId,
      name: item.name,
      sku: item.sku,
      variation: item.variation,
      quantity: item.quantity,
      unitPrice: item.unitPrice,
      subtotal: item.subtotal,
    })),
    buyer: {
      name: api.buyer?.name ?? '',
      nickname: api.buyer?.nickname ?? '',
      email: api.buyer?.email ?? '',
      phone: api.buyer?.phone ?? '',
      totalOrders: 0,
      totalSpent: 0,
    },
    shipping: {
      trackingNumber: api.shipping?.trackingNumber ?? '',
      trackingUrl: api.shipping?.trackingUrl ?? '',
      carrier: api.shipping?.carrier ?? '',
      logisticType: (api.shipping?.logisticType as LogisticType) ?? 'Coleta',
      shippingStatus: api.shipping?.shippingStatus ?? '',
      timeline,
    },
    payment: {
      method: api.payment?.method ?? '',
      installments: api.payment?.installments ?? 1,
      amount: api.payment?.amount ?? api.totalAmount ?? 0,
      status: api.payment?.status ?? '',
      statusVariant: mapPaymentStatusToVariant(api.payment?.status),
    },
    revenue: api.revenue ?? api.totalAmount ?? 0,
    costs,
  };
}

@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, BaseChartDirective, BadgeComponent, ButtonComponent, FormFieldComponent, FormActionsComponent, PageHeaderComponent, FormsModule, ReactiveFormsModule],
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss',
})
export class SaleDetailComponent implements OnInit, OnDestroy {
  // Icons
  readonly packageIcon = Package;
  readonly copyIcon = Copy;
  readonly truckIcon = Truck;
  readonly creditCardIcon = CreditCard;
  readonly userIcon = User;
  readonly mapPinIcon = MapPin;
  readonly clockIcon = Clock;
  readonly checkIcon = Check;
  readonly circleIcon = Circle;
  readonly plusIcon = Plus;
  readonly lockIcon = Lock;
  readonly unlockIcon = Unlock;
  readonly pencilIcon = Pencil;
  readonly trashIcon = Trash2;
  readonly xIcon = X;
  readonly searchIcon = Search;
  readonly chevronDownIcon = ChevronDown;
  readonly refreshCwIcon = RefreshCw;

  // Cost categories reference
  readonly costCategories = COST_CATEGORIES;

  // Available supplies
  readonly availableSupplies = MOCK_SUPPLIES;

  // Injected services
  private readonly orderService = inject(OrderService);
  private readonly toastService = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  // Subscriptions
  private subscriptions = new Subscription();

  // Core state
  loading = signal(true);
  order = signal<OrderDetailView | null>(null);
  private apiOrderId = ''; // The internal API id for API calls
  copySuccess = signal(false);
  recalculating = signal(false);
  orderId = '';

  // US-056: Manual cost CRUD — costs are now managed within order().costs
  showCostForm = signal(false);
  editingCostId = signal<string | null>(null);
  costForm!: FormGroup;
  savingCost = signal(false);

  // US-057: Lock after "Enviado"
  isLocked = computed(() => {
    const o = this.order();
    if (!o) return false;
    const lockedStatuses: OrderStatus[] = ['Enviado', 'Entregue'];
    return lockedStatuses.includes(o.status) && !this.lockOverridden();
  });
  lockOverridden = signal(false);

  // US-061: Supplies
  saleSupplies = signal<SaleSupply[]>([]);
  showSupplyForm = signal(false);
  supplySearchQuery = signal('');
  selectedSupplyId = signal<string | null>(null);
  supplyQuantity = signal(1);
  showSupplyDropdown = signal(false);

  filteredSupplies = computed(() => {
    const query = this.supplySearchQuery().toLowerCase();
    if (!query) return this.availableSupplies;
    return this.availableSupplies.filter(s => s.name.toLowerCase().includes(query));
  });

  totalSuppliesCost = computed(() => {
    return this.saleSupplies().reduce((sum, s) => sum + s.total, 0);
  });

  // Combined costs: order costs + supplies packaging adjustment (filtered: no zero-value costs)
  allCosts = computed(() => {
    const o = this.order();
    if (!o) return [];
    const baseCosts: CostItem[] = [...o.costs];
    const suppliesCost = this.totalSuppliesCost();

    // Add supplies cost as packaging line if > 0
    if (suppliesCost > 0) {
      baseCosts.push({
        id: '__supplies_packaging__',
        category: 'Suprimentos (embalagem)',
        categoryKey: 'packaging',
        value: suppliesCost,
        color: '#FF9800',
        source: 'Calculado' as CostSource,
      });
    }

    // US-022b: Filter out zero-value costs
    return baseCosts.filter(c => c.value > 0);
  });

  // Computeds using allCosts
  itemsSubtotal = computed(() => {
    const o = this.order();
    if (!o) return 0;
    return o.items.reduce((sum, item) => sum + item.subtotal, 0);
  });

  totalCosts = computed(() => {
    return this.allCosts().reduce((sum, c) => sum + c.value, 0);
  });

  netProfit = computed(() => {
    const o = this.order();
    if (!o) return 0;
    return o.revenue - this.totalCosts();
  });

  profitMargin = computed(() => {
    const o = this.order();
    if (!o || o.revenue === 0) return 0;
    return (this.netProfit() / o.revenue) * 100;
  });

  costBarSegments = computed(() => {
    const o = this.order();
    if (!o || o.revenue === 0) return [];
    const costs = this.allCosts();
    const segments = costs.map(c => ({
      category: c.category,
      value: c.value,
      color: c.color,
      pct: (c.value / o.revenue) * 100,
    }));
    const profit = this.netProfit();
    if (profit > 0) {
      segments.push({
        category: 'Lucro Líquido',
        value: profit,
        color: 'var(--success)',
        pct: (profit / o.revenue) * 100,
      });
    }
    return segments;
  });

  // US-022b: Pie chart for cost breakdown
  costPieChartData = computed<ChartData<'doughnut'>>(() => {
    const costs = this.allCosts();
    return {
      labels: costs.map(c => c.category),
      datasets: [{
        data: costs.map(c => c.value),
        backgroundColor: costs.map(c => c.color),
        borderWidth: 0,
        hoverOffset: 6,
      }],
    };
  });

  costPieTotal = computed(() => this.totalCosts());

  costPieCenterPlugin: Plugin<'doughnut'> = {
    id: 'costPieCenterText',
    afterDraw: (chart) => {
      const { ctx, width, height } = chart;
      ctx.save();
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';

      const centerX = width / 2;
      const centerY = height / 2;

      ctx.font = '500 12px Inter';
      ctx.fillStyle = '#757575';
      ctx.fillText('Total Custos', centerX, centerY - 12);

      const formatted = this.costPieTotal().toLocaleString('pt-BR', {
        style: 'currency',
        currency: 'BRL',
      });
      ctx.font = '700 16px Roboto Mono';
      ctx.fillStyle = '#212121';
      ctx.fillText(formatted, centerX, centerY + 12);

      ctx.restore();
    },
  };

  costPieChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    cutout: '65%',
    plugins: {
      legend: {
        display: false, // We already have the table as legend
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleFont: { family: 'Inter', size: 13 },
        bodyFont: { family: 'Roboto Mono', size: 12 },
        padding: 12,
        callbacks: {
          label: (ctx) => {
            const value = ctx.parsed ?? 0;
            const total = (ctx.dataset.data as number[]).reduce((sum, v) => sum + v, 0);
            const pct = ((value / total) * 100).toFixed(1);
            const formatted = value.toLocaleString('pt-BR', {
              style: 'currency',
              currency: 'BRL',
            });
            return `${formatted} (${pct}%)`;
          },
        },
      },
    },
  };

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
  ) {
    this.costForm = this.fb.group({
      categoryKey: ['', Validators.required],
      description: ['', Validators.required],
      value: [null as number | null, [Validators.required, Validators.min(0.01)]],
    });
  }

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.orderId) {
      this.loadOrder(this.orderId);
    } else {
      this.loading.set(false);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  private loadOrder(id: string): void {
    this.loading.set(true);
    const sub = this.orderService.getById(id).subscribe({
      next: (apiOrder) => {
        this.apiOrderId = apiOrder.id;
        this.order.set(mapApiToView(apiOrder));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.show('Erro ao carregar detalhes do pedido', 'danger');
      },
    });
    this.subscriptions.add(sub);
  }

  // --- Formatting helpers ---

  formatBrl = formatBrlUtil;

  formatDate(dateStr: string): string {
    return formatDateUtil(dateStr, true);
  }

  getShippingStatusVariant(status: string): BadgeVariant {
    const map: Record<string, BadgeVariant> = {
      'Pendente': 'neutral',
      'Em preparação': 'warning',
      'Em trânsito': 'primary',
      'Entregue': 'success',
      'Devolvido': 'danger',
      'Não entregue': 'danger',
      'Cancelado': 'danger',
    };
    return map[status] ?? 'neutral';
  }

  getLogisticVariant(type: LogisticType): BadgeVariant {
    const map: Record<LogisticType, BadgeVariant> = {
      'Full': 'success',
      'Coleta': 'primary',
      'Agência': 'warning',
    };
    return map[type];
  }

  getSourceVariant(source: CostSource): BadgeVariant {
    if (source === 'API') return 'primary';
    if (source === 'Manual') return 'accent';
    return 'success'; // Calculated / Calculado
  }

  getSourceLabel(source: CostSource): string {
    if (source === 'API') return 'Fonte: API';
    if (source === 'Manual') return 'Fonte: Manual';
    return 'Fonte: Calculado';
  }

  costPct(value: number): number {
    const o = this.order();
    if (!o || o.revenue === 0) return 0;
    return (value / o.revenue) * 100;
  }

  copyTracking(): void {
    const tracking = this.order()?.shipping.trackingNumber;
    if (tracking) {
      navigator.clipboard.writeText(tracking);
      this.copySuccess.set(true);
      setTimeout(() => this.copySuccess.set(false), 2000);
    }
  }

  // --- US-056: Manual Cost CRUD ---

  getCategoryLabel(key: string): string {
    return COST_CATEGORIES.find(c => c.value === key)?.label ?? key;
  }

  getCategoryColor(key: string): string {
    return COST_CATEGORIES.find(c => c.value === key)?.color ?? '#BDBDBD';
  }

  openCostForm(): void {
    this.costForm.reset();
    this.editingCostId.set(null);
    this.showCostForm.set(true);
  }

  editManualCost(cost: CostItem): void {
    if (this.isLocked()) return;
    this.editingCostId.set(cost.id ?? null);
    this.costForm.patchValue({
      categoryKey: cost.categoryKey,
      description: cost.description,
      value: cost.value,
    });
    this.showCostForm.set(true);
  }

  cancelCostForm(): void {
    this.showCostForm.set(false);
    this.editingCostId.set(null);
    this.costForm.reset();
  }

  saveCost(): void {
    if (this.costForm.invalid || !this.apiOrderId) return;

    const { categoryKey, description, value } = this.costForm.value;
    const editId = this.editingCostId();

    this.savingCost.set(true);

    if (editId) {
      // Update existing cost via API
      const sub = this.orderService.updateCost(this.apiOrderId, editId, {
        category: categoryKey,
        description,
        value: Number(value),
      }).subscribe({
        next: (response) => {
          const mapped = mapCostCategory(response.category);
          const updatedCost: CostItem = {
            id: response.id,
            category: mapped.label,
            categoryKey: mapped.key,
            description: response.description,
            value: response.value,
            color: mapped.color,
            source: response.source as CostSource,
          };
          const current = this.order();
          if (current) {
            this.order.set({
              ...current,
              costs: current.costs.map(c => c.id === editId ? updatedCost : c),
            });
          }
          this.savingCost.set(false);
          this.cancelCostForm();
        },
        error: () => {
          this.savingCost.set(false);
          this.toastService.show('Erro ao atualizar custo', 'danger');
        },
      });
      this.subscriptions.add(sub);
    } else {
      // Add new cost via API
      const sub = this.orderService.addCost(this.apiOrderId, {
        category: categoryKey,
        description,
        value: Number(value),
      }).subscribe({
        next: (response) => {
          const mapped = mapCostCategory(response.category);
          const newCost: CostItem = {
            id: response.id,
            category: mapped.label,
            categoryKey: mapped.key,
            description: response.description,
            value: response.value,
            color: mapped.color,
            source: response.source as CostSource,
          };
          const current = this.order();
          if (current) {
            this.order.set({
              ...current,
              costs: [...current.costs, newCost],
            });
          }
          this.savingCost.set(false);
          this.cancelCostForm();
        },
        error: () => {
          this.savingCost.set(false);
          this.toastService.show('Erro ao adicionar custo', 'danger');
        },
      });
      this.subscriptions.add(sub);
    }
  }

  async deleteCost(costId: string): Promise<void> {
    const currentOrder = this.order();
    if (!currentOrder) return;
    const cost = currentOrder.costs.find(c => c.id === costId);
    const confirmed = await this.confirmDialog.confirm({
      title: 'Excluir custo',
      message: `Deseja remover o custo "${cost?.description || cost?.category || ''}"?`,
      confirmLabel: 'Excluir',
      variant: 'danger',
    });
    if (!confirmed) return;

    const sub = this.orderService.deleteCost(this.apiOrderId, costId).subscribe({
      next: () => {
        const current = this.order();
        if (current) {
          this.order.set({
            ...current,
            costs: current.costs.filter(c => c.id !== costId),
          });
        }
        this.confirmDialog.done();
      },
      error: () => {
        this.confirmDialog.done();
        this.toastService.show('Erro ao excluir custo', 'danger');
      },
    });
    this.subscriptions.add(sub);
  }

  isManualCost(cost: CostItem): boolean {
    return cost.source === 'Manual';
  }

  // --- US-057: Lock ---

  async requestUnlock(): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Desbloquear edição?',
      message: 'Você está prestes a desbloquear uma venda já enviada. Alterações manuais podem causar inconsistências com os dados do marketplace.',
      confirmLabel: 'Confirmar desbloqueio',
      variant: 'danger',
    });
    if (confirmed) {
      this.lockOverridden.set(true);
    }
    this.confirmDialog.done();
  }

  // --- US-061: Supplies ---

  openSupplyForm(): void {
    this.showSupplyForm.set(true);
    this.supplySearchQuery.set('');
    this.selectedSupplyId.set(null);
    this.supplyQuantity.set(1);
  }

  cancelSupplyForm(): void {
    this.showSupplyForm.set(false);
    this.selectedSupplyId.set(null);
    this.supplySearchQuery.set('');
    this.supplyQuantity.set(1);
    this.showSupplyDropdown.set(false);
  }

  selectSupply(supply: Supply): void {
    this.selectedSupplyId.set(supply.id);
    this.supplySearchQuery.set(supply.name);
    this.showSupplyDropdown.set(false);
  }

  onSupplySearchFocus(): void {
    this.showSupplyDropdown.set(true);
  }

  onSupplySearchBlur(): void {
    // Delay to allow click on dropdown item
    setTimeout(() => this.showSupplyDropdown.set(false), 200);
  }

  onSupplySearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.supplySearchQuery.set(value);
    this.showSupplyDropdown.set(true);
    // Clear selection if user modifies search text
    const selected = this.availableSupplies.find(s => s.id === this.selectedSupplyId());
    if (selected && selected.name !== value) {
      this.selectedSupplyId.set(null);
    }
  }

  onSupplyQtyChange(event: Event): void {
    const value = parseInt((event.target as HTMLInputElement).value, 10);
    this.supplyQuantity.set(isNaN(value) || value < 1 ? 1 : value);
  }

  getSelectedSupply(): Supply | undefined {
    return this.availableSupplies.find(s => s.id === this.selectedSupplyId());
  }

  addSupply(): void {
    const supply = this.getSelectedSupply();
    if (!supply) return;
    const qty = this.supplyQuantity();

    const existing = this.saleSupplies();
    const found = existing.find(s => s.supplyId === supply.id);

    if (found) {
      // Update quantity
      const newQty = found.quantity + qty;
      this.saleSupplies.set(existing.map(s =>
        s.supplyId === supply.id
          ? { ...s, quantity: newQty, total: newQty * s.unitCost }
          : s
      ));
    } else {
      this.saleSupplies.set([...existing, {
        supplyId: supply.id,
        name: supply.name,
        quantity: qty,
        unitCost: supply.unitCost,
        total: qty * supply.unitCost,
      }]);
    }

    this.cancelSupplyForm();
  }

  async removeSupply(supplyId: string): Promise<void> {
    const supply = this.saleSupplies().find(s => s.supplyId === supplyId);
    const confirmed = await this.confirmDialog.confirm({
      title: 'Remover suprimento',
      message: `Deseja remover o suprimento "${supply?.name || ''}"?`,
      confirmLabel: 'Remover',
      variant: 'danger',
    });
    if (!confirmed) return;
    this.saleSupplies.set(this.saleSupplies().filter(s => s.supplyId !== supplyId));
    this.confirmDialog.done();
  }

  // --- Recalculate Costs ---

  recalculateCosts(): void {
    if (this.recalculating() || !this.apiOrderId) return;

    this.recalculating.set(true);
    const sub = this.orderService.recalculateCosts(this.apiOrderId).subscribe({
      next: () => {
        // Re-fetch order detail from API to get updated costs
        this.orderService.getById(this.orderId).subscribe({
          next: (apiOrder) => {
            this.apiOrderId = apiOrder.id;
            this.order.set(mapApiToView(apiOrder));
            this.recalculating.set(false);
            this.toastService.show('Custos recalculados com sucesso', 'success');
          },
          error: () => {
            this.recalculating.set(false);
            this.toastService.show('Custos recalculados, mas erro ao recarregar', 'warning');
          },
        });
      },
      error: () => {
        this.recalculating.set(false);
        this.toastService.show('Erro ao recalcular custos', 'danger');
      },
    });
    this.subscriptions.add(sub);
  }
}
