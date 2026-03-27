import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { KpiCard, ChartDataPoint, CostBreakdownItem, ProductRow, PendingAction } from '../models/api.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/dashboard`;

  getSummary(period: string): Observable<KpiCard[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<KpiCard[]>(`${this.baseUrl}/summary`, { params });
  }

  getRevenueProfit(days: number): Observable<ChartDataPoint[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<ChartDataPoint[]>(`${this.baseUrl}/chart/revenue-profit`, { params });
  }

  getCostBreakdown(period: string): Observable<CostBreakdownItem[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<CostBreakdownItem[]>(`${this.baseUrl}/chart/cost-breakdown`, { params });
  }

  getTopProducts(limit: number): Observable<ProductRow[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<ProductRow[]>(`${this.baseUrl}/top-products`, { params });
  }

  getLeastProfitable(limit: number): Observable<ProductRow[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<ProductRow[]>(`${this.baseUrl}/least-profitable`, { params });
  }

  getPendingActions(): Observable<PendingAction[]> {
    return this.http.get<PendingAction[]>(`${this.baseUrl}/pending-actions`);
  }
}
