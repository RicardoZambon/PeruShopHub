import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ResponseTemplate {
  id: string;
  name: string;
  category: string;
  body: string;
  placeholders: string | null;
  usageCount: number;
  order: number;
  isActive: boolean;
}

export interface ResponseTemplateDetail extends ResponseTemplate {
  createdAt: string;
  updatedAt: string;
  version: number;
}

export interface CreateResponseTemplateDto {
  name: string;
  category: string;
  body: string;
  placeholders: string | null;
  order: number;
}

export interface UpdateResponseTemplateDto {
  name?: string;
  category?: string;
  body?: string;
  placeholders?: string | null;
  isActive?: boolean;
  order?: number;
  version: number;
}

@Injectable({ providedIn: 'root' })
export class ResponseTemplateService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/response-templates`;

  list(category?: string): Observable<ResponseTemplate[]> {
    if (category) {
      return this.http.get<ResponseTemplate[]>(this.baseUrl, { params: { category } });
    }
    return this.http.get<ResponseTemplate[]>(this.baseUrl);
  }

  getById(id: string): Observable<ResponseTemplateDetail> {
    return this.http.get<ResponseTemplateDetail>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateResponseTemplateDto): Observable<ResponseTemplateDetail> {
    return this.http.post<ResponseTemplateDetail>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateResponseTemplateDto): Observable<ResponseTemplateDetail> {
    return this.http.put<ResponseTemplateDetail>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  incrementUsage(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/usage`, {});
  }
}
