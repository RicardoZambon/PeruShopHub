import { Component, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, User, Mail, Phone, MapPin } from 'lucide-angular';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { BrlCurrencyPipe } from '../../shared/pipes';

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

const MOCK_CUSTOMERS: Record<string, CustomerDetail> = {
  'C001': {
    id: 'C001', name: 'Maria Silva Santos', email: 'maria.silva@email.com',
    phone: '(11) 99999-1234', cpf: '123.456.789-00',
    address: 'Rua das Flores, 123 - São Paulo/SP',
    totalOrders: 12, totalSpent: 4589.80,
    firstOrder: '2025-08-15', lastOrder: '2026-03-22',
  },
  'C002': {
    id: 'C002', name: 'João Pedro Oliveira', email: 'joao.pedro@email.com',
    phone: '(21) 98888-5678', cpf: '987.654.321-00',
    address: 'Av. Copacabana, 456 - Rio de Janeiro/RJ',
    totalOrders: 8, totalSpent: 3210.50,
    firstOrder: '2025-10-02', lastOrder: '2026-03-21',
  },
  'C003': {
    id: 'C003', name: 'Ana Carolina Ferreira', email: 'ana.carolina@email.com',
    phone: '(31) 97777-9012', cpf: '456.789.123-00',
    address: 'Rua dos Mineiros, 789 - Belo Horizonte/MG',
    totalOrders: 15, totalSpent: 6780.20,
    firstOrder: '2025-06-20', lastOrder: '2026-03-20',
  },
  'C004': {
    id: 'C004', name: 'Carlos Eduardo Lima', email: 'carlos.lima@email.com',
    phone: '(41) 96666-3456', cpf: '321.654.987-00',
    address: 'Rua XV de Novembro, 321 - Curitiba/PR',
    totalOrders: 3, totalSpent: 879.70,
    firstOrder: '2026-01-10', lastOrder: '2026-03-19',
  },
  'C005': {
    id: 'C005', name: 'Fernanda Costa Souza', email: 'fernanda.costa@email.com',
    phone: '(51) 95555-7890', cpf: '654.321.987-00',
    address: 'Av. Ipiranga, 654 - Porto Alegre/RS',
    totalOrders: 22, totalSpent: 9450.30,
    firstOrder: '2025-04-12', lastOrder: '2026-03-22',
  },
  'C006': {
    id: 'C006', name: 'Rafael Almeida Gomes', email: 'rafael.gomes@email.com',
    phone: '(85) 94444-1234', cpf: '789.123.456-00',
    address: 'Rua José Avelino, 987 - Fortaleza/CE',
    totalOrders: 6, totalSpent: 2340.60,
    firstOrder: '2025-09-05', lastOrder: '2026-03-18',
  },
  'C007': {
    id: 'C007', name: 'Juliana Ribeiro Martins', email: 'juliana.martins@email.com',
    phone: '(71) 93333-5678', cpf: '147.258.369-00',
    address: 'Rua Chile, 147 - Salvador/BA',
    totalOrders: 4, totalSpent: 1560.40,
    firstOrder: '2025-11-18', lastOrder: '2026-03-17',
  },
  'C008': {
    id: 'C008', name: 'Lucas Mendes Pereira', email: 'lucas.mendes@email.com',
    phone: '(62) 92222-9012', cpf: '258.369.147-00',
    address: 'Av. Goiás, 258 - Goiânia/GO',
    totalOrders: 1, totalSpent: 49.90,
    firstOrder: '2026-03-16', lastOrder: '2026-03-16',
  },
  'C009': {
    id: 'C009', name: 'Beatriz Rodrigues Nunes', email: 'beatriz.nunes@email.com',
    phone: '(27) 91111-3456', cpf: '369.147.258-00',
    address: 'Rua Sete, 369 - Vitória/ES',
    totalOrders: 9, totalSpent: 4120.70,
    firstOrder: '2025-07-30', lastOrder: '2026-03-15',
  },
  'C010': {
    id: 'C010', name: 'Thiago Nascimento Barbosa', email: 'thiago.barbosa@email.com',
    phone: '(91) 90000-7890', cpf: '741.852.963-00',
    address: 'Av. Presidente Vargas, 741 - Belém/PA',
    totalOrders: 5, totalSpent: 1890.40,
    firstOrder: '2025-12-01', lastOrder: '2026-03-14',
  },
};

const MOCK_ORDERS: Record<string, CustomerOrder[]> = {
  'C001': [
    { id: '2087654321', date: '2026-03-22', value: 389.80, status: 'Entregue' },
    { id: '2087654290', date: '2026-03-15', value: 259.90, status: 'Entregue' },
    { id: '2087654245', date: '2026-03-01', value: 479.70, status: 'Entregue' },
    { id: '2087654200', date: '2026-02-18', value: 159.90, status: 'Entregue' },
    { id: '2087654150', date: '2026-02-05', value: 329.90, status: 'Entregue' },
  ],
  'C002': [
    { id: '2087654320', date: '2026-03-21', value: 259.90, status: 'Enviado' },
    { id: '2087654280', date: '2026-03-10', value: 449.90, status: 'Entregue' },
    { id: '2087654230', date: '2026-02-25', value: 189.90, status: 'Entregue' },
    { id: '2087654180', date: '2026-02-10', value: 539.90, status: 'Entregue' },
    { id: '2087654120', date: '2026-01-28', value: 299.90, status: 'Cancelado' },
  ],
  'C003': [
    { id: '2087654315', date: '2026-03-20', value: 719.60, status: 'Entregue' },
    { id: '2087654270', date: '2026-03-08', value: 349.90, status: 'Entregue' },
    { id: '2087654220', date: '2026-02-22', value: 199.90, status: 'Devolvido' },
    { id: '2087654170', date: '2026-02-08', value: 589.90, status: 'Entregue' },
    { id: '2087654110', date: '2026-01-25', value: 419.90, status: 'Entregue' },
  ],
  'C004': [
    { id: '2087654318', date: '2026-03-19', value: 129.90, status: 'Pago' },
    { id: '2087654260', date: '2026-03-05', value: 449.90, status: 'Entregue' },
    { id: '2087654210', date: '2026-02-20', value: 299.90, status: 'Entregue' },
  ],
  'C005': [
    { id: '2087654317', date: '2026-03-22', value: 239.80, status: 'Entregue' },
    { id: '2087654250', date: '2026-03-03', value: 189.90, status: 'Entregue' },
    { id: '2087654195', date: '2026-02-18', value: 659.90, status: 'Entregue' },
    { id: '2087654140', date: '2026-02-02', value: 129.90, status: 'Entregue' },
    { id: '2087654090', date: '2026-01-20', value: 819.90, status: 'Entregue' },
  ],
  'C006': [
    { id: '2087654316', date: '2026-03-18', value: 189.90, status: 'Entregue' },
    { id: '2087654240', date: '2026-02-28', value: 349.90, status: 'Entregue' },
    { id: '2087654185', date: '2026-02-14', value: 499.90, status: 'Entregue' },
    { id: '2087654130', date: '2026-01-30', value: 269.90, status: 'Enviado' },
    { id: '2087654080', date: '2026-01-18', value: 159.90, status: 'Entregue' },
  ],
  'C007': [
    { id: '2087654313', date: '2026-03-17', value: 349.80, status: 'Entregue' },
    { id: '2087654235', date: '2026-02-27', value: 419.90, status: 'Entregue' },
    { id: '2087654175', date: '2026-02-12', value: 299.90, status: 'Entregue' },
    { id: '2087654115', date: '2026-01-26', value: 490.80, status: 'Devolvido' },
  ],
  'C008': [
    { id: '2087654314', date: '2026-03-16', value: 49.90, status: 'Devolvido' },
  ],
  'C009': [
    { id: '2087654309', date: '2026-03-15', value: 409.80, status: 'Entregue' },
    { id: '2087654255', date: '2026-03-04', value: 289.90, status: 'Entregue' },
    { id: '2087654190', date: '2026-02-16', value: 579.90, status: 'Entregue' },
    { id: '2087654135', date: '2026-02-01', value: 349.90, status: 'Entregue' },
    { id: '2087654075', date: '2026-01-17', value: 199.90, status: 'Entregue' },
  ],
  'C010': [
    { id: '2087654307', date: '2026-03-14', value: 339.80, status: 'Entregue' },
    { id: '2087654248', date: '2026-03-02', value: 199.90, status: 'Pago' },
    { id: '2087654183', date: '2026-02-15', value: 449.90, status: 'Entregue' },
    { id: '2087654125', date: '2026-01-29', value: 289.90, status: 'Entregue' },
    { id: '2087654070', date: '2026-01-16', value: 610.90, status: 'Entregue' },
  ],
};

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

  kpis = computed(() => {
    const c = this.customer();
    if (!c) return [];
    return [
      { label: 'Total Pedidos', value: String(c.totalOrders) },
      { label: 'Total Gasto', value: this.formatBrl(c.totalSpent) },
      { label: 'Primeiro Pedido', value: this.formatDate(c.firstOrder) },
      { label: 'Último Pedido', value: this.formatDate(c.lastOrder) },
    ];
  });

  private customerId = '';

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id') || 'C001';
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    setTimeout(() => {
      const customerData = MOCK_CUSTOMERS[this.customerId] || MOCK_CUSTOMERS['C001'];
      this.customer.set(customerData);
      this.recentOrders.set(MOCK_ORDERS[customerData.id] || []);
      this.loading.set(false);
    }, 600);
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
