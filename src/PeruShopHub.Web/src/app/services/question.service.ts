import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildHttpParams } from '../shared/utils';
import { environment } from '../../environments/environment';

export interface QuestionListItem {
  id: string;
  externalId: string;
  externalItemId: string;
  productId: string | null;
  buyerName: string;
  questionText: string;
  answerText: string | null;
  status: string;
  questionDate: string;
  answerDate: string | null;
}

export interface QuestionListResponse {
  items: QuestionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface QuestionListParams {
  status?: string;
  productId?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class QuestionService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/questions`;

  list(params: QuestionListParams = {}): Observable<QuestionListResponse> {
    return this.http.get<QuestionListResponse>(this.baseUrl, { params: buildHttpParams(params) });
  }

  answer(id: string, answer: string): Observable<QuestionListItem> {
    return this.http.post<QuestionListItem>(`${this.baseUrl}/${id}/answer`, { answer });
  }
}
