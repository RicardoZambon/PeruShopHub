import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page">
      <div class="auth-card">
        <div class="auth-header">
          <h1>PeruShopHub</h1>
          <p>Crie sua loja e comece a vender</p>
        </div>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="auth-form">
          <div class="form-field">
            <label for="shopName">Nome da Loja</label>
            <input id="shopName" formControlName="shopName" placeholder="Minha Loja" />
            @if (form.get('shopName')?.touched && form.get('shopName')?.hasError('required')) {
              <span class="error">Nome da loja é obrigatório</span>
            }
          </div>

          <div class="form-field">
            <label for="name">Seu Nome</label>
            <input id="name" formControlName="name" placeholder="João Silva" />
            @if (form.get('name')?.touched && form.get('name')?.hasError('required')) {
              <span class="error">Nome é obrigatório</span>
            }
          </div>

          <div class="form-field">
            <label for="email">E-mail</label>
            <input id="email" type="email" formControlName="email" placeholder="joao@exemplo.com" />
            @if (form.get('email')?.touched && form.get('email')?.hasError('required')) {
              <span class="error">E-mail é obrigatório</span>
            }
            @if (form.get('email')?.touched && form.get('email')?.hasError('email')) {
              <span class="error">E-mail inválido</span>
            }
          </div>

          <div class="form-field">
            <label for="password">Senha</label>
            <input id="password" type="password" formControlName="password" placeholder="Mínimo 8 caracteres" />
            @if (form.get('password')?.touched && form.get('password')?.hasError('required')) {
              <span class="error">Senha é obrigatória</span>
            }
            @if (form.get('password')?.touched && form.get('password')?.hasError('minlength')) {
              <span class="error">Senha deve ter no mínimo 8 caracteres</span>
            }
          </div>

          @if (serverError()) {
            <div class="server-error">{{ serverError() }}</div>
          }

          <button type="submit" [disabled]="loading()" class="btn-primary">
            {{ loading() ? 'Criando...' : 'Criar Conta' }}
          </button>
        </form>

        <p class="auth-link">
          Já tem uma conta? <a routerLink="/login">Fazer login</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--neutral-50);
      padding: var(--space-4);
    }
    .auth-card {
      width: 100%;
      max-width: 400px;
      background: var(--surface);
      border-radius: var(--radius-lg);
      padding: var(--space-6);
      box-shadow: var(--shadow-lg);
    }
    .auth-header { text-align: center; margin-bottom: var(--space-5); }
    .auth-header h1 { font-size: 1.5rem; font-weight: 700; color: var(--primary); }
    .auth-header p { color: var(--neutral-500); margin-top: var(--space-1); }
    .auth-form { display: flex; flex-direction: column; gap: var(--space-3); }
    .form-field { display: flex; flex-direction: column; gap: var(--space-1); }
    .form-field label { font-size: 0.875rem; font-weight: 500; color: var(--neutral-700); }
    .form-field input {
      padding: var(--space-2) var(--space-3);
      border: 1px solid var(--neutral-300);
      border-radius: var(--radius-sm);
      font-size: 0.875rem;
    }
    .form-field input:focus { outline: 2px solid var(--primary); border-color: var(--primary); }
    .error { font-size: 0.75rem; color: var(--danger); }
    .server-error {
      padding: var(--space-2);
      background: var(--danger-light, #fef2f2);
      color: var(--danger);
      border-radius: var(--radius-sm);
      font-size: 0.875rem;
    }
    .btn-primary {
      padding: var(--space-2) var(--space-4);
      background: var(--primary);
      color: white;
      border: none;
      border-radius: var(--radius-sm);
      font-weight: 600;
      cursor: pointer;
    }
    .btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
    .auth-link { text-align: center; margin-top: var(--space-4); font-size: 0.875rem; color: var(--neutral-500); }
    .auth-link a { color: var(--primary); text-decoration: none; font-weight: 500; }
  `]
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  loading = signal(false);
  serverError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    shopName: ['', Validators.required],
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor() {
    if (this.auth.isAuthenticated) {
      this.router.navigate(['/dashboard']);
    }
  }

  async onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.serverError.set(null);

    try {
      const { shopName, name, email, password } = this.form.getRawValue();
      await this.auth.register(shopName, name, email, password);
      this.router.navigate(['/onboarding']);
    } catch (err: any) {
      const msg = err?.error?.errors
        ? Object.values(err.error.errors).flat().join('. ')
        : err?.error?.message || 'Erro ao criar conta. Tente novamente.';
      this.serverError.set(msg as string);
    } finally {
      this.loading.set(false);
    }
  }
}
