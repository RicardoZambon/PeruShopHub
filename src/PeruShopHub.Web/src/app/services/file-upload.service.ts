import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FileUploadResponse } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class FileUploadService {
  private http = inject(HttpClient);

  upload(file: File, entityType: string, entityId: string, sortOrder: number = 0): Observable<FileUploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('entityType', entityType);
    formData.append('entityId', entityId);
    formData.append('sortOrder', sortOrder.toString());
    return this.http.post<FileUploadResponse>('/api/files', formData);
  }

  getFiles(entityType: string, entityId: string): Observable<FileUploadResponse[]> {
    const params = new HttpParams()
      .set('entityType', entityType)
      .set('entityId', entityId);
    return this.http.get<FileUploadResponse[]>('/api/files', { params });
  }

  delete(fileId: string): Observable<void> {
    return this.http.delete<void>(`/api/files/${fileId}`);
  }
}
