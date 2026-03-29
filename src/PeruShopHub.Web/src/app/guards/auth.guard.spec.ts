import '../../test-setup';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('authGuard', () => {
  let authService: { isAuthenticated: boolean };
  let router: Router;

  beforeEach(() => {
    authService = { isAuthenticated: false };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        {
          provide: Router,
          useValue: {
            createUrlTree: vi.fn().mockReturnValue({ toString: () => '/login' } as UrlTree),
          },
        },
      ],
    });

    router = TestBed.inject(Router);
  });

  it('should allow access when authenticated', () => {
    authService.isAuthenticated = true;
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, {} as any)
    );
    expect(result).toBe(true);
  });

  it('should redirect to login when not authenticated', () => {
    authService.isAuthenticated = false;
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as any, {} as any)
    );
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(result).toBeTruthy();
    expect(result).not.toBe(true);
  });
});
