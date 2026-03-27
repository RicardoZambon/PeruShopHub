import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { buildHttpParams } from '../shared/utils';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/api.models';

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
    return this.http.get<PagedResult<InventoryItem>>(this.baseUrl, { params: buildHttpParams(params) });
  }

  getMovements(params: MovementQueryParams): Observable<PagedResult<StockMovement>> {
    const { type, ...rest } = params;
    return this.http.get<PagedResult<StockMovement>>(`${this.baseUrl}/movements`, {
      params: buildHttpParams({ ...rest, type: type && type !== 'all' ? type : undefined }),
    });
  }

  adjust(dto: StockAdjustmentDto): Observable<unknown> {
    return this.http.post(`${this.baseUrl}/adjust`, dto);
  }
}
