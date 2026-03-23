import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserRow {
  id: number;
  nome: string;
  email: string;
  role: 'Admin' | 'Manager' | 'Viewer';
  ativo: boolean;
}

export interface Integration {
  id: string;
  name: string;
  logo: string;
  connected: boolean;
  sellerNickname?: string;
  lastSync?: string;
  comingSoon?: boolean;
}

export interface FixedCostsResponse {
  embalagemPadrao: number;
  aliquotaSimples: number;
  fixedCosts: { id: number; nome: string; valor: number }[];
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/settings`;

  getUsers(): Observable<UserRow[]> {
    return this.http.get<UserRow[]>(`${this.baseUrl}/users`);
  }

  getIntegrations(): Observable<Integration[]> {
    return this.http.get<Integration[]>(`${this.baseUrl}/integrations`);
  }

  getCosts(): Observable<FixedCostsResponse> {
    return this.http.get<FixedCostsResponse>(`${this.baseUrl}/costs`);
  }
}
