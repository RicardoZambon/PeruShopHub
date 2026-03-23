import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { KpiCard, ChartDataPoint } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  getSummary(period: string): Observable<KpiCard[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<KpiCard[]>('/api/dashboard/summary', { params });
  }

  getRevenueProfit(days: number): Observable<ChartDataPoint[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<ChartDataPoint[]>('/api/dashboard/revenue-profit', { params });
  }

  getCostBreakdown(period: string): Observable<ChartDataPoint[]> {
    const params = new HttpParams().set('period', period);
    return this.http.get<ChartDataPoint[]>('/api/dashboard/cost-breakdown', { params });
  }

  getTopProducts(limit: number): Observable<any[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<any[]>('/api/dashboard/top-products', { params });
  }

  getLeastProfitable(limit: number): Observable<any[]> {
    const params = new HttpParams().set('limit', limit);
    return this.http.get<any[]>('/api/dashboard/least-profitable', { params });
  }
}
