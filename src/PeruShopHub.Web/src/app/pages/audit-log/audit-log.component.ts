import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule, Search, FileText, ArrowLeft } from 'lucide-angular';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { SelectDropdownComponent, type SelectOption } from '../../shared/components/select-dropdown/select-dropdown.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { AuditLogService, type AuditLogItem } from '../../services/audit-log.service';
import { ToastService } from '../../services/toast.service';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule, PageHeaderComponent, ButtonComponent, SelectDropdownComponent, EmptyStateComponent],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss',
})
export class AuditLogComponent implements OnInit {
  private readonly auditLogService = inject(AuditLogService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  readonly searchIcon = Search;
  readonly fileIcon = FileText;
  readonly backIcon = ArrowLeft;

  readonly loading = signal(true);
  readonly items = signal<AuditLogItem[]>([]);
  readonly totalCount = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = 20;

  readonly entityTypeFilter = signal('');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');

  readonly entityTypeOptions: SelectOption[] = [
    { value: '', label: 'Todos' },
    { value: 'Product', label: 'Produto' },
    { value: 'ProductVariant', label: 'Variante' },
    { value: 'Order', label: 'Pedido' },
    { value: 'PurchaseOrder', label: 'Pedido de Compra' },
    { value: 'CommissionRule', label: 'Regra de Comissão' },
  ];

  readonly totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  readonly hasData = computed(() => this.items().length > 0 || this.loading());

  ngOnInit(): void {
    this.loadData();
  }

  async loadData(): Promise<void> {
    this.loading.set(true);
    try {
      const result = await firstValueFrom(this.auditLogService.list({
        page: this.currentPage(),
        pageSize: this.pageSize,
        entityType: this.entityTypeFilter() || undefined,
        dateFrom: this.dateFrom() || undefined,
        dateTo: this.dateTo() || undefined,
      }));
      this.items.set(result.items);
      this.totalCount.set(result.totalCount);
    } catch {
      this.toastService.show('Erro ao carregar log de atividades', 'danger');
    } finally {
      this.loading.set(false);
    }
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadData();
  }

  clearFilters(): void {
    this.entityTypeFilter.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.applyFilters();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadData();
  }

  goBack(): void {
    this.router.navigate(['/configuracoes']);
  }

  formatEntityType(type: string): string {
    const map: Record<string, string> = {
      Product: 'Produto',
      ProductVariant: 'Variante',
      Order: 'Pedido',
      PurchaseOrder: 'Pedido de Compra',
      CommissionRule: 'Regra de Comissão',
    };
    return map[type] ?? type;
  }

  formatDate(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('pt-BR', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
    });
  }

  formatJson(json: string | null): string {
    if (!json) return '—';
    try {
      const obj = JSON.parse(json);
      return Object.entries(obj)
        .map(([k, v]) => `${k}: ${v}`)
        .join(', ');
    } catch {
      return json;
    }
  }
}
