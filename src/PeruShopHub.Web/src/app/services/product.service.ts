import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';
import { PagedResult } from '../models/api.models';

export interface Product {
  id: string;
  name: string;
  nome: string;
  sku: string;
  description?: string;
  categoryId?: string;
  supplier?: string;
  price: number;
  preco: number;
  acquisitionCost: number;
  weight?: number;
  height?: number;
  width?: number;
  length?: number;
  imageUrl?: string;
  status: string;
  stock: number;
  estoque: number;
  variantCount: number;
  needsReview: boolean;
  margin: number;
  margem: number;
  sales30d: number;
  revenue30d: number;
  profit30d: number;
  margin30d: number;
  [key: string]: any;
}

export type PaginatedResult<T> = PagedResult<T>;

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  list(params: { page?: number; pageSize?: number; search?: string; status?: string; sortBy?: string; sortDir?: string; sortDirection?: string } = {}): Promise<PagedResult<any>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    const sortDir = params.sortDir ?? params.sortDirection;
    if (sortDir) httpParams = httpParams.set('sortDir', sortDir);
    return firstValueFrom(this.http.get<PagedResult<any>>('/api/products', { params: httpParams }));
  }

  getById(id: string): Promise<any> {
    return firstValueFrom(this.http.get<any>(`/api/products/${id}`));
  }

  getVariants(id: string): Promise<any[]> {
    return firstValueFrom(this.http.get<any[]>(`/api/products/${id}/variants`));
  }

  create(dto: any): Promise<any> {
    return firstValueFrom(this.http.post<any>('/api/products', dto));
  }

  update(id: string, dto: any): Promise<any> {
    return firstValueFrom(this.http.put<any>(`/api/products/${id}`, dto));
  }
}
