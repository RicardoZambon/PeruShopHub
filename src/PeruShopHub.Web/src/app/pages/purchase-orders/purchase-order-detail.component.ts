import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideAngularModule, ArrowLeft, Package, CheckCircle } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import type { BadgeVariant } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { PageSkeletonComponent } from '../../shared/components/page-skeleton/page-skeleton.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PurchaseOrderService, type PurchaseOrderDetail } from '../../services/purchase-order.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-purchase-order-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, LucideAngularModule, BadgeComponent, BrlCurrencyPipe, ButtonComponent, PageSkeletonComponent],
  templateUrl: './purchase-order-detail.component.html',
  styleUrl: './purchase-order-detail.component.scss',
})
export class PurchaseOrderDetailComponent implements OnInit {
  readonly arrowLeftIcon = ArrowLeft;
  readonly packageIcon = Package;
  readonly checkCircleIcon = CheckCircle;

  private readonly route = inject(ActivatedRoute);
  private readonly poService = inject(PurchaseOrderService);
  private readonly toastService = inject(ToastService);

  readonly loading = signal(true);
  readonly receiving = signal(false);
  readonly order = signal<PurchaseOrderDetail | null>(null);

  orderId = '';

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadOrder();
  }

  private loadOrder(): void {
    this.loading.set(true);
    this.poService.getById(this.orderId).subscribe({
      next: (data) => {
        this.order.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.order.set(null);
        this.loading.set(false);
      },
    });
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'Rascunho': return 'neutral';
      case 'Recebido': return 'success';
      case 'Cancelado': return 'danger';
      default: return 'neutral';
    }
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '-';
    const date = new Date(dateStr);
    return date.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  formatBrl(value: number): string {
    return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  getMethodLabel(method: string): string {
    switch (method) {
      case 'Por Valor': return 'Por Valor';
      case 'Por Quantidade': return 'Por Quantidade';
      case 'Manual': return 'Manual';
      default: return method;
    }
  }

  receiveStock(): void {
    if (this.receiving()) return;
    this.receiving.set(true);

    this.poService.receive(this.orderId).subscribe({
      next: (updated) => {
        this.order.set(updated);
        this.toastService.show('Estoque recebido com sucesso!', 'success');
        this.receiving.set(false);
      },
      error: () => {
        this.toastService.show('Erro ao receber estoque.', 'danger');
        this.receiving.set(false);
      },
    });
  }
}
