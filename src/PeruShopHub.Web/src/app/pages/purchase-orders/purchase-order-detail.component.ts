import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { LucideAngularModule, Package, CheckCircle } from 'lucide-angular';
import { BadgeComponent } from '../../shared/components/badge/badge.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { PageSkeletonComponent } from '../../shared/components/page-skeleton/page-skeleton.component';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { BrlCurrencyPipe } from '../../shared/pipes';
import { PurchaseOrderService, type PurchaseOrderDetail } from '../../services/purchase-order.service';
import { ToastService } from '../../services/toast.service';
import { formatBrl, formatDate as formatDateUtil, getPurchaseOrderStatusVariant } from '../../shared/utils';

@Component({
  selector: 'app-purchase-order-detail',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, BadgeComponent, BrlCurrencyPipe, ButtonComponent, PageSkeletonComponent, PageHeaderComponent],
  templateUrl: './purchase-order-detail.component.html',
  styleUrl: './purchase-order-detail.component.scss',
})
export class PurchaseOrderDetailComponent implements OnInit {
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

  getStatusVariant = getPurchaseOrderStatusVariant;

  formatDate(dateStr: string | null): string {
    return formatDateUtil(dateStr, true);
  }

  formatBrl = formatBrl;

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
