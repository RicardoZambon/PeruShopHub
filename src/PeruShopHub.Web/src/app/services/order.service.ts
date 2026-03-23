import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { PagedResult } from '../models/api.models';

export interface OrderListItem {
  id: string;
  externalOrderId: string;
  data: string;
  comprador: string;
  itens: number;
  valor: number;
  lucro: number;
  status: string;
  marketplace: string;
}

export interface SupplyItem {
  id: string;
  name: string;
  unitCost: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);

  list(params: { page?: number; pageSize?: number; search?: string; status?: string; dateFrom?: string; dateTo?: string; sortBy?: string; sortDir?: string } = {}): Observable<PagedResult<any>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<any>>('/api/orders', { params: httpParams });
  }

  getById(id: string): Promise<any> {
    return firstValueFrom(this.http.get<any>(`/api/orders/${id}`));
  }

  getSupplies(): Promise<SupplyItem[]> {
    return firstValueFrom(this.http.get<SupplyItem[]>('/api/supplies/simple'));
  }
}
