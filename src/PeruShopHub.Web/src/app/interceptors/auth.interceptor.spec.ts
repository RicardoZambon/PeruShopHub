import { ensureTestBedInit } from '../../test-setup';
import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from '../services/auth.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpTesting: HttpTestingController;
  let authService: {
    accessToken: string | null;
    refreshToken: string | null;
    refreshAccessToken: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    TestBed.resetTestingModule();
    ensureTestBedInit();

    authService = {
      accessToken: 'test-token',
      refreshToken: 'refresh-token',
      refreshAccessToken: vi.fn(),
      logout: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authService },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should add Authorization header for regular requests', () => {
    httpClient.get('/api/products').subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/products'));
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token');
    req.flush([]);
  });

  it('should skip auth header for login endpoint', () => {
    httpClient.post('/api/auth/login', {}).subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/auth/login'));
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should skip auth header for refresh endpoint', () => {
    httpClient.post('/api/auth/refresh', {}).subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/auth/refresh'));
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should not add header when no token', () => {
    authService.accessToken = null;
    httpClient.get('/api/products').subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/api/products'));
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush([]);
  });

  it('should skip auth header for forgot-password endpoint', () => {
    httpClient.post('/api/auth/forgot-password', {}).subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/auth/forgot-password'));
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('should skip auth header for reset-password endpoint', () => {
    httpClient.post('/api/auth/reset-password', {}).subscribe();

    const req = httpTesting.expectOne(r => r.url.endsWith('/auth/reset-password'));
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });
});
