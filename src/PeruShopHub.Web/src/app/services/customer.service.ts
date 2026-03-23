import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { PagedResult } from '../models/api.models';

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

  list(params: { page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<PagedResult<any>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<any>>('/api/customers', { params: httpParams });
  }

  getById(id: string): Promise<any> {
    return firstValueFrom(this.http.get<any>(`/api/customers/${id}`));
  }
}
