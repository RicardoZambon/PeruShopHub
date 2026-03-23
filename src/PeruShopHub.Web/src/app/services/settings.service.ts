import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private http = inject(HttpClient);

  getUsers(): Observable<any[]> {
    return this.http.get<any[]>('/api/settings/users');
  }

  getIntegrations(): Observable<any[]> {
    return this.http.get<any[]>('/api/settings/integrations');
  }

  getCosts(): Observable<any[]> {
    return this.http.get<any[]>('/api/settings/costs');
  }
}
