import { Component, signal, computed, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, User, Mail, Phone, MapPin } from 'lucide-angular';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { CustomerService } from '../../services/customer.service';
import { ToastService } from '../../services/toast.service';
import { formatBrl, formatDateShort, getOrderStatusVariant } from '../../shared/utils';

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
  imports: [CommonModule, RouterLink, LucideAngularModule, KpiCardComponent, BadgeComponent, BrlCurrencyPipe, PageHeaderComponent],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss',
})
export class CustomerDetailComponent implements OnInit {
  readonly UserIcon = User;
  readonly MailIcon = Mail;
  readonly PhoneIcon = Phone;
  readonly MapPinIcon = MapPin;

  loading = signal(true);
  customer = signal<CustomerDetail | null>(null);
  recentOrders = signal<CustomerOrder[]>([]);

  private readonly customerService = inject(CustomerService);
  private readonly toastService = inject(ToastService);

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

      const orders: CustomerOrder[] = response.recentOrders.map((order: any) => ({
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
      this.toastService.show('Erro ao carregar detalhes do cliente', 'danger');
    } finally {
      this.loading.set(false);
    }
  }

  formatBrl = formatBrl;
  formatDate = formatDateShort;

  getStatusVariant = getOrderStatusVariant;
}
