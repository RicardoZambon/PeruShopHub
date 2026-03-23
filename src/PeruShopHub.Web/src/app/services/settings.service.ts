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

export interface CommissionRule {
  id: string;
  marketplace: string;
  categoryPattern: string;
  listingType: string;
  rate: number;
  isDefault: boolean;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);
  private readonly baseUrl = '/api/settings';

  getUsers(): Observable<UserRow[]> {
    return this.http.get<UserRow[]>('/api/settings/users');
  }

  getIntegrations(): Observable<Integration[]> {
    return this.http.get<Integration[]>('/api/settings/integrations');
  }

  getCosts(): Observable<FixedCostsResponse> {
    return this.http.get<FixedCostsResponse>('/api/settings/costs');
  }

  getCommissionRules(): Observable<CommissionRule[]> {
    return this.http.get<CommissionRule[]>(`${this.baseUrl}/commission-rules`);
  }

  createCommissionRule(dto: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/commission-rules`, dto);
  }

  updateCommissionRule(id: string, dto: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/commission-rules/${id}`, dto);
  }

  deleteCommissionRule(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/commission-rules/${id}`);
  }

  updateCosts(dto: { taxRate: number }): Observable<any> {
    return this.http.put(`${this.baseUrl}/costs`, dto);
  }
}
