import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { PagedResult } from '../models/api.models';
import { buildHttpParams } from '../shared/utils';
import { environment } from '../../environments/environment';

export interface CustomerListItem {
  id: string;
  nome: string;
  nickname: string;
  email: string;
  phone: string;
  totalPedidos: number;
  totalGasto: number;
  ultimaCompra: string;
}

@Injectable({ providedIn: 'root' })
export class CustomerService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/customers`;

  list(params: { page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<PagedResult<any>> {
    return this.http.get<PagedResult<any>>(this.baseUrl, { params: buildHttpParams(params) });
  }

  getById(id: string): Promise<any> {
    return firstValueFrom(this.http.get<any>(`${this.baseUrl}/${id}`));
  }
}
