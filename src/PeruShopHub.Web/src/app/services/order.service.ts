import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface OrderListItem {
  id: string;
  date: string;
  buyer: string;
  status: string;
  itemCount: number;
  total: number;
  profit: number;
  margin: number;
  marketplace: string;
}

export interface OrderListResponse {
  items: OrderListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface OrderListParams {
  search?: string;
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/orders`;

  list(params: OrderListParams = {}): Observable<OrderListResponse> {
    let httpParams = new HttpParams();
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    return this.http.get<OrderListResponse>(this.baseUrl, { params: httpParams });
  }

  recalculateCosts(orderId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${orderId}/recalculate-costs`, {});
  }
}
