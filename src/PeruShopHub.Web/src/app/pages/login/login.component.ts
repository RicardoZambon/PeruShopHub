import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { OnboardingService } from '../../services/onboarding.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly onboarding = inject(OnboardingService);

  form: FormGroup;
  loading = signal(false);
  errorMessage = signal('');

  constructor() {
    if (this.auth.isAuthenticated) {
      this.router.navigate(['/dashboard']);
    }

    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  get email() {
    return this.form.get('email')!;
  }

  get password() {
    return this.form.get('password')!;
  }

  private async redirectAfterLogin(): Promise<void> {
    try {
      const progress = await firstValueFrom(this.onboarding.getProgress());
      if (!progress.isCompleted && localStorage.getItem('psh_onboarding_dismissed') !== 'true') {
        this.router.navigate(['/onboarding']);
        return;
      }
    } catch {
      // If onboarding check fails, just go to dashboard
    }
    this.router.navigate(['/dashboard']);
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    try {
      await this.auth.login(this.form.value.email, this.form.value.password);
      await this.redirectAfterLogin();
    } catch (err: any) {
      const msg = err?.error?.message || 'Erro ao fazer login. Tente novamente.';
      this.errorMessage.set(msg);
    } finally {
      this.loading.set(false);
    }
  }
}
