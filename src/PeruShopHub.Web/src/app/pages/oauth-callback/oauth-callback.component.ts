import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { SettingsService } from '../../services/settings.service';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-oauth-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="oauth-callback">
      @if (loading()) {
        <div class="oauth-callback__spinner">
          <div class="spinner"></div>
          <p>Conectando ao Mercado Livre...</p>
        </div>
      } @else if (error()) {
        <div class="oauth-callback__error">
          <h2>Erro na conexão</h2>
          <p>{{ errorMessage() }}</p>
          <button (click)="goToSettings()">Voltar às Configurações</button>
        </div>
      } @else {
        <div class="oauth-callback__success">
          <h2>Conectado com sucesso!</h2>
          <p>Redirecionando...</p>
        </div>
      }
    </div>
  `,
  styles: [`
    .oauth-callback {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background: var(--surface, #f5f5f5);
    }
    .oauth-callback__spinner,
    .oauth-callback__error,
    .oauth-callback__success {
      text-align: center;
      padding: 2rem;
    }
    .spinner {
      width: 40px;
      height: 40px;
      border: 4px solid var(--neutral-200, #e0e0e0);
      border-top-color: var(--primary, #1A237E);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1rem;
    }
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    .oauth-callback__error h2 { color: var(--danger, #d32f2f); }
    .oauth-callback__error button {
      margin-top: 1rem;
      padding: 0.5rem 1.5rem;
      background: var(--primary, #1A237E);
      color: white;
      border: none;
      border-radius: 6px;
      cursor: pointer;
    }
  `]
})
export class OAuthCallbackComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private settingsService = inject(SettingsService);
  private toastService = inject(ToastService);

  loading = signal(true);
  error = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code');
    const state = this.route.snapshot.queryParamMap.get('state');

    if (!code || !state) {
      this.loading.set(false);
      this.error.set(true);
      this.errorMessage.set('Parâmetros OAuth inválidos. Tente novamente.');
      return;
    }

    // The marketplaceId is mercadolivre for now — could be extracted from state in future
    this.settingsService.handleOAuthCallback('mercadolivre', code, state).subscribe({
      next: (result) => {
        this.loading.set(false);
        this.toastService.show(`Conectado ao Mercado Livre como ${result.sellerNickname}`, 'success');
        setTimeout(() => {
          this.router.navigate(['/configuracoes'], { queryParams: { tab: 'integracoes' } });
        }, 1500);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(true);
        this.errorMessage.set(err.error?.message || 'Erro ao completar conexão OAuth. Tente novamente.');
      },
    });
  }

  goToSettings(): void {
    this.router.navigate(['/configuracoes'], { queryParams: { tab: 'integracoes' } });
  }
}
