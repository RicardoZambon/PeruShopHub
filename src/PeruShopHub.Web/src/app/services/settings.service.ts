import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface UserRow {
  id: number;
  nome: string;
  email: string;
  role: string;
  ativo: boolean;
}

export interface Integration {
  id: string;
  name: string;
  marketplace: string;
  status: string;
  connected: boolean;
  comingSoon: boolean;
  logo: string;
  sellerNickname?: string;
  lastSync?: string;
  connectedAt?: string;
}

export interface FixedCostsResponse {
  embalagemPadrao: number;
  aliquotaSimples: number;
  fixedCosts: { id: number; nome: string; valor: number }[];
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);

  getUsers(): Observable<UserRow[]> {
    return this.http.get<UserRow[]>('/api/settings/users');
  }

  getIntegrations(): Observable<Integration[]> {
    return this.http.get<Integration[]>('/api/settings/integrations');
  }

  getCosts(): Observable<FixedCostsResponse> {
    return this.http.get<FixedCostsResponse>('/api/settings/costs');
  }
}
