import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private http = inject(HttpClient);

  list(params: { page?: number; pageSize?: number; search?: string; status?: string; sortBy?: string; sortDir?: string } = {}): Observable<PagedResult<any>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<any>>('/api/products', { params: httpParams });
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`/api/products/${id}`);
  }

  getVariants(id: string): Observable<any[]> {
    return this.http.get<any[]>(`/api/products/${id}/variants`);
  }

  create(dto: any): Observable<any> {
    return this.http.post<any>('/api/products', dto);
  }

  update(id: string, dto: any): Observable<any> {
    return this.http.put<any>(`/api/products/${id}`, dto);
  }
}
