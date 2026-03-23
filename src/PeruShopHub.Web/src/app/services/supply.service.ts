import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class SupplyService {
  private http = inject(HttpClient);

  list(params: { page?: number; pageSize?: number; search?: string; sortBy?: string; sortDir?: string } = {}): Observable<PagedResult<any>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.sortBy) httpParams = httpParams.set('sortBy', params.sortBy);
    if (params.sortDir) httpParams = httpParams.set('sortDir', params.sortDir);
    return this.http.get<PagedResult<any>>('/api/supplies', { params: httpParams });
  }

  create(dto: any): Observable<any> {
    return this.http.post<any>('/api/supplies', dto);
  }

  update(id: string, dto: any): Observable<any> {
    return this.http.put<any>(`/api/supplies/${id}`, dto);
  }
}
