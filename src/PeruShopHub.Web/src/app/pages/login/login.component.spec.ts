import '../../../test-setup';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { ReactiveFormsModule } from '@angular/forms';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';
import { OnboardingService } from '../../services/onboarding.service';
import { of, throwError } from 'rxjs';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: { login: ReturnType<typeof vi.fn>; isAuthenticated: boolean; };
  let router: Router;
  let onboardingService: { getProgress: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authService = {
      login: vi.fn(),
      isAuthenticated: false,
    };

    onboardingService = {
      getProgress: vi.fn().mockReturnValue(of({ isCompleted: true, stepsCompleted: [], steps: [] })),
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent, ReactiveFormsModule],
      providers: [
        provideHttpClient(),
        provideRouter([
          { path: 'dashboard', component: LoginComponent },
          { path: 'onboarding', component: LoginComponent },
        ]),
        { provide: AuthService, useValue: authService },
        { provide: OnboardingService, useValue: onboardingService },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have email and password form fields', () => {
    expect(component.form.get('email')).toBeTruthy();
    expect(component.form.get('password')).toBeTruthy();
  });

  it('should mark email as invalid when empty', () => {
    component.email.setValue('');
    component.email.markAsTouched();
    expect(component.email.hasError('required')).toBe(true);
  });

  it('should mark email as invalid with bad format', () => {
    component.email.setValue('notanemail');
    component.email.markAsTouched();
    expect(component.email.hasError('email')).toBe(true);
  });

  it('should mark password as invalid when too short', () => {
    component.password.setValue('123');
    component.password.markAsTouched();
    expect(component.password.hasError('minlength')).toBe(true);
  });

  it('should not submit when form is invalid', async () => {
    component.form.setValue({ email: '', password: '' });
    await component.onSubmit();
    expect(authService.login).not.toHaveBeenCalled();
  });

  it('should call login and navigate on valid submit', async () => {
    authService.login.mockResolvedValue({ id: '1', name: 'User', email: 'a@b.com' });

    component.form.setValue({ email: 'test@example.com', password: 'password123' });
    await component.onSubmit();

    expect(authService.login).toHaveBeenCalledWith('test@example.com', 'password123');
    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });

  it('should redirect to onboarding when not completed', async () => {
    authService.login.mockResolvedValue({ id: '1', name: 'User', email: 'a@b.com' });
    onboardingService.getProgress.mockReturnValue(of({
      isCompleted: false,
      stepsCompleted: [],
      steps: [],
    }));
    localStorage.removeItem('psh_onboarding_dismissed');

    component.form.setValue({ email: 'test@example.com', password: 'password123' });
    await component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/onboarding']);
  });

  it('should show error message on login failure', async () => {
    authService.login.mockRejectedValue({
      error: { message: 'Credenciais inválidas' },
    });

    component.form.setValue({ email: 'test@example.com', password: 'wrongpass' });
    await component.onSubmit();

    expect(component.errorMessage()).toBe('Credenciais inválidas');
    expect(component.loading()).toBe(false);
  });

  it('should show default error when no message in response', async () => {
    authService.login.mockRejectedValue({});

    component.form.setValue({ email: 'test@example.com', password: 'wrongpass' });
    await component.onSubmit();

    expect(component.errorMessage()).toBe('Erro ao fazer login. Tente novamente.');
  });

  it('should set loading during submit', async () => {
    let resolveLogin: (value: any) => void;
    authService.login.mockReturnValue(new Promise(r => { resolveLogin = r; }));

    component.form.setValue({ email: 'test@example.com', password: 'password123' });
    const submitPromise = component.onSubmit();

    expect(component.loading()).toBe(true);

    resolveLogin!({ id: '1', name: 'User', email: 'a@b.com' });
    await submitPromise;

    expect(component.loading()).toBe(false);
  });

  it('should redirect to dashboard if already authenticated', async () => {
    // Re-create with authenticated user
    authService.isAuthenticated = true;

    const fixture2 = TestBed.createComponent(LoginComponent);
    fixture2.detectChanges();

    expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
  });
});
