import { Component, inject, signal, computed, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { LucideAngularModule, AlertTriangle, Clock, Package, Send, X, Search, ShieldAlert, CheckCircle, Eye } from 'lucide-angular';
import { BadgeComponent, type BadgeVariant } from '../../shared/components';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { TabBarComponent, type TabItem } from '../../shared/components/tab-bar/tab-bar.component';
import { ButtonComponent } from '../../shared/components/button/button.component';
import { RelativeDatePipe } from '../../shared/pipes';
import { ToastService } from '../../services/toast.service';
import { ClaimService, type ClaimListItem, type ClaimDetail } from '../../services/claim.service';
import { SignalRService } from '../../services/signalr.service';

type TabFilter = 'opened' | 'closed' | 'all';

@Component({
  selector: 'app-claims',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LucideAngularModule, BadgeComponent, RelativeDatePipe, PageHeaderComponent, TabBarComponent, ButtonComponent],
  templateUrl: './claims.component.html',
  styleUrl: './claims.component.scss',
})
export class ClaimsComponent implements OnInit, OnDestroy {
  private readonly claimService = inject(ClaimService);
  private readonly signalR = inject(SignalRService);
  private readonly toastService = inject(ToastService);
  private subs = new Subscription();

  readonly alertIcon = AlertTriangle;
  readonly clockIcon = Clock;
  readonly packageIcon = Package;
  readonly sendIcon = Send;
  readonly xIcon = X;
  readonly searchIcon = Search;
  readonly shieldIcon = ShieldAlert;
  readonly checkIcon = CheckCircle;
  readonly eyeIcon = Eye;

  readonly tabItems: TabItem[] = [
    { key: 'opened', label: 'Abertas' },
    { key: 'closed', label: 'Fechadas' },
    { key: 'all', label: 'Todas' },
  ];

  readonly activeTab = signal<TabFilter>('opened');
  readonly loading = signal(true);
  readonly searchQuery = signal('');
  readonly respondingTo = signal<string | null>(null);
  readonly responseText = signal('');
  readonly submitting = signal(false);
  readonly selectedClaim = signal<ClaimDetail | null>(null);
  readonly loadingDetail = signal(false);

  readonly claims = signal<ClaimListItem[]>([]);

  readonly openCount = computed(() =>
    this.claims().filter(c => c.status === 'opened').length
  );

  readonly filteredClaims = computed(() => {
    const tab = this.activeTab();
    const search = this.searchQuery().toLowerCase().trim();
    let items = this.claims();

    if (tab === 'opened') {
      items = items.filter(c => c.status === 'opened');
    } else if (tab === 'closed') {
      items = items.filter(c => c.status !== 'opened');
    }

    if (search) {
      items = items.filter(c =>
        (c.buyerName?.toLowerCase().includes(search)) ||
        (c.productName?.toLowerCase().includes(search)) ||
        c.externalId.toLowerCase().includes(search) ||
        c.reason.toLowerCase().includes(search)
      );
    }

    return [...items].sort((a, b) =>
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  });

  ngOnInit(): void {
    this.loadClaims();

    this.subs.add(
      this.signalR.dataChanged$.subscribe(event => {
        if (event.entity === 'claim') {
          this.loadClaims();
        }
      })
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  setTab(tab: TabFilter): void {
    this.activeTab.set(tab);
    this.closeDetail();
  }

  openDetail(claim: ClaimListItem): void {
    this.loadingDetail.set(true);
    this.selectedClaim.set(null);
    this.claimService.get(claim.id).subscribe({
      next: (detail) => {
        this.selectedClaim.set(detail);
        this.loadingDetail.set(false);
      },
      error: () => {
        this.loadingDetail.set(false);
        this.toastService.show('Erro ao carregar detalhes da reclamação.', 'danger');
      },
    });
  }

  closeDetail(): void {
    this.selectedClaim.set(null);
    this.respondingTo.set(null);
    this.responseText.set('');
  }

  startRespond(claimId: string): void {
    this.respondingTo.set(claimId);
    this.responseText.set('');
  }

  cancelRespond(): void {
    this.respondingTo.set(null);
    this.responseText.set('');
  }

  submitResponse(): void {
    const detail = this.selectedClaim();
    const text = this.responseText().trim();
    if (!detail || !text || this.submitting()) return;

    this.submitting.set(true);
    this.claimService.respond(detail.id, text).subscribe({
      next: (updated) => {
        this.selectedClaim.set(updated);
        this.respondingTo.set(null);
        this.responseText.set('');
        this.submitting.set(false);
        this.toastService.show('Resposta enviada com sucesso!', 'success');
      },
      error: () => {
        this.submitting.set(false);
        this.toastService.show('Erro ao enviar resposta. Tente novamente.', 'danger');
      },
    });
  }

  getStatusVariant(status: string): BadgeVariant {
    switch (status) {
      case 'opened': return 'accent';
      case 'closed': return 'success';
      default: return 'neutral';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'opened': return 'Aberta';
      case 'closed': return 'Fechada';
      default: return status;
    }
  }

  getTypeLabel(type: string): string {
    switch (type) {
      case 'claim': return 'Reclamação';
      case 'return': return 'Devolução';
      case 'mediations': return 'Mediação';
      default: return type;
    }
  }

  formatAmount(amount: number | null): string {
    if (amount == null) return '-';
    return `R$ ${amount.toFixed(2).replace('.', ',')}`;
  }

  private loadClaims(): void {
    this.loading.set(true);
    this.claimService.list({ pageSize: 100 }).subscribe({
      next: (res) => {
        this.claims.set(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.show('Erro ao carregar reclamações.', 'danger');
      },
    });
  }
}
