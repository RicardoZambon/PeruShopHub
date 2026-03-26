import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Product {
  id: string;
  name: string;
  sku: string;
  description?: string;
  categoryId?: string;
  supplier?: string;
  price: number;
  acquisitionCost: number;
  purchaseCost: number;
  packagingCost: number;
  weight?: number;
  height?: number;
  width?: number;
  length?: number;
  imageUrl: string | null;
  photoUrls?: string[];
  stock: number;
  status: string;
  isActive: boolean;
  margin: number | null;
  variantCount: number;
  needsReview: boolean;
  variants?: any[];
  createdAt?: string;
  updatedAt?: string;
}

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

export interface ProductListParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  categoryId?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface ProductAnalytics {
  totalSales: number;
  totalRevenue: number;
  totalProfit: number;
  margin: number | null;
  salesChange: number | null;
  revenueChange: number | null;
  profitChange: number | null;
  marginChange: number | null;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/products`;

  async list(params: ProductListParams = {}): Promise<PagedResult<Product>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.categoryId) httpParams = httpParams.set('categoryId', params.categoryId);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDirection) httpParams = httpParams.set('sortDirection', params.sortDirection);
    return firstValueFrom(
      this.http.get<PagedResult<Product>>(this.baseUrl, { params: httpParams }),
    );
  }

  async getById(id: string): Promise<Product> {
    return firstValueFrom(
      this.http.get<Product>(`${this.baseUrl}/${id}`),
    );
  }

  async create(dto: Partial<Product>): Promise<Product> {
    return firstValueFrom(
      this.http.post<Product>(this.baseUrl, dto),
    );
  }

  async update(id: string, dto: Partial<Product>): Promise<Product> {
    return firstValueFrom(
      this.http.put<Product>(`${this.baseUrl}/${id}`, dto),
    );
  }

  async delete(id: string): Promise<void> {
    await firstValueFrom(
      this.http.delete<void>(`${this.baseUrl}/${id}`),
    );
  }

  async getNextSku(categoryId: string): Promise<string | null> {
    const result = await firstValueFrom(
      this.http.get<{ suggestedSku: string | null }>(
        `${this.baseUrl}/next-sku`,
        { params: new HttpParams().set('categoryId', categoryId) },
      ),
    );
    return result.suggestedSku;
  }

  getVariants(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/${id}/variants`);
  }

  getCostHistory(id: string, page = 1, pageSize = 20): Observable<PagedResult<CostHistoryItem>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<CostHistoryItem>>(`${this.baseUrl}/${id}/cost-history`, { params });
  }

  async getAnalytics(id: string, days = 30): Promise<ProductAnalytics> {
    const params = new HttpParams().set('days', days.toString());
    return firstValueFrom(
      this.http.get<ProductAnalytics>(`${this.baseUrl}/${id}/analytics`, { params }),
    );
  }

  getRecentOrders(id: string, days = 30, page = 1, pageSize = 10): Observable<PagedResult<any>> {
    const params = new HttpParams()
      .set('days', days.toString())
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<any>>(`${this.baseUrl}/${id}/recent-orders`, { params });
  }
}
