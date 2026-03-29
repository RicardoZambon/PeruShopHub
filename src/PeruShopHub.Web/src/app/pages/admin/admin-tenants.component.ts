import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ToastService } from '../../services/toast.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';

interface TenantRow {
  id: string;
  name: string;
  slug: string;
  isActive: boolean;
  memberCount: number;
  createdAt: string;
}

@Component({
  selector: 'app-admin-tenants',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-container">
      <div class="page-header">
        <h1>Administração — Lojas</h1>
        <p class="subtitle">Gerenciamento de todos os tenants da plataforma</p>
      </div>

      @if (loading()) {
        <div class="loading">Carregando...</div>
      } @else {
        <div class="table-container">
          <table>
            <thead>
              <tr>
                <th>Nome</th>
                <th>Slug</th>
                <th>Membros</th>
                <th>Status</th>
                <th>Criado em</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              @for (tenant of tenants(); track tenant.id) {
                <tr>
                  <td>{{ tenant.name }}</td>
                  <td><code>{{ tenant.slug }}</code></td>
                  <td>{{ tenant.memberCount }}</td>
                  <td>
                    <span [class]="tenant.isActive ? 'badge-success' : 'badge-danger'">
                      {{ tenant.isActive ? 'Ativo' : 'Inativo' }}
                    </span>
                  </td>
                  <td>{{ tenant.createdAt | date:'dd/MM/yyyy' }}</td>
                  <td>
                    <button class="btn-ghost" (click)="toggleActive(tenant)">
                      {{ tenant.isActive ? 'Desativar' : 'Ativar' }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-container { padding: var(--space-4); }
    .page-header h1 { font-size: 1.25rem; font-weight: 600; }
    .subtitle { color: var(--neutral-500); font-size: 0.875rem; }
    .table-container { margin-top: var(--space-4); overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: var(--space-2) var(--space-3); text-align: left; border-bottom: 1px solid var(--neutral-200); }
    th { font-weight: 600; font-size: 0.75rem; text-transform: uppercase; color: var(--neutral-500); }
    code { font-family: 'Roboto Mono', monospace; font-size: 0.8125rem; }
    .badge-success { color: var(--success); font-weight: 500; }
    .badge-danger { color: var(--danger); font-weight: 500; }
    .btn-ghost {
      background: none; border: none; color: var(--primary);
      cursor: pointer; font-size: 0.875rem;
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-sm);
    }
    .btn-ghost:hover { background: var(--neutral-100); }
    .loading { padding: var(--space-6); text-align: center; color: var(--neutral-500); }
  `]
})
export class AdminTenantsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly toastService = inject(ToastService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly baseUrl = `${environment.apiUrl}/admin`;

  tenants = signal<TenantRow[]>([]);
  loading = signal(true);

  ngOnInit() {
    this.loadTenants();
  }

  private loadTenants() {
    this.loading.set(true);
    this.http.get<TenantRow[]>(`${this.baseUrl}/tenants`).subscribe({
      next: data => {
        this.tenants.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toastService.show('Erro ao carregar lojas', 'danger');
      }
    });
  }

  toggleActive(tenant: TenantRow) {
    const action = tenant.isActive ? 'deactivate' : 'activate';
    const label = tenant.isActive ? 'Desativar' : 'Ativar';
    this.confirmDialog.confirm({
      title: `${label} loja`,
      message: `Deseja ${label.toLowerCase()} a loja "${tenant.name}"?`,
      confirmLabel: label,
      variant: tenant.isActive ? 'danger' : 'primary',
    }).then((confirmed) => {
      if (!confirmed) return;
      this.http.put(`${this.baseUrl}/tenants/${tenant.id}/${action}`, {}).subscribe({
        next: () => {
          this.tenants.update(list =>
            list.map(t => t.id === tenant.id ? { ...t, isActive: !t.isActive } : t)
          );
          this.toastService.show(`Loja ${label.toLowerCase()}da com sucesso`, 'success');
        },
        error: () => {
          this.toastService.show(`Erro ao ${label.toLowerCase()} loja`, 'danger');
        }
      });
    });
  }
}
