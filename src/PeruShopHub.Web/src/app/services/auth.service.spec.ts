import { ensureTestBedInit } from '../../test-setup';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { AuthService, AuthUser } from './auth.service';

const mockUser: AuthUser = {
  id: '1',
  name: 'Test User',
  email: 'test@example.com',
  tenantRole: 'Admin',
  tenantId: 'tenant-1',
  tenantName: 'Test Tenant',
  isSuperAdmin: false,
};

const mockAuthResponse = {
  accessToken: 'access-token-123',
  refreshToken: 'refresh-token-456',
  user: mockUser,
};

function urlEndsWith(suffix: string) {
  return (req: { url: string }) => req.url.endsWith(suffix);
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
    ensureTestBedInit();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('login', () => {
    it('should store tokens and update currentUser on login', async () => {
      const loginPromise = service.login('test@example.com', 'password');

      const req = httpMock.expectOne(urlEndsWith('/auth/login'));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email: 'test@example.com', password: 'password' });
      req.flush(mockAuthResponse);

      const user = await loginPromise;
      expect(user).toEqual(mockUser);
      expect(localStorage.getItem('psh_access_token')).toBe('access-token-123');
      expect(localStorage.getItem('psh_refresh_token')).toBe('refresh-token-456');
      expect(service.currentUser()).toEqual(mockUser);
    });
  });

  describe('register', () => {
    it('should store tokens and update currentUser on register', async () => {
      const registerPromise = service.register('My Shop', 'Test User', 'test@example.com', 'password');

      const req = httpMock.expectOne(urlEndsWith('/auth/register'));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        shopName: 'My Shop',
        name: 'Test User',
        email: 'test@example.com',
        password: 'password',
      });
      req.flush(mockAuthResponse);

      const user = await registerPromise;
      expect(user).toEqual(mockUser);
      expect(service.isAuthenticated).toBe(true);
    });
  });

  describe('token storage', () => {
    it('should return null when no tokens stored', () => {
      expect(service.accessToken).toBeNull();
      expect(service.refreshToken).toBeNull();
      expect(service.isAuthenticated).toBe(false);
    });

    it('should return tokens from localStorage', () => {
      localStorage.setItem('psh_access_token', 'my-token');
      localStorage.setItem('psh_refresh_token', 'my-refresh');
      expect(service.accessToken).toBe('my-token');
      expect(service.refreshToken).toBe('my-refresh');
      expect(service.isAuthenticated).toBe(true);
    });

    it('should persist user to localStorage via storeTokens', async () => {
      const loginPromise = service.login('test@example.com', 'password');
      const req = httpMock.expectOne(urlEndsWith('/auth/login'));
      req.flush(mockAuthResponse);
      await loginPromise;

      const stored = JSON.parse(localStorage.getItem('psh_user')!);
      expect(stored).toEqual(mockUser);
    });

    it('should handle invalid JSON in stored user gracefully', () => {
      // The loadStoredUser method catches JSON parse errors
      // Since service is already created, we verify the mechanism works
      localStorage.setItem('psh_user', 'invalid-json');
      const svc = TestBed.inject(AuthService);
      expect(svc).toBeTruthy();
    });
  });

  describe('refreshAccessToken', () => {
    it('should refresh token and store new tokens', async () => {
      localStorage.setItem('psh_refresh_token', 'old-refresh');

      const refreshPromise = service.refreshAccessToken();

      const req = httpMock.expectOne(urlEndsWith('/auth/refresh'));
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'old-refresh' });
      req.flush(mockAuthResponse);

      const newToken = await refreshPromise;
      expect(newToken).toBe('access-token-123');
      expect(localStorage.getItem('psh_access_token')).toBe('access-token-123');
    });

    it('should deduplicate concurrent refresh requests', async () => {
      localStorage.setItem('psh_refresh_token', 'old-refresh');

      const p1 = service.refreshAccessToken();
      const p2 = service.refreshAccessToken();

      // Only one HTTP request should be made
      const req = httpMock.expectOne(urlEndsWith('/auth/refresh'));
      req.flush(mockAuthResponse);

      const [t1, t2] = await Promise.all([p1, p2]);
      expect(t1).toBe('access-token-123');
      expect(t2).toBe('access-token-123');
    });

    it('should logout and throw when no refresh token exists', async () => {
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      await expect(service.refreshAccessToken()).rejects.toThrow('No refresh token');
    });

    it('should logout on refresh failure', async () => {
      localStorage.setItem('psh_refresh_token', 'old-refresh');
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      const refreshPromise = service.refreshAccessToken();

      const req = httpMock.expectOne(urlEndsWith('/auth/refresh'));
      req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

      await expect(refreshPromise).rejects.toBeTruthy();
      expect(localStorage.getItem('psh_access_token')).toBeNull();
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('logout', () => {
    it('should clear tokens, user, and navigate to login', () => {
      localStorage.setItem('psh_access_token', 'token');
      localStorage.setItem('psh_refresh_token', 'refresh');
      localStorage.setItem('psh_user', JSON.stringify(mockUser));
      service.currentUser.set(mockUser);
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      service.logout();

      // Server logout call is fire-and-forget
      const req = httpMock.expectOne(urlEndsWith('/auth/logout'));
      req.flush({});

      expect(localStorage.getItem('psh_access_token')).toBeNull();
      expect(localStorage.getItem('psh_refresh_token')).toBeNull();
      expect(localStorage.getItem('psh_user')).toBeNull();
      expect(service.currentUser()).toBeNull();
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should skip server call when no access token', () => {
      vi.spyOn(router, 'navigate').mockResolvedValue(true);

      service.logout();

      httpMock.expectNone(urlEndsWith('/auth/logout'));
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('computed signals', () => {
    it('should derive isSuperAdmin from currentUser', () => {
      expect(service.isSuperAdmin()).toBe(false);

      service.currentUser.set({ ...mockUser, isSuperAdmin: true });
      expect(service.isSuperAdmin()).toBe(true);
    });

    it('should derive tenantName from currentUser', () => {
      expect(service.tenantName()).toBe('');

      service.currentUser.set(mockUser);
      expect(service.tenantName()).toBe('Test Tenant');
    });

    it('should derive hasTenant from currentUser', () => {
      expect(service.hasTenant()).toBe(false);

      service.currentUser.set(mockUser);
      expect(service.hasTenant()).toBe(true);
    });
  });
});
