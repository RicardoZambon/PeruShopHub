import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  LucideAngularModule, ArrowLeft, Package, Copy, Truck, CreditCard,
  User, MapPin, Clock, Check, Circle, Plus, Lock, Unlock, Pencil,
  Trash2, X, Search, ChevronDown, RefreshCw
} from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { FormFieldComponent } from '../../shared/components/form-field/form-field.component';
import { FormActionsComponent } from '../../shared/components/form-actions/form-actions.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { OrderService } from '../../services/order.service';
import { ToastService } from '../../services/toast.service';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';
type LogisticType = 'Full' | 'Coleta' | 'Agência';
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

const MOCK_ORDER: OrderDetail = {
  id: '2087654321',
  date: '2026-03-22T14:30:00',
  status: 'Enviado',
  statusVariant: 'warning',
  items: [
    {
      productId: 'MLB-001',
      name: 'Fone Bluetooth TWS Pro Max',
      sku: 'FN-BT-PRO-001',
      variation: 'Preto',
      quantity: 1,
      unitPrice: 159.90,
      subtotal: 159.90,
    },
    {
      productId: 'MLB-002',
      name: 'Capa Protetora Case Slim',
      sku: 'CP-SL-001',
      variation: 'Transparente',
      quantity: 2,
      unitPrice: 29.90,
      subtotal: 59.80,
    },
  ],
  buyer: {
    name: 'Maria Silva Santos',
    nickname: 'MARI.SILVA',
    email: 'mar***@gmail.com',
    phone: '(11) 9****-4567',
    totalOrders: 8,
    totalSpent: 2340.60,
  },
  shipping: {
    trackingNumber: 'BR123456789ML',
    carrier: 'Mercado Envios - CORREIOS',
    logisticType: 'Coleta',
    timeline: [
      { label: 'Pedido criado', date: '22/03/2026, 14:30', completed: true },
      { label: 'Pagamento aprovado', date: '22/03/2026, 14:32', completed: true },
      { label: 'Enviado', date: '22/03/2026, 18:45', completed: true },
      { label: 'Entregue', date: null, completed: false },
    ],
  },
  payment: {
    method: 'Cartão de Crédito',
    installments: 3,
    amount: 219.70,
    status: 'Aprovado',
    statusVariant: 'success',
  },
  revenue: 159.90,
  costs: [
    { category: 'Comissão ML', value: 17.59, color: '#5C6BC0', source: 'API' },
    { category: 'Taxa fixa', value: 6.00, color: '#7986CB', source: 'API' },
    { category: 'Frete vendedor', value: 18.90, color: '#42A5F5', source: 'API' },
    { category: 'Taxa de pagamento', value: 7.52, color: '#64B5F6', source: 'API' },
    { category: 'Custo do produto', value: 45.00, color: '#66BB6A', source: 'Manual' },
    { category: 'Embalagem', value: 3.50, color: '#FFA726', source: 'Manual' },
    { category: 'Impostos', value: 9.59, color: '#EF5350', source: 'Calculado' },
    { category: 'Armazenagem', value: 2.40, color: '#AB47BC', source: 'Calculado' },
    { category: 'Advertising', value: 8.00, color: '#26C6DA', source: 'Manual' },
  ],
};

@Component({
  selector: 'app-sale-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, BadgeComponent, ButtonComponent, FormFieldComponent, FormActionsComponent, FormsModule, ReactiveFormsModule],
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
  readonly refreshCwIcon = RefreshCw;

  // Cost categories reference
  readonly costCategories = COST_CATEGORIES;

  // Available supplies
  readonly availableSupplies = MOCK_SUPPLIES;

  // Injected services
  private readonly orderService = inject(OrderService);
  private readonly toastService = inject(ToastService);

  // Core state
  loading = signal(true);
  order = signal<OrderDetail | null>(null);
  copySuccess = signal(false);
  recalculating = signal(false);
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
        category: 'Lucro Líquido',
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
    setTimeout(() => {
      this.order.set({ ...MOCK_ORDER, id: this.orderId || MOCK_ORDER.id });
      this.loading.set(false);
    }, 600);
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
      'Agência': 'warning',
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

  // --- Recalculate Costs ---

  recalculateCosts(): void {
    if (this.recalculating() || !this.orderId) return;

    this.recalculating.set(true);
    this.orderService.recalculateCosts(this.orderId).subscribe({
      next: () => {
        // Reload order detail (mock for now — in real app this would refetch from API)
        this.loading.set(true);
        setTimeout(() => {
          this.order.set({ ...MOCK_ORDER, id: this.orderId || MOCK_ORDER.id });
          this.loading.set(false);
          this.recalculating.set(false);
          this.toastService.show('Custos recalculados com sucesso', 'success');
        }, 600);
      },
      error: () => {
        this.recalculating.set(false);
        this.toastService.show('Erro ao recalcular custos', 'danger');
      },
    });
  }
}
