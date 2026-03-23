import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, User, Mail, Phone, MapPin } from 'lucide-angular';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { CustomerService } from '../../services/customer.service';

type OrderStatus = 'Pago' | 'Enviado' | 'Entregue' | 'Cancelado' | 'Devolvido';

interface CustomerDetail {
  id: string;
  name: string;
  email: string;
  phone: string;
  cpf: string;
  address: string;
  totalOrders: number;
  totalSpent: number;
  firstOrder: string;
  lastOrder: string;
}

interface CustomerOrder {
  id: string;
  date: string;
  value: number;
  status: OrderStatus;
}

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, KpiCardComponent, BadgeComponent, BrlCurrencyPipe],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss',
})
export class CustomerDetailComponent implements OnInit {
  readonly ArrowLeft = ArrowLeft;
  readonly UserIcon = User;
  readonly MailIcon = Mail;
  readonly PhoneIcon = Phone;
  readonly MapPinIcon = MapPin;

  loading = signal(true);
  customer = signal<CustomerDetail | null>(null);
  recentOrders = signal<CustomerOrder[]>([]);

  private readonly customerService = inject(CustomerService);

  kpis = computed(() => {
    const c = this.customer();
    if (!c) return [];
    return [
      { label: 'Total Pedidos', value: String(c.totalOrders) },
      { label: 'Total Gasto', value: this.formatBrl(c.totalSpent) },
      { label: 'Primeiro Pedido', value: this.formatDate(c.firstOrder) },
      { label: '\u00daltimo Pedido', value: this.formatDate(c.lastOrder) },
    ];
  });

  private customerId = '';

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id') || '';
    this.loadData();
  }

  private async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.customerService.getById(this.customerId);

      const customerData: CustomerDetail = {
        id: response.id,
        name: response.name,
        email: response.email ?? '',
        phone: response.phone ?? '',
        cpf: '',
        address: '',
        totalOrders: response.totalOrders,
        totalSpent: response.totalSpent,
        firstOrder: response.createdAt,
        lastOrder: response.lastPurchase ?? response.createdAt,
      };

      const orders: CustomerOrder[] = response.recentOrders.map(order => ({
        id: order.externalOrderId || order.id,
        date: order.orderDate,
        value: order.totalAmount,
        status: order.status as OrderStatus,
      }));

      this.customer.set(customerData);
      this.recentOrders.set(orders);
    } catch {
      this.customer.set(null);
      this.recentOrders.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', {
      style: 'currency',
      currency: 'BRL',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getStatusVariant(status: OrderStatus): BadgeVariant {
    switch (status) {
      case 'Pago': return 'primary';
      case 'Enviado': return 'warning';
      case 'Entregue': return 'success';
      case 'Cancelado': return 'danger';
      case 'Devolvido': return 'neutral';
    }
  }
}
