import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/orders`;

  recalculateCosts(orderId: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/${orderId}/recalculate-costs`, {});
  }
}
