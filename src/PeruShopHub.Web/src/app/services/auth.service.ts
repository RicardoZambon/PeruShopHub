import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  tenantRole: string | null;
  tenantId: string | null;
  tenantName: string | null;
  isSuperAdmin: boolean;
}

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
  role: string;
}

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

const TOKEN_KEY = 'psh_access_token';
const REFRESH_KEY = 'psh_refresh_token';
const USER_KEY = 'psh_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  readonly currentUser = signal<AuthUser | null>(this.loadStoredUser());

  readonly isSuperAdmin = computed(() => this.currentUser()?.isSuperAdmin ?? false);
  readonly tenantName = computed(() => this.currentUser()?.tenantName ?? '');
  readonly tenantRole = computed(() => this.currentUser()?.tenantRole ?? '');
  readonly hasTenant = computed(() => !!this.currentUser()?.tenantId);

  private refreshPromise: Promise<string> | null = null;

  get accessToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get refreshToken(): string | null {
    return localStorage.getItem(REFRESH_KEY);
  }

  get isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  async login(email: string, password: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password })
    );
    this.storeTokens(res);
    return res.user;
  }

  async register(shopName: string, name: string, email: string, password: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/register`, { shopName, name, email, password })
    );
    this.storeTokens(res);
    return res.user;
  }

  async getMyTenants(): Promise<TenantSummary[]> {
    return await firstValueFrom(
      this.http.get<TenantSummary[]>(`${this.baseUrl}/tenants`)
    );
  }

  async switchTenant(tenantId: string): Promise<AuthUser> {
    const res = await firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/switch-tenant`, { tenantId })
    );
    this.storeTokens(res);
    return res.user;
  }

  async refreshAccessToken(): Promise<string> {
    if (this.refreshPromise) return this.refreshPromise;

    const token = this.refreshToken;
    if (!token) {
      this.logout();
      throw new Error('No refresh token');
    }

    this.refreshPromise = firstValueFrom(
      this.http.post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken: token })
    ).then(res => {
      this.storeTokens(res);
      return res.accessToken;
    }).catch(err => {
      this.logout();
      throw err;
    }).finally(() => {
      this.refreshPromise = null;
    });

    return this.refreshPromise;
  }

  logout(): void {
    const token = this.accessToken;
    if (token) {
      this.http.post(`${this.baseUrl}/logout`, {}).subscribe({ error: () => {} });
    }
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(REFRESH_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  private storeTokens(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.accessToken);
    localStorage.setItem(REFRESH_KEY, res.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(res.user));
    this.currentUser.set(res.user);
  }

  private loadStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }
}
