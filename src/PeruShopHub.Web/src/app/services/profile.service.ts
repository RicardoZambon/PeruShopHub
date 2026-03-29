import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Profile {
  id: string;
  name: string;
  email: string;
  avatarUrl: string | null;
  lastLogin: string | null;
  createdAt: string;
}

export interface UserDataExport {
  id: string;
  status: string;
  createdAt: string;
  completedAt: string | null;
  expiresAt: string | null;
}

export interface AccountDeletion {
  id: string;
  status: string;
  createdAt: string;
  scheduledDeletionAt: string;
  cancelledAt: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/profile`;

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(this.baseUrl);
  }

  updateProfile(name: string): Observable<Profile> {
    return this.http.put<Profile>(this.baseUrl, { name });
  }

  updateEmail(newEmail: string, currentPassword: string): Observable<Profile> {
    return this.http.put<Profile>(`${this.baseUrl}/email`, { newEmail, currentPassword });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/password`, { currentPassword, newPassword });
  }

  uploadAvatar(file: File): Observable<Profile> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<Profile>(`${this.baseUrl}/avatar`, formData);
  }

  removeAvatar(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/avatar`);
  }

  requestDataExport(): Observable<UserDataExport> {
    return this.http.post<UserDataExport>(`${this.baseUrl}/export-data`, {});
  }

  getExportStatus(id: string): Observable<UserDataExport> {
    return this.http.get<UserDataExport>(`${this.baseUrl}/export-data/${id}`);
  }

  downloadExport(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/export-data/${id}/download`, {
      responseType: 'blob',
    });
  }

  requestAccountDeletion(password: string, confirmPhrase: string): Observable<AccountDeletion> {
    return this.http.post<AccountDeletion>(`${this.baseUrl}/delete-account`, { password, confirmPhrase });
  }

  cancelAccountDeletion(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/cancel-deletion`, {});
  }

  getDeletionStatus(): Observable<AccountDeletion | null> {
    return this.http.get<AccountDeletion | null>(`${this.baseUrl}/deletion-status`);
  }
}
