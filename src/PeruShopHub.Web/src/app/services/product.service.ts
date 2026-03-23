import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CostHistoryItem {
  id: string;
  date: string;
  previousCost: number;
  newCost: number;
  quantity: number;
  unitCostPaid: number;
  purchaseOrderId: string | null;
  reason: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  getCostHistory(id: string, page = 1, pageSize = 20): Observable<PagedResult<CostHistoryItem>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<CostHistoryItem>>(`${this.baseUrl}/${id}/cost-history`, { params });
  }
}
