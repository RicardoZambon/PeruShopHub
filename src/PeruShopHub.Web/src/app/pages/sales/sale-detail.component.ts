import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  LucideAngularModule, ArrowLeft, Package, Copy, Truck, CreditCard,
  User, MapPin, Clock, Check, Circle, Plus, Lock, Unlock, Pencil,
  Trash2, X, Search, ChevronDown
} from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { OrderService } from '../../services/order.service';
import type { SupplyItem } from '../../services/order.service';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';
type LogisticType = 'Full' | 'Coleta' | 'Ag\u00eancia';
type CostSource = 'API' | 'Manual' | 'Calculado';

interface OrderItem {
  productId: string;
  name: string;
  sku: string;
  variation: string;
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
  carrier: string;
  logisticType: LogisticType;
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

interface OrderDetail {
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
  { value: 'marketplace_commission', label: 'Comiss\u00e3o marketplace', color: '#5C6BC0' },
  { value: 'fixed_fee', label: 'Taxa fixa', color: '#7986CB' },
  { value: 'shipping_seller', label: 'Frete vendedor', color: '#42A5F5' },
  { value: 'payment_fee', label: 'Taxa pagamento', color: '#64B5F6' },
  { value: 'tax_icms', label: 'ICMS', color: '#EF5350' },
  { value: 'tax_pis_cofins', label: 'PIS/COFINS', color: '#E53935' },
  { value: 'storage_daily', label: 'Armazenagem di\u00e1ria', color: '#AB47BC' },
  { value: 'fulfillment_fee', label: 'Fulfillment', color: '#7E57C2' },
  { value: 'packaging', label: 'Embalagem', color: '#FFA726' },
  { value: 'advertising', label: 'Publicidade', color: '#26C6DA' },
  { value: 'other', label: 'Outros', color: '#BDBDBD' },
];

const COST_COLOR_MAP: Record<string, string> = {
  'Comiss\u00e3o ML': '#5C6BC0',
  'Comiss\u00e3o marketplace': '#5C6BC0',
  'Taxa fixa': '#7986CB',
  'Frete vendedor': '#42A5F5',
  'Taxa de pagamento': '#64B5F6',
  'Taxa pagamento': '#64B5F6',
  'Custo do produto': '#66BB6A',
  'Embalagem': '#FFA726',
  'Impostos': '#EF5350',
  'ICMS': '#EF5350',
  'PIS/COFINS': '#E53935',
  'Armazenagem': '#AB47BC',
  'Armazenagem di\u00e1ria': '#AB47BC',
  'Fulfillment': '#7E57C2',
  'Advertising': '#26C6DA',
  'Publicidade': '#26C6DA',
};

function getStatusVariant(status: string): BadgeVariant {
  switch (status) {
    case 'Pago': return 'primary';
    case 'Enviado': return 'warning';
    case 'Entregue': return 'success';
    case 'Cancelado': return 'danger';
    case 'Devolvido': return 'neutral';
    case 'Aprovado': return 'success';
    default: return 'neutral';
  }
}

@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, BadgeComponent, FormsModule, ReactiveFormsModule],
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss',
})
export class SaleDetailComponent implements OnInit {
  // Icons
  readonly arrowLeftIcon = ArrowLeft;
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

  // Cost categories reference
  readonly costCategories = COST_CATEGORIES;

  // Available supplies (loaded from API)
  availableSupplies: Supply[] = [];

  // Injected services
  private readonly orderService = inject(OrderService);

  // Core state
  loading = signal(true);
  order = signal<OrderDetail | null>(null);
  copySuccess = signal(false);
  orderId = '';

  // US-056: Manual cost CRUD
  manualCosts = signal<CostItem[]>([]);
  showCostForm = signal(false);
  editingCostId = signal<string | null>(null);
  costForm!: FormGroup;
  deleteConfirmCostId = signal<string | null>(null);

  // US-057: Lock after "Enviado"
  isLocked = computed(() => {
    const o = this.order();
    if (!o) return false;
    const lockedStatuses: OrderStatus[] = ['Enviado', 'Entregue'];
    return lockedStatuses.includes(o.status) && !this.lockOverridden();
  });
  lockOverridden = signal(false);
  showUnlockConfirm = signal(false);

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

  // Combined costs: order costs + manual costs + supplies packaging adjustment
  allCosts = computed(() => {
    const o = this.order();
    if (!o) return [];
    const baseCosts = [...o.costs];
    const manual = this.manualCosts();
    const suppliesCost = this.totalSuppliesCost();

    const combined = [...baseCosts, ...manual];

    // Add supplies cost as packaging line if > 0
    if (suppliesCost > 0) {
      combined.push({
        id: '__supplies_packaging__',
        category: 'Suprimentos (embalagem)',
        categoryKey: 'packaging',
        value: suppliesCost,
        color: '#FF9800',
        source: 'Calculado' as CostSource,
      });
    }

    return combined;
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
        category: 'Lucro L\u00edquido',
        value: profit,
        color: 'var(--success)',
        pct: (profit / o.revenue) * 100,
      });
    }
    return segments;
  });

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
    this.loadOrder();
    this.loadSupplies();
  }

  private async loadOrder(): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.orderService.getById(this.orderId);

      const orderDetail: OrderDetail = {
        id: response.externalOrderId || response.id,
        date: response.orderDate,
        status: response.status as OrderStatus,
        statusVariant: getStatusVariant(response.status),
        items: response.items.map((item: any) => ({
          productId: item.productId ?? item.id,
          name: item.name,
          sku: item.sku,
          variation: item.variation ?? '',
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          subtotal: item.subtotal,
        })),
        buyer: {
          name: response.buyer.name,
          nickname: response.buyer.nickname ?? '',
          email: response.buyer.email ?? '',
          phone: response.buyer.phone ?? '',
          totalOrders: 0,
          totalSpent: 0,
        },
        shipping: {
          trackingNumber: response.shipping.trackingNumber ?? '',
          carrier: response.shipping.carrier ?? '',
          logisticType: (response.shipping.logisticType as LogisticType) ?? 'Coleta',
          timeline: response.shipping.timeline?.map((step: any) => ({
            label: step.description ?? step.status,
            date: step.timestamp ? new Date(step.timestamp).toLocaleString('pt-BR') : null,
            completed: step.timestamp !== null,
          })) ?? [],
        },
        payment: {
          method: response.payment.method ?? '',
          installments: response.payment.installments ?? 1,
          amount: response.payment.amount ?? response.totalAmount,
          status: response.payment.status ?? '',
          statusVariant: getStatusVariant(response.payment.status ?? ''),
        },
        revenue: response.totalAmount,
        costs: response.costs.map((cost: any) => ({
          id: cost.id,
          category: cost.category,
          value: cost.value,
          color: COST_COLOR_MAP[cost.category] ?? '#BDBDBD',
          source: cost.source as CostSource,
          description: cost.description ?? undefined,
        })),
      };

      this.order.set(orderDetail);
    } catch {
      this.order.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  private async loadSupplies(): Promise<void> {
    try {
      this.availableSupplies = await this.orderService.getSupplies();
    } catch {
      this.availableSupplies = [];
    }
  }

  // --- Formatting helpers ---

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  getLogisticVariant(type: LogisticType): BadgeVariant {
    const map: Record<LogisticType, BadgeVariant> = {
      'Full': 'success',
      'Coleta': 'primary',
      'Ag\u00eancia': 'warning',
    };
    return map[type];
  }

  getSourceVariant(source: CostSource): BadgeVariant {
    const map: Record<CostSource, BadgeVariant> = {
      'API': 'primary',
      'Manual': 'accent',
      'Calculado': 'success',
    };
    return map[source];
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
    if (this.costForm.invalid) return;

    const { categoryKey, description, value } = this.costForm.value;
    const category = COST_CATEGORIES.find(c => c.value === categoryKey);

    const costItem: CostItem = {
      id: this.editingCostId() ?? `manual-${Date.now()}`,
      category: category?.label ?? categoryKey,
      categoryKey,
      description,
      value: Number(value),
      color: category?.color ?? '#BDBDBD',
      source: 'Manual',
    };

    const current = this.manualCosts();
    const editId = this.editingCostId();

    if (editId) {
      this.manualCosts.set(current.map(c => c.id === editId ? costItem : c));
    } else {
      this.manualCosts.set([...current, costItem]);
    }

    this.cancelCostForm();
  }

  confirmDeleteCost(costId: string): void {
    this.deleteConfirmCostId.set(costId);
  }

  cancelDeleteCost(): void {
    this.deleteConfirmCostId.set(null);
  }

  deleteCost(costId: string): void {
    this.manualCosts.set(this.manualCosts().filter(c => c.id !== costId));
    this.deleteConfirmCostId.set(null);
  }

  isManualCost(cost: CostItem): boolean {
    return cost.source === 'Manual' && !!cost.id && cost.id.startsWith('manual-');
  }

  // --- US-057: Lock ---

  requestUnlock(): void {
    this.showUnlockConfirm.set(true);
  }

  confirmUnlock(): void {
    this.lockOverridden.set(true);
    this.showUnlockConfirm.set(false);
  }

  cancelUnlock(): void {
    this.showUnlockConfirm.set(false);
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

  removeSupply(supplyId: string): void {
    this.saleSupplies.set(this.saleSupplies().filter(s => s.supplyId !== supplyId));
  }
}
