import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, Copy, Truck, CreditCard, User, MapPin, Clock, Check, Circle } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';
type LogisticType = 'Full' | 'Coleta' | 'Agência';

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

interface OrderDetail {
  id: string;
  date: string;
  status: OrderStatus;
  statusVariant: BadgeVariant;
  items: OrderItem[];
  buyer: Buyer;
  shipping: ShippingInfo;
  payment: PaymentInfo;
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
};

@Component({
  selector: 'app-venda-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, BadgeComponent],
  templateUrl: './venda-detail.component.html',
  styleUrl: './venda-detail.component.scss',
})
export class VendaDetailComponent implements OnInit {
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

  loading = signal(true);
  order = signal<OrderDetail | null>(null);
  copySuccess = signal(false);
  orderId = '';

  itemsSubtotal = computed(() => {
    const o = this.order();
    if (!o) return 0;
    return o.items.reduce((sum, item) => sum + item.subtotal, 0);
  });

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
