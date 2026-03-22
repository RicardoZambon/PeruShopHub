import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Copy, Truck, CreditCard, User, MapPin, Clock, Check, Circle, Plus } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

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
  category: string;
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
  imports: [CommonModule, RouterLink, LucideAngularModule, BadgeComponent],
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss',
})
export class SaleDetailComponent implements OnInit {
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

  loading = signal(true);
  order = signal<OrderDetail | null>(null);
  copySuccess = signal(false);
  orderId = '';

  itemsSubtotal = computed(() => {
    const o = this.order();
    if (!o) return 0;
    return o.items.reduce((sum, item) => sum + item.subtotal, 0);
  });

  totalCosts = computed(() => {
    const o = this.order();
    if (!o) return 0;
    return o.costs.reduce((sum, c) => sum + c.value, 0);
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
    const segments = o.costs.map(c => ({
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

  getSourceVariant(source: CostSource): BadgeVariant {
    const map: Record<CostSource, BadgeVariant> = {
      'API': 'primary',
      'Manual': 'warning',
      'Calculado': 'success',
    };
    return map[source];
  }

  costPct(value: number): number {
    const o = this.order();
    if (!o || o.revenue === 0) return 0;
    return (value / o.revenue) * 100;
  }

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id') ?? '';
    setTimeout(() => {
      this.order.set({ ...MOCK_ORDER, id: this.orderId || MOCK_ORDER.id });
      this.loading.set(false);
    }, 600);
  }

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

  copyTracking(): void {
    const tracking = this.order()?.shipping.trackingNumber;
    if (tracking) {
      navigator.clipboard.writeText(tracking);
      this.copySuccess.set(true);
      setTimeout(() => this.copySuccess.set(false), 2000);
    }
  }
}
