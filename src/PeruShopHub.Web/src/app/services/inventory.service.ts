import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface InventoryItem {
  sku: string;
  productName: string;
  totalStock: number;
  reserved: number;
  available: number;
  unitCost: number;
  stockValue: number;
}

export interface StockMovement {
  data: string;
  sku: string;
  produto: string;
  tipo: 'Entrada' | 'Saída' | 'Ajuste';
  quantidade: number;
  custoUnitario?: number;
  motivo: string;
  usuario: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface MovementQueryParams {
  productId?: string;
  type?: string;
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}

export interface InventoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: string;
}

export interface StockAdjustmentDto {
  productId: string;
  variantId: string;
  quantity: number;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/inventory`;

  getInventory(params: InventoryQueryParams = {}): Observable<PagedResult<InventoryItem>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<InventoryItem>>(this.baseUrl, { params: httpParams });
  }

  getMovements(params: MovementQueryParams): Observable<PagedResult<StockMovement>> {
    let httpParams = new HttpParams();
    if (params.productId) httpParams = httpParams.set('productId', params.productId);
    if (params.type && params.type !== 'all') httpParams = httpParams.set('type', params.type);
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    return this.http.get<PagedResult<StockMovement>>(`${this.baseUrl}/movements`, { params: httpParams });
  }

  adjust(dto: StockAdjustmentDto): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/adjust`, dto);
  }
}
