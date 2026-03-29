import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);

  form = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
  });

  loading = signal(false);
  success = signal(false);
  errorMessage = signal('');

  private token = '';
  private email = '';

  get password() {
    return this.form.get('password')!;
  }

  get confirmPassword() {
    return this.form.get('confirmPassword')!;
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    this.email = this.route.snapshot.queryParamMap.get('email') || '';

    if (!this.token || !this.email) {
      this.errorMessage.set('Link de redefinição inválido.');
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const pw = this.form.value.password!;
    const confirm = this.form.value.confirmPassword!;

    if (pw !== confirm) {
      this.errorMessage.set('As senhas não coincidem.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    try {
      await this.auth.resetPassword(this.email, this.token, pw);
      this.success.set(true);
    } catch (err: any) {
      const msg = err?.error?.message || err?.error?.errors?.NewPassword?.[0] || 'Erro ao redefinir senha. Tente novamente.';
      this.errorMessage.set(msg);
    } finally {
      this.loading.set(false);
    }
  }
}
